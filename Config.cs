using DocuSign.eSign.Api;

namespace DSBatchDownloader
{
  public class Config
  {
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string OAuthBase { get; set; } = string.Empty;

    public string TopLevelDirectory { get; set; } = ".";
    public double TokenExpiration { get; set; } = 8;
    public bool ClearPreviousDownloads { get; set; } = true;
    public string DownloadMode { get; set; } = "combined";
    public int DownloadIntervalMilliseconds { get; set; } = 0;
    public bool FailOnFirstError { get; set; } = false;
    public string Browser { get; set; } = "edge";
    public bool UsePrivate { get; set; } = false;
    public EnvelopesApi.ListStatusChangesOptions EnvelopeOptions { get; set; } = new EnvelopesApi.ListStatusChangesOptions()
    {
      fromDate = "2000-01-01",
      fromToStatus = "Completed",
      status = "completed",
      count = "10000",
      startPosition = "0",
      folderTypes = "normal,inbox,sentitems",
      order = "asc",
      orderBy = "subject"
    };
  }
}
