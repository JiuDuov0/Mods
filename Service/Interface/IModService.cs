using Entity.Approve;
using Entity.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IModService
    {
        public List<ModEntity> ModListPage(dynamic json);
        public void ApproveModVersion(string modVersionId, string approverUserId, string status, string comments);
        public Task ApproveModVersionAsync(string modVersionId, string approverUserId, string status, string comments);
    }
}
