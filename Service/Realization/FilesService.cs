using EF;
using EF.Interface;
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

        public bool AddFiles(FilesEntity filesEntity)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            context.Add(filesEntity);
            return context.SaveChanges() > 0;
        }
    }
}
