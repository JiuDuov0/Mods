using Entity;
using Entity.Mod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        /// 分页获取Mod列表
        /// </summary>
        /// <param name="json">Take=取出多少数据，Skip=跳过多少数据，Select=查询框，Types=类型  json示例{"Skip":"0","Take":"10","Select":"","Types":["",""]}</param>
        /// <returns></returns>
        [HttpPost(Name = "ModListPage")]
        public ResultEntity<List<ModEntity>> ModListPage([FromBody] dynamic json)
        {
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            return new ResultEntity<List<ModEntity>> { ResultData = _IModService.ModListPage(json) };
        }
    }
}
