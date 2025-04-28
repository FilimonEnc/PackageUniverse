using System.Reflection;
using PackageUniverse.ApiService.Validators;
using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpResponseValidation(this IServiceCollection services)
    {
        services.AddScoped<HttpResponseValidationPipeline>();

        var validatorType = typeof(IHttpResponseValidator);
        var types = Assembly.GetAssembly(validatorType)!
            .GetTypes()
            .Where(t => validatorType.IsAssignableFrom(t)
                        && t.IsClass
                        && !t.IsAbstract);

        foreach (var impl in types)
        {
            services.AddScoped(typeof(IHttpResponseValidator), impl);
        }

        return services;
    }
}
