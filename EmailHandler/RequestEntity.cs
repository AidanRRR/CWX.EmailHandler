using System;
using System.Globalization;
using Microsoft.WindowsAzure.Storage.Table;

namespace EmailHandler
{
    public class RequestEntity : TableEntity
    {
        public RequestEntity(string ip)
        {
            this.PartitionKey = ip;
            this.RowKey = DateTime.UtcNow.ToString("yyyy-MM-ddHH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        public RequestEntity() {}
    }
}
