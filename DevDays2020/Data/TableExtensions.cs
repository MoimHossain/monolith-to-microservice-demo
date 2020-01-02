using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DevDays2020.Data
{
    public static class TableExtensions
    {
        private const int MAX_FILTER_PARAMETER_COUNT = 50;
        private const int MAX_ROW_FETCH = 100;

        private static Dictionary<Type, List<string>> _validPropertyNames = new Dictionary<Type, List<string>>();

        private static TPayload SafeConvertBack<TPayload>(DynamicTableEntity dte)
        {
            var payloadType = typeof(TPayload);

            if (!_validPropertyNames.ContainsKey(payloadType))
            {
                lock (payloadType)
                {
                    if (!_validPropertyNames.ContainsKey(payloadType))
                    {
                        var properties = payloadType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        _validPropertyNames[payloadType] = properties.Select(p => p.Name).ToList();
                    }
                }

            }

            var propertyNames = _validPropertyNames[payloadType];

            var entityProperties = new Dictionary<string, EntityProperty>();
            foreach (var kv in dte.Properties.Where(p => propertyNames.Contains(p.Key)))
            {
                entityProperties[kv.Key] = kv.Value;
            }
            var payload = EntityPropertyConverter.ConvertBack<TPayload>(entityProperties, new OperationContext());
            return payload;
        }


        private static TableQuery CreateQuery()
        {
            var query = new TableQuery
            {
                TakeCount = MAX_ROW_FETCH
            };
            query.Take(MAX_ROW_FETCH);

            return query;
        }

        public static async Task RemoveAsync(
            this CloudTable table,
            KeysPair keys)
        {
            try
            {
                var entity = new DynamicTableEntity(keys.PartitionKey, keys.RowKey) { ETag = "*" };
                await table.ExecuteAsync(TableOperation.Delete(entity))
                   .ConfigureAwait(false);
            }
            catch (StorageException ex)
            {
                var requestInformation = ex.RequestInformation;
                if (requestInformation != null && requestInformation.HttpStatusCode == 404)
                {
                    // Do nothing
                    return;
                }

                throw ex;
            }
        }

        public static async Task AddAsync<TPayload>(
            this CloudTable table, TPayload payload,
            KeysPair keys)
        {
            Ensure.ArgumentNotNull(keys, nameof(keys));
            Ensure.ArgumentNotNull(payload, nameof(payload));
            Ensure.ArgumentNotNull(table, nameof(table));

            await table.ExecuteAsync(
                TableOperation.Insert(
                    new DynamicTableEntity(keys.PartitionKey, keys.RowKey)
                    {
                        Timestamp = DateTime.UtcNow,
                        Properties = EntityPropertyConverter.Flatten(payload, new OperationContext())
                    }))
                .ConfigureAwait(false);
        }

        public static async Task UpdateAsync<TPayload>(
            this CloudTable table, TPayload payload,
            KeysPair keys)
        {
            Ensure.ArgumentNotNull(keys, nameof(keys));
            Ensure.ArgumentNotNull(payload, nameof(payload));
            Ensure.ArgumentNotNull(table, nameof(table));

            await table.ExecuteAsync(
                    TableOperation.Replace(
                        new DynamicTableEntity(keys.PartitionKey, keys.RowKey)
                        {
                            Timestamp = DateTime.UtcNow,
                            Properties = EntityPropertyConverter.Flatten(payload, new OperationContext()),
                            ETag = "*"
                        }))
                .ConfigureAwait(false);

        }

        public static async Task InsertOrUpdateAsync<TPayLoad>(
           this CloudTable table, TPayLoad payload,
           KeysPair keys)
        {
            Ensure.ArgumentNotNull(keys, nameof(keys));
            Ensure.ArgumentNotNull(payload, nameof(payload));

            await table.ExecuteAsync(
                TableOperation.InsertOrReplace(
                    new DynamicTableEntity(keys.PartitionKey, keys.RowKey)
                    {
                        Timestamp = DateTime.UtcNow,
                        Properties = EntityPropertyConverter.Flatten(payload, new OperationContext()),
                        ETag = "*"
                    }))
             .ConfigureAwait(false);
        }

        public static async Task<TPayload> GetAsync<TPayload>(
            this CloudTable table, string partitionKey, string rowKey)
            where TPayload : class, new()
        {
            Ensure.ArgumentNotNullOrWhiteSpace(partitionKey, nameof(partitionKey));
            Ensure.ArgumentNotNullOrWhiteSpace(rowKey, nameof(rowKey));
            Ensure.ArgumentNotNull(table, nameof(table));

            return await (await table.Retrieve(partitionKey, rowKey)
                .ConfigureAwait(false))
                .Unwrap<TPayload>().ConfigureAwait(false);
        }

        public static async Task<DynamicTableEntity> Retrieve(
            this CloudTable table, string partitionKey, string rowKey)
        {
            Ensure.ArgumentNotNull(table, nameof(table));
            Ensure.ArgumentNotNullOrWhiteSpace(partitionKey, nameof(partitionKey));
            Ensure.ArgumentNotNullOrWhiteSpace(rowKey, nameof(rowKey));

            var tableResult = await table.ExecuteAsync(
                                TableOperation.Retrieve(partitionKey, rowKey))
                                .ConfigureAwait(false);
            return tableResult?.Result as DynamicTableEntity;

        }

        // FYI https://stackoverflow.com/questions/43959589/max-filter-comparisons-in-an-azure-table-query
        // Use this only when you are sure the result sets are way below than 1000 items
        public static async Task<IEnumerable<DynamicTableEntity>> GetAllFromPartition(
            this CloudTable table, string partitionKey, IEnumerable<string> rowKeys = null,
            TableContinuationToken token = null)
        {
            Ensure.ArgumentNotNull(table, nameof(table));
            Ensure.ArgumentNotNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            TableQuery query;

            if (rowKeys == null || !rowKeys.Any())
            {
                query = CreateQuery()
                    .Where(TableQuery.GenerateFilterCondition
                    ("PartitionKey", QueryComparisons.Equal, partitionKey));
            }
            else
            {
                var rowKeyClause = string.Empty;
                foreach (var rowQuery in
                    rowKeys.Select(rk =>
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rk))
                        .Take(MAX_FILTER_PARAMETER_COUNT))
                {
                    rowKeyClause = string.IsNullOrWhiteSpace(rowKeyClause) ? rowQuery :
                        TableQuery.CombineFilters(rowKeyClause, TableOperators.Or, rowQuery);
                }

                query = CreateQuery().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    rowKeyClause));
            }
            var resultSegment = await table.ExecuteQuerySegmentedAsync(query, token)
                .ConfigureAwait(false);
            // Ignoring the token for now. 
            return resultSegment.Results;
        }

        public static async Task<IEnumerable<DynamicTableEntity>> SlowApiScanEntireTableAsync(
            this CloudTable table)
        {
            Ensure.ArgumentNotNull(table, nameof(table));
            var token = default(TableContinuationToken);
            var items = new List<DynamicTableEntity>();
            var query = CreateQuery();

            var resultSegment = await table
                .ExecuteQuerySegmentedAsync(query, token)
                .ConfigureAwait(false);
            items.AddRange(resultSegment.Results);

            while (resultSegment.ContinuationToken != null)
            {
                resultSegment = await table
                .ExecuteQuerySegmentedAsync(query, resultSegment.ContinuationToken)
                .ConfigureAwait(false);

                if (resultSegment != null && resultSegment.Results != null)
                {
                    items.AddRange(resultSegment.Results);
                }
            }
            return items;
        }

        public static async Task<IEnumerable<DynamicTableEntity>> ScanMultiplePartitions(
            this CloudTable table, IEnumerable<string> partitionKeys,
            TableContinuationToken token = null)
        {
            Ensure.ArgumentNotNull(table, nameof(table));
            Ensure.ArgumentNotNull(partitionKeys, nameof(partitionKeys));


            var partitionClause = string.Empty;
            foreach (var pQuery in
                partitionKeys.Select(pk =>
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, pk))
                .Take(MAX_FILTER_PARAMETER_COUNT))
            {
                partitionClause = string.IsNullOrWhiteSpace(partitionClause) ? pQuery :
                    TableQuery.CombineFilters(partitionClause, TableOperators.Or, pQuery);
            }

            var items = new List<DynamicTableEntity>();
            var query = CreateQuery().Where(partitionClause);

            var resultSegment = await table
                .ExecuteQuerySegmentedAsync(query, token)
                .ConfigureAwait(false);
            items.AddRange(resultSegment.Results);

            while (resultSegment.ContinuationToken != null)
            {
                resultSegment = await table
                .ExecuteQuerySegmentedAsync(query, resultSegment.ContinuationToken)
                .ConfigureAwait(false);

                if (resultSegment != null && resultSegment.Results != null)
                {
                    items.AddRange(resultSegment.Results);
                }
            }
            return items;
        }


        public static async Task<IEnumerable<DynamicTableEntity>> GetAll(
            this CloudTable table, string partitionKey, string rowKey = null,
            TableContinuationToken token = null)
        {
            Ensure.ArgumentNotNull(table, nameof(table));
            Ensure.ArgumentNotNullOrWhiteSpace(partitionKey, nameof(partitionKey));

            TableQuery query;

            if (string.IsNullOrWhiteSpace(rowKey))
            {
                query = CreateQuery()
                    .Where(TableQuery.GenerateFilterCondition
                    ("PartitionKey", QueryComparisons.Equal, partitionKey));
            }
            else
            {
                query = CreateQuery().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey)));
            }
            var resultSegment = await table.ExecuteQuerySegmentedAsync(query, token)
                .ConfigureAwait(false);
            // Ignoring the token for now. 

            return resultSegment?.Results;
        }

        public static async Task<TPayload> Unwrap<TPayload>(this DynamicTableEntity dte)
        {
            if (dte == null)
            {
                return default(TPayload);
            }

            return await Task.Factory.StartNew(() =>
            {
                return SafeConvertBack<TPayload>(dte);

            }).ConfigureAwait(false);
        }

        public static async Task<DynamicTableEntity> Wrap<TPayload>(this KeysPair keyPair, TPayload payload)
        {
            return await Task.Factory.StartNew(() =>
            {
                return new DynamicTableEntity(keyPair.PartitionKey, keyPair.RowKey)
                {
                    Timestamp = DateTime.UtcNow,
                    Properties = EntityPropertyConverter.Flatten(payload, new OperationContext()),
                    ETag = "*"
                };

            }).ConfigureAwait(false);
        }

        public static async Task<CloudTable> CreateTableClientAsync(
            StorageCredentials credentials, string tableName,
            bool createTableIfNotExists, ILogger logger)
        {
            Ensure.ArgumentNotNullOrWhiteSpace(tableName, nameof(tableName));
            Ensure.ArgumentNotNull(credentials, nameof(credentials));
            Ensure.ArgumentNotNull(logger, nameof(logger));

            var table = default(CloudTable);
            await SafetyExtensions.ExecuteAsync(logger, async () =>
            {
                var storageAccount = new CloudStorageAccount(credentials, true);
                var tableClient = storageAccount.CreateCloudTableClient();
                table = tableClient.GetTableReference(tableName);
                if (createTableIfNotExists)
                {
                    var created = await table.CreateIfNotExistsAsync()
                               .ConfigureAwait(false);
                }
            });
            return table;
        }
    }

    public class KeysPair
    {
        public KeysPair(string partitionkey, string rowKey)
        {
            PartitionKey = partitionkey;
            RowKey = rowKey;
        }

        public string PartitionKey { get; private set; }
        public string RowKey { get; private set; }

        public string CompositeKey
        {
            get
            {

                return ToString();
            }
        }

        public override string ToString()
        {
            return $"{PartitionKey}{RowKey}";
        }
    }

    public static class PartitionKeyExtensions
    {
        public static string GetPartitionKey(this DateTime dt)
        {
            return string.Format("{0}-{1}-{2}", dt.Year, dt.Month, dt.Day);
        }

        public static string ToSafeStorageKey(this Guid id)
        {
            return id.ToString("N").Replace("-", string.Empty);
        }
    }

    public static class StorageConstants
    {
        public const string PARTITION_KEY = "PartitionKey";
        public const string ROWKEY = "RowKey";



        public static class Tables
        {
            public const string Products = "products";
            public const string SalesOrders = "salesorders";
        }

        public static class Columns
        {
            public const string Json = "JSON";
        }
    }


    public class CustomLogger : ILogger, IDisposable
    {
        private string categoryName;

        public CustomLogger(string categoryName)
        {
            this.categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }

        public void Dispose()
        {
          
        }

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel)
        {
            return true;
            //throw new NotImplementedException();
        }

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //throw new NotImplementedException();
        }
    }
    public class CustomLogFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {

        }

        public ILogger CreateLogger(string categoryName)
        {
            return new CustomLogger(categoryName);
        }

        public void Dispose()
        {

        }
    }
}
