using DevDays2020.Models;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevDays2020.Data
{
    public class ProductTable : TableBase
    {
        private const string ProductPartition = "PRODUCTS";

        public ProductTable(
            StorageCredentials credentials,
            ILoggerFactory factory)
            : base(StorageConstants.Tables.Products,
                  credentials,
                  factory)
        {
            // for now it's good
            this.Init().Wait();
        }

        public async Task<List<Product>> GetAllAsync()
        {
            var items = new List<Product>();
            var ets = await this.Table.GetAllFromPartition(ProductPartition);

            foreach(var item in ets.OrderByDescending(t=> t.Timestamp))
            {
                items.Add(await item.Unwrap<Product>());
            }
            return items;
        }

        public async Task<Guid> CreateProductAsync(Product product)
        {
            Ensure.ArgumentNotNull(product, nameof(product));

            product.ID = Guid.NewGuid();
            await this.Table.AddAsync<Product>(product, GetKeys(product.ID));
            return product.ID;
        }


        public async Task RemoveSessionAsync(Guid productId)
        {
            var keys = GetKeys(productId);

            await this.Table.RemoveAsync(keys);
        }

        private KeysPair GetKeys(Guid productId)
        {
            return new KeysPair(ProductPartition.ToUpperInvariant(), productId.ToSafeStorageKey());
        }
    }
}
