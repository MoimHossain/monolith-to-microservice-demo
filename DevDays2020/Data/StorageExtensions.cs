using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevDays2020.Data
{
    public static class StorageExtensions
    {
        public static void RegisterStorageImplementations(this IServiceCollection services)
        {
            var credentials = GetStorageCredentials();

            services.AddSingleton<StorageCredentials>(credentials);

            services.AddSingleton<ProductTable>();
            services.AddSingleton<SalesOrderTable>();
        }

        public static StorageCredentials GetStorageCredentials()
        {
            var accountName = Environment.GetEnvironmentVariable("devday2020saname");
            var storageKey = Environment.GetEnvironmentVariable("devday2020sakey");

            var credentials = new StorageCredentials(accountName, storageKey);
            return credentials;
        }
    }
}
