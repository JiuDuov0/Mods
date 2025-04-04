﻿using Entity;
using Entity.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;
using Service.Realization;
using System.Web;

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
            #region 记录访问
            _IAPILogService.WriteLogAsync("LoginController/UserLogin", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

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
        [Authorize]
        public ResultEntity<ResponseToken> CreateToken([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("LoginController/CreateToken", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

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
                //if (!string.IsNullOrWhiteSpace(UserInfo.Token))
                //{
                //    return new ResultEntity<ResponseToken>() { ResultData = new ResponseToken() { Token = UserInfo.Token } };
                //}
                var res = _JwtHelper.CreateYearsToken(UserInfo);
                UserInfo.Token = res.Token;
                _IUserService.UpdateUserAsync(UserInfo);
                return new ResultEntity<ResponseToken> { ResultData = res };
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
            #region 记录访问
            _IAPILogService.WriteLogAsync("LoginController/UserRegister", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

            #region 验证一下传过来的是什么鬼东西
            if ((string)json.LoginAccount == "" || (string)json.LoginAccount == null)
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查账号或密码" };
            }
            else if (!((string)json.LoginAccount).Contains('@') && !((string)json.LoginAccount).Contains('.'))
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查账号或密码" };
            }
            if ((string)json.NickName == "" || (string)json.NickName == null)
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查昵称" };
            }
            if ((string)json.Password == "" || (string)json.Password == null)
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查账号或密码" };
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
            else
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "邮箱已注册" };
            }
            return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "信息错误" };
        }

        /// <summary>
        /// 测试用的
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "Test")]
        [Authorize]
        public string Test()
        {
            #region 记录访问
            //让我康康谁请求的
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("LoginController/Test", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion
            var roleid = _JwtHelper.GetTokenStr(token, "UserRoleIDs");
            var role = _JwtHelper.GetTokenStr(token, "UserId");


            //string Get(string url, string content)
            //{
            //    try
            //    {
            //        using (HttpClient client = new HttpClient())
            //        {
            //            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")); // 设置响应数据的ContentType
            //            return client.GetStringAsync(url + content).Result;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        return null;
            //    }
            //}
            //var collection = HttpUtility.ParseQueryString(new Uri("https://api.bilibili.com/x/web-interface/view?bvid=BV1NJ411g7Ui").Query);
            //var bvid = collection["bvid"];

            //Get("https://api.bilibili.com/x/web-interface/view?bvid=BV1NJ411g7Ui", "");



            return null;
        }
    }
}
