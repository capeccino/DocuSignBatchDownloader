using DocuSign.eSign.Api;
using DocuSign.eSign.Client;
using DocuSign.eSign.Model;

namespace DSBatchDownloader
{
  public class DSController
  {
    private readonly HttpClient webClient = new HttpClient();
    private readonly EnvelopesApi envelopesApi;
    private readonly AuthInfo authInfo;

    public DSController(AuthInfo authInfo)
    {
      var apiClient = new DocuSignClient($"{authInfo.BaseUri}/restapi", webClient);
      apiClient.Configuration.AccessToken = authInfo.AccessToken;

      envelopesApi = new EnvelopesApi(apiClient);
      this.authInfo = authInfo;
    }

    public EnvelopesInformation GetEnvelopes(EnvelopesApi.ListStatusChangesOptions envelopeOptions)
    {
      return envelopesApi.ListStatusChanges(authInfo.AccountId, envelopeOptions);
    }

    public Stream GetDocuments(string envelopeId, string downloadMode)
    {
      return envelopesApi.GetDocument(authInfo.AccountId, envelopeId, downloadMode);
    }
  }
}
