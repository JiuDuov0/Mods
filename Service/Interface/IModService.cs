using Entity.Approve;
using Entity.Mod;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IModService
    {
        public List<ModEntity> ModListPage(dynamic json, string UserId);
        public void ApproveModVersion(string modVersionId, string approverUserId, string status, string comments);
        public Task ApproveModVersionAsync(string modVersionId, string approverUserId, string status, string comments);
        public bool AddModAndModVersion(ModEntity modEntity, ModVersionEntity modVersionEntity, List<ModTypeEntity> list);
        public bool AddModVersion(ModVersionEntity modVersionEntity);
        public ModVersionEntity GetByModVersionId(string modVersionId);
        public bool AddModTypes(JArray array);
        public List<ApproveModVersionEntity> GetApproveModVersionPageList(int Skip, int Take);
        public bool IsLoginUserMods(List<string> list, string UserId);
        public bool IsLoginUserMods(string VersionId, string UserId);
        public List<ModEntity> GetMyCreateMod(string UserId, dynamic json);
        public ModEntity ModDetail(string UserId, string ModId);
        public ModEntity ModDetailUpd(string UserId, string ModId);
    }
}
