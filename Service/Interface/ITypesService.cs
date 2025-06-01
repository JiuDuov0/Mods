using Entity.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ITypesService
    {
        public Task<List<TypesEntity>?> GetTypesListAsync(string GameId);
    }
}
