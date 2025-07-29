// Worker.cs FINAL E COMPLETO

using RecrutamentoRobo.Core.Interfaces;

namespace RecrutamentoRobo.Worker;

public class Worker : BackgroundService
{
    private readonly string _downloadPath;
    private readonly IEmailService _emailService;
    private readonly List<string> _keywords;
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger, IEmailService emailService, IConfiguration configuration)
    {
        _logger = logger;
        _emailService = emailService;

        // Carrega as configurações importantes uma única vez
        _downloadPath = configuration["EmailMonitor:DownloadPath"];
        _keywords = configuration.GetSection("MatchingCriteria:Keywords").Get<List<string>>() ?? new List<string>();

        if (_keywords.Count == 0)
            _logger.LogWarning("Nenhuma palavra-chave de match foi configurada no appsettings.json.");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Robô de Recrutamento iniciado em: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("--- Iniciando ciclo de verificação de e-mails ---");
                var emails = await _emailService.GetUnreadEmailsAsync();

                foreach (var email in emails)
                {
                    _logger.LogInformation("Processando e-mail: '{subject}'", email.Subject);

                    foreach (var attachment in email.Attachments)
                    {
                        if (!attachment.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
                            !attachment.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                            continue; // Pula anexos que não são documentos

                        var textoDoAnexo = await _emailService.GetAttachmentTextAsync(email.Id, attachment.Id);

                        if (string.IsNullOrWhiteSpace(textoDoAnexo)) continue; // Pula se não conseguiu ler o texto

                        // LÓGICA DE MATCH
                        var lowerCaseText = textoDoAnexo.ToLowerInvariant();
                        var matchFound = false;
                        foreach (var keyword in _keywords)
                            if (lowerCaseText.Contains(keyword.ToLowerInvariant()))
                            {
                                _logger.LogInformation(
                                    "MATCH ENCONTRADO! Palavra-chave: '{keyword}' no anexo '{attachmentName}'.",
                                    keyword, attachment.Name);

                                // AÇÃO DE DOWNLOAD
                                var finalDownloadPath = Path.Combine(_downloadPath, attachment.Name);
                                await _emailService.DownloadAttachmentAsync(email.Id, attachment.Id, finalDownloadPath);
                                matchFound = true;
                                break; // Para de procurar keywords, pois já encontrou uma
                            }

                        if (matchFound) break; // Para de olhar outros anexos, pois este e-mail já deu match
                    }

                    // AÇÃO PÓS-PROCESSAMENTO (ESSENCIAL)
                    _logger.LogInformation(
                        "Finalizado processamento do e-mail '{subject}'. Movendo para a pasta 'Processados'.",
                        email.Subject);
                    await _emailService.MoveEmailAsync(email.Id, "Processados");
                }

                _logger.LogInformation("--- Ciclo finalizado. Aguardando 5 minutos para o próximo. ---");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro inesperado no ciclo principal do robô.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}