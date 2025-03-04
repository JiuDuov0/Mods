using Entity;
using Entity.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iUserService"></param>
        public UserController(IUserService iUserService)
        {
            _IUserService = iUserService;
        }
        [HttpPost(Name = "GetPage")]
        [Authorize(Roles = "Developer")]
        public ResultEntity<List<UserEntity>> GetPage([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            return new ResultEntity<List<UserEntity>>() { ResultData = _IUserService.GetPages(json) };
        }
    }
}
