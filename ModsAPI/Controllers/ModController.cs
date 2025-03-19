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
using System.Threading.Tasks;

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
            #region 记录访问 不确定是否含有Token
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
            return new ResultEntity<List<ModEntity>> { ResultData = _IModService.ModListPage(json, UserId) };
        }

        /// <summary>
        /// 获取所有mod类型
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "GetAllModTypes")]
        public ResultEntity<List<TypesEntity>> GetAllModTypes()
        {
            #region 记录访问 不确定是否含有Token
            string UserId = null;
            if (!string.IsNullOrWhiteSpace(Request.Headers["Authorization"].FirstOrDefault()))
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                UserId = _JwtHelper.GetTokenStr(token, "UserId");
                _IAPILogService.WriteLogAsync("ModController/GetAllModTypes", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
            else
            {
                _IAPILogService.WriteLogAsync("ModController/GetAllModTypes", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
            #endregion
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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/CreateMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

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
            var ListTypes = new List<ModTypeEntity>();
            if (((JArray)json.ModTypeEntities).HasValues)
            {
                ListTypes = ((JArray)json.ModTypeEntities).ToObject<List<ModTypeEntity>>();
            }
            var Mod = new ModEntity()
            {
                ModId = ModId,
                Name = (string)json.Name,
                Description = (string)json.Description,
                CreatorUserId = UserId,
                CreatedAt = DateTime.Now,
                VideoUrl = (string)json.VideoUrl,
                DownloadCount = 0
            };
            if (_IModService.AddModAndModVersion(Mod, ModVersion, ListTypes))
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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/ModAddModType", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/ModAddVersion", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/GetMyCreateMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.Skip))
            {
                return new ResultEntity<List<ModEntity>>() { ResultCode = 400, ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModEntity>>() { ResultCode = 400, ResultMsg = "无Take" };
            }
            #endregion
            return new ResultEntity<List<ModEntity>> { ResultData = _IModService.GetMyCreateMod(UserId, json) };
        }

        /// <summary>
        /// 获取Mod详细信息（展示，非作者）
        /// </summary>
        /// <param name="json">{"ModId":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "ModDetail")]
        public async Task<ResultEntity<ModEntity>> ModDetail([FromBody] dynamic json)
        {
            #region 记录访问 不确定是否含有Token
            string UserId = null;
            if (!string.IsNullOrWhiteSpace(Request.Headers["Authorization"].FirstOrDefault()))
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                UserId = _JwtHelper.GetTokenStr(token, "UserId");
                _IAPILogService.WriteLogAsync("ModController/ModDetail", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
            else
            {
                _IAPILogService.WriteLogAsync("ModController/ModDetail", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModId))
            {
                return new ResultEntity<ModEntity>() { ResultMsg = "无ModId" };
            }
            #endregion
            return new ResultEntity<ModEntity> { ResultData = await _IModService.ModDetail(UserId, (string)json.ModId) };
        }

        /// <summary>
        /// 获取Mod详细信息（编辑，作者）
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetModDetailUpdate")]
        [Authorize]
        public ResultEntity<ModEntity> GetModDetailUpdate([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/GetModDetailUpdate", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModId))
            {
                return new ResultEntity<ModEntity>() { ResultMsg = "无ModId" };
            }
            #endregion
            var entity = _IModService.ModDetailUpd(UserId, (string)json.ModId);
            if (entity == null)
            {
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "非本人Mod！" };
            }
            return new ResultEntity<ModEntity> { ResultData = entity };
        }

        /// <summary>
        /// 更新Mod信息
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "UpdateModInfo")]
        [Authorize]
        public ResultEntity<bool> UpdateModInfo([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/UpdateModInfo", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            var ListTypes = new List<ModTypeEntity>();
            if (((JArray)json.ModTypeEntities).HasValues)
            {
                ListTypes = ((JArray)json.ModTypeEntities).ToObject<List<ModTypeEntity>>();
            }
            var mod = new ModEntity()
            {
                ModId = (string)json.ModId,
                Description = (string)json.Description,
                VideoUrl = (string)json.VideoUrl,
                ModTypeEntities = ListTypes
            };
            var UpdateRelult = _IModService.UpdateModInfo(mod, UserId);
            if (UpdateRelult == null)
            {
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "非本人Mod" };
            }
            if ((bool)UpdateRelult)
            {
                return new ResultEntity<bool> { ResultMsg = "更新成功" };
            }
            else
            {
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "更新失败" };
            }
            return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "更新失败" };
        }

        /// <summary>
        /// 删除Mod
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "DeleteMod")]
        [Authorize]
        public ResultEntity<bool> DeleteMod([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/UpdateModInfo", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModId))
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "无ModId" };
            }
            #endregion
            var res = _IModService.DeleteMod((string)json.ModId, UserId);
            if (res == null)
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "非本人Mod" };
            }
            if ((bool)res)
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "删除成功" };
            }
            else
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "删除失败" };
            }
        }

        /// <summary>
        /// 添加Mod评分
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "AddModPoint")]
        [Authorize]
        public ResultEntity<bool> AddModPoint([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/AddModPoint", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModId))
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "无ModId" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Point))
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "无Point" };
            }
            #endregion
            var entity = new ModPointEntity()
            {
                ModPointId = Guid.NewGuid().ToString(),
                ModId = (string)json.ModId,
                UserId = UserId,
                Point = (int)json.Point
            };
            return new ResultEntity<bool> { ResultData = _IModService.AddModPoint(entity) };
        }

        /// <summary>
        /// 获得Mod评分
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "GetModPointByModId")]
        [Authorize]
        public ResultEntity<ModPointEntity> GetModPointByModId([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/GetModPointByModId", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModId))
            {
                return new ResultEntity<ModPointEntity>() { ResultCode = 400, ResultMsg = "无ModId" };
            }
            #endregion
            return new ResultEntity<ModPointEntity> { ResultData = _IModService.GetModPointEntity((string)json.ModId, UserId) };
        }

        /// <summary>
        /// 更新Mod评分
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "UpdateModPoint")]
        [Authorize]
        public ResultEntity<ModPointEntity> UpdateModPoint([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/UpdateModPoint", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModPointId))
            {
                return new ResultEntity<ModPointEntity>() { ResultCode = 400, ResultMsg = "无ModPointId" };
            }
            if (string.IsNullOrWhiteSpace((string)json.ModId))
            {
                return new ResultEntity<ModPointEntity>() { ResultCode = 400, ResultMsg = "无ModId" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Point))
            {
                return new ResultEntity<ModPointEntity>() { ResultCode = 400, ResultMsg = "无Point" };
            }
            int point = 0;
            if (!int.TryParse((string)json.Point, out point))
            {
                return new ResultEntity<ModPointEntity>() { ResultCode = 400, ResultMsg = "Point不是数字" };
            }
            if (point > 5)
            {
                return new ResultEntity<ModPointEntity>() { ResultCode = 400, ResultMsg = "Point不大于5" };
            }
            #endregion
            var entity = new ModPointEntity()
            {
                ModPointId = (string)json.ModPointId,
                UserId = (string)json.UserId,
                ModId = (string)json.ModId,
                Point = (int)json.Point
            };
            var result = _IModService.UpdateModPointEntity(entity);
            if (result == null)
            {
                return new ResultEntity<ModPointEntity>() { ResultCode = 400, ResultMsg = "更新失败" };
            }
            return new ResultEntity<ModPointEntity>() { ResultData = result };
        }

        /// <summary>
        /// 删除Mod评分
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost(Name = "DeleteModPoint")]
        [Authorize]
        public ResultEntity<bool> DeleteModPoint([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ModController/DeleteModPoint", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.ModPointId))
            {
                return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "无ModPointId" };
            }
            #endregion
            return new ResultEntity<bool> { ResultData = _IModService.DeleteModPoint((string)json.ModPointId) };
        }
    }
}
