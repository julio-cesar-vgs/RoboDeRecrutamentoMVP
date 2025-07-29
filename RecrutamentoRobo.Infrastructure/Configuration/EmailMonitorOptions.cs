using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecrutamentoRobo.Infrastructure.Configuration
{
    public class EmailMonitorOptions
    {
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string TargetUserEmail { get; set; }
        public string EmailFolderToScan { get; set; }
        public string DownloadPath { get; set; }
    }
}
