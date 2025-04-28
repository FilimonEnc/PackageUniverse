using PackageUniverse.ApiService.Extensions;
using PackageUniverse.ApiService.Services;
using PackageUniverse.Application;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Infrastructure.Data;
using Scalar.AspNetCore;
using UrlShortener.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

//БД
builder.AddNpgsqlDbContext<PUContext>("packagedb");

//Валидатоция ответов HTTP
builder.Services.AddHttpResponseValidation();

var services = builder.Services;


//Контекст БД
services.AddScoped<IPUContext, PUContext>();


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

#pragma warning disable S6966
app.Run();
#pragma warning restore S6966