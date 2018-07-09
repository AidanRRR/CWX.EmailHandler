using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace EmailHandler
{
    public static class Handler
    {
        [FunctionName("EmailHandler")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, null, Route = "{*url}")]HttpRequestMessage req, TraceWriter log, ExecutionContext context)
        {
            if (req.RequestUri.Segments.Length != 4)
            {
                return null;
            }

            var ip = GetIp(req);

            if (ip == null)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            var email = $"{req.RequestUri.Segments[1].Replace("/", "")}" +
                                   $"@{req.RequestUri.Segments[2].Replace("/", "")}" +
                                   $".{req.RequestUri.Segments[3].Replace("/", "")}";

            var formData = req.GetQueryNameValuePairs();
            const string margin = "<span style='padding-bottom: 5px;'";
            string formDataFormatted = String.Join($"<br /><br />", formData.Select(kv => kv.Key + ": " + "<b>" + kv.Value + "</b>"));

            var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey");
            var client = new SendGridClient(apiKey);

            var from = new EmailAddress(email);
            const string subject = "New form submission";
            var to = new EmailAddress(email);

            const string plainTextContent = "";
            var htmlContent = "<h1>New form submission</h1>" + $"{formDataFormatted} <br /> <br /> <br />";

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        public static string GetIp(HttpRequestMessage request)
        {
            return request.Properties.ContainsKey("MS_HttpContext") ? ((HttpContext)request.Properties["MS_HttpContext"]).Request.UserHostAddress : null;
        }
    }

}