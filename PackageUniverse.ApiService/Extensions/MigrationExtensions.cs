using PackageUniverse.Infrastructure.Data;

namespace UrlShortener.Api.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();

        using var dbContext =
            scope.ServiceProvider.GetRequiredService<PUContext>();

        //dbContext.Database.Migrate();
    }
}