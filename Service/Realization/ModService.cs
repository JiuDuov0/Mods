using Entity.Mod;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class ModService : IModService
    {
        public List<ModEntity> ModListPage(dynamic json)
        {
            int pageNumber = json.pageNumber;
            int pageSize = json.pageSize;
            string searchTerm = json.searchTerm;


        }
    }
}
