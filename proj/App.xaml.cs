using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Cafe.Services;
using CafeOrders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CafeOrders
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();
            var connString = config.GetConnectionString("Default");

            var services = new ServiceCollection();

            services.AddDbContextFactory<CafeDbContext>(options =>
                options.UseSqlServer(connString)
                    .EnableSensitiveDataLogging()
                    .LogTo(msg => System.Diagnostics.Debug.WriteLine(msg), LogLevel.Information)
                    .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information));

            services.AddSingleton<CafeService>();
            services.AddTransient<MainViewModel>();

            Services = services.BuildServiceProvider();

            // применяем миграции при старте
            using var scope = Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CafeDbContext>>();
            using var db = factory.CreateDbContext();
            db.Database.Migrate();

            var main = new MainWindow { DataContext = Services.GetRequiredService<MainViewModel>() };
            main.Show();
        }
    }
}