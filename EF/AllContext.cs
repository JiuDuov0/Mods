using Entity.Mod;
using Entity.Tag;
using Entity.Type;
using Entity.User;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EF
{
    public class AllContext : DbContext
    {
        private string _strConn;
        public AllContext(string Conn)
        {
            //连接字符串
            _strConn = Conn;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_strConn);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>().HasKey(x => x.UserId);
        }
        public DbSet<UserEntity> UserEntity { get; set; }
        public DbSet<UserRoleEntity> UserRoleEntity { get; set; }
        public DbSet<TypesEntity> TypesEntity { get; set; }
        public DbSet<TagEntity> TagEntity { get; set; }
        public DbSet<ModEntity> ModEntity { get; set; }
        public DbSet<ModPictureEntity> ModPictureEntity { get; set; }
        public DbSet<ModTagsEntity> ModTagsEntity { get; set; }
        public DbSet<ModTypeEntity> ModTypeEntity { get; set; }
        public DbSet<ModVersionEntity> ModVersionEntity { get; set; }
    }
}
