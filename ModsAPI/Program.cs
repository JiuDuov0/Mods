using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ModsAPI.Middlewares;
using ModsAPI.tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Host
.UseServiceProviderFactory(new AutofacServiceProviderFactory())
.ConfigureContainer<ContainerBuilder>(builder =>
{
    builder.RegisterModule(new AutofacModuleRegister());
});
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    //设置JSON返回数据格式大小写与Model一致
    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
    //设置一般API的日期格式
    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(opt =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
    byte[] keyBytes = Encoding.UTF8.GetBytes(jwtSettings.SecrentKey);
    var secKey = new SymmetricSecurityKey(keyBytes);
    opt.TokenValidationParameters = new()
    {
        ValidateIssuer = true, //验证颁发者
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,  // 验证使用者
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true, //是否验证Token有效期，使用当前时间与Token的Claims中的NotBefore和Expires对比
        ValidateIssuerSigningKey = true, //验证秘钥
        IssuerSigningKey = secKey,
        RequireExpirationTime = true,//要求Token的Claims中必须包含Expires
        ClockSkew = TimeSpan.Zero, //允许服务器时间偏移量300秒，即我们配置的过期时间加上这个允许偏移的时间值，才是真正过期的时间(过期时间 + 偏移值)你也可以设置为0，ClockSkew = TimeSpan.Zero
    };

});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(s =>
{
    //添加安全定义
    s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "请输入token,格式为 Bearer xxxxxxxx（注意中间必须有空格）",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    //添加安全要求
    s.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme{
                Reference =new OpenApiReference{
                    Type = ReferenceType.SecurityScheme,
                    Id ="Bearer"
                }
            },new string[]{ }
        }
    });

    s.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Swagger",
        Version = "V1",
        Description = "Swagger测试接口"
    });
    //s.OperationFilter<SwaggerFileOperationFilter>();
    var file = Path.Combine(AppContext.BaseDirectory, "xml.xml");  // xml文档绝对路径
    var path = Path.Combine(AppContext.BaseDirectory, file); // xml文档绝对路径
    s.IncludeXmlComments(path, true); // true : 显示控制器层注释
    s.OrderActionsBy(o => o.RelativePath); // 对action的名称进行排序，如果有多个，就可以看见效果了。
});
builder.Services.AddRateLimiter(_ => _
    .AddConcurrencyLimiter(policyName: "Concurrency", options =>
    {
        options.PermitLimit = Convert.ToInt32(builder.Configuration["PermitLimit"]);//时间窗口内允许的最大请求;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = Convert.ToInt32(builder.Configuration["QueueLimit"]);// 队列中允许的最大请求数
    }));
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
//builder.Services.AddCors(cor =>
//{
//    var cors = builder.Configuration.GetSection("CorsUrls").GetChildren().Select(p => p.Value);
//    cor.AddPolicy("Cors", policy =>
//    {
//        policy.WithOrigins(cors.ToArray())//设置允许的请求头
//        .WithExposedHeaders("x-custom-header")//设置公开的响应头
//        //.SetIsOriginAllowedToAllowWildcardSubdomains()
//        .AllowAnyHeader()//允许所有请求头
//        .AllowAnyMethod()//允许任何方法
//        .AllowCredentials()//允许跨源凭据----服务器必须允许凭据
//        .SetIsOriginAllowed(_ => true)
//        .SetPreflightMaxAge(TimeSpan.FromSeconds(2520));
//    });
//});
builder.Services.AddCors(cor =>
{
    var cors = builder.Configuration.GetSection("CorsUrls").GetChildren().Select(p => p.Value);
    cor.AddPolicy("Cors", policy =>
    {
        policy.WithOrigins(cors.ToArray()) //设置允许的请求头
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyHeader() //允许所有请求头
              .AllowAnyMethod() //允许任何方法
              .AllowCredentials() //允许跨源凭据
              .SetPreflightMaxAge(TimeSpan.FromSeconds(2520));
    });
});

var app = builder.Build();
//Configure the HTTP request pipeline.

//app.UseFileServer(new FileServerOptions()
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot")),
//    RequestPath = new PathString("/wwwroot"),
//    EnableDirectoryBrowsing = true
//});//静态文件访问
//DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
//defaultFilesOptions.DefaultFileNames.Clear();
//defaultFilesOptions.DefaultFileNames.Add("/html/Login/Index.html");
//app.UseDefaultFiles(defaultFilesOptions);
//app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1"));
}

app.UseRateLimiter();
app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Cors");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
