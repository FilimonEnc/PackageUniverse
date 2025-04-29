namespace PackageUniverse.ApiService.Validators;

public record HttpValidationContext(HttpResponseMessage Response, string Uri);