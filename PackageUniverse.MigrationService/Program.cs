using PackageUniverse.MigrationService;
using PackageUniverse.Infrastructure.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);


builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.AddNpgsqlDbContext<PUContext>("packagedb");

var host = builder.Build();
host.Run();
