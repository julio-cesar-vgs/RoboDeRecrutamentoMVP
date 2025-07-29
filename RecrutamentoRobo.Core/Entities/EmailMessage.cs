using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecrutamentoRobo.Core.Entities;

public record AttachmentInfo(string Id, string Name);
public record EmailMessage(string Id, string Subject, IEnumerable<AttachmentInfo> Attachments);
