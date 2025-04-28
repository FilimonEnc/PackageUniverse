using PackageUniverse.Infrastructure.Data;
using PackageUniverse.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", false, true);


builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.AddNpgsqlDbContext<PUContext>("packagedb");

var host = builder.Build();
host.Run();