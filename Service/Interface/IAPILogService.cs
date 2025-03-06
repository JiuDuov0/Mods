using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IAPILogService
    {
        public Task WriteLogAsync(string API, string UserId, string IP);
    }
}
