using System.Text;

namespace DSBatchDownloader
{
  public class ReportDetails
  {
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTime RunDate { get; set; }
    public bool Succeeded { get; set; } = false;
    public string TotalEnvelopes { get; set; } = string.Empty;
    public int UnpurgedEnvelopes { get; set; }
    public List<ReportEnvelopeResult> Results { get; set; } = new List<ReportEnvelopeResult>();
    public Exception? OverallException { get; set; } 
  }

  public class ReportEnvelopeResult
  {
    public string EnvelopeId { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long ResponseSize { get; set; }
    public long FileSize { get; set; } = 0;
    public Exception? Exception { get; set; }
    public bool Verified { get; set; } = false;

    public void Verify(int allowance)
    {
      if(FileSize > 0 && Math.Abs(ResponseSize - FileSize) <= allowance)
        Verified = true;
    }
  }

  public static class ReportBuilder
  {
    public static string BuildReport(ReportDetails report, Config? config)
    {
      return $@"
<!DOCTYPE html>
<html>
  <head>
    <title>DSBD Report</title>
    <meta name=""description"" content=""Report of a batch download."" />
    {BuildStyles()}
  </head>
  <body>
    {BuildTitlePart(report)}
    {BuildStatusPart(report)}
    {(config == null ? string.Empty : BuildConfigPart(config))}
    {(report.OverallException == null ? BuildEnvelopesPart(report) : string.Empty)}
    {(report.OverallException == null ? BuildResultsPart(report) : string.Empty)}
    {BuildErrorsPart(report)}
  </body>
</html>
";
    }

    private static string BuildStyles()
    {
      return $@"
    <style>
      html {{
        font-family: sans-serif;
      }}

      table {{
        border-spacing: 0;
        letter-spacing: 1px;
        font-size: 0.8rem;
        display: inline;
      }}

      td,
      th {{
        border: 1px solid rgb(190, 190, 190);
        padding: 10px 20px;
      }}

      th {{
        background-color: rgb(235, 235, 235);
      }}

      td {{
        text-align: center;
      }}

      tr:nth-child(even) td {{
        background-color: rgb(250, 250, 250);
      }}

      tr:nth-child(odd) td {{
        background-color: rgb(245, 245, 245);
      }}

      div {{
        margin: 1rem;
        padding: 1rem;
        border: 1px solid rgb(190, 190, 190);
        border-radius: 10px;
        text-align: center;
      }}

      .caption {{
        padding: 10px;
        font-weight: bold;
        font-size: 2rem;
      }}

      .tlr {{
        border-top-left-radius: 10px;
      }}

      .trr {{
        border-top-right-radius: 10px;
      }}

      .blr {{
        border-bottom-left-radius: 10px;
      }}

      .brr {{
        border-bottom-right-radius: 10px;
      }}

      .title {{
        background-color: #ddd;
      }}

      .status {{
        background-color: #ccc;
      }}

      .status td {{
        font-size: 2rem;
        font-weight: bolder;
      }}

      .config {{
        background-color: #aaaacf;
      }}

      .envcount {{
        background-color: #aaaa66;
      }}

      .results {{
        background-color: #aaccaa;
      }}

      tr.failed td {{
        background-color: #ff6868;
      }}

      .userinfo {{
        background-color: #ccc;
      }}

      .errors {{
        background-color: #ff6868;
      }}
    </style>
";
    }

    private static string BuildTitlePart(ReportDetails report)
    {
      return $@"
    <div class=""title"">
      <h1>DocuSign Batch Downloader Report</h1>
      <h2>Tool Run Date: {report.RunDate}</h2>
      <div class=""userinfo"">
        <h2>Run For:</h2>
        <h3>{report.UserName}</h3>
        <h3>{report.AccountName}</h3>
        <h4>{report.UserEmail}</h4>
      </div>
    </div>
";
    }

    private static string BuildStatusPart(ReportDetails report)
    {
      return $@"
    <div class=""status"">
      <table>
        <tbody>
          <tr>
            <td class=""tlr blr"">Status</td>
            {(report.Succeeded ? "<td class=\"trr brr\" style=\"color: green\">SUCCESS</td>" : "<td class=\"trr brr\" style=\"color: red\">FAILED</td>")}
          </tr>
        </tbody>
      </table>
    </div>
";
    }

