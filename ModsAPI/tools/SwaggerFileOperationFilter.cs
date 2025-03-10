using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ModsAPI.tools
{
    /// <summary>
    /// SwaggerFileOperationFilter 用于在 Swagger 文档中添加文件上传参数。
    /// </summary>
    public class SwaggerFileOperationFilter : IOperationFilter
    {
        /// <summary>
        /// 应用文件上传参数到 Swagger 操作。
        /// </summary>
        /// <param name="operation">Swagger 操作。</param>
        /// <param name="context">操作过滤器上下文。</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile));
            operation.Parameters.Clear();
            foreach (var fileParameter in fileParameters)
            {
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = fileParameter.Name,
                    In = ParameterLocation.Header,
                    Description = "File to upload",
                    Schema = new OpenApiSchema
                    {
                        Type = "file",
                        Format = "binary"
                    }
                });
            }
        }
    }
}
