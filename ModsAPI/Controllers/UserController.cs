using Entity;
using Entity.Mod;
using Entity.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 用户相关api
    /// </summary>
    [Route("api/[controller]/[action]")]
    [Authorize]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _IUserService;
        private readonly IAPILogService _IAPILogService;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly JwtHelper _JwtHelper;

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iUserService"></param>
        /// <param name="iAPILogService"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="jwtHelper"></param>
        public UserController(IUserService iUserService, IAPILogService iAPILogService, IHttpContextAccessor iHttpContextAccessor, JwtHelper jwtHelper)
        {
            _IUserService = iUserService;
            _IAPILogService = iAPILogService;
            _IHttpContextAccessor = iHttpContextAccessor;
            _JwtHelper = jwtHelper;
        }

        /// <summary>
        /// 获取用户列表
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetUserPage")]
        [Authorize(Roles = "Developer")]
        public ResultEntity<List<UserEntity>> GetUserPage([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/GetUserPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            return new ResultEntity<List<UserEntity>>() { ResultData = _IUserService.GetPages(json) };
        }

        /// <summary>
        /// Mod订阅
        /// </summary>
        /// <param name="json">ModId:ModId json示例：{"ModId":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "ModSubscribe")]
        [Authorize]
        public ResultEntity<bool> ModSubscribe([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/ModSubscribe", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            string ModId = (string)json.ModId;
            if (string.IsNullOrWhiteSpace(ModId))
            {
                return new ResultEntity<bool>() { ResultMsg = "ModId无效" };
            }
            return new ResultEntity<bool> { ResultData = _IUserService.SubscribeToMod(UserId, ModId) };
        }

        /// <summary>
        /// 当前用户所有已订阅Mod列表
        /// </summary>
        /// <param name="json">Take=取出多少数据，Skip=跳过多少数据，Select=查询框，Types=类型  json示例{"Skip":"0","Take":"10","Select":"","Types":["",""]}</param>
        /// <returns></returns>
        [HttpPost(Name = "UserAllSubscribeModPage")]
        [Authorize]
        public ResultEntity<List<ModEntity>> UserAllSubscribeModPage([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/UserAllSubscribeModPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.Skip))
            {
                return new ResultEntity<List<ModEntity>>() { ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModEntity>>() { ResultMsg = "无Take" };
            }
            #endregion
            return new ResultEntity<List<ModEntity>> { ResultData = _IUserService.UserAllSubscribeModPage(json, UserId) };
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="json">{"ModId":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "UserUnsubscribeMod")]
        [Authorize]
        public ResultEntity<bool> UserUnsubscribeMod([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/UserAllSubscribeModPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #region 验证
            string ModId = (string)json.ModId;
            if (string.IsNullOrWhiteSpace(ModId))
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultData = false, ResultMsg = "无ModId" };
            }
            #endregion
            if (_IUserService.UserUnsubscribeMod(UserId, ModId))
            {
                return new ResultEntity<bool>() { ResultData = true };
            }
            else
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultData = false, ResultMsg = "取消订阅失败" };
            }
        }
    }
}
