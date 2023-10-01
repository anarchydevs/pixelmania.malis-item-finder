using MalisItemFinder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace EFDataAccessLibrary.Models
{
    public class SqliteContext : DbContext
    {
        public DbSet<CharacterInventory> Inventories { get; set; }
      
        public DbSet<ItemContainer> ItemContainers { get; set; }
      
        public DbSet<ItemInfo> ItemInfos { get; set; }
     
        public DbSet<Slot> Slots { get; set; }

        private string _path;

        public SqliteContext(string dbPath)
        {
            _path = dbPath; 
            
            var serviceProvider = new ServiceCollection()
             .AddTransient<SqliteContext>()
             .BuildServiceProvider();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseSqlite($"Data Source={_path}", option =>
            {
            });      
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}