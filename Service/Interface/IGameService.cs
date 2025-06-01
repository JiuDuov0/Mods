using Entity.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IGameService
    {
        public Task<List<GameEntity>?> GamePageListAsync(int Skip,int Take);
        public Task<bool> AddGameEntityAsync(GameEntity entity);
        public Task<bool> UpdateGameEntityAsync(GameEntity entity);
        public Task<bool> RemoveGameEntityAsync(string gameId);
    }
}
