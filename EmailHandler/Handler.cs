using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmailHandler
{
    public static class Handler
    {
        [FunctionName("EmailHandler")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, null, Route = "{*url}")]HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {
            var reqEntity = new RequestEntity(GetIp(req), DateTime.UtcNow);
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));

            if (!CheckRequestValid(reqEntity, req, storageAccount))
            {
                return null;
            }

            var email = $"{req.RequestUri.Segments[1].Replace("/", "")}" +
                                   $"@{req.RequestUri.Segments[2].Replace("/", "")}" +
                                   $".{req.RequestUri.Segments[3].Replace("/", "")}";

            var formData = req.GetQueryNameValuePairs();
            var formDataFormatted = string.Join($"<br /><br />", formData.Select(kv => kv.Key + ": " + "<b>" + kv.Value + "</b>"));

            var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey");
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(email);
            const string subject = "New form submission";
            var to = new EmailAddress(email);

            const string plainTextContent = "";
            var htmlContent = "<h1>New form submission</h1>" + $"{formDataFormatted} <br /> <br /> <br />";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                InsertRequest(reqEntity, storageAccount);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        public static bool CheckRequestValid(RequestEntity reqEntity, HttpRequestMessage req, CloudStorageAccount storageAccount)
        {
            //if (ip == null)
            //{
            //    return new HttpResponseMessage(HttpStatusCode.Forbidden);
            //}

            if (req.RequestUri.Segments.Length != 4)
            {
                return false;
            }

            return true;
        }

        public static async void InsertRequest(RequestEntity req, CloudStorageAccount storageAccount)
        {
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("Requests");

            await table.CreateIfNotExistsAsync();

            var insertOperation = TableOperation.Insert(req);

            await table.ExecuteAsync(insertOperation);
        }

        public static bool CheckClientValid()
        {

            return true;
        }

        public static string GetIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContext) request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            return null;
        }
    }

}