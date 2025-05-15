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
using System.Security.Claims;
using System.Text.RegularExpressions;

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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            var UserRoleIDs = _JwtHelper.GetTokenStr(token, "UserRoleIDs");
            var Role = _JwtHelper.GetTokenStr(token, ClaimTypes.Role);
            await _IAPILogService.WriteLogAsync("FilesController/UploadMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion

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
                CreatedAt = DateTime.Now,
                Status = "0"
            };
            if (Role.Contains("Dev") || Role.Contains("Aud"))
            {
                approveModVersionEntity.UserId = UserId;
                approveModVersionEntity.Comments = "本人权限审批";
                approveModVersionEntity.ApprovedAt = DateTime.Now;
                approveModVersionEntity.Status = "20";
            }

            if (file.ContentType == "application/json")
            {
                approveModVersionEntity.UserId = "0";
                approveModVersionEntity.Comments = "json文件无需审批";
                approveModVersionEntity.ApprovedAt = DateTime.Now;
                approveModVersionEntity.Status = "20";
            }

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
            else if (file.ContentType != "application/x-zip-compressed" && file.ContentType != "application/zip" && file.ContentType != "application/json" && file.ContentType!= "text/plain")//前面是Windows请求，后面是MACOS请求
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
                if (entity.FilesType == ".json" || entity.FilesType == ".txt")
                {
                    // 读取文件内容并验证JSON格式
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        var content = await reader.ReadToEndAsync();
                        try
                        {
                            Newtonsoft.Json.Linq.JToken.Parse(content);
                        }
                        catch (Exception)
                        {
                            result.ResultCode = 400;
                            result.ResultMsg = "JSON文件格式不正确";
                            return result;
                        }
                    }
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
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            await _IAPILogService.WriteLogAsync($"FilesController/DownloadFile/{json.FileId}", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion

            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            string FileId = json.FileId;

            if (string.IsNullOrWhiteSpace(FileId))
            {
                return Ok(new ResultEntity<string> { ResultCode = 400, ResultMsg = "无FileId" });
            }
            if (_IFilesService.CheckMod(FileId) == null)
            {
                return Ok(new ResultEntity<string> { ResultCode = 400, ResultMsg = "作者已删除" });
            }
            var file = _IFilesService.GetFilesEntityById(FileId);
            var filePath = Path.Combine(_IConfiguration["FilePath"], FileId + file.FilesType);
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

            var contentType = (file.FilesType ?? "").ToLowerInvariant().TrimStart('.') switch
            {
                "zip" => "application/zip",
                "json" => "application/json",
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "txt" => "text/plain",
                "rar" => "application/x-rar-compressed",
                _ => "application/octet-stream" // 默认二进制流
            };
            var fileName = $"{entity.Name}{entity.ModVersionEntities[0].VersionNumber}{file.FilesType}".Replace(' ','_');
            fileName = Regex.Replace(fileName, "[\u4e00-\u9fa5]", "");
            Response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");
            return File(memory, contentType);
        }


    }
}
