using Entity.Approve;
using Entity.Mod;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewEntity.Mod;

namespace Service.Interface
{
    public interface IModService
    {
        public List<ModListViewEntity> ModListPage(dynamic json, string UserId);
        public Task<List<ModEntity>?> ModListPageSearch(int Skip, int Take, string Search);
        public void ApproveModVersion(string modVersionId, string approverUserId, string status, string comments);
        public Task ApproveModVersionAsync(string modVersionId, string approverUserId, string status, string comments);
        public bool AddModAndModVersion(ModEntity modEntity, ModVersionEntity modVersionEntity, List<ModTypeEntity>? list, List<ModDependenceEntity>? ModDependenceEntities);
        public bool AddModVersion(ModVersionEntity modVersionEntity);
        public ModVersionEntity GetByModVersionId(string modVersionId);
        public bool AddModTypes(JArray array);
        public List<ApproveModVersionEntity> GetApproveModVersionPageList(int Skip, int Take, string Search);
        public bool IsLoginUserMods(List<string> list, string UserId);
        public bool IsLoginUserMods(string VersionId, string UserId);
        public List<ModEntity> GetMyCreateMod(string UserId, dynamic json);
        public Task<ModEntity> ModDetail(string UserId, string ModId);
        public Task<ModEntity> ModDetailAllModVersion(string UserId, string ModId);
        public ModEntity ModDetailUpd(string UserId, string ModId);
        public bool? UpdateModInfo(ModEntity entity, string UserId);
        public bool? DeleteMod(string ModId, string UserId);
        public bool AddModPoint(ModPointEntity entity);
        public ModPointEntity UpdateModPointEntity(ModPointEntity entity);
        public bool DeleteModPoint(string ModId, string UserId);
        public bool DeleteModPoint(string ModPointId);
        public ModPointEntity? GetModPointEntity(string ModId, string UserId);
    }
}
