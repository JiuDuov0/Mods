using EF;
using EF.Interface;
using Entity.Approve;
using Entity.File;
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
                context.Add(filesEntity);
                context.Add(approveModVersionEntity);
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
    }
}
