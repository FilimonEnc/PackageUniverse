namespace PackageUniverse.ApiService.Validators.Interfaces;

public interface IHttpResponseValidator
{
    ISet<HttpValidationTag> Tags { get; }

    Task ValidateAsync(HttpValidationContext context);
}