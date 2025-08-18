using Entity.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IAPILogService
    {
        public ValueTask WriteLogAsync(string API, string UserId, string IP);
        public Task<List<APILogEntity>> GetLoginLogsAsync(DateTime start, DateTime end);
        public Task<Dictionary<string, int>> GetLostUsersAsync(int days);
    }
}
