using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.Messages.Item.Move;
using RecrutamentoRobo.Core.Entities;
using RecrutamentoRobo.Core.Interfaces;
using RecrutamentoRobo.Infrastructure.TextExtraction;
using AttachmentInfo = RecrutamentoRobo.Core.Entities.AttachmentInfo;

namespace RecrutamentoRobo.Infrastructure.Services;

public class GraphEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly GraphServiceClient _graphClient;

    private readonly ILogger<GraphEmailService> _logger;

    public GraphEmailService(ILogger<GraphEmailService> logger, IConfiguration configuration,
        GraphServiceClient graphClient)
    {
        _logger = logger;
        _configuration = configuration;
        _graphClient = graphClient; // Apenas recebe o cliente, não o cria.
    }

    public async Task MarkEmailAsReadAsync(string messageId)
    {
        var targetUser = _configuration["EmailMonitor:TargetUserEmail"];
        _logger.LogInformation("Marcando e-mail {MessageId} como lido.", messageId);

        try
        {
            // Para marcar como lido, precisamos criar um objeto Message com a propriedade IsRead = true
            var emailUpdate = new Message
            {
                IsRead = true
            };

            // Enviamos uma requisição PATCH para atualizar apenas essa propriedade (sem .Request() e usando PatchAsync)
            await _graphClient.Users[targetUser]
                .Messages[messageId]
                .PatchAsync(emailUpdate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar e-mail {MessageId} como lido.", messageId);
        }
    }


    public async Task<IEnumerable<EmailMessage>> GetUnreadEmailsAsync()
    {
        var targetUser = _configuration["EmailMonitor:TargetUserEmail"];
        _logger.LogInformation("Buscando e-mails não lidos para {User}", targetUser);

        try
        {
            // SINTAXE CORRIGIDA: .Request() removido e parâmetros movidos para GetAsync
            var messages = await _graphClient.Users[targetUser]
                .MailFolders[_configuration["EmailMonitor:EmailFolderToScan"]]
                .Messages
                .GetAsync(requestConfiguration =>
                {
                    // Parâmetros de consulta são configurados aqui
                    requestConfiguration.QueryParameters.Filter = "isRead eq false and hasAttachments eq true";
                    requestConfiguration.QueryParameters.Expand = new[] { "attachments" };
                });

            if (messages?.Value == null)
            {
                _logger.LogInformation("Nenhum e-mail novo encontrado.");
                return Enumerable.Empty<EmailMessage>();
            }

            _logger.LogInformation("{Count} e-mails encontrados.", messages.Value.Count);

            // Mapeia o resultado para nossa entidade do Core
            // Note o uso de 'messages.Value' para acessar a lista
            return messages.Value.Select(message => new EmailMessage(
                message.Id,
                message.Subject,
                message.Attachments.Select(attachment => new AttachmentInfo(attachment.Id, attachment.Name))
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar e-mails no Microsoft Graph.");
            return Enumerable.Empty<EmailMessage>(); // Retorna lista vazia em caso de erro
        }
    }

    public async Task DownloadAttachmentAsync(string messageId, string attachmentId, string downloadPath)
    {
        var targetUser = _configuration["EmailMonitor:TargetUserEmail"];
        try
        {
            // Pede o anexo específico para a API (sem .Request())
            var attachment = await _graphClient.Users[targetUser]
                .Messages[messageId]
                .Attachments[attachmentId]
                .GetAsync();

            // Verifica se o anexo é do tipo FileAttachment, que contém o conteúdo binário
            // O SDK mais recente pode retornar um tipo base, então o cast é importante
            if (attachment is FileAttachment fileAttachment && fileAttachment.ContentBytes != null)
            {
                _logger.LogInformation("Baixando anexo '{Name}' para '{Path}'", fileAttachment.Name, downloadPath);

                // Garante que o diretório de destino exista
                Directory.CreateDirectory(Path.GetDirectoryName(downloadPath));

                // Salva o conteúdo em um arquivo
                await File.WriteAllBytesAsync(downloadPath, fileAttachment.ContentBytes);
            }
            else
            {
                _logger.LogWarning("Anexo {AttachmentId} não é um FileAttachment ou não contém conteúdo.",
                    attachmentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao baixar o anexo {AttachmentId}.", attachmentId);
        }
    }

    public async Task MoveEmailAsync(string messageId, string destinationFolderName)
    {
        var targetUser = _configuration["EmailMonitor:TargetUserEmail"];
        _logger.LogInformation("Movendo e-mail {MessageId} para a pasta {Folder}", messageId, destinationFolderName);

        try
        {
            // 1. Criar o corpo da requisição usando o nome completo do tipo para evitar ambiguidade
            var requestBody = new MovePostRequestBody
            {
                DestinationId = destinationFolderName
            };

            // 2. Chamar a ação .Move e passar o corpo da requisição para o PostAsync
            await _graphClient.Users[targetUser]
                .Messages[messageId]
                .Move
                .PostAsync(requestBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao mover e-mail {MessageId}", messageId);
        }
    }

    public async Task<string> GetAttachmentTextAsync(string messageId, string attachmentId)
    {
        var targetUser = _configuration["EmailMonitor:TargetUserEmail"];

        // Etapa 1: Obter os detalhes E o conteúdo do anexo em uma única chamada
        // (Sintaxe corrigida, sem .Request())
        var attachment = await _graphClient.Users[targetUser]
            .Messages[messageId]
            .Attachments[attachmentId]
            .GetAsync();

        // Verifica se o anexo é um arquivo válido (FileAttachment) e se tem conteúdo (ContentBytes)
        if (attachment is not FileAttachment fileAttachment || fileAttachment.ContentBytes == null)
        {
            _logger.LogWarning("Anexo {AttachmentId} não é um arquivo válido ou está vazio.", attachmentId);
            return string.Empty;
        }

        // Cria um caminho temporário para salvar o arquivo
        var tempPath = Path.Combine(Path.GetTempPath(), fileAttachment.Name);
        var extractedText = string.Empty;

        try
        {
            // Etapa 2: Salva os bytes do anexo (que já temos em memória) em um arquivo temporário.
            // Isso evita a necessidade de chamar o método DownloadAttachmentAsync novamente.
            await File.WriteAllBytesAsync(tempPath, fileAttachment.ContentBytes);

            _logger.LogInformation("Extraindo texto do arquivo: {FileName}", fileAttachment.Name);

            // Etapa 3: Usa a classe auxiliar para ler o texto baseado na extensão do arquivo
            if (fileAttachment.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                extractedText = DocumentReader.ReadPdf(tempPath);
            else if (fileAttachment.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                extractedText = DocumentReader.ReadDocx(tempPath);
            else
                _logger.LogWarning("Tipo de anexo não suportado para extração de texto: {FileName}",
                    fileAttachment.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair texto do anexo {AttachmentId}.", attachmentId);
        }
        finally
        {
            // Etapa 4: Garante que o arquivo temporário seja deletado ao final do processo
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }

        return extractedText;
    }
}