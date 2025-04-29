using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators.Rules;

public class SuccessStatusCodeValidator : IHttpResponseValidator
{
    public ISet<HttpValidationTag> Tags => new HashSet<HttpValidationTag>
    {
        HttpValidationTag.ExpectBody,
        HttpValidationTag.Post,
        HttpValidationTag.Get
    };

    public Task ValidateAsync(HttpValidationContext context)
    {
        if (!context.Response.IsSuccessStatusCode)
            throw new HttpRequestException($"Ошибка при запросе {context.Uri}: {context.Response.StatusCode}");
        return Task.CompletedTask;
    }
}