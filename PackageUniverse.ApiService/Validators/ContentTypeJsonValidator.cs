using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators
{
    public class ContentTypeJsonValidator : IHttpResponseValidator
    {
        public Task ValidateAsync(HttpResponseMessage response, string uri)
        {
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
                throw new InvalidOperationException($"Ожидался 'application/json', но получено '{response.Content.Headers.ContentType?.MediaType}' от {uri}");
            return Task.CompletedTask;
        }
    }
}