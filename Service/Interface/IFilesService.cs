using Entity.Approve;
using Entity.File;
using Entity.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IFilesService
    {
        public bool AddFilesAndApprove(FilesEntity filesEntity,ApproveModVersionEntity approveModVersionEntity);
        public FilesEntity GetFilesEntityById(string FileId);
        public ModEntity AddModDownLoadCount(string FileId);
    }
}
