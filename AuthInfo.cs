namespace DSBatchDownloader
{
  public class AuthInfo
  {
    public string AccessToken { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string BaseUri { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;

    public static bool AuthInfoIsValid(AuthInfo authInfo)
    {
      //Just validate access token, account id, and base uri. The other props are for reports and are optional.
      //Can't actually validate access token because DocuSign tokens can't be decoded like normal jwts.

      if (string.IsNullOrEmpty(authInfo.AccessToken) ||
          !Uri.TryCreate(authInfo.BaseUri, UriKind.Absolute, out _) || 
          !Guid.TryParse(authInfo.AccountId, out _))
        return false;

      return true;
    }
  }
}
