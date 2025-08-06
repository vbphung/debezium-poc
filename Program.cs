using debezium_poc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices(services =>
{
    services.AddDbContext<BusinessDbContext>(options =>
        options.UseNpgsql("Host=localhost;Port=5432;Username=testuser;Password=testpassword;Database=postgres"));

    services.AddHostedService<UserService>();
});

await builder.Build().RunAsync();
