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
            var ip = req.GetClientIpString();
            var reqEntity = new RequestEntity(ip);

            log.Info($"Request from ip: {req.GetClientIpString()}");

            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("StorageConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(Environment.GetEnvironmentVariable("RequestsTableName"));

            if (req.RequestUri.Segments.Length != 4)
            {
                return null;
            }
            if (!CheckClientValid(ip, table))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
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
                InsertRequest(reqEntity, table);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }

        public static async void InsertRequest(RequestEntity req, CloudTable table)
        {
            await table.CreateIfNotExistsAsync();
            var insertOperation = TableOperation.Insert(req);

            await table.ExecuteAsync(insertOperation);
        }

        public static bool CheckClientValid(string ip, CloudTable table)
        {
            var query = new TableQuery<RequestEntity>()
                .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, ip));

            var requestCount = 0;
            foreach (RequestEntity entity in table.ExecuteQuery(query))
            {
                var t = entity.Timestamp.UtcDateTime.Date;
                if (t.Hour == DateTime.UtcNow.Date.Hour)
                {
                    requestCount++;
                }
            }

            if (requestCount > Int32.Parse(Environment.GetEnvironmentVariable("MaxMailsFromIpPerHour") ?? throw new InvalidOperationException()))
            {
                return false;
            }

            return true;
        }
    }

}