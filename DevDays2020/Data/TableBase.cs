using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevDays2020.Data
{
    public abstract class TableBase
    {
        private readonly string _tableName;
        private CloudTable _table;
        private readonly ILogger _logger;
        private readonly StorageCredentials _credentials;

        protected TableBase(
            string tableName,
            StorageCredentials credentials,
            ILoggerFactory factory)
        {
            Ensure.ArgumentNotNull(factory, nameof(factory));
            Ensure.ArgumentNotNull(credentials, nameof(credentials));
            Ensure.ArgumentNotNullOrWhiteSpace(tableName, nameof(tableName));

            _tableName = tableName;
            _credentials = credentials;
            _logger = factory.CreateLogger(GetType().FullName);
        }

        public virtual async Task<bool> Init()
        {
            _table = await TableExtensions
                .CreateTableClientAsync(
                    _credentials, _tableName, true, _logger)
                    .ConfigureAwait(false);

            return _table != null;
        }

        protected virtual CloudTable Table => _table;
        protected ILogger Logger => _logger;

        public StorageCredentials Credentials => _credentials;
    }
}
