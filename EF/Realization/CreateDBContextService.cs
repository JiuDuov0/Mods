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
        private string[] _Conns;
        public IConfiguration _configuration { get; set; }
        public CreateDBContextService(IConfiguration configuration)
        {
            _configuration = configuration;
            _Conns = _configuration["ReadConnectionString"].Split("|");
        }

        public AllContext CreateContext(ReadOrWriteEnum @enum)
        {
            var StrConn = string.Empty;
            switch (@enum)
            {
                case ReadOrWriteEnum.Write:
                    StrConn = _configuration["WriteConnectionString"];
                    break;
                case ReadOrWriteEnum.Read:
                    StrConn = _Conns[new Random().Next(0, _Conns.Length - 1)];
                    break;
                default:
                    break;
            }
            return new AllContext(StrConn);
        }
        /*select * from openrowset( 'SQLOLEDB', '127.0.0.1'; 'sa'; '123','SELECT r.session_id,r.status,r.start_time,r.command,r.sql_handle,t.text as sql_text,s.host_name,s.login_name from sys.dm_exec_requests as r join sys.dm_exec_sessions as s on r.session_id=s.session_id cross apply sys.dm_exec_sql_text(r.sql_handle) as t where r.session_id > 50 
and t.text not like(''%SELECT r.session_id,r.status,r.start_time,r.command%'')')
exec sp_configure 'show advanced options',1
reconfigure
exec sp_configure 'Ad Hoc Distributed Queries',1
reconfigure
         */
    }
}
