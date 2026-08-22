using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class CafeDbContextFactory : IDesignTimeDbContextFactory<CafeDbContext>
{
    public CafeDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connString = config.GetConnectionString("Default");

        var optionsBuilder = new DbContextOptionsBuilder<CafeDbContext>();
        optionsBuilder.UseSqlServer(connString);

        return new CafeDbContext(optionsBuilder.Options);
    }
}