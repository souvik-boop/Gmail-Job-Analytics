using GeminiCSharp;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System;
using System.IO;
using System.Threading;
using System.Configuration;
using GeminiCSharp;
using System.Text;
using System.Globalization;
using Newtonsoft.Json;

namespace GmailExample
{
    class Program
    {
        // The scope of the Gmail API
        static string[] Scopes = { GmailService.Scope.GmailReadonly };

        // The client secret JSON file path
        static string ClientSecretPath = "client_secret.json";

        // The user identifier
        static string UserId = "souvik.mazumder.work@gmail.com";

        static void Main(string[] args)
        {
            Dictionary<string, int> appliedCount = new Dictionary<string, int>();

            // Authorize the application and get a UserCredential object
            UserCredential credential;
            using (var stream = new FileStream(ClientSecretPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    UserId,
                    CancellationToken.None).Result;
            }

            // Create a GmailService object
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "GmailExample",
            });

            // List the messages in the inbox
            var request = service.Users.Messages.List(UserId);
            request.LabelIds = "INBOX";
            request.MaxResults = 500; // change this to a larger value or null to get more results
            var response = request.Execute();
            while (response != null)
            {
                if (response.Messages != null && response.Messages.Count > 0)
                {
                    string result = string.Empty;
                    foreach (var message in response.Messages)
                    {
                        //ExtractAppliedCompanyInfoV1(appliedCount, service, message);

                        // Get the message details
                        var messageRequest = service.Users.Messages.Get(UserId, message.Id);
                        var messageResponse = messageRequest.Execute();

                        // Initialize the gemini client
                        var apiKey = "AIzaSyArgxniFT1_Pe8J6Khr-39PWCqbFsl4y8c";
                        var geminiChat = new GeminiChat(apiKey);
                        using var httpClient1 = new HttpClient();

                        string subject = messageResponse.Payload.Headers.FirstOrDefault(s => s.Name == "Subject") != null ? messageResponse.Payload.Headers.FirstOrDefault(s => s.Name == "Subject").Value : "";
                        string From = messageResponse.Payload.Headers.FirstOrDefault(s => s.Name == "From").Value;

                        if ((subject.Contains("application", StringComparison.InvariantCultureIgnoreCase) ||
                                 subject.Contains("applying", StringComparison.InvariantCultureIgnoreCase)) &&
                            !From.Contains("@walmart.com", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string row = geminiChat.SendMessageAsync($"Extract the date (in mm/dd/yyyy), company name, job application status (possible options include - applied, rejected, accepted, interview scheduled) and  job role from the following json data formatted like this ([Date] Company Name | Status | Role): {JsonConvert.SerializeObject(messageResponse, Formatting.Indented)}", httpClient1).Result;
                            result += row + "\n";
                            
                            geminiChat.ResetToNewChat();
                            using var httpClient2 = new HttpClient();
                            string finalText = geminiChat.SendMessageAsync($"Format this data into json array string: {result}", httpClient2).Result;

                            Console.Clear();
                            Console.WriteLine(finalText);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No messages found.");
                }

                // Get the next page token
                var pageToken = response.NextPageToken;

                // Check if there are more pages
                if (pageToken != null)
                {
                    // Set the page token for the next request
                    request.PageToken = pageToken;

                    // Execute the next request
                    response = request.Execute();
                }
                else
                {
                    // Break the loop if there are no more pages
                    response = null;
                }
            }

            foreach (var item in appliedCount)
            {
                Console.WriteLine($"Applied Company: {item.Key} | Total Applied Jobs: {item.Value}");
            }
        }

        private static void ExtractAppliedCompanyInfoV1(Dictionary<string, int> appliedCount, GmailService service, Message? message)
        {
            // Get the message details
            var messageRequest = service.Users.Messages.Get(UserId, message.Id);
            var messageResponse = messageRequest.Execute();

            string subject = messageResponse.Payload.Headers.FirstOrDefault(s => s.Name == "Subject") != null ? messageResponse.Payload.Headers.FirstOrDefault(s => s.Name == "Subject").Value : "";
            string From = messageResponse.Payload.Headers.FirstOrDefault(s => s.Name == "From").Value;
            string date = messageResponse.Payload.Headers.FirstOrDefault(s => s.Name == "Date").Value;

            // messageResponse.Payload.Headers.Select(s => s.Name).ToList().ForEach(s => Console.WriteLine(s));


            if ((subject.Contains("application", StringComparison.InvariantCultureIgnoreCase) ||
                subject.Contains("applying", StringComparison.InvariantCultureIgnoreCase)) &&
                !From.Contains("@walmart.com", StringComparison.InvariantCultureIgnoreCase))
            {
                var apiKey = "AIzaSyArgxniFT1_Pe8J6Khr-39PWCqbFsl4y8c";
                var geminiChat = new GeminiChat(apiKey);
                using var httpClient1 = new HttpClient();

                string textData = string.Empty;
                if (messageResponse.Payload.Parts != null && messageResponse.Payload.Parts.FirstOrDefault(s => s.MimeType == "text/plain") != null)
                {
                    textData = Encoding.UTF8.GetString(Convert.FromBase64String(
                    messageResponse.Payload.Parts.FirstOrDefault(s => s.MimeType == "text/plain").Body.Data.Replace("-", "+").Replace("_", "/")));
                }

                string bodyData = string.Empty;
                if (messageResponse.Payload.Body != null && messageResponse.Payload.Body.Data != null)
                {
                    bodyData = Encoding.UTF8.GetString(Convert.FromBase64String(
                    messageResponse.Payload.Body.Data.Replace("-", "+").Replace("_", "/")));
                }

                string company = geminiChat.SendMessageAsync($"Extract the company name from the following sentence and Return 0 if company name not found : {subject + "\n" + textData + "\n" + bodyData}", httpClient1).Result;

                geminiChat.ResetToNewChat();

                using var httpClient2 = new HttpClient();

                string status = geminiChat.SendMessageAsync($"Respond only with either applied, rejected, accepted, interview scheduled based on the following text content : {subject + "\n" + textData + "\n" + bodyData}", httpClient2).Result;

                geminiChat.ResetToNewChat();

                using var httpClient3 = new HttpClient();

                string role = geminiChat.SendMessageAsync($"Extract the role if possible from the following text content, otherwise return NOT_FOUND : {subject + "\n" + textData + "\n" + bodyData}", httpClient3).Result;

                geminiChat.ResetToNewChat();

                if (company != "0")
                    Console.WriteLine($"[{date}] Company: {company} | \tStatus: {status} | \tRole: {role}");

                if (!appliedCount.ContainsKey(company)) appliedCount.Add(company, 1);
                else appliedCount[company] += 1;
            }
        }
    }
}
