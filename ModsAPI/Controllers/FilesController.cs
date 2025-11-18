using Entity;
using Entity.Approve;
using Entity.File;
using Entity.Mod;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Interface;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Net.Http.Headers;

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
        [HttpPost(Name = "UploadMod")]
        [SwaggerOperation(Summary = "上传Mod文件", Description = "上传一个Mod文件和相关的JSON数据")]
        public async Task<ResultEntity<string>> UploadMod([SwaggerParameter(Description = "要上传的文件")] IFormFile file, [FromForm, SwaggerRequestBody(Description = "包含VersionId的JSON数据")] string VersionId)
        {
            var _token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(_token, "UserId");
            var UserRoleIDs = _JwtHelper.GetTokenStr(_token, "UserRoleIDs");
            var Role = _JwtHelper.GetTokenStr(_token, ClaimTypes.Role);
            await _IAPILogService.WriteLogAsync("FilesController/UploadMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());

            if (string.IsNullOrWhiteSpace(VersionId))
                return new ResultEntity<string> { ResultCode = 400, ResultMsg = "VersionId不能为空" };
            if (_IModService.GetByModVersionId(VersionId) == null)
                return new ResultEntity<string> { ResultCode = 400, ResultMsg = "VersionId错误" };
            if (_IModService.IsLoginUserMods(VersionId, UserId))
                return new ResultEntity<string> { ResultCode = 400, ResultMsg = "非本人Mod" };

            var guid = Guid.NewGuid().ToString();
            var filetype = Path.GetExtension(file.FileName);
            var approveModVersionEntity = new ApproveModVersionEntity
            {
                ApproveModVersionId = Guid.NewGuid().ToString(),
                VersionId = VersionId,
                CreatedAt = DateTime.Now,
                Status = "0"
            };
            if (Role?.Contains("Dev") == true || Role?.Contains("Aud") == true)
            {
                approveModVersionEntity.UserId = UserId;
                approveModVersionEntity.Comments = "本人权限审批";
                approveModVersionEntity.ApprovedAt = DateTime.Now;
                approveModVersionEntity.Status = "20";
            }
            if (file.ContentType == "application/json" || file.ContentType == "text/plain")
            {
                approveModVersionEntity.UserId = "0";
                approveModVersionEntity.Comments = "json文件无需审批";
                approveModVersionEntity.ApprovedAt = DateTime.Now;
                approveModVersionEntity.Status = "20";
            }

            var entity = new FilesEntity
            {
                FilesId = guid,
                FilesType = filetype,
                FilesName = file.FileName,
                Size = file.Length.ToString(),
                Path = Path.Combine(_IConfiguration["FilePath"], guid + filetype),
                UserId = UserId,
                CreatedAt = DateTime.Now
            };

            var result = new ResultEntity<string>();

            if (file == null || file.Length == 0)
            {
                result.ResultCode = 400; result.ResultMsg = "文件不能为空"; return result;
            }
            if (file.Length > 1024 * 1024)
            {
                if (UserRoleIDs?.Contains("b156c735-fe7b-421a-4764-78867798ef42") == true ||
                    UserRoleIDs?.Contains("45166589-67eb-4012-abcc-817a0fa12c0e") == true)
                {
                    approveModVersionEntity.Status = "20";
                    if (file.Length > 50 * 1024 * 1024)
                    {
                        result.ResultCode = 400; result.ResultMsg = "文件大小不能超过50M"; return result;
                    }
                }
                else
                {
                    result.ResultCode = 400; result.ResultMsg = "文件大小不能超过1M"; return result;
                }
            }
            if (!new[] {
                "application/x-zip-compressed","application/zip","application/json","text/plain",
                "application/x-rar-compressed","application/vnd.rar"
            }.Contains(file.ContentType))
            {
                result.ResultCode = 400; result.ResultMsg = "文件格式错误"; return result;
            }

            try
            {
                var filePath = entity.Path;
                if (filetype is ".json" or ".txt")
                {
                    using var reader = new StreamReader(file.OpenReadStream());
                    var content = await reader.ReadToEndAsync();
                    try
                    {
                        var token = JsonConvert.DeserializeObject(content) as JToken;
                        if (token == null)
                            throw new Exception("JSON格式错误");
                        void Sanitize(JToken t)
                        {
                            if (t.Type == JTokenType.Object)
                                foreach (var p in ((JObject)t).Properties()) Sanitize(p.Value);
                            else if (t.Type == JTokenType.Array)
                                foreach (var v in (JArray)t) Sanitize(v);
                            else if (t.Type == JTokenType.String)
                            {
                                var val = t.Value<string>();
                                if (val != null && (val.Contains('\n') || val.Contains('\r')))
                                    ((JValue)t).Value = val.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                            }
                        }
                        Sanitize(token);
                        await System.IO.File.WriteAllTextAsync(filePath, token.ToString(Formatting.Indented));
                    }
                    catch (Exception ex)
                    {
                        result.ResultCode = 400; result.ResultMsg = "JSON格式错误: " + ex.Message; return result;
                    }
                }
                else
                {
                    await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await file.CopyToAsync(fs);
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
                result.ResultMsg = "文件上传失败: " + ex.Message;
            }

            return result;
        }

        /// <summary>
        /// 下载文件（支持重复下载 / 断点续传 / 缓存验证）
        /// 请求示例：{"FileId":"xxxxx","NoCount":true}
        /// 说明：
        /// 1. FileId 必填
        /// 2. 支持 Range 头（例如：Range: bytes=0-1023）
        /// 3. 支持 If-None-Match / ETag 缓存，命中返回 304
        /// 4. 可通过传入 NoCount=true 跳过下载次数统计（重复下载不希望累计时）
        /// </summary>
        [HttpPost(Name = "DownloadFile")]
        public async Task<IActionResult> DownloadFile([FromBody] dynamic json)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var userId = _JwtHelper.GetTokenStr(token, "UserId");
            await _IAPILogService.WriteLogAsync("FilesController/DownloadFile", userId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress?.ToString());

            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            string fileId = json.FileId;
            bool noCount = false;
            try { if (json.NoCount != null) bool.TryParse(json.NoCount.ToString(), out noCount); } catch { }

            if (string.IsNullOrWhiteSpace(fileId))
                return Ok(new ResultEntity<string> { ResultCode = 400, ResultMsg = "无FileId" });

            if (_IFilesService.CheckMod(fileId) == null)
                return Ok(new ResultEntity<string> { ResultCode = 400, ResultMsg = "作者已删除" });

            var fileMeta = _IFilesService.GetFilesEntityById(fileId);
            if (fileMeta == null)
                return NotFound(new ResultEntity<string> { ResultCode = 404, ResultMsg = "文件元数据不存在" });

            var fullPath = Path.Combine(_IConfiguration["FilePath"], fileId + fileMeta.FilesType);
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new ResultEntity<string> { ResultCode = 404, ResultMsg = "文件未找到" });

            var fileInfo = new FileInfo(fullPath);
            var etag = $"{fileInfo.Length:x}-{fileInfo.LastWriteTimeUtc.Ticks:x}";

            var inm = Request.Headers["If-None-Match"].ToString();
            if (!string.IsNullOrWhiteSpace(inm) && inm.Replace("\"", "") == etag)
            {
                Response.Headers[HeaderNames.ETag] = $"\"{etag}\"";
                return StatusCode(StatusCodes.Status304NotModified);
            }

            long start = 0;
            long end = fileInfo.Length - 1;
            bool isPartial = false;
            var rangeHeader = Request.Headers["Range"].ToString();
            if (!string.IsNullOrWhiteSpace(rangeHeader) && rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            {
                var range = rangeHeader.Substring(6).Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (range.Length > 0 && long.TryParse(range[0], out var s)) start = s;
                if (range.Length > 1 && long.TryParse(range[1], out var e)) end = e;
                if (start < 0) start = 0;
                if (end >= fileInfo.Length) end = fileInfo.Length - 1;
                if (start <= end) isPartial = true;
            }

            var length = end - start + 1;
            if (length <= 0)
                return StatusCode(StatusCodes.Status416RangeNotSatisfiable);

            // 更新下载次数（可跳过）
            if (!noCount)
                _IFilesService.AddModDownLoadCount(fileId);

            var contentType = (fileMeta.FilesType ?? "").ToLowerInvariant().TrimStart('.') switch
            {
                "zip" => "application/zip",
                "json" => "application/json",
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "txt" => "text/plain",
                "rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };

            // 忽略原始文件名与 Mod 名称：仅使用 FileId 作为文件区分依据
            // 始终输出 ASCII 安全文件名：{FileId}{扩展名}
            var pureName = $"{fileId}{fileMeta.FilesType}";
            // 去除潜在非法头部字符（理论上 GUID + 扩展名不会有问题，防御性处理）
            var safeName = Regex.Replace(pureName, @"[^\x20-\x7E]", "_");

            Response.Headers[HeaderNames.AcceptRanges] = "bytes";
            Response.Headers[HeaderNames.ETag] = $"\"{etag}\"";
            Response.Headers[HeaderNames.LastModified] = fileInfo.LastWriteTimeUtc.ToString("R");
            Response.Headers[HeaderNames.ContentDisposition] = $"attachment; filename=\"{safeName}\"";

            if (isPartial)
            {
                Response.StatusCode = StatusCodes.Status206PartialContent;
                Response.Headers["Content-Range"] = $"bytes {start}-{end}/{fileInfo.Length}";
            }

            Response.ContentType = contentType;

            // 在 isPartial 与全量响应公共头部设置之后（紧接着 Accept-Ranges / ETag / LastModified / ContentDisposition）追加：
            Response.Headers.TryAdd("Access-Control-Expose-Headers", "Content-Range,Content-Disposition,ETag,Last-Modified,Accept-Ranges");

            // 可选：补充 Content-Length 明确长度，避免中间层剥离 Content-Range
            Response.Headers[HeaderNames.ContentLength] = length.ToString();

            await using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(start, SeekOrigin.Begin);

            const int bufferSize = 64 * 1024;
            long remaining = length;
            var buffer = new byte[bufferSize];
            while (remaining > 0)
            {
                int read = await fs.ReadAsync(buffer.AsMemory(0, (int)Math.Min(bufferSize, remaining)));
                if (read <= 0) break;
                await Response.Body.WriteAsync(buffer.AsMemory(0, read));
                remaining -= read;
            }
            return new EmptyResult();
        }
    }
}