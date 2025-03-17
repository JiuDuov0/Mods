using EF;
using EF.Interface;
using Entity.Approve;
using Entity.File;
using Entity.Mod;
using Microsoft.EntityFrameworkCore;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class FilesService : IFilesService
    {
        private readonly ICreateDBContextService _IDbContextServices;

        public FilesService(ICreateDBContextService iDbContextServices)
        {
            _IDbContextServices = iDbContextServices;
        }

        public bool AddFilesAndApprove(FilesEntity filesEntity, ApproveModVersionEntity approveModVersionEntity)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var transaction = context.Database.BeginTransaction();
            try
            {
                var ModVersionEntity = context.ModVersionEntity.FirstOrDefault(x => x.VersionId == approveModVersionEntity.VersionId);
                ModVersionEntity.FilesId = filesEntity.FilesId;
                context.Update(ModVersionEntity);
                context.AddAsync(filesEntity);
                context.AddAsync(approveModVersionEntity);
                context.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }

        public FilesEntity GetFilesEntityById(string FileId)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).FilesEntity.FirstOrDefault(x => x.FilesId == FileId);
        }

        public ModEntity AddModDownLoadCount(string FileId)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var modVersion = context.ModVersionEntity.FirstOrDefault(x => x.FilesId == FileId);
            var mod = context.ModEntity.FirstOrDefault(x => x.ModId == modVersion.ModId);
            mod.DownloadCount = mod.DownloadCount + 1;
            context.ModEntity.Update(mod);
            if (context.SaveChanges() > 0)
            {
                mod.ModVersionEntities = new List<ModVersionEntity>() { modVersion };
                return mod;
            }
            return null;
        }
    }
}
