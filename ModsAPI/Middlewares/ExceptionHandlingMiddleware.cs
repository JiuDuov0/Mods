using Entity;
using ModsAPI.tools;
using Newtonsoft.Json;
using Redis.Interface;
using System.Net;

namespace ModsAPI.Middlewares
{
    /// <summary>
    /// 全局异常捕获中间件
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IRedisManageService _redisManageService;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IRedisManageService redisManageService)
        {
            _next = next;
            _logger = logger;
            _redisManageService = redisManageService;
        }

        /// <summary>
        /// 执行中间件
        /// </summary>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        /// <summary>
        /// 异步处理异常
        /// </summary>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.TraceIdentifier;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Trace-Id"] = traceId;

            var env = context.RequestServices.GetService<IHostEnvironment>();
            var path = context.Request?.Path.Value;
            var method = context.Request?.Method;

            _logger.LogError(
                exception,
                "Unhandled exception: {Message} | Method:{Method} Path:{Path} TraceId:{TraceId}",
                exception.Message,
                method,
                path,
                traceId);

            await SaveExceptionLogToRedisAsync(context, exception, traceId);

            var response = context.Response;
            var error = new ResultEntity<string>()
            {
                ResultData = string.Empty
            };

            switch (exception)
            {
                case ApplicationException ex when ex.Message.Contains("Invalid token", StringComparison.OrdinalIgnoreCase):
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    error.ResultMsg = ex.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    error.ResultMsg = "未授权的访问。";
                    break;

                case KeyNotFoundException ex:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    error.ResultMsg = string.IsNullOrWhiteSpace(ex.Message) ? "资源不存在。" : ex.Message;
                    break;

                case ArgumentNullException ex:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    error.ResultMsg = $"参数不能为空: {ex.ParamName}";
                    break;

                case ArgumentException ex:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    error.ResultMsg = string.IsNullOrWhiteSpace(ex.Message) ? "参数错误。" : ex.Message;
                    break;

                case JsonReaderException:
                case JsonSerializationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    error.ResultMsg = "JSON 解析/序列化错误。";
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
                    error.ResultMsg = "操作超时。";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    error.ResultMsg = "Internal Server Error.";
                    break;
            }

            error.ResultCode = response.StatusCode;

            if (env?.IsDevelopment() == true)
            {
                error.ResultData = JsonConvert.SerializeObject(new
                {
                    Exception = exception.GetType().Name,
                    Message = exception.Message,
                    StackTrace = exception.StackTrace,
                    TraceId = traceId,
                    Path = path,
                    Method = method
                });
            }
            else
            {
                error.ResultData = JsonConvert.SerializeObject(new
                {
                    TraceId = traceId
                });
            }

            var outputJson = JsonConvert.SerializeObject(error);
            await context.Response.WriteAsync(outputJson);
        }

        private async Task SaveExceptionLogToRedisAsync(HttpContext context, Exception exception, string traceId)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;

                var redisKey = $"ExceptionLog:{DateTime.Now:yyyyMMdd}:{traceId}";
                var redisValue = JsonConvert.SerializeObject(new
                {
                    TraceId = traceId,
                    Request = new
                    {
                        Scheme = request.Scheme,
                        Host = request.Host.Value,
                        Path = request.Path.Value,
                        QueryString = request.QueryString.Value,
                        Method = request.Method,
                        ContentType = request.ContentType,
                        Headers = request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString()),
                        RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = request.Headers.UserAgent.ToString()
                    },
                    Response = new
                    {
                        StatusCode = response.StatusCode,
                        ContentType = response.ContentType
                    },
                    Exception = new
                    {
                        Type = exception.GetType().FullName,
                        Message = exception.Message,
                        Source = exception.Source,
                        StackTrace = exception.StackTrace,
                        InnerException = exception.InnerException == null
                            ? null
                            : new
                            {
                                Type = exception.InnerException.GetType().FullName,
                                Message = exception.InnerException.Message,
                                Source = exception.InnerException.Source,
                                StackTrace = exception.InnerException.StackTrace
                            }
                    },
                    Server = new
                    {
                        MachineName = Environment.MachineName,
                        EnvironmentName = context.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName
                    },
                    CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                await _redisManageService.SetAsync(
                    redisKey,
                    redisValue,
                    TimeSpan.FromDays(7),
                    15);
            }
            catch (Exception redisEx)
            {
                _logger.LogError(
                    redisEx,
                    "将异常日志写入 Redis 失败。TraceId:{TraceId}",
                    traceId);
            }
        }
    }
}