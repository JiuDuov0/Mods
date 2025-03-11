using EF;
using EF.Interface;
using Entity.Approve;
using Entity.Mod;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Service.Realization
{
    public class ModService : IModService
    {
        private readonly ICreateDBContextService _IDbContextServices;

        public ModService(ICreateDBContextService iDbContextServices)
        {
            _IDbContextServices = iDbContextServices;
        }

        public List<ModEntity> ModListPage(dynamic json)
        {
            int Skip = Convert.ToInt32(json.Skip);
            int Take = Convert.ToInt32(json.Take);
            var Types = ((JArray)json.Types).ToObject<List<string>>();//Newtonsoft.Json纯纯的勾失
            Types.RemoveAll(x => x == null || x == "");
            IQueryable<ModEntity> Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModTypeEntities).Include(x => x.ModVersionEntities);
            #region 条件
            if (!string.IsNullOrWhiteSpace((string)json.Select))
            {
                string Select = json.Select;
                Context = Context.Where(x => x.Name.Contains(Select));
            }
            if (Types.Count > 0)
            {
                foreach (var item in Types)
                {
                    Context = Context.Where(x => x.ModTypeEntities.Any(y => y.TypesId == item));
                }
            }
            Context = Context.Where(x =>
            (x.ModVersionEntities.Any(y => y.ApproveModVersionEntity.Any(z => z.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())) ||
            x.ModVersionEntities.Any(y => y.Status == ((int)ApproveModVersionStatusEnum.Approved).ToString())));
            #endregion
            return Context.OrderBy(x => x.DownLoadCount).Skip(Skip).Take(Take).ToList();
        }

        public void ApproveModVersion(string modVersionId, string approverUserId, string status, string comments)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var approval = new ApproveModVersionEntity
            {
                ApproveModVersionId = Guid.NewGuid().ToString(),
                VersionId = modVersionId,
                ApproverUserId = approverUserId,
                ApprovedAt = DateTime.Now,
                Status = status,
                Comments = comments
            };
            context.ApproveModEntity.Add(approval);
            context.SaveChanges();
        }
        public async Task ApproveModVersionAsync(string modVersionId, string approverUserId, string status, string comments)
        {
            var context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var approval = new ApproveModVersionEntity
            {
                ApproveModVersionId = Guid.NewGuid().ToString(),
                VersionId = modVersionId,
                ApproverUserId = approverUserId,
                ApprovedAt = DateTime.Now,
                Status = status,
                Comments = comments
            };
            await context.ApproveModEntity.AddAsync(approval);
            await context.SaveChangesAsync();
        }

        public bool AddModAndModVersion(ModEntity modEntity, ModVersionEntity modVersionEntity)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var transaction = Context.Database.BeginTransaction();
            try
            {
                Context.ModEntity.Add(modEntity);
                Context.ModVersionEntity.Add(modVersionEntity);
                Context.SaveChanges();
                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }

        public bool AddModVersion(ModVersionEntity modVersionEntity)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            Context.Add(modVersionEntity);
            if (Context.SaveChanges() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public ModVersionEntity GetByModVersionId(string modVersionId)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModVersionEntity.FirstOrDefault(x => x.VersionId == modVersionId);
        }

        public bool AddModTypes(JArray array)
        {
            var Context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Write);
            var Transaction = Context.Database.BeginTransaction();
            try
            {
                foreach (JObject item in array)
                {
                    Context.Add(new ModTypeEntity()
                    {
                        ModTypeId = Guid.NewGuid().ToString(),
                        ModId = item["ModId"].ToString(),
                        TypesId = item["TypesId"].ToString()
                    });
                }
                Context.SaveChanges();
                Transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                Transaction.Rollback();
                return false;
            }
        }

        public List<ApproveModVersionEntity> GetApproveModVersionPageList(int Skip, int Take)
        {
            return _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ApproveModEntity.Include(x => x.ModVersion).Where(x => x.Status == "0").Skip(Skip).Take(Take).ToList();
        }

        public bool IsLoginUserMods(List<string> list, string UserId)
        {
            IQueryable<ModEntity> context = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity;
            foreach (var item in list)
            {
                context = context.Where(x => x.ModId == item);
            }
            var Mods = context.ToList();
            List<ModEntity> querylist = Mods.FindAll(x => x.CreatorUserId != UserId).ToList();
            if (querylist == null || querylist.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsLoginUserMods(string VersionId, string UserId)
        {
            var entity = _IDbContextServices.CreateContext(ReadOrWriteEnum.Read).ModEntity.Include(x => x.ModVersionEntities).Where(x => x.ModVersionEntities.Any(y => y.VersionId == VersionId)).Where(x => x.CreatorUserId == UserId).FirstOrDefault();
            return entity == null;
        }
    }
}
