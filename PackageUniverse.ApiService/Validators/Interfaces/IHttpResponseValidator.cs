namespace PackageUniverse.ApiService.Validators.Interfaces;

public interface IHttpResponseValidator
{
    Task ValidateAsync(HttpResponseMessage response, string uri);
}