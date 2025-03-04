using Entity;
using Entity.Mod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Service.Interface;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// Mod相关API
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ModController : ControllerBase
    {
        private readonly IModService _IModService;

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iModService"></param>
        public ModController(IModService iModService)
        {
            _IModService = iModService;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="json">Take=取出多少数据，Skip=跳过多少数据，Select=查询框，Tags=标签，Types=类型</param>
        /// <returns></returns>
        [HttpPost(Name = "ModListPage")]
        public ResultEntity<List<ModEntity>> ModListPage([FromBody] dynamic json)
        {
            return null;
        }
    }
}
