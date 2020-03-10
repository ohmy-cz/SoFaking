using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace net.jancerveny.sofaking.DataLayer
{
    public class SoFakingContextFactory : IDesignTimeDbContextFactory<SoFakingContext>
    {
        private static string _connectionString;

        public SoFakingContext CreateDbContext()
        {
            return CreateDbContext(null);
        }

        public SoFakingContext CreateDbContext(string[] args)
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                LoadConnectionString();
            }

            var builder = new DbContextOptionsBuilder<SoFakingContext>();
            builder.UseNpgsql(_connectionString);

            return new SoFakingContext(builder.Options);
        }

        private static void LoadConnectionString()
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("dbsettings.Production.json", optional: false);

            var configuration = builder.Build();

            _connectionString = configuration.GetConnectionString("Db");
        }
    }
}
