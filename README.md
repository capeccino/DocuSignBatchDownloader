# DocuSign Batch Downloader Tool

## Overview

As of late 2022, DocuSign does not provide the ability to batch download files from an account. This tool uses DocuSign's eSignature API through the C# SDK to provide the batch download functionality.

## Disclaimer

This tool has been created for personal use. It's unlikely it will mess anything up for you, because it's only retrieving existing data. Still, I make no guarantees whatsoever about the tool's safety and/or whether it will work for you. Also, don't expect many updates. Feel free to tailor it to your own purposes.

## Supported Platforms

The tool has only been tested on Windows 10, and I can't guarantee it will work on any other platform, even though it's written using .NET 6 (supposedly cross platform). Theoretically, if you install a .NET 6 runtime, it should at least run.

## How to Use

You need a DocuSign Developer Account to use this tool. If this is your first DocuSign app, you might want to read up on the using their APIs in general...[eSignature API overview](https://developers.docusign.com/docs/esign-rest-api/).

The general steps to use this tool will look like this:

1. Download the latest release package of this tool and extract.
2. Read sampleconfig.json.txt and do as it says. Create the config.json file and be ready to copy some values into it.
3. Set up a DocuSign Developer Account.
4. In the Apps portal of your new account, create a new App, call it whatever you want.
5. Open the App Details in the portal.
6. Copy the Integration Key into config.json as the ClientId value.
7. Create a client secret in App Details and copy that into config.json in the appropriate place. Do this immediately because after you leave App details you'll never be able to see the full value again.
8. Create a redirect URI in App Details. If you're unsure, just copy it exactly from the sample config file. One way or another, whatever shows up in App Details must exactly match the config file. Bear in mind that an HTTP listener is started to listen for requests on this endpoint, so it's probably best to keep it a localhost URI.
9. Save the changes to App details.
10. Adjust any other values in the config, if you want. Of particular important are the EnvelopeOptions, since they determine which files are ultimately found. Read up on the DocuSign API docs to understand which values do what. I'm using the C# SDK, so these are the valid options: [ListStatusChangesOptions](https://docusign.github.io/docusign-esign-csharp-client/class_docu_sign_1_1e_sign_1_1_api_1_1_envelopes_api_1_1_list_status_changes_options.html)
11. Run the executable, and sign in with your developer account credentials.

### Go-Live

If you followed the steps above, you're using the DocuSign Demo environment. In order to use the tool on real accounts, you need to go through the [Go-Live](https://developers.docusign.com/docs/esign-rest-api/go-live/) process. Basically, you need a paid API account to access real data.

The easiest way to Go Live is to add a few envelopes and a few documents to your developer account, then use the tool to download them. This will quickly achieve the minimum quota of 20 successful API calls, and you can then begin the Go-Live review process.

Once you have successfully gone live, be sure to update the config.json file with the new values (new client secret and remove "-d" from OAuth base.)

## What happens when I run the tool?

The steps the tool moves through are as follows:

1. Load the configurations from config.json
2. Create a directory called DownloadedFiles, or clear out an existing one, according to settings in the config file.
3. Go through OAuth process. A browser will open for you to log into the account you want to download docs from, and an HTTP listener is opened behind the scenes to wait for DocuSign to redirect to it with an auth code. If you successfully log in, OAuth will succeed and this process ends with a file called authInfo.json being created. The purpose of the file is so that if you want to make tweaks in the config file and run the tool again, you don't need to authenticate again for the life of the token (currently 8 hours, according to DocuSign).
4. After OAuth succeeds, get all the envelopes according to the filters set in the config file.
5. Loops through each envelope and download its documents as either a combined PDF or a zip archive, according to settings. I have found the archive functionality to be broken on DocuSign's end (it returns nothing on some envelopes that do have documents, even if the API is called directly with a tool like Postman). So I recommend using "combined". Files are saved to the DownloadedFiles and verified.
6. A report is written and opens in the OS default browser.

## Limitations

### Multiple Account Users

Apparently, a DocuSign user can be associated to multiple "accounts". So for a given token, retrieved from a user login, multiple accounts might show up when retrieving user info. This tool does not handle that situation. It will only use the first account in the list if there are multiple.

### Individual Documents

This tool does not download individual files, only combined PDFs where each document is combined into a single PDF, or a zip archive where each file is included in an archive. As previously noted, the archive functionality doesn't seem to be working, so "combined" is really the only way to go with this tool.

### Security

Frankly, it's not very secure to be storing the client secret and access tokens in plain text on the file system. If you set the tool up and then share it with someone else, be very careful not to include any information you don't mean to. As originally stated in the disclaimer, this tool was created for personal use. I know what I'm doing on my own machine with my own API account. Take the time to understand your own situation too.

## Development

If you'd like to change the tool to your own purposes, it should be pretty easy to get started. There are almost no dependencies. I recommend Visual Studio 2022, since it will already have the tools necessary to develop with .NET 6.

Open the project in VS2022, restore the NuGet packages, and build. When running the built tool from the IDE, make sure to overwrite TopLevelDirectory in the config file to the build folder that houses the executable (that's really why I added it as a config option).
