using Microsoft.EntityFrameworkCore;

namespace PackageUniverse.ApiService.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void MigrateDbContext<TDbContext>(this IApplicationBuilder app)
        where TDbContext : DbContext
    {
        using var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var context = serviceScope.ServiceProvider.GetService<TDbContext>();
        var logger = serviceScope.ServiceProvider.GetService<ILogger<TDbContext>>();

        if (context == null)
        {
            logger?.LogError("The database context of type {DbContextType} could not be resolved.",
                typeof(TDbContext).Name);
            return;
        }

        try
        {
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}