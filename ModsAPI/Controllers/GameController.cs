using Entity;
using Entity.Game;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 游戏api
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly IGameService _IGameService;
        private readonly IAPILogService _IAPILogService;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly JwtHelper _JwtHelper;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="iGameService"></param>
        /// <param name="iAPILogService"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="jwtHelper"></param>
        public GameController(IGameService iGameService, IAPILogService iAPILogService, IHttpContextAccessor iHttpContextAccessor, JwtHelper jwtHelper)
        {
            _IGameService = iGameService;
            _IAPILogService = iAPILogService;
            _IHttpContextAccessor = iHttpContextAccessor;
            _JwtHelper = jwtHelper;
        }

        /// <summary>
        /// 获取游戏列表
        /// </summary>
        /// <param name="json">{"Skip":"0","Take":"10"}</param>
        /// <returns></returns>
        [HttpPost(Name = "GetGamePageList")]
        [EnableRateLimiting("Concurrency")]
        public async Task<ResultEntity<List<GameEntity>?>> GetGamePageListAsync([FromBody] dynamic json)
        {
            #region 记录访问 不确定是否含有Token
            string? UserId = null;
            if (!string.IsNullOrWhiteSpace(Request.Headers["Authorization"].FirstOrDefault()))
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                UserId = _JwtHelper.GetTokenStr(token, "UserId");
            }
            await _IAPILogService.WriteLogAsync($"{GetType().Name}/GetGamePageListAsync", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            var booltake = int.TryParse((string)json.Take, out int Take);
            if (!int.TryParse((string)json.Skip, out int Skip) && !booltake)
            {
                return new ResultEntity<List<GameEntity>?>() { ResultCode = 400, ResultMsg = "缺少参数" };
            }
            return new ResultEntity<List<GameEntity>?>() { ResultData = await _IGameService.GamePageListAsync(Skip, Take) };
        }
    }
}
