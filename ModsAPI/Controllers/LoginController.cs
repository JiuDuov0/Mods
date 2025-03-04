using Entity;
using Entity.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;

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
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="iUserService"></param>
        /// <param name="jwtHelper"></param>
        /// <param name="iHttpContextAccessor"></param>
        public LoginController(IUserService iUserService, JwtHelper jwtHelper, IHttpContextAccessor iHttpContextAccessor)
        {
            _IUserService = iUserService;
            _JwtHelper = jwtHelper;
            _IHttpContextAccessor = iHttpContextAccessor;
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
        /// 
        /// </summary>
        /// <param name="json">LoginAccount=账号（Email），Password=密码 json示例：{"LoginAccount":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "UserRegister")]
        [EnableRateLimiting("Concurrency")]
        public ResultEntity<ResponseToken> UserRegister([FromBody] dynamic json)
        {
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

            var User = new UserEntity()
            {
                UserId = Guid.NewGuid().ToString(),
                Mail = (string)json.LoginAccount,
                Password = (string)json.Password
            };
            if (_IUserService.Register(User) != null)
            {
                return new ResultEntity<ResponseToken> { ResultData = _JwtHelper.CreateYearsToken(User) };
            }
            return new ResultEntity<ResponseToken> { ResultMsg = "账号或密码错误" };
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
            return "";
        }
    }
}
