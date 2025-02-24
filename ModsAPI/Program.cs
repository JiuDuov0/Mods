using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ModsAPI.Middlewares;
using ModsAPI.tools;
using Newtonsoft.Json.Serialization;
using System.Text;

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
    //����JSON�������ݸ�ʽ��Сд��Modelһ��
    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
    //����һ��API�����ڸ�ʽ
    options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
        ValidateIssuer = true, //��֤�䷢��
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,  // ��֤ʹ����
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true, //�Ƿ���֤Token��Ч�ڣ�ʹ�õ�ǰʱ����Token��Claims�е�NotBefore��Expires�Ա�
        ValidateIssuerSigningKey = true, //��֤��Կ
        IssuerSigningKey = secKey,
        RequireExpirationTime = true,//Ҫ��Token��Claims�б������Expires
        ClockSkew = TimeSpan.Zero, //���������ʱ��ƫ����300�룬���������õĹ���ʱ������������ƫ�Ƶ�ʱ��ֵ�������������ڵ�ʱ��(����ʱ�� + ƫ��ֵ)��Ҳ��������Ϊ0��ClockSkew = TimeSpan.Zero
    };

});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(s =>
{
    //��Ӱ�ȫ����
    s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "������token,��ʽΪ Bearer xxxxxxxx��ע���м�����пո�",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    //��Ӱ�ȫҪ��
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
        Description = "Swagger���Խӿ�"
    });
    var file = Path.Combine(AppContext.BaseDirectory, "xml.xml");  // xml�ĵ�����·��
    var path = Path.Combine(AppContext.BaseDirectory, file); // xml�ĵ�����·��
    s.IncludeXmlComments(path, true); // true : ��ʾ��������ע��
    s.OrderActionsBy(o => o.RelativePath); // ��action�����ƽ�����������ж�����Ϳ��Կ���Ч���ˡ�
});

var app = builder.Build();
// Configure the HTTP request pipeline.
//app.UseFileServer(new FileServerOptions()
//{
//    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot")),
//    RequestPath = new PathString("/wwwroot"),
//    EnableDirectoryBrowsing = true
//});//��̬�ļ�����
//DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
//defaultFilesOptions.DefaultFileNames.Clear();
//defaultFilesOptions.DefaultFileNames.Add("/html/Login/Index.html");
//app.UseDefaultFiles(defaultFilesOptions);
//app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
