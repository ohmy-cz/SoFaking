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
    }
}
