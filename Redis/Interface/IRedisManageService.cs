using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis.Interface
{
    public interface IRedisManageService
    {
        /// <summary>
        /// 设置一个 键值对
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="ts">TimeSpan时间</param>
        void Set(string key, object value, object ts = null, int DB = 0);
        /// <summary>
        ///  //获取 Reids 缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetValue(string key, int DB = 0);
        /// <summary>
        /// 获取序列化值
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        TEntity Get<TEntity>(string key, int DB = 0);

        /// <summary>
        /// 异步获取序列化值
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="key"></param>
        /// <param name="DB"></param>
        /// <returns></returns>
        Task<TEntity> GetAsync<TEntity>(string key, int DB = 0);
        /// <summary>
        /// 判断Key是否存在
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool KeyExists(string key, int DB = 0);
        /// <summary>
        /// 移除某个Key和值
        /// </summary>
        /// <param name="key"></param>
        void Remove(string key, int DB = 0);
        /// <summary>
        /// 清空Redis
        /// </summary>
        void Clear(int? DB = null);
        /// <summary>
        /// 异步获取 Reids 缓存值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<string> GetValueAsync(string key, int DB = 0);
        /// <summary>
        /// 异步获取序列化值
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<TEntity> GetEntityAsync<TEntity>(string key, int DB = 0);
        Task SetAsync(string key, object value, object cacheTime = null, int DB = 0);
        Task<bool> KeyExistsAsync(string key, int DB = 0);
        /// <summary>
        /// 异步移除指定的key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(string key, int DB = 0);
        /// <summary>
        /// 异步移除模糊查询到的key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveByKey(string key, int DB = 0);
        /// <summary>
        /// 异步全部清空
        /// </summary>
        /// <returns></returns>
        Task ClearAsync(int? DB = null);
        public List<object> SelectTags(string pattern);
        public void LikeRemoveAsync(string pattern);
    }
}
