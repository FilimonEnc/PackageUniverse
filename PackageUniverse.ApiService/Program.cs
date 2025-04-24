using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using PackageUniverse.ApiService.Services;
using PackageUniverse.Application;
using PackageUniverse.Application.CQRS.Packages.Queries.GetPackages;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Infrastructure.Data;

using Scalar.AspNetCore;

using System.Reflection;

using UrlShortener.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<PUContext>(connectionName: "packagedb");

var services = builder.Services;

services.AddScoped<IPUContext, PUContext>();

//services.AddMediatRConfig();
//services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Startup).Assembly));
//services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
//builder.Services.AddMediatR(cfg => { cfg.RegisterServicesFromAssemblies([typeof(Program).Assembly, typeof(PackageUniverse.Application.CQRS.BaseHandlerWithDB).Assembly]); });

services.AddApplication();

// Add services to the container.
services.AddProblemDetails();

services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("https://localhost:7469")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
services.AddOpenApi();
services.AddEndpointsApiExplorer();

services.AddControllers();

services.AddHttpClient<NuGetPackageCheckerService>();
services.AddHostedService<NuGetPackageCheckerService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
    app.ApplyMigrations();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.UseCors("AllowSpecificOrigin");

app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
