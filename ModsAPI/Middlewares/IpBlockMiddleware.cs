using Redis.Interface;

namespace ModsAPI.Middlewares
{
    public class IpBlockMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IRedisManageService _redisManageService;

        public IpBlockMiddleware(RequestDelegate next, IRedisManageService redisManageService)
        {
            _next = next;
            _redisManageService = redisManageService;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var currentIp = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(currentIp))
            {
                await _next(context);
                return;
            }

            if (_redisManageService.KeyExists($"BlockIp:{currentIp}", 14))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("""{"ResultCode":403,"ResultMsg":"当前IP已被拦截,详细咨询群827532190"}""");
                return;
            }

            await _next(context);
        }
    }
}