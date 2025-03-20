using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Redis.Interface;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis.Realization
{
    internal class RedisManageService : IRedisManageService
    {
        private volatile ConnectionMultiplexer _redisConnection;
        private readonly object _redisConnectionLock = new object();
        private readonly ConfigurationOptions _configOptions;
        public IConfiguration _configuration { get; set; }
        public RedisManageService(IConfiguration configuration)
        {
            _configuration = configuration;
            _configOptions = ReadRedisSetting();
            _redisConnection = ConnectionRedis();
        }
        private ConfigurationOptions ReadRedisSetting()
        {
            try
            {
                ConfigurationOptions options = new ConfigurationOptions
                {
                    EndPoints =
                        {
                                {
                                    _configuration["Ip"].ToString(),
                                    Convert.ToInt32(_configuration["Port"])
                                }
                            },
                    ClientName = "JD_Soft_FrameWork_Redis",
                    Password = _configuration["Password"].ToString(),
                    ConnectTimeout = Convert.ToInt32(_configuration["Timeout"]),
                    DefaultDatabase = Convert.ToInt32(_configuration["DB"]),
                    AllowAdmin = true
                };
                return options;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        private ConnectionMultiplexer ConnectionRedis()
        {
            if (this._redisConnection != null && this._redisConnection.IsConnected)
            {
                return this._redisConnection; // 已有连接，直接使用
            }
            lock (_redisConnectionLock)
            {
                if (this._redisConnection != null)
                {
                    this._redisConnection.Dispose(); // 释放，重连
                }
                try
                {
                    return ConnectionMultiplexer.Connect(_configOptions);
                }
                catch (Exception ex)
                {
                }
            }
            return this._redisConnection;
        }
        public string GetValue(string key, int DB = 0)
        {
            return _redisConnection.GetDatabase(DB).StringGet(key);
        }

        public void Set(string key, object value, object ts = null, int DB = 0)
        {
            if (value != null)
            {
                if (ts == null)
                {
                    _redisConnection.GetDatabase(DB).StringSet(key, JsonConvert.SerializeObject(value, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, DateFormatString = "yyyy-MM-dd HH:mm:ss", ContractResolver = new DefaultContractResolver() }), new TimeSpan(24, 0, 0));
                }
                else
                {
                    _redisConnection.GetDatabase(DB).StringSet(key, JsonConvert.SerializeObject(value, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, DateFormatString = "yyyy-MM-dd HH:mm:ss", ContractResolver = new DefaultContractResolver() }), (TimeSpan)ts);
                }
            }
        }

        public void Clear(int? DB = null)
        {
            if (DB == null)
            {
                _redisConnection.GetServer(_configuration["Ip"].ToString(), Convert.ToInt32(_configuration["Port"])).FlushAllDatabases();
            }
            else
            {
                _redisConnection.GetServer(_configuration["Ip"].ToString(), Convert.ToInt32(_configuration["Port"])).FlushDatabase(Convert.ToInt32(DB));
            }
            //foreach (var endPoint in this.ConnectionRedis().GetEndPoints()) {
            //    var server = this.ConnectionRedis().GetServer(endPoint);
            //    foreach (var key in server.Keys()) {
            //        _redisConnection.GetDatabase().KeyDelete(key);
            //    }
            //}
        }

        public bool KeyExists(string key, int DB = 0)
        {
            return _redisConnection.GetDatabase(DB).KeyExists(key);
        }


        public TEntity Get<TEntity>(string key, int DB = 0)
        {
            var value = _redisConnection.GetDatabase(DB).StringGet(key);
            if (value.HasValue)
            {
                string sadf = value.ToString();
                //需要用的反序列化，将Redis存储的Byte[]，进行反序列化
                return JsonConvert.DeserializeObject<TEntity>(value);
            }
            else
            {
                return default(TEntity);
            }
        }

        public async Task<TEntity> GetAsync<TEntity>(string key, int DB = 0)
        {
            var value = await _redisConnection.GetDatabase(DB).StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonConvert.DeserializeObject<TEntity>(JObject.Parse(value).ToString());
            }
            else
            {
                return default(TEntity);
            }
        }

        public void Remove(string key, int DB = 0)
        {
            _redisConnection.GetDatabase(DB).KeyDelete(key);
        }


        public async Task ClearAsync(int? DB = null)
        {
            if (DB == null)
            {
                await _redisConnection.GetServer(_configuration["Ip"].ToString(), Convert.ToInt32(_configuration["Port"])).FlushAllDatabasesAsync();
            }
            else
            {
                await _redisConnection.GetServer(_configuration["Ip"].ToString(), Convert.ToInt32(_configuration["Port"])).FlushDatabaseAsync(Convert.ToInt32(DB));
            }
            //foreach (var endPoint in this.ConnectionRedis().GetEndPoints()) {
            //    var server = this.ConnectionRedis().GetServer(endPoint);
            //    foreach (var key in server.Keys()) {
            //        await _redisConnection.GetDatabase().KeyDeleteAsync(key);
            //    }
            //}
        }

        public async Task<bool> KeyExistsAsync(string key, int DB = 0)
        {
            return await _redisConnection.GetDatabase(DB).KeyExistsAsync(key);
        }

        public async Task<string> GetValueAsync(string key, int DB = 0)
        {
            return await _redisConnection.GetDatabase(DB).StringGetAsync(key);
        }

        public async Task<TEntity> GetEntityAsync<TEntity>(string key, int DB = 0)
        {
            var value = await _redisConnection.GetDatabase(DB).StringGetAsync(key);
            if (value.HasValue)
            {
                return JsonConvert.DeserializeObject<TEntity>(value);
            }
            else
            {
                return default;
            }
        }

        public async Task RemoveAsync(string key, int DB = 0)
        {
            await _redisConnection.GetDatabase(DB).KeyDeleteAsync(key);
        }

        public async Task RemoveByKey(string key, int DB = 0)
        {
            var redisResult = await _redisConnection.GetDatabase(DB).ScriptEvaluateAsync(LuaScript.Prepare(
                //模糊查询：
                " local res = redis.call('KEYS', @keypattern) " +
                " return res "), new { @keypattern = key });

            if (!redisResult.IsNull)
            {
                RedisKey[] preSult = (RedisKey[])redisResult;
                _redisConnection.GetDatabase(DB).KeyDelete(preSult);
            }
        }

        public async Task SetAsync(string key, object value, object cacheTime = null, int DB = 0)
        {
            if (value != null)
            {
                if (cacheTime == null)
                {
                    await _redisConnection.GetDatabase(DB).StringSetAsync(key, JsonConvert.SerializeObject(value, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, DateFormatString = "yyyy-MM-dd HH:mm:ss", ContractResolver = new DefaultContractResolver() }), new TimeSpan(24, 0, 0));
                }
                else
                {
                    await _redisConnection.GetDatabase(DB).StringSetAsync(key, JsonConvert.SerializeObject(value, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, DateFormatString = "yyyy-MM-dd HH:mm:ss", ContractResolver = new DefaultContractResolver() }), (TimeSpan)cacheTime);
                }
            }
        }

        /// <summary>
        /// 模糊查找
        /// </summary>
        /// <param name="key"></param>
        public List<object> SelectTags(string pattern)
        {
            List<object> result = new List<object>();
            try
            {


                var redisResult = _redisConnection.GetDatabase().ScriptEvaluate(LuaScript.Prepare(
                                //Redis的keys模糊查询：
                                " local res = redis.call('KEYS', @keypattern) " +
                                " return res "), new { @keypattern = pattern });

                if (!redisResult.IsNull)
                {
                    RedisKey[] preSult = (RedisKey[])redisResult;
                    var Tags = _redisConnection.GetDatabase().StringGet(preSult);

                    if (Tags != null)
                    {
                        //result = Tags.Select(o => XJDataDll.Json.DataJsonSerializer.JsonToObject<object>(o.ToString())).ToList();
                    }
                }



            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (result == null)
                {
                    result = new List<object>();
                }
            }

            return result;
        }
        /// <summary>
        /// 异步模糊删除
        /// </summary>
        /// <param name="key"></param>
        public async void LikeRemoveAsync(string pattern)
        {
            try
            {


                var redisResult = _redisConnection.GetDatabase().ScriptEvaluate(LuaScript.Prepare(
                                //Redis的keys模糊查询：
                                " local res = redis.call('KEYS', @keypattern) " +
                                " return res "), new { @keypattern = pattern });

                if (!redisResult.IsNull)
                {
                    RedisKey[] preSult = (RedisKey[])redisResult;
                    await _redisConnection.GetDatabase().KeyDeleteAsync(preSult);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
