using Microsoft.EntityFrameworkCore;
using TechAppFor.Models;

namespace TechAppFor
{


    public class ApplicationContext : DbContext
    {
        public DbSet<ResultOfCountingCharacters> ResultOfCountingCharacters { get; set; }
        public ApplicationContext()
        {
            
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetValue<String>("ConnectionString"));
        }
    }
}
