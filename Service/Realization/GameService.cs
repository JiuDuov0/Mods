using EF;
using EF.Interface;
using Entity.Game;
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
    public class GameService : IGameService
    {
        private readonly ICreateDBContextService _IDbContextServices;
        private readonly IRedisManageService _IRedisManageService;

        public GameService(ICreateDBContextService iDbContextServices, IRedisManageService iRedisManageService)
        {
            _IDbContextServices = iDbContextServices;
            _IRedisManageService = iRedisManageService;
        }

        public async Task<List<GameEntity>?> GamePageListAsync(int Skip, int Take)
        {
            var list = await _IRedisManageService.GetAsync<List<GameEntity>>($"GamePageList", 1);
            if (list == null)
            {
                var ReadContext = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read);
                list = await ReadContext.GameEntity.ToListAsync();
                foreach (var item in list)
                {
                    item.SubscribeCount = await ReadContext.UserModSubscribeEntity.Include(x => x.Mod).ThenInclude(x => x.GameEntity).Where(x => x.Mod.GameEntity.GameId == item.GameId).CountAsync();
                    item.DownLoadCount = Convert.ToInt32(await ReadContext.ModEntity.Include(x => x.GameEntity).Where(x => x.GameId == item.GameId).SumAsync(x => x.DownloadCount ?? 0));
                }
                await ReadContext.DisposeAsync();
                await _IRedisManageService.SetAsync("GamePageList", list, new TimeSpan(24, 0, 0), 1);
            }
            list = [.. list.Skip(Skip).Take(Take)];
            //GC.Collect();
            return list;
        }

        public async Task<bool> AddGameEntityAsync(GameEntity entity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> RemoveGameEntityAsync(string gameId)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateGameEntityAsync(GameEntity entity)
        {
            throw new NotImplementedException();
        }
    }
}
