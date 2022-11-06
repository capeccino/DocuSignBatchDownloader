using DocuSign.eSign.Model;
using System.Diagnostics;
using System.Text.Json;

namespace DSBatchDownloader
{
  internal class Program
  {
    static void Main()
    {
      ReportDetails report = new ReportDetails();
      report.RunDate = DateTime.Now;

      //SETUP FROM CONFIG
      Config? config = null;
      string downloadDir = string.Empty;
      try
      {
        var jsonConfig = File.ReadAllText($@".\config.json");

        config = JsonSerializer.Deserialize<Config>(jsonConfig);
        if (config == null) 
          throw new Exception("Config file is invalid.");

        downloadDir = $@"{config.TopLevelDirectory}\DownloadedFiles";
        if (Directory.Exists(downloadDir))
        {
          if (config.ClearPreviousDownloads)
          {
            Directory.Delete(downloadDir, true);
            Directory.CreateDirectory(downloadDir);
          }
          else if (Directory.GetFiles(downloadDir, "*", SearchOption.AllDirectories).Any())
          {
            throw new Exception("Files exist in DownloadedFiles directory but ClearPreviousDownloads setting is false. Cannot proceed.");
          }
        }
        else
          Directory.CreateDirectory(downloadDir);
      }
      catch(Exception ex)
      {
        FailForOverallException(report, config, ex);
      }
      
      //AUTHENTICATION
      AuthInfo authInfo = new AuthInfo();
      try
      {
        authInfo = Authentication.Authenticate(config!, report);
      }
      catch(Exception ex)
      {
        FailForOverallException(report, config, ex);
      }

      var dsController = new DSController(authInfo);

      //GET ENVELOPES
      EnvelopesInformation? envelopeInformation = null;
      IEnumerable<Envelope>? filteredEnvelopes = null;
      try
      {
        envelopeInformation = dsController.GetEnvelopes(config!.EnvelopeOptions);
        if (envelopeInformation == null || envelopeInformation.ResultSetSize == "0")
          throw new Exception("No envelopes were found.");

        report.TotalEnvelopes = envelopeInformation!.ResultSetSize;
        filteredEnvelopes =
          envelopeInformation.Envelopes
          .Where(e => string.IsNullOrEmpty(e.PurgeState) || e.PurgeState == "unpurged");
        report.UnpurgedEnvelopes = filteredEnvelopes.Count();

        if (filteredEnvelopes == null || filteredEnvelopes.Count() == 0)
          throw new Exception("No unpurged envelopes found");

        ColorConsole.WriteMarkedUpString($"Total # Envelopes Found   : <green>{envelopeInformation.ResultSetSize}</green>");
        ColorConsole.WriteMarkedUpString($"Unpurged # Envelopes Found: <green>{filteredEnvelopes.Count()}</green>");
        Console.WriteLine();
      }
      catch(Exception ex)
      {
        FailForOverallException(report, config, ex);
      }

      //DOWNLOAD FILES
      var fileNameDict = new Dictionary<string, int>();
      foreach(var envelope in filteredEnvelopes!)
      {
        var reportResult = new ReportEnvelopeResult();
        reportResult.EnvelopeId = envelope.EnvelopeId;
        reportResult.OriginalName = envelope.EmailSubject;

        Stream responseStream;
        try
        {
          responseStream = dsController.GetDocuments(envelope.EnvelopeId, config!.DownloadMode);
        }
        catch(Exception ex)
        {
          reportResult.Exception = ex;
          reportResult.ResponseSize = 0;
          reportResult.FileName = string.Empty;
          reportResult.FileSize = 0;
          report.Results.Add(reportResult);

          ColorConsole.WriteMarkedUpString($"<red>Error getting file from envelope id {envelope.EnvelopeId}</red>");
          Console.WriteLine(ex.Message);

          if (config!.FailOnFirstError)
            FailForOverallException(report, config, null);

          continue;
        }

        reportResult.ResponseSize = responseStream.Length;

        var fileName = $"{string.Join("", envelope.EmailSubject.Split(Path.GetInvalidFileNameChars()))}";
        var extension = config.DownloadMode == "combined" ? ".pdf" : ".zip";
        if (fileName.EndsWith(".pdf") || fileName.EndsWith(".zip"))
        {
          fileName = fileName.Substring(0, fileName.Length - 4);
        }

        if (!fileNameDict.ContainsKey(fileName))
        {
          fileNameDict.Add(fileName, 0);
          fileName = $"{fileName}{extension}";
        }
        else
        {
          var fnNum = ++fileNameDict[fileName];
          fileName = $"{fileName}({fnNum}){extension}";
        }
        
        using var fileStream = File.Create($@"{downloadDir}\{fileName}");

        responseStream.Seek(0, SeekOrigin.Begin);
        responseStream.CopyTo(fileStream);

        responseStream.Flush();
        responseStream.Close();

        fileStream.Flush();
        fileStream.Close();

        ColorConsole.WriteMarkedUpString($"Saved file: <darkcyan>{fileName}</darkcyan>");

        //Verify for report
        reportResult.FileName = fileName;
        try
        {
          var fileBytes = File.ReadAllBytes($@"{downloadDir}\{fileName}");
          reportResult.FileSize = fileBytes.Length;
          if (!reportResult.Verified)
            throw new Exception("File cannot be verified.");
        }
        catch(Exception ex) 
        {
          reportResult.Exception = ex;
          if (config.FailOnFirstError)
          {
            report.Results.Add(reportResult);
            FailForOverallException(report, config, null);
          } 
        }

        report.Results.Add(reportResult);

        if(config.DownloadIntervalMilliseconds > 0)
          Thread.Sleep(config.DownloadIntervalMilliseconds);
      }
      Console.WriteLine();
      Console.WriteLine($"Done. Opening report...");

      if (report.Results.Where(r => !r.Verified).Any())
        report.Succeeded = false;
      else
        report.Succeeded = true;

      PrintAndOpenReport(report, config);
    }

    private static void PrintAndOpenReport(ReportDetails report, Config? config)
    {
      var reportHtml = ReportBuilder.BuildReport(report, config);
      string reportDirectory = ".";
      try
      {
        if(Directory.Exists(config?.TopLevelDirectory))
          reportDirectory = config.TopLevelDirectory;
      }
      catch { }
      var reportFileName = $@"{reportDirectory}\report{report.RunDate.ToString("MM.dd.yyyy-HH.mm.ss")}.html";
      File.WriteAllText(reportFileName, reportHtml);

      var reportProcess = new Process();
      reportProcess.StartInfo.FileName = reportFileName;
      reportProcess.StartInfo.UseShellExecute = true;
      reportProcess.Start();
    }

    private static void FailForOverallException(ReportDetails report, Config? config, Exception? ex)
    {
      report.Succeeded = false;
      report.OverallException = ex;
      PrintAndOpenReport(report, config);
      Environment.Exit(1);
    }
  }
}