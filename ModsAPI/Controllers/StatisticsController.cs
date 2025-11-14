using Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Service.Interface;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using ModsAPI.tools;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 数据统计相关 API
    /// 说明：
    /// 1. 角色限制：Developer
    /// 2. 日期区间：start 为包含，end 为不包含（右开区间），内部实现时会对 end +1 天
    /// 3. 默认日期范围：最近 7 天（start=今天-7，end=今天+1）
    /// 4. 返回统一使用 ResultEntity，成功时 ResultCode=200
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Developer")]
    public class StatisticsController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAPILogService _apiLogService;
        private readonly JwtHelper _jwtHelper;

        public StatisticsController(IHttpContextAccessor httpContextAccessor, IAPILogService apiLogService, JwtHelper jwtHelper)
        {
            _httpContextAccessor = httpContextAccessor;
            _apiLogService = apiLogService;
            _jwtHelper = jwtHelper;
        }

        #region 辅助方法

        /// <summary>
        /// 解析前端传入的 dynamic 为 JObject，失败返回空对象
        /// </summary>
        private dynamic ParseJson(dynamic json)
        {
            if (json == null) return new Newtonsoft.Json.Linq.JObject();
            try { return JsonConvert.DeserializeObject(Convert.ToString(json)); }
            catch { return new Newtonsoft.Json.Linq.JObject(); }
        }

        /// <summary>
        /// 解析日期字符串，返回 Date 部分，失败返回 null
        /// 支持格式：yyyy-MM-dd / yyyy-MM-dd HH:mm:ss / yyyy/MM/dd / yyyy/MM/dd HH:mm:ss 或常规可解析字符串
        /// </summary>
        private DateTime? ParseDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            string[] formats = { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy/MM/dd", "yyyy/MM/dd HH:mm:ss" };
            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.Date;
            if (DateTime.TryParse(input, out var dt2))
                return dt2.Date;
            return null;
        }

        /// <summary>
        /// 标准化日期范围：若 start >= end 自动回退到最近 7 天；最大跨度限制 90 天
        /// </summary>
        private void NormalizeDateRange(ref DateTime start, ref DateTime end, int maxDays = 90)
        {
            if (start >= end)
                start = end.AddDays(-7);
            var span = (end - start).TotalDays;
            if (span > maxDays)
                start = end.AddDays(-maxDays);
        }

        /// <summary>
        /// 获取当前请求用户 UserId（无则返回 null）
        /// </summary>
        private string? GetUserId()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrWhiteSpace(token)) return null;
            return _jwtHelper.GetTokenStr(token, "UserId");
        }

        /// <summary>
        /// 获取客户端 IP
        /// </summary>
        private string GetClientIp() => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "";

        #endregion

        /// <summary>
        /// 获取指定时间范围内每日活跃用户数（去重 UserId）
        /// 默认：最近 7 天（含今天），end 右开（内部 +1）
        /// 支持参数：{"start":"2024-10-01","end":"2024-10-08"}
        /// 说明：内部已排除登录/注册等非活跃行为接口
        /// </summary>
        /// <param name="json">dynamic - 可选 start/end 字段</param>
        /// <returns>ResultEntity(Dictionary(key=yyyy-MM-dd,value=活跃用户数))</returns>
        [HttpPost(Name = "GetDailyLoginCount")]
        [SwaggerOperation(Summary = "获取每日活跃用户数", Description = "去重统计用户活跃行为")]
        public async Task<ActionResult<ResultEntity<Dictionary<string, int>>>> GetDailyLoginCount([FromBody] dynamic json)
        {

            json = ParseJson(json);

            var start = DateTime.Today.AddDays(-7);
            var end = DateTime.Today.AddDays(1); // 右开区间

            var parsedStart = ParseDate((string?)json?.start);
            var parsedEnd = ParseDate((string?)json?.end);

            if (parsedStart.HasValue) start = parsedStart.Value;
            if (parsedEnd.HasValue) end = parsedEnd.Value.AddDays(1);

            NormalizeDateRange(ref start, ref end);

            try
            {
                var data = await _apiLogService.GetDailyActiveUserCountAsync(start, end);
                return new ResultEntity<Dictionary<string, int>>
                {
                    ResultCode = 200,
                    ResultMsg = "success",
                    ResultData = data
                };
            }
            catch (Exception ex)
            {
                return new ResultEntity<Dictionary<string, int>>
                {
                    ResultCode = 500,
                    ResultMsg = "查询失败: " + ex.Message,
                    ResultData = null
                };
            }
        }

        /// <summary>
        /// 获取最近 N 天每天累计流失用户数
        /// 流失定义：最近 30 天未登录（含从未登录且注册时间已超过 30 天）
        /// 请求示例：{"days":7}，days 范围 1~30，默认 7
        /// 返回：key=yyyy-MM-dd，value=截至该日的累计流失用户数
        /// </summary>
        /// <param name="json">dynamic - {"days":7}</param>
        /// <returns>ResultEntity(Dictionary(key=日期,value=累计流失数))</returns>
        [HttpPost(Name = "GetDailyLostUserCount")]
        [SwaggerOperation(Summary = "获取每日累计流失用户数", Description = "流失：30天未登录")]
        public async Task<ActionResult<ResultEntity<Dictionary<string, int>>>> GetDailyLostUserCount([FromBody] dynamic json)
        {

            json = ParseJson(json);

            int days = 7;
            int d = 0;
            if (json?.days != null && int.TryParse((string)json.days, out d))
            {
                days = Math.Clamp(d, 1, 30);
            }

            try
            {
                var data = await _apiLogService.GetLostUsersAsync(days);
                return new ResultEntity<Dictionary<string, int>>
                {
                    ResultCode = 200,
                    ResultMsg = "success",
                    ResultData = data
                };
            }
            catch (Exception ex)
            {
                return new ResultEntity<Dictionary<string, int>>
                {
                    ResultCode = 500,
                    ResultMsg = "查询失败: " + ex.Message,
                    ResultData = null
                };
            }
        }

        /// <summary>
        /// 统计指定时间范围内各接口请求次数（已在服务层合并 DownloadFile 的文件Id）
        /// 默认：最近 7 天，end 右开
        /// 请求示例：{"start":"2024-09-01","end":"2024-09-09"}
        /// 返回：key=接口标识 value=次数（已按次数降序）
        /// </summary>
        /// <param name="json">dynamic - 可选 start/end</param>
        /// <returns>ResultEntity(Dictionary(key=API,value=调用次数))</returns>
        [HttpPost(Name = "GetApiRequestCounts")]
        [SwaggerOperation(Summary = "统计接口调用次数", Description = "DownloadFile 已忽略文件Id统一统计")]
        public async Task<ActionResult<ResultEntity<Dictionary<string, int>>>> GetApiRequestCountsAsync([FromBody] dynamic json)
        {

            json = ParseJson(json);

            var start = DateTime.Today.AddDays(-7);
            var end = DateTime.Today.AddDays(1);

            var parsedStart = ParseDate((string?)json?.start);
            var parsedEnd = ParseDate((string?)json?.end);

            if (parsedStart.HasValue) start = parsedStart.Value;
            if (parsedEnd.HasValue) end = parsedEnd.Value.AddDays(1);

            NormalizeDateRange(ref start, ref end);

            try
            {
                var data = await _apiLogService.GetApiRequestCountsAsync(start, end);
                return new ResultEntity<Dictionary<string, int>>
                {
                    ResultCode = 200,
                    ResultMsg = "success",
                    ResultData = data
                };
            }
            catch (Exception ex)
            {
                return new ResultEntity<Dictionary<string, int>>
                {
                    ResultCode = 500,
                    ResultMsg = "查询失败: " + ex.Message,
                    ResultData = null
                };
            }
        }
    }
}