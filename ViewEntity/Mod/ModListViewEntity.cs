using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewEntity.Mod
{
    public class ModListViewEntity
    {
        /// <summary>
        /// Mod的唯一标识符
        /// </summary>
        public string? ModId { get; set; }

        /// <summary>
        /// Mod的名称
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// 图片地址
        /// </summary>
        public string? PicUrl { get; set; }

        /// <summary>
        /// 导航属性，指向Mod的类型
        /// </summary>
        public List<ModTypesListViewEntity> ModTypeEntities { get; set; }

        /// <summary>
        /// 是否订阅了本mod
        /// </summary>
        public bool? IsMySubscribe { get; set; }
    }
}
