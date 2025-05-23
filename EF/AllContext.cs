﻿using Entity.Approve;
using Entity.File;
using Entity.Game;
using Entity.Mod;
using Entity.Type;
using Entity.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EF
{
    public class AllContext : DbContext
    {
        public static readonly ILoggerFactory MyLogFactory = LoggerFactory.Create(build =>
        {
            build.AddConsole();  // 用于控制台程序的输出
            build.AddDebug();    // 用于VS调试，输出窗口的输出
        });
        private string _strConn;
        public AllContext(string Conn)
        {
            //连接字符串
            _strConn = Conn;
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLoggerFactory(MyLogFactory);
            optionsBuilder.UseSqlServer(_strConn, x => { x.CommandTimeout(120); });
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>().HasKey(x => x.UserId);
            modelBuilder.Entity<ModEntity>().HasQueryFilter(p => !p.SoftDeleted);
            modelBuilder.Entity<FilesEntity>().HasQueryFilter(p => !p.SoftDeleted);
        }
        public DbSet<UserEntity> UserEntity { get; set; }
        public DbSet<UserRoleEntity> UserRoleEntity { get; set; }
        public DbSet<TypesEntity> TypesEntity { get; set; }
        public DbSet<ModEntity> ModEntity { get; set; }
        public DbSet<ModPictureEntity> ModPictureEntity { get; set; }
        public DbSet<ModTypeEntity> ModTypeEntity { get; set; }
        public DbSet<ModVersionEntity> ModVersionEntity { get; set; }
        public DbSet<UserModSubscribeEntity> UserModSubscribeEntity { get; set; }
        public DbSet<ApproveModVersionEntity> ApproveModEntity { get; set; }
        public DbSet<FilesEntity> FilesEntity { get; set; }
        public DbSet<ModPointEntity> ModPointEntity { get; set; }
        public DbSet<ModDependenceEntity> ModDependenceEntity { get; set; }
        public DbSet<GameEntity> GameEntity { get; set; }
    }
}
