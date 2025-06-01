using Entity.Mod;
using Entity.Type;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.Game
{
    [Table("Game")]
    public class GameEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the game.
        /// </summary>
        [Key]
        public string? GameId { get; set; }

        /// <summary>
        /// 游戏名称
        /// </summary>
        public string? GameName { get; set; }

        /// <summary>
        /// 图片
        /// </summary>
        public string? Picture { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 游戏Mod
        /// </summary>
        public List<ModEntity> ModEntities { get; set; }

        /// <summary>
        /// 游戏下所有的Mod类型
        /// </summary>
        public List<TypesEntity> TypesEntities { get; set; }

        /// <summary>
        /// 下载次数
        /// </summary>
        [NotMapped]
        public int DownLoadCount { get; set; }

        /// <summary>
        /// 总订阅数
        /// </summary>
        [NotMapped]
        public int SubscribeCount { get; set; }
    }
}
