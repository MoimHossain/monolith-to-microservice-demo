using DevDays2020.Models;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevDays2020.Data
{
    public class SalesOrderTable : TableBase
    {
        private const string SOPartition = "SO";

        public SalesOrderTable(
            StorageCredentials credentials,
            ILoggerFactory factory)
            : base(StorageConstants.Tables.SalesOrders,
                  credentials,
                  factory)
        {
            // for now it's good
            this.Init().Wait();
        }

        public async Task<List<SalesOrder>> GetAllAsync()
        {
            var items = new List<SalesOrder>();
            var ets = await this.Table.GetAllFromPartition(SOPartition);

            foreach(var item in ets.OrderByDescending(t=> t.Timestamp))
            {
                items.Add(await item.Unwrap<SalesOrder>());
            }
            return items;
        }

        public async Task<Guid> CreateSOAsync(SalesOrder so, List<LineItem> items)
        {
            Ensure.ArgumentNotNull(so, nameof(so));

            so.ID = Guid.NewGuid();
            await this.Table.AddAsync<SalesOrder>(so, GetKeys(so.ID));

            foreach(var item in items)
            {
                item.OrderID = so.ID;
                await this.Table.AddAsync<LineItem>(item, new KeysPair(so.ID.ToSafeStorageKey(), item.ID.ToSafeStorageKey()));
            }
            return so.ID;
        }


        public async Task RemoveSessionAsync(Guid productId)
        {
            var keys = GetKeys(productId);

            await this.Table.RemoveAsync(keys);
        }

        private KeysPair GetKeys(Guid productId)
        {
            return new KeysPair(SOPartition.ToUpperInvariant(), productId.ToSafeStorageKey());
        }
    }
}
