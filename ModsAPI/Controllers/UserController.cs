using Entity;
using Entity.Mod;
using Entity.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;
using System.Threading.Tasks;

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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/GetUserPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/ModSubscribe", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/UserAllSubscribeModPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("UserController/UserUnsubscribeMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
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

        /// <summary>
        /// 获取有角色的人
        /// </summary>
        /// <param name="json">{"Skip":"0","Take":"10","Mail":"","RoleId":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "GetAllUserRole")]
        [Authorize(Roles = "Developer")]
        public ResultEntity<List<UserRoleEntity>> GetAllUserRole([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            return new ResultEntity<List<UserRoleEntity>>() { ResultData = _IUserService.GetUserRolePages(json) };
        }

        /// <summary>
        /// 根据Id获取用户角色
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetUserRoleById")]
        [Authorize(Roles = "Developer")]
        public async Task<ResultEntity<UserRoleEntity?>> GetUserRoleById([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            return new ResultEntity<UserRoleEntity?>() { ResultData = await _IUserService.GetUserRoleByIdAsync((string)json.Id) };
        }

        /// <summary>
        /// 添加用户角色
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "AddUserRole")]
        [Authorize(Roles = "Developer")]
        public ResultEntity<bool> AddUserRole([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            var entity = new UserRoleEntity()
            {
                UserId = (string)json.UserId,
                RoleId = (string)json.RoleId
            };
            return new ResultEntity<bool>() { ResultData = _IUserService.AddUserRole(entity) };
        }

        /// <summary>
        /// 更新用户角色
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "UpdateUserRole")]
        [Authorize(Roles = "Developer")]
        public ResultEntity<UserRoleEntity> UpdateUserRole([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            var entity = new UserRoleEntity()
            {
                Id = (string)json.Id,
                UserId = (string)json.Mail,
                RoleId = (string)json.RoleId
            };
            return new ResultEntity<UserRoleEntity>() { ResultData = _IUserService.UpdateUserRole(entity) };
        }

        /// <summary>
        /// 删除用户角色
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "DeleteUserRole")]
        [Authorize(Roles = "Developer")]
        public ResultEntity<bool> DeleteUserRole([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            return new ResultEntity<bool>() { ResultData = _IUserService.DeleteUserRole((string)json.Id) };
        }

        /// <summary>
        /// 根据UserId获取个人资料(公开资料)
        /// </summary>
        /// <param name="json">{"UserId":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "GetUserByUserIdPublic")]
        public async Task<ResultEntity<UserEntity?>> GetUserByUserIdPublicAsync([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            await _IAPILogService.WriteLogAsync($"{GetType().Name}/GetUserByUserIdPublicAsync", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.UserId))
            {
                return new ResultEntity<UserEntity?>() { ResultCode = 400, ResultMsg = "无UserId" };
            }
            #endregion
            var entity = await _IUserService.GetUserByUserIdAsync((string)json.UserId);
            if (entity != null)
            {
                entity.Password = null;
                entity.Token = null;
                entity.Mail = null;
            }
            return new ResultEntity<UserEntity?>() { ResultData = entity };
        }

        /// <summary>
        /// 根据UserId获取个人资料
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "GetUserByUserId")]
        public async Task<ResultEntity<UserEntity?>> GetUserByUserIdAsync()
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            await _IAPILogService.WriteLogAsync($"{GetType().Name}/GetUserByUserIdAsync", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion
            var entity = await _IUserService.GetUserByUserIdAsync(UserId);
            if (entity != null)
            {
                entity.Password = null;
            }
            return new ResultEntity<UserEntity?>() { ResultData = entity };
        }


        [HttpPost(Name = "UpdateUserInfo")]
        public async Task<ResultEntity<bool>> UpdateUserInfoAsync([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            await _IAPILogService.WriteLogAsync($"{GetType().Name}/UpdateUserInfo", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            var entity = new UserEntity()
            {
                UserId = (string)json.UserId,
                NickName = (string)json.NickName,
                HeadPic = (string)json.HeadPic,
                FeedBackMail = (string)json.FeedBackMail
            };
            return new ResultEntity<bool>() { ResultData = await _IUserService.UpdateUserAsync(entity) };
        }
    }
}
