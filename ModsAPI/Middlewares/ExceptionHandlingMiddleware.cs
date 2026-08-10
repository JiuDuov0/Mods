using Entity;
using Microsoft.AspNetCore.Connections;
using ModsAPI.tools;
using Newtonsoft.Json;
using Redis.Interface;
using System.Net;
using System.Text;

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

        private static readonly JsonSerializer CachedJsonSerializer = JsonSerializer.CreateDefault();

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            var traceId = context.TraceIdentifier;
            var statusCode = GetStatusCode(exception);

            try
            {
                await SaveExceptionLogToRedisAsync(context, exception, traceId);
            }
            catch
            {
                // 记录异常日志失败时，不再抛出，避免覆盖原始异常处理流程
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new
            {
                ResultCode = statusCode,
                ResultData = GetClientMessage(exception, statusCode),
                TraceId = traceId
            };

            await using var writer = new StreamWriter(
                context.Response.Body,
                new UTF8Encoding(false),
                bufferSize: 1024,
                leaveOpen: true);

            using var jsonWriter = new JsonTextWriter(writer)
            {
                CloseOutput = false,
                AutoCompleteOnClose = true
            };

            CachedJsonSerializer.Serialize(jsonWriter, response);
            await jsonWriter.FlushAsync(context.RequestAborted);
            await writer.FlushAsync(context.RequestAborted);
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ArgumentException => StatusCodes.Status400BadRequest,
                FormatException => StatusCodes.Status400BadRequest,
                KeyNotFoundException => StatusCodes.Status404NotFound,
                NotImplementedException => StatusCodes.Status501NotImplemented,
                ConnectionResetException => StatusCodes.Status408RequestTimeout,
                OperationCanceledException => StatusCodes.Status408RequestTimeout,
                _ => StatusCodes.Status500InternalServerError
            };
        }

        private static string GetClientMessage(Exception exception, int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => exception.Message,
                StatusCodes.Status401Unauthorized when exception is UnauthorizedAccessException =>
                    exception.Message,
                StatusCodes.Status401Unauthorized => "未授权访问。",
                StatusCodes.Status404NotFound => "资源不存在。",
                StatusCodes.Status408RequestTimeout => "请求已超时。",
                StatusCodes.Status501NotImplemented => "功能尚未实现。",
                _ => "服务器内部错误。"
            };
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
                        RequestSource = new
                        {
                            Origin = request.Headers.Origin.ToString(),
                            Referer = request.Headers.Referer.ToString(),
                            XForwardedFor = request.Headers["X-Forwarded-For"].ToString(),
                            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
                        },
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