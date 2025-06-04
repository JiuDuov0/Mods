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
using System;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ViewEntity.Mod;

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
        private readonly ITypesService _ITypesService;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly JwtHelper _JwtHelper;

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iModService"></param>
        /// <param name="iAPILogService"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="jwtHelper"></param>
        public ModController(IModService iModService, IAPILogService iAPILogService, IHttpContextAccessor iHttpContextAccessor, JwtHelper jwtHelper, ITypesService iTypesService)
        {
            _IModService = iModService;
            _IAPILogService = iAPILogService;
            _IHttpContextAccessor = iHttpContextAccessor;
            _JwtHelper = jwtHelper;
            _ITypesService = iTypesService;
        }
        /// <summary>
        /// 分页获取Mod列表
        /// </summary>
        /// <param name="json">Take=取出多少数据，Skip=跳过多少数据，Search=查询框，Types=类型  json示例{"Skip":"0","Take":"10","Search":"","Types":["",""]}</param>
        /// <returns></returns>
        [HttpPost(Name = "ModListPage")]
        public ResultEntity<List<ModListViewEntity>> ModListPage([FromBody] dynamic json)
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
                return new ResultEntity<List<ModListViewEntity>>() { ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModListViewEntity>>() { ResultMsg = "无Take" };
            }
            #endregion
            var list = _IModService.ModListPage(json, UserId);
            //GC.Collect();
            return new ResultEntity<List<ModListViewEntity>> { ResultData = list };
        }

        /// <summary>
        /// 分页搜索Mod列表
        /// </summary>
        /// <param name="json">{"Skip":"0","Take":"10","Search":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "ModListPageSearch")]
        public async Task<ResultEntity<List<ModEntity>?>> ModListPageSearch([FromBody] dynamic json)
        {
            #region 记录访问 不确定是否含有Token
            string? UserId = null;
            if (!string.IsNullOrWhiteSpace(Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "")))
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                UserId = _JwtHelper.GetTokenStr(token, "UserId");
            }
            await _IAPILogService.WriteLogAsync($"{GetType().Name}/ModListPageSearch", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.Skip))
            {
                return new ResultEntity<List<ModEntity>?>() { ResultCode = 400, ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModEntity>?>() { ResultCode = 400, ResultMsg = "无Take" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Search))
            {
                return new ResultEntity<List<ModEntity>?>() { ResultCode = 400, ResultMsg = "无Search" };
            }
            #endregion
            return new ResultEntity<List<ModEntity>?>() { ResultData = await _IModService.ModListPageSearch((int)json.Skip, (int)json.Take, (string)json.Search) };
        }

        /// <summary>
        /// 获取所有mod类型
        /// </summary>
        /// <param name="json">{"":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "GetAllModTypes")]
        public ResultEntity<List<TypesEntity>> GetAllModTypes([FromBody] dynamic json)
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
            return new ResultEntity<List<TypesEntity>>() { ResultData = _ITypesService.GetTypesListAsync((string)json.GameId).Result };
        }

        /// <summary>
        /// 创建mod,并创建版本,创建mod依赖
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
            ModVersion.Description = ModVersion.Description.Replace("\n", "</br>");
            var ListTypes = new List<ModTypeEntity>();
            if (((JArray)json.ModTypeEntities).HasValues)
            {
                ListTypes = ((JArray)json.ModTypeEntities).ToObject<List<ModTypeEntity>>();
            }
            var ModDependenceList = new List<ModDependenceEntity>();
            if (((JArray)json.ModDependenceEntities).HasValues)
            {
                ModDependenceList = ((JArray)json.ModDependenceEntities).ToObject<List<ModDependenceEntity>>();
                ModDependenceList.ForEach(x => { x.ModId = ModId; x.ModDependenceId = Guid.NewGuid().ToString(); });
            }
            var Mod = new ModEntity()
            {
                ModId = ModId,
                Name = (string)json.Name,
                Description = (string)json.Description,
                CreatorUserId = UserId,
                CreatedAt = DateTime.Now,
                VideoUrl = (string)json.VideoUrl,
                PicUrl = (string)json.PicUrl,
                GameId = (string)json.GameId,
                DownloadCount = 0
            };
            Mod.Description = Mod.Description.Replace("\n", "</br>");
            #region Get方法获取视频封面信息
            string Get(string url, string content)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")); // 设置响应数据的ContentType
                        return client.GetStringAsync(url + content).Result;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            #endregion
            if (!string.IsNullOrWhiteSpace(Mod.VideoUrl))
            {
                var res = Get($"https://api.bilibili.com/x/web-interface/view?bvid={Mod.VideoUrl}", "");
                if (res == null)
                {
                    return new ResultEntity<ModEntity>() { ResultCode = 400, ResultMsg = "BV号不正确" };
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Mod.PicUrl))
                    {
                        Mod.PicUrl = JObject.Parse(res)["data"]["pic"].ToString();
                    }
                }
                Mod.VideoUrl = $"//player.bilibili.com/player.html?bvid={Mod.VideoUrl}&autoplay=false&danmaku=false";
            }
            if (_IModService.AddModAndModVersion(Mod, ModVersion, ListTypes, ModDependenceList))
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
            entity.Description = entity.Description.Replace("\n", "</br>");
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
        public ResultEntity<List<ModListViewEntity>> GetMyCreateMod([FromBody] dynamic json)
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
                return new ResultEntity<List<ModListViewEntity>>() { ResultCode = 400, ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModListViewEntity>>() { ResultCode = 400, ResultMsg = "无Take" };
            }
            #endregion
            return new ResultEntity<List<ModListViewEntity>> { ResultData = _IModService.GetMyCreateMod(UserId, json) };
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
            string token = null;
            string UserRoleIDs = null;
            if (!string.IsNullOrWhiteSpace(Request.Headers["Authorization"].FirstOrDefault()))
            {
                token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                UserId = _JwtHelper.GetTokenStr(token, "UserId");
                UserRoleIDs = _JwtHelper.GetTokenStr(token, ClaimTypes.Role);
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
            //根据角色返回不同的内容
            if (!string.IsNullOrWhiteSpace(UserRoleIDs) && (UserRoleIDs.Contains("Developer") || UserRoleIDs.Contains("Auditors")))
            {
                return new ResultEntity<ModEntity> { ResultData = await _IModService.ModDetailAllModVersion(UserId, (string)json.ModId) };
            }
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
            entity.Description = entity.Description.Replace("</br>", "\n");
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
            var ModDependenceList = new List<ModDependenceEntity>();
            if (((JArray)json.ModDependenceEntities).HasValues)
            {
                ModDependenceList = ((JArray)json.ModDependenceEntities).ToObject<List<ModDependenceEntity>>();
                ModDependenceList.ForEach(x => x.ModDependenceId = Guid.NewGuid().ToString());
            }
            var mod = new ModEntity()
            {
                ModId = (string)json.ModId,
                Description = (string)json.Description,
                VideoUrl = (string)json.VideoUrl,
                PicUrl = (string)json.PicUrl,
                GameId = (string)json.GameId,
                ModTypeEntities = ListTypes,
                ModDependenceEntities = ModDependenceList
            };
            mod.Description = mod.Description.Replace("\n", "</br>");
            #region Get方法获取视频封面信息
            string Get(string url, string content)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")); // 设置响应数据的ContentType
                        return client.GetStringAsync(url + content).Result;
                    }
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            #endregion
            if (!string.IsNullOrWhiteSpace(mod.VideoUrl))
            {
                var res = Get($"https://api.bilibili.com/x/web-interface/view?bvid={mod.VideoUrl}", "");
                if (res == null)
                {
                    return new ResultEntity<bool>() { ResultCode = 400, ResultMsg = "BV号不正确" };
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(mod.PicUrl))
                    {
                        mod.PicUrl = JObject.Parse(res)["data"]["pic"].ToString();
                    }
                }
                mod.VideoUrl = $"//player.bilibili.com/player.html?bvid={mod.VideoUrl}&autoplay=false&danmaku=false";
            }
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
            _IAPILogService.WriteLogAsync("ModController/DeleteMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
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
                return new ResultEntity<bool>() { ResultCode = 400, ResultData = true, ResultMsg = "删除成功" };
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

        /// <summary>
        /// 分页获取某人创建的Mod
        /// </summary>
        /// <param name="json">{"Skip":"0","Take":"100","UserId":"b68eb200-4c81-4285-b5c9-af9ccfcbcd75","Search":"","Types":[]}</param>
        /// <returns></returns>
        [HttpPost(Name = "GetModPageListByUserId")]
        [Authorize]
        public ResultEntity<List<ModListViewEntity>> GetModPageListByUserId([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync($"{GetType().Name}/GetModPageListByUserId", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace((string)json.Skip))
            {
                return new ResultEntity<List<ModListViewEntity>>() { ResultCode = 400, ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModListViewEntity>>() { ResultCode = 400, ResultMsg = "无Take" };
            }
            if (string.IsNullOrWhiteSpace((string)json.UserId))
            {
                return new ResultEntity<List<ModListViewEntity>>() { ResultCode = 400, ResultMsg = "无UserId" };
            }
            #endregion
            return new ResultEntity<List<ModListViewEntity>> { ResultData = _IModService.GetMyCreateMod((string)json.UserId, json) };
        }
    }
}
