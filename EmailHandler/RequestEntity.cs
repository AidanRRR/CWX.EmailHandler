using System;
using System.Globalization;
using Microsoft.WindowsAzure.Storage.Table;

namespace EmailHandler
{
    public class RequestEntity : TableEntity
    {
        public RequestEntity(string ip, DateTime dateTime)
        {
            this.PartitionKey = ip;
            this.RowKey = dateTime.ToString(CultureInfo.CurrentCulture);
        }

        public string Ip { get; set; }
        public DateTime InsertTimestamp { get; set; }
    }
}
