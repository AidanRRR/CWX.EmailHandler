using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs.Host;

namespace EmailHandler
{
    static class HttpRequestMessageExtensions
    {

        private const string HttpContext = "MS_HttpContext";
        private const string RemoteEndpointMessage = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
        private const string OwinContext = "MS_OwinContext";

        public static string GetClientIpString(this HttpRequestMessage request, TraceWriter log)
        {
            if (System.Web.HttpContext.Current != null)
            {
                log.Info($"Logging user host address: {System.Web.HttpContext.Current.Request.UserHostAddress}");
                return System.Web.HttpContext.Current.Request.UserHostAddress;
            }

            //Web-hosting
            if (request.Properties.ContainsKey(HttpContext))
            {
                dynamic ctx = request.Properties[HttpContext];
                if (ctx != null)
                {
                    log.Info($"Logging user host address: {ctx.Request.UserHostAddress}");
                    return ctx.Request.UserHostAddress;
                }
            }

            //Self-hosting
            if (request.Properties.ContainsKey(RemoteEndpointMessage))
            {
                dynamic remoteEndpoint = request.Properties[RemoteEndpointMessage];
                if (remoteEndpoint != null)
                {
                    log.Info($"Logging endpoint address: {remoteEndpoint.Address}");
                    return remoteEndpoint.Address;
                }
            }

            //Owin-hosting
            if (request.Properties.ContainsKey(OwinContext))
            {
                dynamic ctx = request.Properties[OwinContext];
                if (ctx != null)
                {
                    return ctx.Request.RemoteIpAddress;
                }
            }

            // Always return all zeroes for any failure
            log.Info($"Logging fallback ip: 0.0.0.0");
            return "0.0.0.0";
        }

        public static IPAddress GetClientIpAddress(this HttpRequestMessage request, TraceWriter log)
        {
            var ipString = request.GetClientIpString(log);
            IPAddress ipAddress = new IPAddress(0);
            if (IPAddress.TryParse(ipString, out ipAddress))
            {
                return ipAddress;
            }

            return ipAddress;
        }

    }

}