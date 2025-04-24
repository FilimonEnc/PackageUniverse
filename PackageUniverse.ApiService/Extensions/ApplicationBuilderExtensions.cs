using Grpc.Net.Client.Configuration;

using Microsoft.EntityFrameworkCore;

using Polly;
using Polly.Retry;

namespace PackageUniverse.ApiService.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void MigrateDbContext<TDbContext>(this IApplicationBuilder app)
        where TDbContext : DbContext
    {
        using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetService<TDbContext>();
            try
            {
                context.Database.Migrate();
            }
            catch (Exception ex)
            {
                var logger = serviceScope.ServiceProvider.GetService<ILogger<TDbContext>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }
    }
}