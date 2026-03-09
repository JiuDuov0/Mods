using Entity;
using Entity.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ModsAPI.tools;
using Newtonsoft.Json;
using Redis.Interface;
using Service.Interface;
using Service.Realization;
using System.IO.Compression;
using System.Web;

namespace ModsAPI.Controllers
{
    /// <summary>
    /// 登录api
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IUserService _IUserService;
        private readonly JwtHelper _JwtHelper;
        private readonly IHttpContextAccessor _IHttpContextAccessor;
        private readonly IAPILogService _IAPILogService;
        private readonly IMailService _IMailService;
        private readonly IRedisManageService _RedisManageService;
        /// <summary>
        /// 初始化 LoginController 类的新实例，并注入所需的服务以支持用户认证、JWT 令牌生成、HTTP 上下文访问、API 日志记录、邮件发送及 Redis 管理功能。
        /// </summary>
        /// <param name="iUserService">用于处理用户相关操作的服务实例，如用户验证和信息查询。</param>
        /// <param name="jwtHelper">用于生成和验证 JWT 令牌的辅助工具实例。</param>
        /// <param name="iHttpContextAccessor">用于访问当前 HTTP 请求上下文的服务实例。</param>
        /// <param name="iAPILogService">用于记录 API 调用日志的服务实例。</param>
        /// <param name="iMailService">用于发送邮件通知的服务实例。</param>
        /// <param name="redisManageService">用于管理 Redis 缓存和会话的服务实例。</param>
        public LoginController(IUserService iUserService, JwtHelper jwtHelper, IHttpContextAccessor iHttpContextAccessor, IAPILogService iAPILogService, IMailService iMailService, IRedisManageService redisManageService)
        {
            _IUserService = iUserService;
            _JwtHelper = jwtHelper;
            _IHttpContextAccessor = iHttpContextAccessor;
            _IAPILogService = iAPILogService;
            _IMailService = iMailService;
            _RedisManageService = redisManageService;
        }

