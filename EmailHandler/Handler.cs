using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Results;
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
                //return new HttpResponseMessage(HttpStatusCode.BadGateway);
                return null;
            }

            var email = $"{req.RequestUri.Segments[1].Replace("/", "")}" +
                                   $"@{req.RequestUri.Segments[2].Replace("/", "")}" +
                                   $".{req.RequestUri.Segments[3].Replace("/", "")}";

            var formData = req.GetQueryNameValuePairs();
            const string margin = "<span style='padding-bottom: 5px;'";
            string formDataFormatted = String.Join($"{margin}<br />", formData.Select(kv => kv.Key + ": " + "<b>" + kv.Value + "</b>"));

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
    }
}