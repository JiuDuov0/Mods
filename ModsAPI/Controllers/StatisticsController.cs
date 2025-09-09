using Entity;
using Entity.Log;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Service.Interface;
using Service.Realization;
using Swashbuckle.AspNetCore.Annotations;
using System.Globalization;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 数据统计相关API
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Developer")]
    public class StatisticsController : ControllerBase
    {
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly IAPILogService _IAPILogService;
        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="iAPILogService"></param>
        public StatisticsController(IHttpContextAccessor iHttpContextAccessor, IAPILogService iAPILogService)
        {
            _IHttpContextAccessor = iHttpContextAccessor;
            _IAPILogService = iAPILogService;
        }

        /// <summary>
        /// 获取最近一段时间每天的登录数量（只统计成功数量）
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetDailyLoginCount")]
        [SwaggerOperation(Summary = "获取最近一段时间每天的登录数量")]
        public async Task<ActionResult<ResultEntity<Dictionary<string, int>>>> GetDailyLoginCount([FromBody] dynamic json)
        {
            DateTime startTime = DateTime.Today.AddDays(-7);
            DateTime endTime = DateTime.Today.AddDays(1);

            if (json != null)
            {
                json = JsonConvert.DeserializeObject(Convert.ToString(json));
                var obj = JsonConvert.DeserializeObject(Convert.ToString(json));
                if (!string.IsNullOrWhiteSpace((string)obj.start))
                {
                    DateTime.TryParse(obj.start.ToString(), out startTime);
                }
                if (!string.IsNullOrWhiteSpace((string)obj.end))
                {
                    DateTime.TryParse(obj.end.ToString(), out endTime);
                }
            }

            var dailyCounts = await _IAPILogService.GetDailyActiveUserCountAsync(startTime, endTime);

            return new ResultEntity<Dictionary<string, int>>
            {
                ResultCode = 200,
                ResultMsg = "success",
                ResultData = dailyCounts
            };
        }

        /// <summary>
        /// 获取最近N天每天流失用户数量（未登录用户）
        /// </summary>
        /// <param name="json">{ "days": 7 }</param>
        /// <returns>每天流失用户数量字典</returns>
        [HttpPost(Name = "GetDailyLostUserCount")]
        [SwaggerOperation(Summary = "获取最近N天每天流失用户数量")]
        public async Task<ActionResult<ResultEntity<Dictionary<string, int>>>> GetDailyLostUserCount([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));

            int days = 7;
            if (json != null)
            {
                var obj = JsonConvert.DeserializeObject(Convert.ToString(json));
                int d = 0;
                if (obj.days != null && int.TryParse(obj.days.ToString(), out d))
                {
                    days = d;
                }
            }

            var lostUserCounts = await _IAPILogService.GetLostUsersAsync(days);

            return new ResultEntity<Dictionary<string, int>>
            {
                ResultCode = 200,
                ResultMsg = "success",
                ResultData = lostUserCounts
            };
        }

        /// <summary>
        /// 统计每个接口的请求次数
        /// </summary>
        /// <param name="json">{ "start": "2024-09-01", "end": "2024-09-09" }</param>
        /// <returns></returns>
        [HttpPost(Name = "GetApiRequestCounts")]
        public async Task<ActionResult<ResultEntity<Dictionary<string, int>>>> GetApiRequestCountsAsync([FromBody] dynamic json)
        {
            DateTime startTime = DateTime.Today.AddDays(-7);
            DateTime endTime = DateTime.Today.AddDays(1);

            if (json != null)
            {
                json = JsonConvert.DeserializeObject(Convert.ToString(json));
                if (!string.IsNullOrWhiteSpace((string)json.start))
                {
                    DateTime.TryParse((string)json.start, out startTime);
                }
                if (!string.IsNullOrWhiteSpace((string)json.end))
                {
                    DateTime.TryParse((string)json.end, out endTime);
                }
            }

            var apiCounts = await _IAPILogService.GetApiRequestCountsAsync(startTime, endTime);

            return new ResultEntity<Dictionary<string, int>>
            {
                ResultCode = 200,
                ResultMsg = "success",
                ResultData = apiCounts
            };
        }
    }
}
