using Microsoft.EntityFrameworkCore;
using PackageUniverse.Infrastructure.Data;

namespace UrlShortener.Api.Extensions;

public static class PostgreSqlExtensions
{
    private const string ConnectionString = "dbs";

    public static IServiceCollection AddPostgreSqlConfig(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<PUContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString(ConnectionString)));

        return services;
    }
}