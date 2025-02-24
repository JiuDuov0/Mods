using Entity;
using Newtonsoft.Json;
using System.Net;

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
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext); //要么在中间件中处理，要么被传递到下一个中间件中去
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(httpContext, ex); // 捕获异常了 在HandleExceptionAsync中处理
            }
        }

        /// <summary>
        /// 异步处理异常
        /// </summary>
        /// <param name="context"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";  // 返回json 类型
            var response = context.Response;
            var errorResponse = new ResultEntity<string>()
            {
                ResultCode = 500,
                ResultData = string.Empty
            };  // 自定义的异常错误信息类型
            switch (exception)
            {
                case ApplicationException ex:
                    if (ex.Message.Contains("Invalid token"))
                    {
                        response.StatusCode = (int)HttpStatusCode.Forbidden;
                        errorResponse.ResultMsg = ex.Message;
                        break;
                    }
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.ResultMsg = ex.Message;
                    break;
                case KeyNotFoundException ex:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.ResultMsg = ex.Message;
                    break;
                case JsonReaderException ex:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.ResultMsg = "JSON 格式不正确";
                    break;
                case JsonSerializationException ex:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.ResultMsg = "JSON 字符串中的数据类型与目标对象的属性类型不匹配";
                    break;
                case ArgumentNullException ex:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.ResultMsg = "JSON为空";
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.ResultMsg = "Internal Server errors." + exception.Message;
                    break;
            }
            await context.Response.WriteAsync(JsonConvert.SerializeObject(errorResponse));
        }
    }
}
