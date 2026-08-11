using PackageUniverse.ApiService.Extensions;
using PackageUniverse.ApiService.Services;
using PackageUniverse.Application;
using PackageUniverse.Application.Interfaces;
using PackageUniverse.Infrastructure.Data;
using Scalar.AspNetCore;

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

// Настраиваем HttpClient для NuGetPackageCheckerService с увеличенными таймаутами
// Стандартные настройки Polly (10 сек) слишком агрессивны для медленного API NuGet
services.AddHttpClient<NuGetPackageCheckerService>((sp, client) =>
    {
        client.Timeout = TimeSpan.FromMinutes(2); // Общий таймаут на операцию
    })
    .AddResilienceHandler(options =>
    {
        // Увеличиваем таймаут попытки до 60 секунд (вместо стандартных 10)
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
        
        // Увеличиваем общий таймаут выполнения
        options.ExecutionTimeout.Timeout = TimeSpan.FromMinutes(5);
        
        // Настройка повторных попыток - более консервативная
        options.Retry.ShouldHandle = args => 
            new ValueTask<bool>(args.Outcome.Exception is HttpRequestException or TimeoutRejectedException);
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
        options.Retry.BaseDelay = TimeSpan.FromSeconds(2);
        
        // Circuit breaker - отключаем для фоновой задачи, чтобы не прерывать обработку надолго
        options.CircuitBreaker.Disabled = true;
    });

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