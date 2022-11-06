using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

namespace DSBatchDownloader
{
  public static class Authentication
  {
    public static AuthInfo Authenticate(Config config, ReportDetails report)
    {
      //Check for valid authInfo file to possibly save ourselves having to reauthenticate
      var lastWriteTime = File.GetLastWriteTimeUtc($@"{config.TopLevelDirectory}\authInfo.json");
      if(lastWriteTime > DateTime.UtcNow.AddHours(-config.TokenExpiration))
      {
        var existingAuthInfoJson = File.ReadAllText($@"{config.TopLevelDirectory}\authInfo.json");
        var authInfoObj = JsonSerializer.Deserialize<AuthInfo>(existingAuthInfoJson);
        if (authInfoObj != null && AuthInfo.AuthInfoIsValid(authInfoObj))
        {
          report.UserName = authInfoObj.UserName;
          report.UserEmail = authInfoObj.UserEmail;
          report.AccountName = authInfoObj.AccountName;

          return authInfoObj;
        }
      }

      bool authenticated = false;
      string authCode = string.Empty;

      //Set up a browser for the user to log in to DocuSign
      var authUrl = $"{config.OAuthBase}/auth?response_type=code&scope=signature&client_id={config.ClientId}&redirect_uri={config.RedirectUri}";
      var browserProcess = new Process();
      switch (config.Browser)
      {
        case "edge":
          browserProcess.StartInfo.FileName = "msedge";
          browserProcess.StartInfo.Arguments = $@"{(config.UsePrivate ? "-inprivate" : string.Empty)} --new-window ""{authUrl}""";
          break;
        case "chrome":
          browserProcess.StartInfo.FileName = "chrome";
          browserProcess.StartInfo.Arguments = $@"{(config.UsePrivate ? "--incognito" : string.Empty)} --new-window ""{authUrl}""";
          break;
        case "firefox":
          browserProcess.StartInfo.FileName = "firefox";
          browserProcess.StartInfo.Arguments = $@"{(config.UsePrivate ? "-private-window" : "--new-window")} ""{authUrl}""";
          break;
        default:
          throw new Exception($"Unsupported browser: {config.Browser}");
      }
      browserProcess.StartInfo.UseShellExecute = true;

      //Start a listener that will receive the redirect from DocuSign OAuth
      using var listener = new HttpListener();
      listener.Prefixes.Add($"{config.RedirectUri}{(config.RedirectUri.EndsWith("/") ? string.Empty : "/")}");
      listener.Start();
      listener.BeginGetContext(new AsyncCallback((result) =>
      {
        HttpListener? innerListener = (HttpListener?)result.AsyncState;
        if (innerListener == null) 
          throw new Exception("Listener handler is not working.");
        HttpListenerContext context = innerListener.EndGetContext(result);
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        response.StatusCode = 200;
        response.StatusDescription = "OK";
        response.OutputStream.Write(new byte[0], 0, 0);
        response.OutputStream.Close();

        authCode = request.QueryString["code"] ?? string.Empty;
        authenticated = true;
      }), listener);

      browserProcess.Start();

      while (!authenticated)
      {
        Thread.Sleep(1000);
      }

      //Cleanup
      listener.Stop();
      listener.Close();
      //Might not work if other instances of the same browser are open (i.e. firefox and another firefox)
      browserProcess.Kill(true);

      //Check auth code is valid
      if (string.IsNullOrEmpty(authCode))
        throw new Exception("Did not receive auth code.");

      using var client = new HttpClient();

      //Exchange the auth code for an access token
      using var tokenExchangeMessage = new HttpRequestMessage(HttpMethod.Post, $"{config.OAuthBase}/token");
      tokenExchangeMessage.Headers.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", $"{Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"))}");
      tokenExchangeMessage.Content = new StringContent($"grant_type=authorization_code&code={authCode}", Encoding.UTF8, "application/x-www-form-urlencoded");
      var response = client.Send(tokenExchangeMessage);
      var responseAuthData = JsonSerializer.Deserialize<ResponseAuthData>(response.Content.ReadAsStream());
      if (responseAuthData == null || string.IsNullOrEmpty(responseAuthData.access_token))
        throw new Exception("Unable to get access token.");

      //Get user info
      using var userInfoMessage = new HttpRequestMessage(HttpMethod.Get, $"{config.OAuthBase}/userinfo");
      userInfoMessage.Headers.Authorization = 
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", responseAuthData.access_token);
      var uiResponse = client.Send(userInfoMessage);
      var responseUserInfo = JsonSerializer.Deserialize<ResponseUserInfo>(uiResponse.Content.ReadAsStream());
      if (responseUserInfo == null || responseUserInfo.accounts.Count == 0)
        throw new Exception("Unable to get user info.");

      var authInfo = new AuthInfo()
      {
        AccessToken = responseAuthData.access_token,
        AccountId = responseUserInfo.accounts[0].account_id,
        BaseUri = responseUserInfo.accounts[0].base_uri,
        UserName = responseUserInfo.name,
        UserEmail = responseUserInfo.email,
        AccountName = responseUserInfo.accounts[0].account_name
    };

      report.UserName = authInfo.UserName;
      report.UserEmail = authInfo.UserEmail;
      report.AccountName = authInfo.AccountName;

      if (!AuthInfo.AuthInfoIsValid(authInfo))
        throw new Exception("New auth info is invalid...");

      File.WriteAllText($@"{config.TopLevelDirectory}\authInfo.json", JsonSerializer.Serialize(authInfo));
      return authInfo;
    }

    //Helper classes for DocuSign auth results, since we're not using the SDK
    private class ResponseAuthData
    {
      public string access_token { get; set; } = string.Empty;
      public string token_type { get; set; } = string.Empty;
      public string refresh_token { get; set; } = string.Empty;
      public long expires_in { get; set; }
      public string scope { get; set; } = string.Empty;
    }

    private class ResponseUserInfo
    {
      public string sub { get; set; } = string.Empty;
      public string name { get; set; } = string.Empty;
      public string given_name { get; set; } = string.Empty;
      public string family_name { get; set; } = string.Empty;
      public string created { get; set; } = string.Empty;
      public string email { get; set; } = string.Empty;
      public List<DSAccount> accounts { get; set; } = new List<DSAccount>();
    }

    private class DSAccount
    {
      public string account_id { get; set; } = string.Empty;
      public bool is_default { get; set; }
      public string account_name { get; set; } = string.Empty;
      public string base_uri { get; set; } = string.Empty;
    }
  }
}