    private static string BuildConfigPart(Config? config)
    {
      if(config == null) return string.Empty;

      static string BuildTableRow(string optionName, string optionValue, bool last = false)
      {
        return $@"
          <tr>
            <td{(last ? $@" class=""blr""" : string.Empty)}>{optionName}</td>
            <td{(last ? $@" class=""brr""" : string.Empty)}>{optionValue}</td>
          </tr>
";
      }

      static string ObscureMostOfString(string str)
      {
        List<char> chars = new List<char>();
        for (int i = 0; i < str.Length; i++)
          if (i >= 0 && i < 5)
            chars.Add(str[i]);
          else
            chars.Add('*');

        return string.Join("", chars);
      }

      var dictTr = new Dictionary<string, string>();
      var sb = new StringBuilder();
      dictTr.Add("Client ID", ObscureMostOfString(config.ClientId));
      dictTr.Add("Client Secret", ObscureMostOfString(config.ClientSecret));
      dictTr.Add("Redirect URI", config.RedirectUri);
      dictTr.Add("OAuth Base", config.OAuthBase);
      dictTr.Add("Clear Previous Downloads", config.ClearPreviousDownloads.ToString());
      dictTr.Add("Download Mode", config.DownloadMode);
      dictTr.Add("Download Interval Milliseconds", config.DownloadIntervalMilliseconds.ToString());
      dictTr.Add("Fail on First Error", config.FailOnFirstError.ToString());
      dictTr.Add("Browser", config.Browser);
      dictTr.Add("Use Private", config.UsePrivate.ToString());

      foreach(var prop in config.EnvelopeOptions.GetType().GetProperties())
      {
        var value = prop.GetValue(config.EnvelopeOptions, null)?.ToString();
        if(!string.IsNullOrEmpty(value))
          dictTr.Add(prop.Name, value);
      }

      var keysList = dictTr.Keys.ToList();
      for(int i = 0; i < keysList.Count; i++)
      {
        var key = keysList[i];
        if (i == keysList.Count - 1)
          sb.AppendLine(BuildTableRow(key, dictTr[key], true));
        else
          sb.AppendLine(BuildTableRow(key, dictTr[key], false));
      }


      return $@"
<div class=""config"">
      <table>
        <p class=""caption"">Configuration Options</p>
        <thead>
          <tr>
            <th class=""tlr"">Option</th>
            <th class=""trr"">Value</th>
          </tr>
        </thead>
        <tbody>
          {sb}
        </tbody>
      </table>
    </div>
";
    }

    private static string BuildEnvelopesPart(ReportDetails report)
    {
      return $@"
  <div class=""envcount"">
      <table>
        <p class=""caption"">Envelopes Found Count</p>
        <tbody>
          <tr>
            <td class=""tlr"">Total</td>
            <td class=""trr"">{report.TotalEnvelopes}</td>
          </tr>
          <tr>
            <td class=""blr"">Unpurged</td>
            <td class=""brr"">{report.UnpurgedEnvelopes}</td>
          </tr>
        </tbody>
      </table>
    </div>
";
    }

    private static string BuildResultsPart(ReportDetails report)
    {
      static string BuildTableRow(ReportEnvelopeResult result, bool last = false)
      {
        return $@"
          <tr{(result.Verified ? string.Empty : $@" class=""failed""")}>
            <td{(last ? $@" class=""blr""" : string.Empty)}>{result.EnvelopeId}</td>
            <td>{result.OriginalName}</td>
            <td>{result.ResponseSize}</td>
            <td>{result.FileName}</td>
            <td>{result.FileSize}</td>
            <td{(last ? $@" class=""brr""" : string.Empty)}>{result.Verified}</td>
          </tr>
";
      }

      var sb = new StringBuilder();
      foreach(var result in report.Results)
        sb.AppendLine(BuildTableRow(result, result == report.Results.Last()));

      return $@"
<div class=""results"">
      <table>
        <p class=""caption"">Download Results</p>
        <thead>
          <tr>
            <th class=""tlr"">Envelope ID</th>
            <th>Original Name</th>
            <th>Response Size</th>
            <th>Saved File Name</th>
            <th>Saved File Size</th>
            <th class=""trr"">Verified</th>
          </tr>
        </thead>
        <tbody>
          {sb}
        </tbody>
      </table>
    </div>
";
    }

    private static string BuildErrorsPart(ReportDetails report)
    {
      //check if there are any exceptions to report
      if (report.OverallException == null && !report.Results.Where(r => r.Exception != null).Any())
        return string.Empty;

      if(report.OverallException != null)
      {
        return $@"
  <div class=""errors"">
      <table>
        <p class=""caption"">Errors</p>
        <thead>
          <tr>
            <th class=""trr tlr"">Message</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td class=""brr blr"">{report.OverallException.Message}</td>
          </tr>
        </tbody>
      </table>
    </div>
";
      }

      //Now that we know there is no overall exception, loop through result exceptions and add.
      static string BuildTableRow(ReportEnvelopeResult result, bool last = false)
      {
        return $@"
          <tr>
            <td{(last ? $@" class=""blr""" : string.Empty)}>{result.EnvelopeId}</td>
            <td{(last ? $@" class=""brr""" : string.Empty)}>{result.Exception!.Message}</td>
          </tr>
";
      }

      var sb = new StringBuilder();
      foreach (var result in report.Results.Where(r => r.Exception != null))
        sb.AppendLine(BuildTableRow(result, result == report.Results.Last()));

      return $@"
  <div class=""errors"">
      <table>
        <p class=""caption"">Errors</p>
        <thead>
          <tr>
            <th class=""tlr"">Envelope ID</th>
            <th class=""trr"">Message</th>
          </tr>
        </thead>
        <tbody>
          {sb}
        </tbody>
      </table>
    </div>
";
    }
  }
}
