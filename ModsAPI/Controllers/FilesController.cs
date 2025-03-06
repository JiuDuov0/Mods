using Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
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
        /// 上传Mod
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ResultEntity<string>> UploadMod(IFormFile file)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ApproveController/ApproveMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            var result = new ResultEntity<string>();

            if (file == null || file.Length == 0)
            {
                result.ResultCode = 400;
                result.ResultMsg = "文件不能为空";
                return result;
            }else if (file.Length > 1024 * 1024 * 10)
            {
                result.ResultCode = 400;
                result.ResultMsg = "文件大小不能超过10M";
                return result;
            }else if (file.ContentType != "application/zip")
            {
                result.ResultCode = 400;
                result.ResultMsg = "文件格式错误";
                return result;
            }
            var path = _IConfiguration["FilePath"];
            var filename = file.FileName;
            var filesize = file.Length;

            //try
            //{
            //    var filePath = Path.Combine("uploads", file.FileName);

            //    using (var stream = new FileStream(filePath, FileMode.Create))
            //    {
            //        await file.CopyToAsync(stream);
            //    }
            //    result.ResultCode = 200;
            //    result.ResultMsg = "文件上传成功";
            //    result.ResultData = filePath;
            //}
            //catch (Exception ex)
            //{
            //    result.ResultCode = 500;
            //    result.ResultMsg = $"文件上传失败: {ex.Message}";
            //}

            return result;
        }
    }
}
