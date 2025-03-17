using Entity;
using Entity.Approve;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ModsAPI.tools;
using Newtonsoft.Json;
using Service.Interface;
using System.Threading.Tasks;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 审核相关API
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Developer,Auditors")]
    public class ApproveController : ControllerBase
    {
        private readonly JwtHelper _JwtHelper;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly IAPILogService _IAPILogService;
        private readonly IModService _IModService;

        /// <summary>
        /// 构造函数依赖注入
        /// </summary>
        /// <param name="jwtHelper"></param>
        /// <param name="iHttpContextAccessor"></param>
        /// <param name="iAPILogService"></param>
        /// <param name="iModService"></param>
        public ApproveController(JwtHelper jwtHelper, IHttpContextAccessor iHttpContextAccessor, IAPILogService iAPILogService, IModService iModService)
        {
            _JwtHelper = jwtHelper;
            _IHttpContextAccessor = iHttpContextAccessor;
            _IAPILogService = iAPILogService;
            _IModService = iModService;
        }

        /// <summary>
        /// 审核Mod版本
        /// </summary>
        /// <param name="json">VersionId:VersionId，Comments:审批意见，Status:状态{Approved = 20, Rejected = 10, Pending = 0} json示例：{"VersionId":"","Comments":"","Status":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "ApproveMod")]
        public async Task<ResultEntity<string>> ApproveMod([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ApproveController/ApproveMod", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            string VersionId = (string)json.VersionId;
            string Comments = (string)json.Comments;
            string Status = (string)json.Status;
            #region 验证
            if (string.IsNullOrWhiteSpace(VersionId))
            {
                return new ResultEntity<string>() { ResultMsg = "VersionId不能为空" };
            }
            if (string.IsNullOrWhiteSpace(Comments))
            {
                return new ResultEntity<string>() { ResultMsg = "Comments不能为空" };
            }
            if (string.IsNullOrWhiteSpace(Status))
            {
                return new ResultEntity<string>() { ResultMsg = "Status不能为空" };
            }
            else if (Status != "20" || Status != "10")
            {
                return new ResultEntity<string>() { ResultMsg = "Status不正确" };
            }
            #endregion
            await _IModService.ApproveModVersionAsync(VersionId, UserId, Status, Comments);
            return new ResultEntity<string>() { ResultData = "审核成功" };
        }

        /// <summary>
        /// 获取待审核Mod版本列表
        /// </summary>
        /// <param name="json">{"Skip":"","Take":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "GetApproveModVersionPageList")]
        public ResultEntity<List<ApproveModVersionEntity>> GetApproveModVersionPageList([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("ApproveController/GetApproveModVersionPageList", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion
            #region 验证
            int Skip = 0, Take = 0;
            if (string.IsNullOrWhiteSpace((string)json.Skip) && string.IsNullOrWhiteSpace((string)json.Take))
            {
                return new ResultEntity<List<ApproveModVersionEntity>>() { ResultMsg = "缺少参数" };
            }
            if (int.TryParse((string)json.Skip, out Skip) && int.TryParse((string)json.Take, out Take))
            {
                return new ResultEntity<List<ApproveModVersionEntity>>() { ResultMsg = "缺少参数" };
            }
            #endregion
            return new ResultEntity<List<ApproveModVersionEntity>>() { ResultData = _IModService.GetApproveModVersionPageList(Skip, Take) };
        }
    }
}
