using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {            
            base.OnModelCreating(builder);
        }

        public DbSet<Tb_UserInfo> Tb_UserInfos { get; set; }
        public DbSet<Tb_UserActivities> Tb_UserActivities { get; set; }

        
    }
}
