using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Redis.Interface;
using StackExchange.Redis;
using System.Net;

namespace Redis.Realization
{
    public class RedisManageService : IRedisManageService
    {
        private const int RedisScanPageSize = 500;
        private const int RedisBatchSize = 128;
        private readonly IConfiguration _configuration;
        private readonly ConfigurationOptions _configurationOptions;
        private readonly EndPoint _redisEndPoint;
        private readonly object _connectionLock = new();

        private ConnectionMultiplexer? _redisConnection;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatString = "yyyy-MM-dd HH:mm:ss",
            ContractResolver = new DefaultContractResolver()
        };

        public RedisManageService(IConfiguration configuration)
        {
            _configuration = configuration;

            var host = configuration["Ip"];
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new InvalidOperationException("Redis Ip 未配置。");
            }

            if (!int.TryParse(configuration["Port"], out var port))
            {
                throw new InvalidOperationException("Redis Port 配置无效。");
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

        private int GetIntConfiguration(string key, int defaultValue)
        {
            return int.TryParse(_configuration[key], out var value)
                ? value
                : defaultValue;
        }

        private ConnectionMultiplexer? GetConnection()
        {
            if (_redisConnection is { IsConnected: true })
            {
                return _redisConnection;
            }

            lock (_connectionLock)
            {
                if (_redisConnection is { IsConnected: true })
                {
                    return _redisConnection;
                }

                try
                {
                    _redisConnection?.Dispose();
                }
                catch
                {
                    // 忽略旧连接释放异常
                }

                try
                {
                    _redisConnection = ConnectionMultiplexer.Connect(_configurationOptions);
                    return _redisConnection;
                }
                catch
                {
                    _redisConnection = null;
                    return null;
                }
            }
        }

        private IDatabase? GetDatabase(int DB = 0)
        {
            return GetConnection()?.GetDatabase(DB);
        }

        private IServer? GetServer()
        {
            var connection = GetConnection();
            if (connection == null)
            {
                return null;
            }

            try
            {
                return connection.GetServer(_redisEndPoint);
            }
            catch
            {
                return null;
            }
        }

        private static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, JsonSettings);
        }

        public void Set(string key, object value, object? ts = null, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return;
            }

            var database = GetDatabase(DB);
            if (database == null)
            {
                return;
            }

            var cacheTime = ts is TimeSpan timeSpan
                ? timeSpan
                : TimeSpan.FromDays(1);

            database.StringSet(key, Serialize(value), cacheTime);
        }

        public async Task SetAsync(string key, object value, object? cacheTime = null, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key) || value == null)
            {
                return;
            }

            var database = GetDatabase(DB);
            if (database == null)
            {
                return;
            }

            var expiration = cacheTime is TimeSpan timeSpan
                ? timeSpan
                : TimeSpan.FromDays(1);

            await database.StringSetAsync(
                key,
                Serialize(value),
                expiration);
        }

        public string GetValue(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var database = GetDatabase(DB);
            return database?.StringGet(key).ToString() ?? string.Empty;
        }

        public async Task<string> GetValueAsync(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var database = GetDatabase(DB);
            if (database == null)
            {
                return string.Empty;
            }

            return (await database.StringGetAsync(key)).ToString();
        }

        public TEntity? Get<TEntity>(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return default;
            }

            try
            {
                var database = GetDatabase(DB);
                var value = database?.StringGet(key);

                if (!value.HasValue)
                {
                    return default;
                }

                return JsonConvert.DeserializeObject<TEntity>(value.ToString());
            }
            catch
            {
                return default;
            }
        }

        public async Task<TEntity> GetAsync<TEntity>(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return default;
            }

            try
            {
                var database = GetDatabase(DB);
                if (database == null)
                {
                    return default;
                }

                var value = await database.StringGetAsync(key);
                if (!value.HasValue)
                {
                    return default;
                }

                return JsonConvert.DeserializeObject<TEntity>(value.ToString());
            }
            catch
            {
                return default;
            }
        }

        public async Task<TEntity> GetEntityAsync<TEntity>(string key, int DB = 0)
        {
            return await GetAsync<TEntity>(key, DB);
        }

        public bool KeyExists(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return GetDatabase(DB)?.KeyExists(key) ?? false;
        }

        public async Task<bool> KeyExistsAsync(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            var database = GetDatabase(DB);
            if (database == null)
            {
                return false;
            }

            return await database.KeyExistsAsync(key);
        }

        public void Remove(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            GetDatabase(DB)?.KeyDelete(key);
        }

        public async Task RemoveAsync(string key, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var database = GetDatabase(DB);
            if (database != null)
            {
                await database.KeyDeleteAsync(key);
            }
        }

        public void Clear(int? DB = null)
        {
            var server = GetServer();
            if (server == null)
            {
                return;
            }

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
            var server = GetServer();
            if (server == null)
            {
                return;
            }

            if (DB.HasValue)
            {
                await server.FlushDatabaseAsync(DB.Value);
            }
            else
            {
                await server.FlushAllDatabasesAsync();
            }
        }

        public async Task RemoveByKey(string pattern, int DB = 0)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return;
            }

            var database = GetDatabase(DB);
            var server = GetServer();

            if (database == null || server == null)
            {
                return;
            }

            var keys = new List<RedisKey>(RedisBatchSize);

            try
            {
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

                    await database.KeyDeleteAsync(keys.ToArray());
                    keys.Clear();
                }

                if (keys.Count > 0)
                {
                    await database.KeyDeleteAsync(keys.ToArray());
                }
            }
            catch
            {
                // Redis 异常时忽略，保持原有行为
            }
        }

        public async Task<List<string>> GetValuesByPatternAsync(string pattern, int DB = 0)
        {
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(pattern))
            {
                return result;
            }

            var database = GetDatabase(DB);
            var server = GetServer();

            if (database == null || server == null)
            {
                return result;
            }

            var keys = new List<RedisKey>(RedisBatchSize);

            try
            {
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

                    await AppendValuesAsync(database, keys, result);
                    keys.Clear();
                }

                if (keys.Count > 0)
                {
                    await AppendValuesAsync(database, keys, result);
                }
            }
            catch
            {
                // Redis 异常时返回已读取到的结果
            }

            return result;
        }

        private static async Task AppendValuesAsync(IDatabase database, List<RedisKey> keys, List<string> result)
        {
            var values = await database.StringGetAsync(keys.ToArray());

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

            if (database == null || server == null)
            {
                return result;
            }

            try
            {
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
            }
            catch
            {
                // Redis 异常时返回空集合
            }

            return result;
        }

        public async Task LikeRemoveAsync(string pattern)
        {
            await RemoveByKey(pattern);
        }
    }
}