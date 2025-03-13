using Entity;
using Entity.Mod;
using Entity.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Interface;
using System.Text;

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
        private readonly IAPILogService _IAPILogService;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly JwtHelper _JwtHelper;

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iModService"></param>
        /// <param name="iAPILogService"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="jwtHelper"></param>
        public ModController(IModService iModService, IAPILogService iAPILogService, IHttpContextAccessor iHttpContextAccessor, JwtHelper jwtHelper)
        {
            _IModService = iModService;
            _IAPILogService = iAPILogService;
            _IHttpContextAccessor = iHttpContextAccessor;
            _JwtHelper = jwtHelper;
        }
        /// <summary>
        /// 分页获取Mod列表
        /// </summary>
        /// <param name="json">Take=取出多少数据，Skip=跳过多少数据，Search=查询框，Types=类型  json示例{"Skip":"0","Take":"10","Search":"","Types":["",""]}</param>
        /// <returns></returns>
        [HttpPost(Name = "ModListPage")]
        public ResultEntity<List<ModEntity>> ModListPage([FromBody] dynamic json)
        {
            string UserId = null;
            if (!string.IsNullOrWhiteSpace(Request.Headers["Authorization"].FirstOrDefault()))
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                UserId = _JwtHelper.GetTokenStr(token, "UserId");
                _IAPILogService.WriteLogAsync("ModController/ModListPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
            else
            {
                _IAPILogService.WriteLogAsync("ModController/ModListPage", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
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
            return new ResultEntity<List<ModEntity>> { ResultData = _IModService.ModListPage(json, UserId) };
        }

        /// <summary>
        /// 获取所有mod类型
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "GetAllModTypes")]
        public ResultEntity<List<TypesEntity>> GetAllModTypes()
        {
            return new ResultEntity<List<TypesEntity>>() { ResultData = new TypesEntity().GetRoleList() };
        }

        /// <summary>
        /// 创建mod,并创建版本
        /// </summary>
        /// <param name="json">{"Name":"","Description":"","VideoUrl":"","ModVersionEntities":[{"VersionNumber":"","Description":""}]}</param>
        /// <returns></returns>
        [HttpPost(Name = "CreateMod")]
        [Authorize]
        public ResultEntity<ModEntity> CreateMod([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/ModListPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));

            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.Name))
            {
                return new ResultEntity<ModEntity>() { ResultMsg = "无Mod名称" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Description))
            {
                return new ResultEntity<ModEntity>() { ResultMsg = "无Mod描述" };
            }
            if (string.IsNullOrWhiteSpace((string)json.ModVersionEntities[0].VersionNumber))
            {
                return new ResultEntity<ModEntity>() { ResultMsg = "无版本号" };
            }
            if (string.IsNullOrWhiteSpace((string)json.ModVersionEntities[0].Description))
            {
                return new ResultEntity<ModEntity>() { ResultMsg = "无版本描述" };
            }
            #endregion

            var ModId = Guid.NewGuid().ToString();
            var ModVersion = new ModVersionEntity()
            {
                VersionId = Guid.NewGuid().ToString(),
                ModId = ModId,
                VersionNumber = (string)json.ModVersionEntities[0].VersionNumber,
                Description = (string)json.ModVersionEntities[0].Description,
                CreatedAt = DateTime.Now
            };
            var Mod = new ModEntity()
            {
                ModId = ModId,
                Name = (string)json.Name,
                Description = (string)json.Description,
                CreatorUserId = UserId,
                CreatedAt = DateTime.Now,
                VideoUrl = (string)json.VideoUrl,
                DownLoadCount = 0
            };
            if (_IModService.AddModAndModVersion(Mod, ModVersion))
            {
                Mod.ModVersionEntities = new List<ModVersionEntity> { ModVersion };
                return new ResultEntity<ModEntity> { ResultData = Mod };
            }
            return new ResultEntity<ModEntity> { ResultMsg = "创建版本失败", ResultData = null };
        }

        /// <summary>
        /// Mod添加ModType
        /// </summary>
        /// <param name="json">[{"ModId":"","TypesId":""},{"ModId":"","TypesId":""}]</param>
        /// <returns></returns>
        [HttpPost(Name = "ModAddModType")]
        [Authorize]
        public ResultEntity<bool> ModAddModType([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/ModListPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));

            JArray jArray = json;

            #region 验证
            if (jArray == null || jArray.Count == 0)
            {
                return new ResultEntity<bool>() { ResultMsg = "不能为空" };
            }
            var ModsIdList = new List<string>();
            foreach (JObject item in jArray)
            {
                if (ModsIdList.FirstOrDefault(x => x == item["ModId"].ToString()) == null)
                {
                    ModsIdList.Add(item["ModId"].ToString());
                }
            }
            if (!_IModService.IsLoginUserMods(ModsIdList, UserId))
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "含有非本人Mod" };
            }
            #endregion
            return new ResultEntity<bool>() { ResultData = _IModService.AddModTypes(jArray) };
        }

        /// <summary>
        /// 添加mod版本
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "ModAddVersion")]
        [Authorize]
        public ResultEntity<ModVersionEntity> ModAddVersion([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/ModListPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModId))
            {
                return new ResultEntity<ModVersionEntity>() { ResultCode = 400, ResultMsg = "无ModId" };
            }
            if (string.IsNullOrWhiteSpace((string)json.VersionNumber))
            {
                return new ResultEntity<ModVersionEntity>() { ResultCode = 400, ResultMsg = "无VersionNumber" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Description))
            {
                return new ResultEntity<ModVersionEntity>() { ResultCode = 400, ResultMsg = "无Description" };
            }
            if (!_IModService.IsLoginUserMods(new List<string>() { (string)json.ModId }, UserId))
            {
                return new ResultEntity<ModVersionEntity>() { ResultCode = 400, ResultMsg = "含有非本人Mod" };
            }
            #endregion
            var entity = new ModVersionEntity()
            {
                VersionId = Guid.NewGuid().ToString(),
                ModId = (string)json.ModId,
                VersionNumber = (string)json.VersionNumber,
                Description = (string)json.Description,
                CreatedAt = DateTime.Now
            };
            if (_IModService.AddModVersion(entity))
            {
                return new ResultEntity<ModVersionEntity>() { ResultData = entity };
            }
            else
            {
                return new ResultEntity<ModVersionEntity>() { ResultCode = 400, ResultMsg = "添加失败" };
            }
        }

        /// <summary>
        /// 获取我创建的Mod
        /// </summary>
        /// <param name="json">Take=取出多少数据，Skip=跳过多少数据，Search=查询框，Types=类型  json示例{"Skip":"0","Take":"10","Search":"","Types":["",""]}</param>
        /// <returns></returns>
        [HttpPost(Name = "GetMyCreateMod")]
        [Authorize]
        public ResultEntity<List<ModEntity>> GetMyCreateMod([FromBody] dynamic json)
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
            return new ResultEntity<List<ModEntity>> { ResultData = _IModService.GetMyCreateMod(UserId, json) };
        }
    }
}
