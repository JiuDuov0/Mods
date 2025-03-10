﻿using Entity;
using Entity.Mod;
using Entity.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
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
        /// <param name="json">Take=取出多少数据，Skip=跳过多少数据，Select=查询框，Types=类型  json示例{"Skip":"0","Take":"10","Select":"","Types":["",""]}</param>
        /// <returns></returns>
        [HttpPost(Name = "ModListPage")]
        public ResultEntity<List<ModEntity>> ModListPage([FromBody] dynamic json)
        {
            if (!string.IsNullOrWhiteSpace(Request.Headers["Authorization"].FirstOrDefault()))
            {
                var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
                var UserId = _JwtHelper.GetTokenStr(token, "UserId");
                _IAPILogService.WriteLogAsync("ModController/ModListPage", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
            else
            {
                _IAPILogService.WriteLogAsync("ModController/ModListPage", "", _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            }
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            return new ResultEntity<List<ModEntity>> { ResultData = _IModService.ModListPage(json) };
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
        /// 创建mod,并创建版本,类型,图片
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
    }
}
