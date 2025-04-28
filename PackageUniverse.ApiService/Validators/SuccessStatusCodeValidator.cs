using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators;

public class SuccessStatusCodeValidator : IHttpResponseValidator
{
    public Task ValidateAsync(HttpResponseMessage response, string uri)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Ошибка при запросе {uri}: {response.StatusCode}");
        return Task.CompletedTask;
    }
}