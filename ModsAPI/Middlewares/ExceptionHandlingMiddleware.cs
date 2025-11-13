using Entity;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;

namespace ModsAPI.Middlewares
{
    /// <summary>
    /// 全局异常捕获中间件
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;  // 用来处理上下文请求
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
        /// 异步处理异常（完善版）
        /// </summary>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var traceId = context.TraceIdentifier;
            context.Response.ContentType = "application/json";
            context.Response.Headers["X-Trace-Id"] = traceId;

            var env = context.RequestServices.GetService<IHostEnvironment>();
            var path = context.Request?.Path.Value;
            var method = context.Request?.Method;

            // 记录结构化日志
            _logger.LogError(exception,
                "Unhandled exception: {Message} | Method:{Method} Path:{Path} TraceId:{TraceId}",
                exception.Message, method, path, traceId);

            var response = context.Response;
            var error = new ResultEntity<string>()
            {
                ResultData = string.Empty
            };

            // 根据异常类型设置 HTTP 状态码与业务消息
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

            // 在开发环境返回更多调试信息（避免生产环境泄露细节）
            if (env?.IsDevelopment() == true)
            {
                error.ResultData = JsonConvert.SerializeObject(new
                {
                    exception = exception.GetType().Name,
                    message = exception.Message,
                    stackTrace = exception.StackTrace,
                    traceId,
                    path,
                    method
                });
            }
            else
            {
                // 生产环境仅附加必要追踪标识
                error.ResultData = JsonConvert.SerializeObject(new
                {
                    traceId
                });
            }

            var outputJson = JsonConvert.SerializeObject(error);
            await context.Response.WriteAsync(outputJson);
        }
    }
}