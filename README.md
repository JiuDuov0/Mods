# .NET 8.0 + Vue

## IDE Setup

- [VS Code](https://code.visualstudio.com/) + [Visual Studio](https://visualstudio.microsoft.com/)

## appsettings.json
```
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "CorsUrls": [
    "http://localhost:5173",
    "http://*:5173"
  ],
  "AllowedHosts": "*",
  ".": null,
  "Ip": "",//redis ip
  "Port": "",//redis port
  "Password": "",//redis
  "Timeout": "30",//redis
  "DB": "0",//redis
  "URL": "", //前端地址
  "Window": 60, //时间窗口（秒
  "PermitLimit": 50, //时间窗口内允许的最大请求
  "QueueLimit": 50, //列中允许的最大请求数
  "FilePath": "",//文件路径
  "WriteConnectionString": "Data Source=43.160.202.17;Initial Catalog=Mods;User ID=sa;Password=JiuDuo0928@.;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False",
  "ReadConnectionString": "Data Source={sqlserverip};Initial Catalog=Mods;User ID={user};Password={password};Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False|Data Source={sqlserverip};Initial Catalog=Mods;User ID={user};Password={password};Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False",
  "JwtSettings": {
    "Issuer": "", //颁发者
    "Audience": "", //使用者
    "SecrentKey": "", //秘钥
    "Expirces": 3600, //Token过期时间
    "RefreshTokenExpirces": 86400 //refresh_Token过期时间
  }
}

```
## 前端位置

```
\ModsAPI\wwwroot\vue\Mod
```
