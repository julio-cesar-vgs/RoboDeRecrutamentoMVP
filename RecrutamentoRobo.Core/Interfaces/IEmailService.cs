using RecrutamentoRobo.Core.Entities;

namespace RecrutamentoRobo.Core.Interfaces;

public interface IEmailService
{
    // Tarefa para buscar e-mails não lidos
    Task<IEnumerable<EmailMessage>> GetUnreadEmailsAsync();

    // Tarefa para extrair o texto de um anexo
    Task<string> GetAttachmentTextAsync(string messageId, string attachmentId);

    // Tarefa para baixar um anexo
    Task DownloadAttachmentAsync(string messageId, string attachmentId, string downloadPath);

    // Tarefa para marcar um e-mail como lido
    Task MarkEmailAsReadAsync(string messageId);
}