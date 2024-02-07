using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderStatefullService
{
    public class AzureStorageClient
    {
        private readonly CloudStorageAccount storageAccount;

        public AzureStorageClient(string connectionString)
        {
            this.storageAccount = CloudStorageAccount.Parse(connectionString);
        }
    }
}
