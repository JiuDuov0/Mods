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
            catch (Exception ex) when (IsClientDisconnected(ex, httpContext))
            {
                _logger.LogDebug(
                    ex,
                    "客户端已断开连接。TraceId:{TraceId},Path:{Path}",
                    httpContext.TraceIdentifier,
                    httpContext.Request.Path);
            }
            catch (Exception ex)
            {
                // 记录未捕获的异常
                _logger.LogError(
                    ex,
                    "未处理的异常。TraceId:{TraceId},Path:{Path},Method:{Method}",
                    httpContext.TraceIdentifier,
                    httpContext.Request.Path,
                    httpContext.Request.Method);

                await HandleExceptionAsync(httpContext, ex);
            }
        }

        /// <summary>
        /// 判断是否是客户端断开连接
        /// </summary>
        private static bool IsClientDisconnected(
            Exception exception,
            HttpContext context)
        {
            if (context.RequestAborted.IsCancellationRequested)
            {
                return true;
            }

            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is ConnectionResetException)
                {
                    return true;
                }

                if (current is OperationCanceledException
                    && context.RequestAborted.IsCancellationRequested)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 缓存的 JSON 序列化器
        /// </summary>
        private static readonly JsonSerializer CachedJsonSerializer = JsonSerializer.CreateDefault();

        /// <summary>
        /// 异步处理异常
        /// </summary>
        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            // 如果响应已启动或请求已取消，无法写入响应
            if (context.Response.HasStarted
                || context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            var traceId = context.TraceIdentifier;
            var statusCode = GetStatusCode(exception);

            // 异步保存异常日志到 Redis，失败不影响主流程
            try
            {
                await SaveExceptionLogToRedisAsync(context, exception, traceId);
            }
            catch (Exception redisEx)
            {
                _logger.LogError(
                    redisEx,
                    "保存异常日志到 Redis 失败。TraceId:{TraceId}",
                    traceId);
                // 不中断主处理流程
            }

            // 再次检查请求是否已被取消
            if (context.RequestAborted.IsCancellationRequested)
            {
                return;
            }

            // 设置响应信息
            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var response = new
            {
                ResultCode = statusCode,
                ResultData = GetClientMessage(exception, statusCode),
                TraceId = traceId
            };

            try
            {
                await WriteResponseAsync(context, response);
            }
            catch (Exception ex) when (IsClientDisconnected(ex, context))
            {
                _logger.LogDebug(
                    ex,
                    "写入错误响应时客户端已断开。TraceId:{TraceId}",
                    traceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "写入错误响应失败。TraceId:{TraceId}",
                    traceId);
            }
        }

        /// <summary>
        /// 将响应写入到客户端
        /// </summary>
        private async Task WriteResponseAsync(HttpContext context, object response)
        {
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

        /// <summary>
        /// 根据异常类型获取 HTTP 状态码
        /// </summary>
        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                // 认证相关
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,

                // 参数验证相关
                ArgumentNullException => StatusCodes.Status400BadRequest,
                ArgumentException => StatusCodes.Status400BadRequest,
                FormatException => StatusCodes.Status400BadRequest,

                // 资源相关
                KeyNotFoundException => StatusCodes.Status404NotFound,
                FileNotFoundException => StatusCodes.Status404NotFound,

                // 功能相关
                NotImplementedException => StatusCodes.Status501NotImplemented,

                // 连接超时
                ConnectionResetException => StatusCodes.Status408RequestTimeout,
                OperationCanceledException => StatusCodes.Status408RequestTimeout,
                TimeoutException => StatusCodes.Status408RequestTimeout,

                // 业务异常（如果实现了自定义异常）
                InvalidOperationException => StatusCodes.Status400BadRequest,
                NotSupportedException => StatusCodes.Status400BadRequest,

                // 默认服务器错误
                _ => StatusCodes.Status500InternalServerError
            };
        }

        /// <summary>
        /// 获取返回给客户端的错误消息
        /// </summary>
        private static string GetClientMessage(Exception exception, int statusCode)
        {
            return statusCode switch
            {
                // 400 Bad Request
                StatusCodes.Status400BadRequest when exception is ArgumentException or ArgumentNullException or FormatException
                    => exception.Message,
                StatusCodes.Status400BadRequest when exception is InvalidOperationException or NotSupportedException
                    => exception.Message,
                StatusCodes.Status400BadRequest
                    => "请求参数有误。",

                // 401 Unauthorized
                StatusCodes.Status401Unauthorized when exception is UnauthorizedAccessException
                    => exception.Message,
                StatusCodes.Status401Unauthorized
                    => "未授权访问。",

                // 404 Not Found
                StatusCodes.Status404NotFound
                    => "资源不存在。",

                // 408 Request Timeout
                StatusCodes.Status408RequestTimeout
                    => "请求已超时。",

                // 501 Not Implemented
                StatusCodes.Status501NotImplemented
                    => "功能尚未实现。",

                // 500 Internal Server Error
                _ => "服务器内部错误。"
            };
        }

        /// <summary>
        /// 保存异常日志到 Redis
        /// </summary>
        private async Task SaveExceptionLogToRedisAsync(HttpContext context, Exception exception, string traceId)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                var redisKey = $"ExceptionLog:{DateTime.Now:yyyyMMdd}:{traceId}";

                var exceptionInfo = BuildExceptionInfo(exception);

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
                        RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString()
                    },
                    RequestSource = new
                    {
                        Origin = request.Headers.Origin.ToString(),
                        Referer = request.Headers.Referer.ToString(),
                        XForwardedFor = request.Headers["X-Forwarded-For"].ToString(),
                        RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                        UserAgent = request.Headers.UserAgent.ToString()
                    },
                    Response = new
                    {
                        StatusCode = response.StatusCode,
                        ContentType = response.ContentType
                    },
                    Exception = exceptionInfo,
                    Server = new
                    {
                        MachineName = Environment.MachineName,
                        EnvironmentName = context.RequestServices.GetService<IHostEnvironment>()?.EnvironmentName
                    },
                    CreatedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });

                // 保存到 Redis，7 天过期，出错重试 3 次
                await _redisManageService.SetAsync(
                    redisKey,
                    redisValue,
                    TimeSpan.FromDays(7),
                    3);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "将异常日志写入 Redis 失败。TraceId:{TraceId}",
                    traceId);
                throw;
            }
        }

        /// <summary>
        /// 构建完整的异常信息（递归处理所有 InnerException）
        /// </summary>
        private static object BuildExceptionInfo(Exception exception)
        {
            var exceptionInfo = new
            {
                Type = exception.GetType().FullName,
                Message = exception.Message,
                Source = exception.Source,
                StackTrace = exception.StackTrace,
                HelpLink = exception.HelpLink,
                InnerException = exception.InnerException != null
                    ? BuildExceptionInfo(exception.InnerException)
                    : null
            };

            return exceptionInfo;
        }
    }
}