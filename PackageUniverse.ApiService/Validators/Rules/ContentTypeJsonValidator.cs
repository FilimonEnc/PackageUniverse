using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators.Rules;

public class ContentTypeJsonValidator : IHttpResponseValidator
{
    public ISet<HttpValidationTag> Tags => new HashSet<HttpValidationTag>
    {
        HttpValidationTag.ExpectBody,
        HttpValidationTag.Post
    };

    public Task ValidateAsync(HttpValidationContext context)
    {
        if (context.Response.Content.Headers.ContentType?.MediaType != "application/json")
            throw new InvalidOperationException("Ожидался JSON");

        return Task.CompletedTask;
    }
}