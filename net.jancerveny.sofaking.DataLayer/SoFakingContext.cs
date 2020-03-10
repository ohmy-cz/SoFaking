using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using net.jancerveny.sofaking.DataLayer.Models;
using System.IO;

namespace net.jancerveny.sofaking.DataLayer
{
    public class SoFakingContext : DbContext
    {
        public SoFakingContext(DbContextOptions<SoFakingContext> options) : base(options)
        {
        }
        public DbSet<Movie> Movies { get; set; }

        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //{
        //    var builder = new ConfigurationBuilder();
        //    builder
        //        .SetBasePath(Directory.GetCurrentDirectory())
        //        .AddJsonFile("appsettings.Production.json", optional: false); // TODO: Make this switch depending on the enviroment

        //    var configuration = builder.Build();

        //    options.UseNpgsql(configuration.GetConnectionString("Db"));
        //}
    }
}
