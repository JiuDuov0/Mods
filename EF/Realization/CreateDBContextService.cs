using EF.Interface;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EF.Realization
{
    public class CreateDBContextService : ICreateDBContextService
    {
        private readonly string[] _Conns;
        private readonly IConfiguration _configuration;

        public CreateDBContextService(IConfiguration configuration)
        {
            _configuration = configuration;

            _Conns = (_configuration["ReadConnectionString"] ?? string.Empty)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public AllContext CreateContext(ReadOrWriteEnum @enum)
        {
            string strConn = @enum switch
            {
                ReadOrWriteEnum.Write => _configuration["WriteConnectionString"] ?? string.Empty,
                ReadOrWriteEnum.Read => _Conns.Length > 0
                    ? _Conns[Random.Shared.Next(_Conns.Length)]
                    : _configuration["WriteConnectionString"] ?? string.Empty,
                _ => _configuration["WriteConnectionString"] ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(strConn))
            {
                throw new InvalidOperationException("数据库连接字符串未配置。");
            }

            return new AllContext(strConn);
        }
    }
}
