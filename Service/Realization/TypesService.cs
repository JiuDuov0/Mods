using EF;
using EF.Interface;
using Entity.Type;
using Microsoft.EntityFrameworkCore;
using Redis.Interface;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class TypesService : ITypesService
    {
        private readonly ICreateDBContextService _IDbContextServices;
        private readonly IRedisManageService _IRedisManageService;

        public TypesService(ICreateDBContextService iDbContextServices, IRedisManageService iRedisManageService)
        {
            _IDbContextServices = iDbContextServices;
            _IRedisManageService = iRedisManageService;
        }

        public async Task<List<TypesEntity>?> GetTypesListAsync(string GameId)
        {
            var list = await _IRedisManageService.GetAsync<List<TypesEntity>>($"TypesList:{GameId}", 1);
            if (list == null)
            {
                var ReadContext = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);
                list = await ReadContext.TypesEntity.Where(x => x.GameId == GameId).ToListAsync();
                await ReadContext.DisposeAsync();
                await _IRedisManageService.SetAsync($"TypesList:{GameId}", list, new TimeSpan(24, 0, 0), 1);
            }
            //GC.Collect();
            return list;
        }
    }
}
