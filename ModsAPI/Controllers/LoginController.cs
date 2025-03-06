using Entity;
using Entity.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;
using Service.Realization;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 登录api
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IUserService _IUserService;
        private readonly JwtHelper _JwtHelper;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly IAPILogService _IAPILogService;

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iUserService"></param>
        /// <param name="jwtHelper"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="iAPILogService"></param>
        public LoginController(IUserService iUserService, JwtHelper jwtHelper, IHttpContextAccessor iHttpContextAccessor, IAPILogService iAPILogService)
        {
            _IUserService = iUserService;
            _JwtHelper = jwtHelper;
            _IHttpContextAccessor = iHttpContextAccessor;
            _IAPILogService = iAPILogService;
        }

        /// <summary>
        /// 登录api
        /// </summary>
        /// <param name="json">LoginAccount=账号（Email），Password=密码 json示例：{"LoginAccount":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "UserLogin")]
        [EnableRateLimiting("Concurrency")]
        public ResultEntity<ResponseToken> UserLogin([FromBody] dynamic json)
        {
            _IAPILogService.WriteLogAsync("UserLogin", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));

            #region 验证一下传过来的是什么鬼东西
            if ((string)json.LoginAccount == "" || (string)json.LoginAccount == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            if ((string)json.Password == "" || (string)json.Password == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            #endregion

            var UserInfo = _IUserService.Login((string)json.LoginAccount, (string)json.Password);
            if (UserInfo != null && UserInfo.UserId != null)
            {
                return new ResultEntity<ResponseToken> { ResultData = _JwtHelper.CreateToken(UserInfo) };
            }
            return new ResultEntity<ResponseToken> { ResultMsg = "账号或密码错误" };
        }

        /// <summary>
        /// 创建80年后过期的token
        /// </summary>
        /// <param name="json">LoginAccount=账号（Email），Password=密码 json示例：{"LoginAccount":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "CreateToken")]
        [EnableRateLimiting("Concurrency")]
        public ResultEntity<ResponseToken> CreateToken([FromBody] dynamic json)
        {
            _IAPILogService.WriteLogAsync("CreateToken", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));

            #region 验证一下传过来的是什么鬼东西
            if ((string)json.LoginAccount == "" || (string)json.LoginAccount == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            if ((string)json.Password == "" || (string)json.Password == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            #endregion

            var UserInfo = _IUserService.Login((string)json.LoginAccount, (string)json.Password);
            if (UserInfo != null && UserInfo.UserId != null)
            {
                return new ResultEntity<ResponseToken> { ResultData = _JwtHelper.CreateYearsToken(UserInfo) };
            }
            return new ResultEntity<ResponseToken> { ResultMsg = "账号或密码错误" };
        }

        /// <summary>
        /// 创建用户并登录
        /// </summary>
        /// <param name="json">LoginAccount=账号（Email），Password=密码，NickName=昵称 json示例：{"LoginAccount":"","NickName":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "UserRegister")]
        [EnableRateLimiting("Concurrency")]
        public ResultEntity<ResponseToken> UserRegister([FromBody] dynamic json)
        {
            _IAPILogService.WriteLogAsync("UserRegister", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));

            #region 验证一下传过来的是什么鬼东西
            if ((string)json.LoginAccount == "" || (string)json.LoginAccount == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            else if (!((string)json.LoginAccount).Contains('@') && !((string)json.LoginAccount).Contains('.'))
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            if ((string)json.NickName == "" || (string)json.NickName == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查昵称" };
            }
            if ((string)json.Password == "" || (string)json.Password == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            #endregion

            var User = new UserEntity()
            {
                UserId = Guid.NewGuid().ToString(),
                Mail = (string)json.LoginAccount,
                NickName = (string)json.NickName,
                Password = (string)json.Password,
                CreatedAt = DateTime.Now
            };
            User = _IUserService.Register(User);
            if (User != null)
            {
                return new ResultEntity<ResponseToken> { ResultData = _JwtHelper.CreateToken(User) };
            }
            return new ResultEntity<ResponseToken> { ResultMsg = "信息错误" };
        }

        /// <summary>
        /// 测试用的
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "Test")]
        public string Test()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var roleid = _JwtHelper.GetTokenStr(token, "UserRoleIDs");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            var role = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("Test", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            return "";
        }
    }
}
