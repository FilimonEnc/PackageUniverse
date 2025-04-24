using Microsoft.EntityFrameworkCore;

using PackageUniverse.Infrastructure.Data;

namespace UrlShortener.Api.Extensions;
public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using PUContext dbContext =
            scope.ServiceProvider.GetRequiredService<PUContext>();

        //dbContext.Database.Migrate();
    }
}