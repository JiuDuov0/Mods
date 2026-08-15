using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Redis.Interface;
using StackExchange.Redis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Redis.Realization
{
    public class RedisManageService : IRedisManageService, IHostedService
    {
        private const int RedisScanPageSize = 500;
        private const int RedisBatchSize = 128;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatString = "yyyy-MM-dd HH:mm:ss",
            ContractResolver = new DefaultContractResolver()
        };

        private readonly IConfiguration _configuration;
        private readonly ConfigurationOptions _configurationOptions;
        private readonly EndPoint _redisEndPoint;

        private ConnectionMultiplexer? _redisConnection;
        private Task<ConnectionMultiplexer>? _connectionTask;

        public RedisManageService(IConfiguration configuration)
        {
            _configuration = configuration;

            var host = configuration["Ip"];
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException(
                    "Redis Ip 未配置。");
            }

            if (!int.TryParse(configuration["Port"], out var port))
            {
                throw new InvalidOperationException(
                    "Redis Port 配置无效。");
            }

            if (port is < 1 or > 65535)
            {
                throw new InvalidOperationException(
                    "Redis Port 配置必须在 1 到 65535 之间。");
            }

            _redisEndPoint = new DnsEndPoint(host, port);

            _configurationOptions = new ConfigurationOptions
            {
                ClientName = "JD_Soft_FrameWork_Redis",
                Password = configuration["Password"],
                ConnectTimeout = GetIntConfiguration("Timeout", 5000),
                DefaultDatabase = GetIntConfiguration("DB", 0),
                AbortOnConnectFail = false,
                AllowAdmin = true
            };

            _configurationOptions.EndPoints.Add(_redisEndPoint);
        }

        private int GetIntConfiguration(
            string key,
            int defaultValue)
        {
            return int.TryParse(_configuration[key], out var value)
                ? value
                : defaultValue;
        }

        private ConnectionMultiplexer GetConnection()
        {
            return GetConnectionAsync()
                .GetAwaiter()
                .GetResult();
        }

        private async Task<ConnectionMultiplexer> GetConnectionAsync()
        {
            var connection = Volatile.Read(ref _redisConnection);

            if (connection != null)
            {
                return connection;
            }

            return await StartConnectionAttemptAsync()
                .ConfigureAwait(false);
        }

        private Task<ConnectionMultiplexer>
            StartConnectionAttemptAsync()
        {
            var currentTask = Volatile.Read(ref _connectionTask);

            if (currentTask is { IsCompleted: false })
            {
                return currentTask;
            }

            var taskSource =
                new TaskCompletionSource<ConnectionMultiplexer>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            if (Interlocked.CompareExchange(
                    ref _connectionTask,
                    taskSource.Task,
                    currentTask) != currentTask)
            {
                return StartConnectionAttemptAsync();
            }

            _ = ConnectAsync(taskSource);

            return taskSource.Task;
        }

        private async Task ConnectAsync(
            TaskCompletionSource<ConnectionMultiplexer> taskSource)
        {
            try
            {
                var connection = await ConnectionMultiplexer
                    .ConnectAsync(_configurationOptions)
                    .ConfigureAwait(false);

                var oldConnection = Interlocked.Exchange(
                    ref _redisConnection,
                    connection);

                if (oldConnection != null &&
                    oldConnection != connection)
                {
                    oldConnection.Dispose();
                }

                taskSource.TrySetResult(connection);
            }
            catch (Exception exception)
            {
                taskSource.TrySetException(exception);
            }
            finally
            {
                Interlocked.CompareExchange(
                    ref _connectionTask,
                    null,
                    taskSource.Task);
            }
        }

        private IDatabase GetDatabase(int DB = 0)
        {
            return GetConnection().GetDatabase(DB);
        }

        private async Task<IDatabase> GetDatabaseAsync(int DB = 0)
        {
            var connection = await GetConnectionAsync()
                .ConfigureAwait(false);

            return connection.GetDatabase(DB);
        }

        private IServer GetServer()
        {
            return GetConnection().GetServer(_redisEndPoint);
        }

        private async Task<IServer> GetServerAsync()
        {
            var connection = await GetConnectionAsync()
                .ConfigureAwait(false);

            return connection.GetServer(_redisEndPoint);
        }

        private static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, JsonSettings);
        }

        public void Set(
            string key,
            object value,
            object? ts = null,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return;
            }

            var cacheTime = ts is TimeSpan timeSpan
                ? timeSpan
                : TimeSpan.FromDays(1);

            GetDatabase(DB).StringSet(
                key,
                Serialize(value),
                cacheTime);
        }

        public async Task SetAsync(
            string key,
            object value,
            object? cacheTime = null,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return;
            }

            var expiration = cacheTime is TimeSpan timeSpan
                ? timeSpan
                : TimeSpan.FromDays(1);

            var database = await GetDatabaseAsync(DB)
                .ConfigureAwait(false);

            await database.StringSetAsync(
                key,
                Serialize(value),
                expiration)
                .ConfigureAwait(false);
        }

        public string GetValue(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            return GetDatabase(DB)
                .StringGet(key)
                .ToString();
        }

        public async Task<string> GetValueAsync(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var database = await GetDatabaseAsync(DB)
                .ConfigureAwait(false);

            var value = await database.StringGetAsync(key)
                .ConfigureAwait(false);

            return value.ToString();
        }

        public TEntity? Get<TEntity>(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return default;
            }

            var value = GetDatabase(DB).StringGet(key);

            if (!value.HasValue)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<TEntity>(
                value.ToString());
        }

        public async Task<TEntity?> GetAsync<TEntity>(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return default;
            }

            var database = await GetDatabaseAsync(DB)
                .ConfigureAwait(false);

            var value = await database.StringGetAsync(key)
                .ConfigureAwait(false);

            if (!value.HasValue)
            {
                return default;
            }

            return JsonConvert.DeserializeObject<TEntity>(
                value.ToString());
        }

        public async Task<TEntity?> GetEntityAsync<TEntity>(
            string key,
            int DB = 0)
        {
            return await GetAsync<TEntity>(key, DB)
                .ConfigureAwait(false);
        }

        public bool KeyExists(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return GetDatabase(DB).KeyExists(key);
        }

        public async Task<bool> KeyExistsAsync(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var database = await GetDatabaseAsync(DB)
                .ConfigureAwait(false);

            return await database.KeyExistsAsync(key)
                .ConfigureAwait(false);
        }

        public void Remove(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            GetDatabase(DB).KeyDelete(key);
        }

        public async Task RemoveAsync(
            string key,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var database = await GetDatabaseAsync(DB)
                .ConfigureAwait(false);

            await database.KeyDeleteAsync(key)
                .ConfigureAwait(false);
        }

        public void Clear(int? DB = null)
        {
            var server = GetServer();

            if (DB.HasValue)
            {
                server.FlushDatabase(DB.Value);
            }
            else
            {
                server.FlushAllDatabases();
            }
        }

        public async Task ClearAsync(int? DB = null)
        {
            var server = await GetServerAsync()
                .ConfigureAwait(false);

            if (DB.HasValue)
            {
                await server.FlushDatabaseAsync(DB.Value)
                    .ConfigureAwait(false);
            }
            else
            {
                await server.FlushAllDatabasesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task RemoveByKey(
            string pattern,
            int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return;
            }

            var database = await GetDatabaseAsync(DB)
                .ConfigureAwait(false);

            var server = await GetServerAsync()
                .ConfigureAwait(false);

            var keys = new List<RedisKey>(RedisBatchSize);

            await foreach (var key in server.KeysAsync(
                database: DB,
                pattern: pattern,
                pageSize: RedisScanPageSize))
            {
                keys.Add(key);

                if (keys.Count < RedisBatchSize)
                {
                    continue;
                }

                await database.KeyDeleteAsync(keys.ToArray())
                    .ConfigureAwait(false);

                keys.Clear();
            }

            if (keys.Count > 0)
            {
                await database.KeyDeleteAsync(keys.ToArray())
                    .ConfigureAwait(false);
            }
        }

        public async Task<List<string>> GetValuesByPatternAsync(
            string pattern,
            int DB = 0)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(pattern))
            {
                return result;
            }

            var database = await GetDatabaseAsync(DB)
                .ConfigureAwait(false);

            var server = await GetServerAsync()
                .ConfigureAwait(false);

            var keys = new List<RedisKey>(RedisBatchSize);

            await foreach (var key in server.KeysAsync(
                database: DB,
                pattern: pattern,
                pageSize: RedisScanPageSize))
            {
                keys.Add(key);

                if (keys.Count < RedisBatchSize)
                {
                    continue;
                }

                await AppendValuesAsync(
                        database,
                        keys,
                        result)
                    .ConfigureAwait(false);

                keys.Clear();
            }

            if (keys.Count > 0)
            {
                await AppendValuesAsync(
                        database,
                        keys,
                        result)
                    .ConfigureAwait(false);
            }

            return result;
        }

        private static async Task AppendValuesAsync(
            IDatabase database,
            List<RedisKey> keys,
            List<string> result)
        {
            var values = await database.StringGetAsync(
                    keys.ToArray())
                .ConfigureAwait(false);

            foreach (var value in values)
            {
                if (value.HasValue &&
                    !string.IsNullOrWhiteSpace(value))
                {
                    result.Add(value.ToString().Trim('"'));
                }
            }
        }

        public List<object> SelectTags(string pattern)
        {
            var result = new List<object>();

            if (string.IsNullOrWhiteSpace(pattern))
            {
                return result;
            }

            var database = GetDatabase();
            var server = GetServer();

            foreach (var key in server.Keys(
                database: 0,
                pattern: pattern))
            {
                var value = database.StringGet(key);

                if (value.HasValue)
                {
                    result.Add(value.ToString());
                }
            }

            return result;
        }

        public async Task LikeRemoveAsync(string pattern)
        {
            await RemoveByKey(pattern)
                .ConfigureAwait(false);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await GetConnectionAsync()
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            var connection = Interlocked.Exchange(
                ref _redisConnection,
                null);

            connection?.Dispose();

            return Task.CompletedTask;
        }
    }
}