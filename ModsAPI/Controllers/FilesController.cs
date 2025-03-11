using Entity;
using Entity.Approve;
using Entity.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;
using Swashbuckle.AspNetCore.Annotations;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 文件相关API
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IAPILogService _IAPILogService;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly JwtHelper _JwtHelper;
        private readonly IConfiguration _IConfiguration;
        private readonly IFilesService _IFilesService;
        private readonly IModService _IModService;

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iAPILogService"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="jwtHelper"></param>
        /// <param name="configuration"></param>
        /// <param name="iFilesService"></param>
        /// <param name="iModService"></param>
        public FilesController(IAPILogService iAPILogService, IHttpContextAccessor iHttpContextAccessor, JwtHelper jwtHelper, IConfiguration configuration, IFilesService iFilesService, IModService iModService)
        {
            _IAPILogService = iAPILogService;
            _IHttpContextAccessor = iHttpContextAccessor;
            _JwtHelper = jwtHelper;
            _IConfiguration = configuration;
            _IFilesService = iFilesService;
            _IModService = iModService;
        }
        /// <summary>
        /// 上传Mod文件并提交审核
        /// </summary>
        /// <param name="file"></param>
        /// <param name="VersionId">Mod版本Id</param>
        /// <returns></returns>
        [HttpPost(Name = "UploadMod")]
        [SwaggerOperation(Summary = "上传Mod文件", Description = "上传一个Mod文件和相关的JSON数据")]
        public async Task<ResultEntity<string>> UploadMod([SwaggerParameter(Description = "要上传的文件")] IFormFile file, [FromForm, SwaggerRequestBody(Description = "包含VersionId的JSON数据")] string VersionId)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            var UserRoleIDs = _JwtHelper.GetTokenStr(token, "UserRoleIDs");
            _IAPILogService.WriteLogAsync("ApproveController/ApproveMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());

            if (string.IsNullOrWhiteSpace(VersionId))
            {
                return new ResultEntity<string>() { ResultCode = 400, ResultMsg = "VersionId不能为空" };
            }
            else if (_IModService.GetByModVersionId(VersionId) == null)
            {
                return new ResultEntity<string>() { ResultCode = 400, ResultMsg = "VersionId错误" };
            }
            else if (_IModService.IsLoginUserMods(VersionId, UserId))
            {
                return new ResultEntity<string>() { ResultCode = 400, ResultMsg = "非本人Mod" };
            }
            var result = new ResultEntity<string>();
            var guid = Guid.NewGuid().ToString();
            var filetype = file.FileName.Substring(file.FileName.LastIndexOf('.'), file.FileName.Length - file.FileName.LastIndexOf('.'));
            var approveModVersionEntity = new ApproveModVersionEntity()
            {
                ApproveModVersionId = Guid.NewGuid().ToString(),
                VersionId = VersionId,
                Status = "0",
            };
            var entity = new FilesEntity()
            {
                FilesId = guid,
                FilesType = filetype,
                FilesName = file.FileName,
                Size = file.Length.ToString(),
                Path = _IConfiguration["FilePath"] + "\\" + guid + filetype,
                UserId = UserId,
                CreatedAt = DateTime.Now
            };

            if (file == null || file.Length == 0)
            {
                result.ResultCode = 400;
                result.ResultMsg = "文件不能为空";
                return result;
            }
            else if (file.Length > 1024 * 1024)
            {
                if (UserRoleIDs.Contains("b156c735-fe7b-421a-4764-78867798ef42") || UserRoleIDs.Contains("45166589-67eb-4012-abcc-817a0fa12c0e"))
                {
                    //有权限
                    approveModVersionEntity.Status = "20";
                    if (file.Length > 1024 * 1024 * 10)
                    {
                        result.ResultCode = 400;
                        result.ResultMsg = "文件大小不能超过10M";
                        return result;
                    }
                }
                else
                {
                    result.ResultCode = 400;
                    result.ResultMsg = "文件大小不能超过1M";
                    return result;
                }
            }
            else if (file.ContentType != "application/x-zip-compressed")
            {
                result.ResultCode = 400;
                result.ResultMsg = "文件格式错误";
                return result;
            }

            try
            {
                var filePath = Path.Combine(_IConfiguration["FilePath"], guid + filetype);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                if (_IFilesService.AddFilesAndApprove(entity, approveModVersionEntity))
                {
                    result.ResultCode = 200;
                    result.ResultMsg = "文件上传成功";
                    result.ResultData = guid + filetype;
                }
                else
                {
                    result.ResultCode = 500;
                    result.ResultMsg = "文件上传失败";
                }
            }
            catch (Exception ex)
            {
                result.ResultCode = 500;
                result.ResultMsg = $"文件上传失败: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="json">{"FileId":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "DownloadFile")]
        public async Task<IActionResult> DownloadFile([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("FilesController/DownloadFile", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());

            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            string FileId = json.FileId;

            if (string.IsNullOrWhiteSpace(FileId))
            {
                return null;
            }

            var filePath = Path.Combine(_IConfiguration["FilePath"], FileId + ".zip");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new ResultEntity<string> { ResultCode = 404, ResultMsg = "文件未找到" });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            var entity = _IFilesService.AddModDownLoadCount(FileId);

            var contentType = "application/x-zip-compressed";
            var sadfsad = entity.Name + entity.ModVersionEntities[0].VersionNumber + ".zip";
            return File(memory, contentType, entity.Name + entity.ModVersionEntities[0].VersionNumber + ".zip");
        }


    }
}