        /// <summary>
        /// 登录api
        /// </summary>
        /// <param name="json">LoginAccount=账号（Email），Password=密码 json示例：{"LoginAccount":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "UserLogin")]
        [EnableRateLimiting("Concurrency")]
        public ResultEntity<ResponseToken> UserLogin([FromBody] dynamic json)
        {
            #region 记录访问
            _IAPILogService.WriteLogAsync("LoginController/UserLogin", (string)json.LoginAccount, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

            #region 验证一下传过来的是什么鬼东西
            if ((string)json.LoginAccount == "" || (string)json.LoginAccount == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            if ((string)json.Password == "" || (string)json.Password == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            #endregion

            var UserInfo = _IUserService.Login((string)json.LoginAccount, (string)json.Password);
            if (UserInfo != null && UserInfo.UserId != null)
            {
                return new ResultEntity<ResponseToken> { ResultData = _JwtHelper.CreateToken(UserInfo) };
            }
            return new ResultEntity<ResponseToken> { ResultMsg = "账号或密码错误" };
        }

        /// <summary>
        /// 创建80年后过期的token
        /// </summary>
        /// <param name="json">LoginAccount=账号（Email），Password=密码 json示例：{"LoginAccount":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "CreateToken")]
        [EnableRateLimiting("Concurrency")]
        [Authorize]
        public ResultEntity<ResponseToken> CreateToken([FromBody] dynamic json)
        {
            #region 记录访问
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("LoginController/CreateToken", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

            #region 验证一下传过来的是什么鬼东西
            if ((string)json.LoginAccount == "" || (string)json.LoginAccount == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            if ((string)json.Password == "" || (string)json.Password == null)
            {
                return new ResultEntity<ResponseToken> { ResultMsg = "请检查账号或密码" };
            }
            #endregion

            var UserInfo = _IUserService.Login((string)json.LoginAccount, (string)json.Password);
            if (UserInfo != null && UserInfo.UserId != null)
            {
                //if (!string.IsNullOrWhiteSpace(UserInfo.Token))
                //{
                //    return new ResultEntity<ResponseToken>() { ResultData = new ResponseToken() { Token = UserInfo.Token } };
                //}
                var res = _JwtHelper.CreateYearsToken(UserInfo);
                UserInfo.Token = res.Token;
                _IUserService.UpdateUserAsync(UserInfo);
                return new ResultEntity<ResponseToken> { ResultData = res };
            }
            return new ResultEntity<ResponseToken> { ResultMsg = "账号或密码错误" };
        }

        /// <summary>
        /// 创建用户并登录
        /// </summary>
        /// <param name="json">LoginAccount=账号（Email），Password=密码，NickName=昵称 json示例：{"LoginAccount":"","NickName":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "UserRegister")]
        [EnableRateLimiting("Concurrency")]
        public ResultEntity<ResponseToken> UserRegister([FromBody] dynamic json)
        {
            #region 记录访问
            _IAPILogService.WriteLogAsync("LoginController/UserRegister", (string)json.LoginAccount, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            #endregion

            #region 验证一下传过来的是什么鬼东西
            if ((string)json.LoginAccount == "" || (string)json.LoginAccount == null)
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查账号或密码" };
            }
            else if (!((string)json.LoginAccount).Contains('@') && !((string)json.LoginAccount).Contains('.'))
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查账号或密码" };
            }
            if ((string)json.NickName == "" || (string)json.NickName == null)
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查昵称" };
            }
            if ((string)json.Password == "" || (string)json.Password == null)
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "请检查账号或密码" };
            }
            #endregion

            var User = new UserEntity()
            {
                UserId = Guid.NewGuid().ToString(),
                Mail = (string)json.LoginAccount,
                NickName = (string)json.NickName,
                Password = (string)json.Password,
                CreatedAt = DateTime.Now
            };
            User = _IUserService.Register(User);
            if (User != null)
            {
                return new ResultEntity<ResponseToken> { ResultData = _JwtHelper.CreateToken(User) };
            }
            else
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "邮箱已注册" };
            }
            return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "信息错误" };
        }

        /// <summary>
        /// 发送验证码
        /// </summary>
        /// <param name="json">{"Mail":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "SendVerificationCode")]
        public async Task<ResultEntity<bool>> SendVerificationCodeAsync([FromBody] dynamic json)
        {
            var Mail = string.Empty;
            #region 记录访问
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            Mail = (string)json.Mail;
            await _IAPILogService.WriteLogAsync($"{GetType().Name}/SendVerificationCodeAsync", Mail, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion
            var resultMsg = await _IMailService.SendVerificationCodeAsync(Mail, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            if (resultMsg == string.Empty)
            {
                return new ResultEntity<bool> { ResultData = true, ResultMsg = resultMsg };
            }
            else
            {
                return new ResultEntity<bool> { ResultData = false, ResultMsg = resultMsg };
            }
        }

        /// <summary>
        /// 验证验证码，修改密码
        /// </summary>
        /// <param name="json">{"Mail":"","VerificationCode":"","Password":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "VerifyEmailCodeAndChangePassWord")]
        public async Task<ResultEntity<bool>> VerifyEmailCodeAndChangePassWordAsync([FromBody] dynamic json)
        {
            var Mail = string.Empty;
            var VerificationCode = string.Empty;
            var Password = string.Empty;
            #region 记录访问
            json = JsonConvert.DeserializeObject(Convert.ToString(json));
            Mail = (string)json.Mail;
            VerificationCode = (string)json.VerificationCode;
            Password = (string)json.Password;
            await _IAPILogService.WriteLogAsync($"{GetType().Name}/VerifyEmailCodeAndChangePassWordAsync", Mail, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion
            #region 验证
            if (string.IsNullOrWhiteSpace(Mail))
            {
                return new ResultEntity<bool> { ResultData = false, ResultMsg = "缺少Mail" };
            }
            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                return new ResultEntity<bool> { ResultData = false, ResultMsg = "缺少VerificationCode" };
            }
            if (string.IsNullOrWhiteSpace(Password))
            {
                return new ResultEntity<bool> { ResultData = false, ResultMsg = "缺少Password" };
            }
            #endregion
            if (await _IMailService.VerifyEmailCodeAsync(Mail, VerificationCode))
            {
                if (await _IUserService.UpdateUserPasswordAsync(Mail, Password))
                {
                    await _IMailService.UpdateCatchState(Mail);
                }
            }
            else
            {
                return new ResultEntity<bool> { ResultData = false, ResultMsg = "验证码不正确" };
            }
            return new ResultEntity<bool> { ResultData = true };
        }

        /// <summary>
        /// Token续签接口
        /// </summary>
        /// <param name="json">{"Token":"","RefreshToken":""}</param>
        /// <returns></returns>
        [HttpPost(Name = "RefreshToken")]
        [EnableRateLimiting("Concurrency")]
        public ResultEntity<ResponseToken> RefreshToken([FromBody] dynamic json)
        {
            #region 记录访问
            var token = (string)json.Token;
            string UserId = string.Empty;
            if (string.IsNullOrWhiteSpace(token)) { UserId = _JwtHelper.GetTokenStr(token, "UserId"); }
            var refreshToken = (string)json.RefreshToken;
            _IAPILogService.WriteLogAsync("LoginController/RefreshToken", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion

            #region 验证参数
            if (string.IsNullOrWhiteSpace(token))
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "缺少Token" };
            }
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new ResultEntity<ResponseToken> { ResultCode = 400, ResultMsg = "缺少RefreshToken" };
            }
            #endregion

            try
            {
                // 调用 JwtHelper 的 Refresh 方法进行 Token 续签
                var newToken = _JwtHelper.Refresh(token, refreshToken, HttpContext);
                if (newToken != null)
                {
                    return new ResultEntity<ResponseToken> { ResultData = newToken, ResultMsg = "Token续签成功" };
                }
                else
                {
                    return new ResultEntity<ResponseToken> { ResultCode = 401, ResultMsg = "Token续签失败" };
                }
            }
            catch (Exception ex)
            {
                return new ResultEntity<ResponseToken> { ResultCode = 500, ResultMsg = $"Token续签异常: {ex.Message}" };
            }
        }

        /// <summary>
        /// 测试用的
        /// </summary>
        /// <returns></returns>
        [HttpPost(Name = "Test")]
        [Authorize]
        public string Test()
        {
            #region 记录访问
            //让我康康谁请求的
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            var UserId = _JwtHelper.GetTokenStr(token, "UserId");
            _IAPILogService.WriteLogAsync("LoginController/Test", UserId, _IHttpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            #endregion
            var roleid = _JwtHelper.GetTokenStr(token, "UserRoleIDs");
            var role = _JwtHelper.GetTokenStr(token, "UserId");


            //string Get(string url, string content)
            //{
            //    try
            //    {
            //        using (HttpClient client = new HttpClient())
            //        {
            //            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")); // 设置响应数据的ContentType
            //            return client.GetStringAsync(url + content).Result;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        return null;
            //    }
            //}
            //var collection = HttpUtility.ParseQueryString(new Uri("https://api.bilibili.com/x/web-interface/view?bvid=BV1NJ411g7Ui").Query);
            //var bvid = collection["bvid"];

            //Get("https://api.bilibili.com/x/web-interface/view?bvid=BV1NJ411g7Ui", "");



            return null;
        }

        /// <summary>
        /// 处理一个 HTTP POST 请求以执行更新 Mintcat 操作。
        /// </summary>
        [HttpPost(Name = "Mintcat")]
        public async Task<ResultEntity<bool>> Mintcat()
        {
            try
            {
                var destDir = @"C:\Web\dist\assets";
                Directory.CreateDirectory(destDir);

                // 从文件名提取版本（示例：mintcat_0.5.0_x64-setup.exe）
                static string? GetVersionFromName(string name)
                {
                    var m = System.Text.RegularExpressions.Regex.Match(name ?? string.Empty, @"mintcat[_\-]?v?([0-9]+(?:\.[0-9]+)*)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    return m.Success ? m.Groups[1].Value : null;
                }

                // 本地最大版本
                string? localVersion = null;
                var localExeFiles = Directory.EnumerateFiles(destDir, "mintcat*.exe", SearchOption.TopDirectoryOnly)
                                             .Select(Path.GetFileName)
                                             .ToList();
                foreach (var f in localExeFiles)
                {
                    var v = GetVersionFromName(f);
                    if (v == null) continue;
                    if (localVersion == null)
                        localVersion = v;
                    else
                    {
                        int CompareVer(string a, string b)
                        {
                            if (a == null && b == null) return 0;
                            if (a == null) return -1;
                            if (b == null) return 1;
                            var aa = a.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
                            var bb = b.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
                            var len = Math.Max(aa.Length, bb.Length);
                            for (int i = 0; i < len; i++)
                            {
                                var av = i < aa.Length ? aa[i] : 0;
                                var bv = i < bb.Length ? bb[i] : 0;
                                if (av != bv) return av > bv ? 1 : -1;
                            }
                            return 0;
                        }
                        if (CompareVer(v, localVersion) > 0) localVersion = v;
                    }
                }

                // 请求 GitHub API 获取最新 release
                var apiUrl = "https://api.github.com/repos/iriscats/mintcat/releases/latest";
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("ModsAPI-Mintcat-Updater");
                http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

                var resp = await http.GetAsync(apiUrl);
                if (!resp.IsSuccessStatusCode)
                    return new ResultEntity<bool> { ResultData = false, ResultMsg = $"无法获取 release 信息: {resp.StatusCode}" };

                var body = await resp.Content.ReadAsStringAsync();
                var jo = Newtonsoft.Json.Linq.JObject.Parse(body);

                // 优先使用 tag_name 作为版本（去掉前导 v）
                var latestVersion = (string?)jo["tag_name"];
                if (!string.IsNullOrWhiteSpace(latestVersion) && latestVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                    latestVersion = latestVersion.Substring(1);

                // 查找 assets 中的 exe 文件
                var assets = jo["assets"] as Newtonsoft.Json.Linq.JArray;
                if (assets == null || assets.Count == 0)
                    return new ResultEntity<bool> { ResultData = false, ResultMsg = "未在 release 中发现 assets" };

                Newtonsoft.Json.Linq.JToken? chosen = null;
                foreach (var a in assets)
                {
                    var name = (string?)a["name"];
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                        name.IndexOf("mintcat", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        chosen = a;
                        break;
                    }
                }
                if (chosen == null)
                {
                    foreach (var a in assets)
                    {
                        var name = (string?)a["name"];
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            chosen = a;
                            break;
                        }
                    }
                }

                if (chosen == null)
                    return new ResultEntity<bool> { ResultData = false, ResultMsg = "未找到可下载的 exe 资源" };

                var assetName = (string?)chosen["name"];
                var downloadUrl = (string?)chosen["browser_download_url"];
                if (string.IsNullOrWhiteSpace(downloadUrl) || string.IsNullOrWhiteSpace(assetName))
                    return new ResultEntity<bool> { ResultData = false, ResultMsg = "找到资源但无下载地址" };

                // 若 tag 不存在，从 asset 名称提取版本
                if (string.IsNullOrWhiteSpace(latestVersion))
                    latestVersion = GetVersionFromName(assetName);

                var redisKey = "mintcat:latest";

                // 使用注入的 _RedisManageService（db4）
                string? redisVal = null;
                try
                {
                    // 尝试异步读取 redis
                    redisVal = await _RedisManageService.GetAsync<string>(redisKey, 4);
                }
                catch
                {
                    // 若读取失败，忽略并继续（不会阻塞下载）
                    redisVal = null;
                }

                if (!string.IsNullOrWhiteSpace(redisVal) && !string.IsNullOrWhiteSpace(latestVersion) && string.Equals(redisVal, latestVersion, StringComparison.Ordinal))
                {
                    return new ResultEntity<bool> { ResultData = true, ResultMsg = "已是最新版本（记录）" };
                }

                // 版本比较函数（语义比较）
                int CompareVersionsPublic(string a, string b)
                {
                    if (a == null && b == null) return 0;
                    if (a == null) return -1;
                    if (b == null) return 1;
                    var aa = a.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
                    var bb = b.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
                    var len = Math.Max(aa.Length, bb.Length);
                    for (int i = 0; i < len; i++)
                    {
                        var av = i < aa.Length ? aa[i] : 0;
                        var bv = i < bb.Length ? bb[i] : 0;
                        if (av != bv) return av > bv ? 1 : -1;
                    }
                    return 0;
                }

                // 若本地已有版本且最新版本不比本地大，则保存到 Redis 并返回“已是最新”
                if (!string.IsNullOrWhiteSpace(localVersion) && !string.IsNullOrWhiteSpace(latestVersion) && CompareVersionsPublic(latestVersion, localVersion) <= 0)
                {
                    // 将最新版本写入 Redis（覆盖）
                    if (!string.IsNullOrWhiteSpace(latestVersion))
                    {
                        try
                        {
                            await _RedisManageService.SetAsync(redisKey, latestVersion, TimeSpan.FromHours(4), 4);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                    return new ResultEntity<bool> { ResultData = true, ResultMsg = "已是最新版本（本地）" };
                }

                // 下载 exe
                var filePath = Path.Combine(destDir, assetName);
                using (var resp2 = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!resp2.IsSuccessStatusCode)
                        return new ResultEntity<bool> { ResultData = false, ResultMsg = $"下载失败: {resp2.StatusCode}" };

                    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await resp2.Content.CopyToAsync(fs);
                }

                // 压缩为 mintcat.zip（只包含刚下载的 exe，覆盖已有）
                var zipPath = Path.Combine(destDir, "mintcat-BvUg5ULE.zip");
                var tempZipPath = Path.Combine(destDir, $"mintcat-{Guid.NewGuid():N}.tmp.zip");

                try
                {
                    // 先写临时文件，避免直接操作目标 zip 导致失败
                    using (var archive = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(filePath, assetName, CompressionLevel.SmallestSize);
                    }

                    // 覆盖旧 zip；不需要先 File.Delete
                    System.IO.File.Move(tempZipPath, zipPath, overwrite: true);
                }
                catch (Exception exZip)
                {
                    return new ResultEntity<bool> { ResultData = false, ResultMsg = $"下载成功但压缩失败: {exZip.Message}" };
                }
                finally
                {
                    // 清理临时文件
                    if (System.IO.File.Exists(tempZipPath))
                    {
                        try { System.IO.File.Delete(tempZipPath); } catch { }
                    }
                }

                // 下载并打包成功后，将最新版本写入 Redis db4（覆盖）
                if (!string.IsNullOrWhiteSpace(latestVersion))
                {
                    try
                    {
                        await _RedisManageService.SetAsync(redisKey, latestVersion, null, 4);
                    }
                    catch
                    {
                        // 忽略 Redis 写入错误
                    }
                }

                return new ResultEntity<bool> { ResultData = true, ResultMsg = "已下载并打包为 mintcat.zip" };
            }
            catch (Exception ex)
            {
                return new ResultEntity<bool> { ResultData = false, ResultMsg = ex.Message };
            }
        }
    }
}
