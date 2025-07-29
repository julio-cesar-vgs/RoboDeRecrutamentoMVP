namespace RecrutamentoRobo.Core.Entities;

public record AttachmentInfo(string Id, string Name);

public record EmailMessage(string Id, string Subject, IEnumerable<AttachmentInfo> Attachments);