using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators;

public class ContentNotEmptyValidator: IHttpResponseValidator
{
    public Task ValidateAsync(HttpResponseMessage response, string uri)
    {
        if (response.Content.Headers.ContentLength is <= 0)
            throw new InvalidOperationException($"Ответ пустой или нулевой от {uri}");
        return Task.CompletedTask;
    }
}