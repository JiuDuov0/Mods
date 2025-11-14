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
using System.Security.Claims;
using System.Threading.Tasks;
using ViewEntity.Mod;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// Mod 相关 API（创建 / 列表 / 详情 / 类型 / 版本 / 评分 / 订阅）
    /// 返回统一使用 ResultEntity，成功默认 ResultCode=200。
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
        private readonly HttpClient _httpClient;

        public ModController(
            IModService iModService,
            IAPILogService iAPILogService,
            IHttpContextAccessor iHttpContextAccessor,
            JwtHelper jwtHelper,
            ITypesService iTypesService,
            HttpClient httpClient)
        {
            _IModService = iModService;
            _IAPILogService = iAPILogService;
            _IHttpContextAccessor = iHttpContextAccessor;
            _JwtHelper = jwtHelper;
            _ITypesService = iTypesService;
            _httpClient = httpClient;
            if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        #region 辅助方法
        /// <summary>
        /// 从请求头中获取 UserId（无或无效返回 null）
        /// </summary>
        private string? GetUserId()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            return string.IsNullOrWhiteSpace(token) ? null : _JwtHelper.GetTokenStr(token, "UserId");
        }

        /// <summary>
        /// 安全反序列化 dynamic JSON
        /// </summary>
        private dynamic ParseJson(dynamic json)
        {
            if (json == null) return new JObject();
            try
            {
                return JsonConvert.DeserializeObject(Convert.ToString(json));
            }
            catch
            {
                return new JObject();
            }
        }

        /// <summary>
        /// 安全转换为 int，失败返回 0
        /// </summary>
        private int ToInt(object? val)
        {
            if (val == null) return 0;
            try
            {
                if (val is int i) return i;
                if (val is long l) return (int)l;
                if (val is string s && int.TryParse(s, out var r)) return r;
                return Convert.ToInt32(val);
            }
            catch { return 0; }
        }
        #endregion

        /// <summary>
        /// 分页获取 Mod 列表（支持搜索 / 类型筛选 / 订阅标记 / 平均分）
        /// </summary>
        /// <param name="json">
        /// JSON:
        /// {
        ///   "Skip":"0",
        ///   "Take":"10",
        ///   "Search":"",
        ///   "Types":["typeId1","typeId2"],
        ///   "GameId":"gameId"
        /// }
        /// </param>
        /// <returns>ResultEntity(List(ModListViewEntity))</returns>
        [HttpPost(Name = "ModListPage")]
        public ResultEntity<List<ModListViewEntity>> ModListPage([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/ModListPage", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());

            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.Skip))
            {
                return new ResultEntity<List<ModListViewEntity>> { ResultCode = 400, ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModListViewEntity>> { ResultCode = 400, ResultMsg = "无Take" };
            }

            var list = _IModService.ModListPage(json, userId);
            return new ResultEntity<List<ModListViewEntity>> { ResultCode = 200, ResultData = list };
        }

        /// <summary>
        /// 分页搜索 Mod（只用于搜索场景，不带缓存平均分逻辑）
        /// </summary>
        /// <param name="json">
        /// {"Skip":"0","Take":"10","Search":"关键字"}
        /// </param>
        /// <returns>ResultEntity(List(ModEntity))</returns>
        [HttpPost(Name = "ModListPageSearch")]
        public async Task<ResultEntity<List<ModEntity>?>> ModListPageSearch([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            await _IAPILogService.WriteLogAsync("ModController/ModListPageSearch", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());

            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.Skip))
            {
                return new ResultEntity<List<ModEntity>?> { ResultCode = 400, ResultMsg = "无Skip" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ModEntity>?> { ResultCode = 400, ResultMsg = "无Take" };
            }
            if (string.IsNullOrWhiteSpace((string)json.Search))
            {
                return new ResultEntity<List<ModEntity>?> { ResultCode = 400, ResultMsg = "无Search" };
            }

            int skip = ToInt(json.Skip);
            int take = ToInt(json.Take);
            var result = await _IModService.ModListPageSearch(skip, take, (string)json.Search);
            return new ResultEntity<List<ModEntity>?> { ResultCode = 200, ResultData = result };
        }

        /// <summary>
        /// 获取所有 Mod 类型
        /// </summary>
        /// <param name="json">{"GameId":"gameId"}</param>
        /// <returns>ResultEntity(List(TypesEntity))</returns>
        [HttpPost(Name = "GetAllModTypes")]
        public ResultEntity<List<TypesEntity>> GetAllModTypes([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/GetAllModTypes", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());

            json = ParseJson(json);
            var gameId = (string)json.GameId;
            if (string.IsNullOrWhiteSpace(gameId))
            {
                return new ResultEntity<List<TypesEntity>> { ResultCode = 400, ResultMsg = "无GameId" };
            }

            var types = _ITypesService.GetTypesListAsync(gameId).Result;
            return new ResultEntity<List<TypesEntity>> { ResultCode = 200, ResultData = types };
        }

        /// <summary>
        /// 创建 Mod（附首版本、类型、依赖、自动获取封面）
        /// </summary>
        /// <param name="json">
        /// {
        ///  "Name":"",
        ///  "Description":"",
        ///  "VideoUrl":"BV号(可选)",
        ///  "PicUrl":"",
        ///  "GameId":"",
        ///  "ModVersionEntities":[{"VersionNumber":"1.0.0","Description":"描述"}],
        ///  "ModTypeEntities":[{"TypesId":"xxx"}],
        ///  "ModDependenceEntities":[{"DependenceModVersionId":"xxx"}]
        /// }
        /// </param>
        /// <returns>ResultEntity(ModEntity)</returns>
        [HttpPost(Name = "CreateMod")]
        [Authorize]
        public async Task<ResultEntity<ModEntity>> CreateMod([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/CreateMod", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());

            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.Name))
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "无Mod名称" };
            if (string.IsNullOrWhiteSpace((string)json.Description))
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "无Mod描述" };
            if (json.ModVersionEntities == null ||
                string.IsNullOrWhiteSpace((string)json.ModVersionEntities[0].VersionNumber))
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "无版本号" };
            if (string.IsNullOrWhiteSpace((string)json.ModVersionEntities[0].Description))
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "无版本描述" };

            var modId = Guid.NewGuid().ToString();
            var version = new ModVersionEntity
            {
                VersionId = Guid.NewGuid().ToString(),
                ModId = modId,
                VersionNumber = (string)json.ModVersionEntities[0].VersionNumber,
                Description = ((string)json.ModVersionEntities[0].Description).Replace("\n", "</br>"),
                CreatedAt = DateTime.Now
            };

            var typesList = new List<ModTypeEntity>();
            if (json.ModTypeEntities is JArray arr1 && arr1.HasValues)
            {
                typesList = arr1.ToObject<List<ModTypeEntity>>() ?? new List<ModTypeEntity>();
            }

            var dependenceList = new List<ModDependenceEntity>();
            if (json.ModDependenceEntities is JArray arr2 && arr2.HasValues)
            {
                dependenceList = arr2.ToObject<List<ModDependenceEntity>>() ?? new List<ModDependenceEntity>();
                dependenceList.ForEach(x =>
                {
                    x.ModDependenceId = Guid.NewGuid().ToString();
                    x.ModId = modId;
                });
            }

            var mod = new ModEntity
            {
                ModId = modId,
                Name = (string)json.Name,
                Description = ((string)json.Description).Replace("\n", "</br>"),
                CreatorUserId = userId,
                CreatedAt = DateTime.Now,
                VideoUrl = (string)json.VideoUrl,
                PicUrl = (string)json.PicUrl,
                GameId = (string)json.GameId,
                DownloadCount = 0
            };

            // 处理视频封面
            if (!string.IsNullOrWhiteSpace(mod.VideoUrl))
            {
                try
                {
                    var api = $"https://api.bilibili.com/x/web-interface/view?bvid={mod.VideoUrl}";
                    var body = await _httpClient.GetStringAsync(api);
                    var jobj = JObject.Parse(body);
                    var cover = jobj["data"]?["pic"]?.ToString();
                    if (string.IsNullOrWhiteSpace(mod.PicUrl) && !string.IsNullOrWhiteSpace(cover))
                        mod.PicUrl = cover;
                    mod.VideoUrl = $"//player.bilibili.com/player.html?bvid={mod.VideoUrl}&autoplay=false&danmaku=false";
                }
                catch
                {
                    return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "BV号不正确" };
                }
            }

            if (_IModService.AddModAndModVersion(mod, version, typesList, dependenceList))
            {
                mod.ModVersionEntities = new List<ModVersionEntity> { version };
                return new ResultEntity<ModEntity> { ResultCode = 200, ResultData = mod };
            }
            return new ResultEntity<ModEntity> { ResultCode = 500, ResultMsg = "创建版本失败" };
        }

        /// <summary>
        /// 为 Mod 批量添加类型
        /// </summary>
        /// <param name="json">
        /// [
        ///   {"ModId":"modId1","TypesId":"typeId1"},
        ///   {"ModId":"modId1","TypesId":"typeId2"}
        /// ]
        /// </param>
        /// <returns>ResultEntity(bool)</returns>
        [HttpPost(Name = "ModAddModType")]
        [Authorize]
        public ResultEntity<bool> ModAddModType([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/ModAddModType", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            JArray jArray = json;
            if (jArray == null || jArray.Count == 0)
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "不能为空" };

            var modIds = new List<string>();
            foreach (JObject item in jArray)
            {
                var mid = item["ModId"]?.ToString();
                if (!string.IsNullOrWhiteSpace(mid) && !modIds.Contains(mid))
                    modIds.Add(mid);
            }

            if (!_IModService.IsLoginUserMods(modIds, userId))
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "含有非本人Mod" };

            var ok = _IModService.AddModTypes(jArray);
            return new ResultEntity<bool> { ResultCode = 200, ResultData = ok };
        }

        /// <summary>
        /// 添加 Mod 版本（作者操作）
        /// </summary>
        /// <param name="json">
        /// {"ModId":"modId","VersionNumber":"1.0.1","Description":"变更说明"}
        /// </param>
        /// <returns>ResultEntity(ModVersionEntity)</returns>
        [HttpPost(Name = "ModAddVersion")]
        [Authorize]
        public ResultEntity<ModVersionEntity> ModAddVersion([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/ModAddVersion", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModId))
                return new ResultEntity<ModVersionEntity> { ResultCode = 400, ResultMsg = "无ModId" };
            if (string.IsNullOrWhiteSpace((string)json.VersionNumber))
                return new ResultEntity<ModVersionEntity> { ResultCode = 400, ResultMsg = "无VersionNumber" };
            if (string.IsNullOrWhiteSpace((string)json.Description))
                return new ResultEntity<ModVersionEntity> { ResultCode = 400, ResultMsg = "无Description" };

            if (!_IModService.IsLoginUserMods(new List<string> { (string)json.ModId }, userId))
                return new ResultEntity<ModVersionEntity> { ResultCode = 400, ResultMsg = "含有非本人Mod" };

            var entity = new ModVersionEntity
            {
                VersionId = Guid.NewGuid().ToString(),
                ModId = (string)json.ModId,
                VersionNumber = (string)json.VersionNumber,
                Description = ((string)json.Description).Replace("\n", "</br>"),
                CreatedAt = DateTime.Now
            };

            if (_IModService.AddModVersion(entity))
                return new ResultEntity<ModVersionEntity> { ResultCode = 200, ResultData = entity };

            return new ResultEntity<ModVersionEntity> { ResultCode = 500, ResultMsg = "添加失败" };
        }

        /// <summary>
        /// 获取我创建的 Mod 列表（分页）
        /// </summary>
        /// <param name="json">
        /// {"Skip":"0","Take":"10","Search":"","Types":["t1","t2"],"GameId":"gameId"}
        /// </param>
        /// <returns>ResultEntity(List(ModListViewEntity))</returns>
        [HttpPost(Name = "GetMyCreateMod")]
        [Authorize]
        public ResultEntity<List<ModListViewEntity>> GetMyCreateMod([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/GetMyCreateMod", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.Skip))
                return new ResultEntity<List<ModListViewEntity>> { ResultCode = 400, ResultMsg = "无Skip" };
            if (string.IsNullOrWhiteSpace((string)json.Take))
                return new ResultEntity<List<ModListViewEntity>> { ResultCode = 400, ResultMsg = "无Take" };

            var data = _IModService.GetMyCreateMod(userId, json);
            return new ResultEntity<List<ModListViewEntity>> { ResultCode = 200, ResultData = data };
        }

        /// <summary>
        /// 获取 Mod 详情（普通用户视角：仅已审批且有文件的版本）
        /// </summary>
        /// <param name="json">{"ModId":"modId"}</param>
        /// <returns>ResultEntity(ModEntity)</returns>
        [HttpPost(Name = "ModDetail")]
        public async Task<ResultEntity<ModEntity>> ModDetail([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            string? token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            string? roles = string.IsNullOrWhiteSpace(token) ? null : _JwtHelper.GetTokenStr(token, ClaimTypes.Role);

            _ = _IAPILogService.WriteLogAsync("ModController/ModDetail", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModId))
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "无ModId" };

            var modId = (string)json.ModId;
            if (!string.IsNullOrWhiteSpace(roles) && (roles.Contains("Developer") || roles.Contains("Auditors")))
            {
                var full = await _IModService.ModDetailAllModVersion(userId, modId);
                return new ResultEntity<ModEntity> { ResultCode = 200, ResultData = full };
            }
            var entity = await _IModService.ModDetail(userId, modId);
            return new ResultEntity<ModEntity> { ResultCode = 200, ResultData = entity };
        }

        /// <summary>
        /// 获取 Mod 编辑详情（作者视角，包含未发布版本）
        /// </summary>
        /// <param name="json">{"ModId":"modId"}</param>
        /// <returns>ResultEntity(ModEntity)</returns>
        [HttpPost(Name = "GetModDetailUpdate")]
        [Authorize]
        public ResultEntity<ModEntity> GetModDetailUpdate([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/GetModDetailUpdate", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModId))
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "无ModId" };

            var entity = _IModService.ModDetailUpd(userId, (string)json.ModId);
            if (entity == null)
                return new ResultEntity<ModEntity> { ResultCode = 400, ResultMsg = "非本人Mod！" };

            entity.Description = entity.Description?.Replace("</br>", "\n");
            return new ResultEntity<ModEntity> { ResultCode = 200, ResultData = entity };
        }

        /// <summary>
        /// 更新 Mod 基本信息（描述 / 视频 / 封面 / 类型 / 依赖）
        /// </summary>
        /// <param name="json">
        /// {
        ///  "ModId":"",
        ///  "Description":"",
        ///  "VideoUrl":"",
        ///  "PicUrl":"",
        ///  "GameId":"",
        ///  "ModTypeEntities":[{"TypesId":""}],
        ///  "ModDependenceEntities":[{"DependenceModVersionId":""}]
        /// }
        /// </param>
        /// <returns>ResultEntity(bool)</returns>
        [HttpPost(Name = "UpdateModInfo")]
        [Authorize]
        public ResultEntity<bool> UpdateModInfo([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/UpdateModInfo", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            var typesList = new List<ModTypeEntity>();
            if (json.ModTypeEntities is JArray arr1 && arr1.HasValues)
                typesList = arr1.ToObject<List<ModTypeEntity>>() ?? new List<ModTypeEntity>();

            var dependenceList = new List<ModDependenceEntity>();
            if (json.ModDependenceEntities is JArray arr2 && arr2.HasValues)
            {
                dependenceList = arr2.ToObject<List<ModDependenceEntity>>() ?? new List<ModDependenceEntity>();
                dependenceList.ForEach(x => x.ModDependenceId = Guid.NewGuid().ToString());
            }

            var mod = new ModEntity
            {
                ModId = (string)json.ModId,
                Description = ((string)json.Description)?.Replace("\n", "</br>"),
                VideoUrl = (string)json.VideoUrl,
                PicUrl = (string)json.PicUrl,
                GameId = (string)json.GameId,
                ModTypeEntities = typesList,
                ModDependenceEntities = dependenceList
            };

            // 处理视频封面（使用注入 HttpClient 而不是 new）
            if (!string.IsNullOrWhiteSpace(mod.VideoUrl))
            {
                try
                {
                    var body = _httpClient.GetStringAsync($"https://api.bilibili.com/x/web-interface/view?bvid={mod.VideoUrl}")
                                          .GetAwaiter().GetResult();
                    var cover = JObject.Parse(body)["data"]?["pic"]?.ToString();
                    if (string.IsNullOrWhiteSpace(mod.PicUrl) && !string.IsNullOrWhiteSpace(cover))
                        mod.PicUrl = cover;
                    mod.VideoUrl = $"//player.bilibili.com/player.html?bvid={mod.VideoUrl}&autoplay=false&danmaku=false";
                }
                catch
                {
                    return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "BV号不正确" };
                }
            }

            var updateResult = _IModService.UpdateModInfo(mod, userId);
            if (updateResult == null)
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "非本人Mod" };
            if (updateResult == true)
                return new ResultEntity<bool> { ResultCode = 200, ResultData = true, ResultMsg = "更新成功" };
            return new ResultEntity<bool> { ResultCode = 500, ResultMsg = "更新失败" };
        }

        /// <summary>
        /// 删除 Mod（软删除）
        /// </summary>
        /// <param name="json">{"ModId":"modId"}</param>
        /// <returns>ResultEntity(bool)</returns>
        [HttpPost(Name = "DeleteMod")]
        [Authorize]
        public ResultEntity<bool> DeleteMod([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/DeleteMod", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModId))
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "无ModId" };

            var res = _IModService.DeleteMod((string)json.ModId, userId);
            if (res == null)
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "非本人Mod" };
            if (res == true)
                return new ResultEntity<bool> { ResultCode = 200, ResultData = true, ResultMsg = "删除成功" };
            return new ResultEntity<bool> { ResultCode = 500, ResultMsg = "删除失败" };
        }

        /// <summary>
        /// 添加 Mod 评分（1~5）
        /// </summary>
        /// <param name="json">{"ModId":"modId","Point":5}</param>
        /// <returns>ResultEntity(bool)</returns>
        [HttpPost(Name = "AddModPoint")]
        [Authorize]
        public ResultEntity<bool> AddModPoint([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/AddModPoint", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModId))
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "无ModId" };
            if (string.IsNullOrWhiteSpace((string)json.Point))
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "无Point" };

            int point = ToInt(json.Point);
            if (point < 1 || point > 5)
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "Point需在1~5之间" };

            var entity = new ModPointEntity
            {
                ModPointId = Guid.NewGuid().ToString(),
                ModId = (string)json.ModId,
                UserId = userId,
                Point = point
            };
            var ok = _IModService.AddModPoint(entity);
            return new ResultEntity<bool> { ResultCode = 200, ResultData = ok, ResultMsg = ok ? "评分成功" : "评分失败" };
        }

        /// <summary>
        /// 获取当前用户对某 Mod 的评分
        /// </summary>
        /// <param name="json">{"ModId":"modId"}</param>
        /// <returns>ResultEntity(ModPointEntity)</returns>
        [HttpPost(Name = "GetModPointByModId")]
        [Authorize]
        public ResultEntity<ModPointEntity> GetModPointByModId([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/GetModPointByModId", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModId))
                return new ResultEntity<ModPointEntity> { ResultCode = 400, ResultMsg = "无ModId" };

            var point = _IModService.GetModPointEntity((string)json.ModId, userId);
            return new ResultEntity<ModPointEntity> { ResultCode = 200, ResultData = point };
        }

        /// <summary>
        /// 更新评分（只能修改自己的评分，分值 1~5）
        /// </summary>
        /// <param name="json">
        /// {"ModPointId":"id","ModId":"modId","Point":"5","UserId":"当前用户Id"}
        /// </param>
        /// <returns>ResultEntity(ModPointEntity)</returns>
        [HttpPost(Name = "UpdateModPoint")]
        [Authorize]
        public ResultEntity<ModPointEntity> UpdateModPoint([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/UpdateModPoint", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModPointId))
                return new ResultEntity<ModPointEntity> { ResultCode = 400, ResultMsg = "无ModPointId" };
            if (string.IsNullOrWhiteSpace((string)json.ModId))
                return new ResultEntity<ModPointEntity> { ResultCode = 400, ResultMsg = "无ModId" };
            if (string.IsNullOrWhiteSpace((string)json.Point))
                return new ResultEntity<ModPointEntity> { ResultCode = 400, ResultMsg = "无Point" };

            int point;
            if (!int.TryParse((string)json.Point, out point))
                return new ResultEntity<ModPointEntity> { ResultCode = 400, ResultMsg = "Point不是数字" };
            if (point < 1 || point > 5)
                return new ResultEntity<ModPointEntity> { ResultCode = 400, ResultMsg = "Point需在1~5之间" };

            var entity = new ModPointEntity
            {
                ModPointId = (string)json.ModPointId,
                UserId = (string)json.UserId, // 前端传入用于校验
                ModId = (string)json.ModId,
                Point = point
            };

            var updated = _IModService.UpdateModPointEntity(entity);
            if (updated == null)
                return new ResultEntity<ModPointEntity> { ResultCode = 400, ResultMsg = "更新失败" };

            return new ResultEntity<ModPointEntity> { ResultCode = 200, ResultData = updated };
        }

        /// <summary>
        /// 删除评分（当前用户 / 或管理控制）
        /// </summary>
        /// <param name="json">{"ModPointId":"scoreId"}</param>
        /// <returns>ResultEntity(bool)</returns>
        [HttpPost(Name = "DeleteModPoint")]
        [Authorize]
        public ResultEntity<bool> DeleteModPoint([FromBody] dynamic json)
        {
            string? userId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/DeleteModPoint", userId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.ModPointId))
                return new ResultEntity<bool> { ResultCode = 400, ResultMsg = "无ModPointId" };

            var ok = _IModService.DeleteModPoint((string)json.ModPointId);
            return new ResultEntity<bool> { ResultCode = ok ? 200 : 500, ResultData = ok, ResultMsg = ok ? "删除成功" : "删除失败" };
        }

        /// <summary>
        /// 分页获取某用户创建的 Mod 列表
        /// </summary>
        /// <param name="json">
        /// {"Skip":"0","Take":"100","UserId":"userId","Search":"","Types":[],"GameId":"gameId"}
        /// </param>
        /// <returns>ResultEntity(List(ModListViewEntity))</returns>
        [HttpPost(Name = "GetModPageListByUserId")]
        [Authorize]
        public ResultEntity<List<ModListViewEntity>> GetModPageListByUserId([FromBody] dynamic json)
        {
            string? callerUserId = GetUserId();
            _ = _IAPILogService.WriteLogAsync("ModController/GetModPageListByUserId", callerUserId ?? "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());
            json = ParseJson(json);

            if (string.IsNullOrWhiteSpace((string)json.Skip))
                return new ResultEntity<List<ModListViewEntity>> { ResultCode = 400, ResultMsg = "无Skip" };
            if (string.IsNullOrWhiteSpace((string)json.Take))
                return new ResultEntity<List<ModListViewEntity>> { ResultCode = 400, ResultMsg = "无Take" };
            if (string.IsNullOrWhiteSpace((string)json.UserId))
                return new ResultEntity<List<ModListViewEntity>> { ResultCode = 400, ResultMsg = "无UserId" };

            var list = _IModService.GetMyCreateMod((string)json.UserId, json);
            return new ResultEntity<List<ModListViewEntity>> { ResultCode = 200, ResultData = list };
        }
    }
}