using System.ComponentModel.DataAnnotations;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

var serviceCollection = new ServiceCollection();

var serilogLogger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

serviceCollection.AddLogging(opt => opt
    .AddSerilog(serilogLogger)
);

serviceCollection.AddDbContext<TestDbContext>(options =>
{
    options.UseMySql("<connection string>",
        MariaDbServerVersion.LatestSupportedServerVersion,
        opts =>
            opts.EnableRetryOnFailure()
                .EnableIndexOptimizedBooleanColumns()
    );
});

LinqToDBForEFTools.Initialize();

var services = serviceCollection.BuildServiceProvider();

using var scope = services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

LinqToDB.Data.DataConnection.TurnTraceSwitchOn();
LinqToDB.Data.DataConnection.WriteTraceLine = (message, category, level) =>
{
    logger.LogInformation(message);
};

var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

var query = db.TestModels
    .Where(x => x.Name.Contains("Test"))
    .OrderBy(x => x.Name);

await query.ToListAsyncLinqToDB();

await db.TestModels
    .Where(p => p.Name.StartsWith("Test"))
    .ToListAsyncEF();

public class TestDbContext : DbContext
{
    public DbSet<TestModel> TestModels { get; set; }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }
}

public class TestModel
{
    public int Id { get; set; }

    [MaxLength(255)]
    public string? Name { get; set; }
}