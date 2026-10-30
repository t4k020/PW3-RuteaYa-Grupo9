using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RuteaYa.Repositorios
{
    public class RuteaYaDbContextFactory : IDesignTimeDbContextFactory<RuteaYaDbContext>
    {
        public RuteaYaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<RuteaYaDbContext>();

            
            optionsBuilder.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=RuteaYaDb;Trusted_Connection=True;TrustServerCertificate=True;");

            return new RuteaYaDbContext(optionsBuilder.Options);
        }
    }
}