using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators.Rules;

public class ContentNotEmptyValidator : IHttpResponseValidator
{
    public ISet<HttpValidationTag> Tags => new HashSet<HttpValidationTag>
    {
        HttpValidationTag.ExpectBody
    };

    public Task ValidateAsync(HttpValidationContext context)
    {
        if (context.Response.Content.Headers.ContentLength == 0)
            throw new InvalidOperationException($"Ожидался непустой ответ от {context.Uri}");

        return Task.CompletedTask;
    }
}