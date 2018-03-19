using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Collections.Generic;

namespace Mimic.Common
{
    public class AuthDatabase : DbContext
    {

        public DbSet<AccountInfo> Accounts { get; set; }
        public DbSet<AccountTutorialFlags> Account_Tutorial { get; set; }
        public DbSet<AccountData> Account_Data { get; set; }

        public AuthDatabase(DbContextOptions opt) : base(opt) {
            
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AccountInfo>(e=>{
                e.HasIndex(u => u.username).IsUnique();
                e.HasIndex(u => u.email).IsUnique();
                e.Property(u => u.join_date).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(u => u.last_login).HasDefaultValueSql("CURRENT_TIMESTAMP");
                e.Property(u => u.email).HasDefaultValue("");
                e.Property(u => u.last_ip).HasDefaultValue("");
                e.Property(u => u.lock_country).HasDefaultValue("");
                e.Property(u => u.os).HasDefaultValue("");
                e.Property(u => u.s).HasDefaultValue("");
                e.Property(u => u.v).HasDefaultValue("");
                e.Property(u => u.sessionkey).HasDefaultValue("");
                e.Property(u => u.token_key).HasDefaultValue("");
                e.Property(u => u.locked).HasDefaultValue(false);
                e.Property(u => u.online).HasDefaultValue(false);
                e.ToTable("Accounts");
                
            });
            modelBuilder.Entity<AccountTutorialFlags>().ToTable("Account_Tutorial");
            modelBuilder.Entity<AccountData>(e=>{
                e.HasKey(p=>new {p.account,p.type});
                e.Property(p=>p.data).HasColumnType("blob");
                e.ToTable("Account_Data");
            });
        }
    }
}
