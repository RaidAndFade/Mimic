using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Mimic.Common
{
    public class WorldDatabase : DbContext
    {
        
        public DbSet<Character> Characters { get; set; }
        public WorldDatabase(DbContextOptions opt) : base(opt) {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder){}
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Character>(e=>{
                e.HasAlternateKey(i => i.account);
                e.HasAlternateKey(i => i.name);
                e.HasAlternateKey(i => i.online);
                e.ToTable("Characters");
            });
        }
    }
}
