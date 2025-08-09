using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RinhaBackend.Shared.Data;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<RinhaContext>(s =>
    s.UseNpgsql(builder.Configuration.GetConnectionString("RinhaDatabase"),
        contextOptionsBuilder => contextOptionsBuilder.MigrationsAssembly("RinhaBackend.Migrations")));


IHost host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RinhaContext>();
    await context.Database.MigrateAsync();
}