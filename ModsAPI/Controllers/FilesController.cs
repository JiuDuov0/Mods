using Entity;
using Entity.File;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;

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

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="iAPILogService"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="jwtHelper"></param>
        /// <param name="configuration"></param>
        public FilesController(IAPILogService iAPILogService, IHttpContextAccessor iHttpContextAccessor, JwtHelper jwtHelper, IConfiguration configuration)
        {
            _IAPILogService = iAPILogService;
            _IHttpContextAccessor = iHttpContextAccessor;
            _JwtHelper = jwtHelper;
            _IConfiguration = configuration;
        }
        /// <summary>
        /// 上传Mod文件
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost(Name = "UploadMod")]
        public async Task<ResultEntity<string>> UploadMod(IFormFile file)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            var UserRoleIDs = _JwtHelper.GetTokenStr(token, "UserRoleIDs");
            _IAPILogService.WriteLogAsync("ApproveController/ApproveMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            var result = new ResultEntity<string>();

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
            var guid = Guid.NewGuid().ToString();
            var filetype = file.FileName.Substring(file.FileName.LastIndexOf('.'), file.FileName.Length - file.FileName.LastIndexOf('.'));
            var entity = new FilesEntity()
            {
                FileId = guid,
                FilesType = filetype,
                FilesName = file.FileName,
                Size = file.Length.ToString(),
                Path = _IConfiguration["FilePath"] + "\\" + guid + filetype,
                UserId = UserId,
                CreatedAt = DateTime.Now
            };
            var savepath = _IConfiguration["FilePath"];
            var filename = file.FileName;
            var filesize = file.Length;

            try
            {
                var filePath = Path.Combine(_IConfiguration["FilePath"], guid + filetype);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                result.ResultCode = 200;
                result.ResultMsg = "文件上传成功";
                result.ResultData = guid + filetype;
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

            var contentType = "application/x-zip-compressed";
            return File(memory, contentType, Path.GetFileName(filePath));
        }


    }
}
