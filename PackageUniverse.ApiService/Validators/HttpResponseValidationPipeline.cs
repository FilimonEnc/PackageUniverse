using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators;

public class HttpResponseValidationPipeline(IEnumerable<IHttpResponseValidator> validators)
{
    public async Task ValidateAsync(HttpResponseMessage response, string uri)
    {
        foreach (var validator in validators) await validator.ValidateAsync(response, uri);
    }
}