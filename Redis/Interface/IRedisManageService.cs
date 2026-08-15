using System.Collections.Generic;
using System.Threading.Tasks;

namespace Redis.Interface
{
    public interface IRedisManageService
    {
        /// <summary>
        /// 设置一个键值对
        /// </summary>
        void Set(string key, object value, object? ts = null, int DB = 0);

        /// <summary>
        /// 获取 Redis 缓存值
        /// </summary>
        string GetValue(string key, int DB = 0);

        /// <summary>
        /// 获取序列化值
        /// </summary>
        TEntity? Get<TEntity>(string key, int DB = 0);

        /// <summary>
        /// 异步获取序列化值
        /// </summary>
        Task<TEntity?> GetAsync<TEntity>(string key, int DB = 0);

        /// <summary>
        /// 判断 Key 是否存在
        /// </summary>
        bool KeyExists(string key, int DB = 0);

        /// <summary>
        /// 移除某个 Key 值
        /// </summary>
        void Remove(string key, int DB = 0);

        /// <summary>
        /// 清空 Redis
        /// </summary>
        void Clear(int? DB = null);

        /// <summary>
        /// 异步获取 Redis 缓存值
        /// </summary>
        Task<string> GetValueAsync(string key, int DB = 0);

        /// <summary>
        /// 异步获取序列化值
        /// </summary>
        Task<TEntity?> GetEntityAsync<TEntity>(string key, int DB = 0);

        Task SetAsync(
            string key,
            object value,
            object? cacheTime = null,
            int DB = 0);

        Task<bool> KeyExistsAsync(string key, int DB = 0);

        /// <summary>
        /// 异步移除指定的 key
        /// </summary>
        Task RemoveAsync(string key, int DB = 0);

        /// <summary>
        /// 异步移除模糊查询到的 key
        /// </summary>
        Task RemoveByKey(string key, int DB = 0);

        /// <summary>
        /// 异步全部清空
        /// </summary>
        Task ClearAsync(int? DB = null);

        List<object> SelectTags(string pattern);

        Task LikeRemoveAsync(string pattern);

        Task<List<string>> GetValuesByPatternAsync(
            string pattern,
            int DB = 0);
    }
}