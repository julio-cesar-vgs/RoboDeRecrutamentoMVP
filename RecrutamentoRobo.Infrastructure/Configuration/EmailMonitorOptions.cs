namespace RecrutamentoRobo.Infrastructure.Configuration;

public class EmailMonitorOptions
{
    public string TenantId { get; set; }
    public string ClientId { get; set; }
    public string TargetUserEmail { get; set; }
    public string EmailFolderToScan { get; set; }
    public string DownloadPath { get; set; }
}