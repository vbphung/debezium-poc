using debezium_poc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

var builder = Host.CreateDefaultBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

builder = builder
    .UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);
    })
    .ConfigureServices(services =>
    {
        services.AddDbContext<BusinessDbContext>(options =>
            options.UseNpgsql(BusinessDbContextFactory.CONN_STR));

        services.AddHostedService<UserService>();
        services.AddHostedService<BookService>();
    });

await builder.Build().RunAsync();
