using Entity.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface IFilesService
    {
        public bool AddFiles(FilesEntity filesEntity);
        public FilesEntity GetFilesEntityById(string FileId);
    }
}
