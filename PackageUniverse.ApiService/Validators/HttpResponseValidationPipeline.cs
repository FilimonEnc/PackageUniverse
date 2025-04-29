using PackageUniverse.ApiService.Validators.Interfaces;

namespace PackageUniverse.ApiService.Validators;

public class HttpResponseValidationPipeline(IEnumerable<IHttpResponseValidator> validators)
{
    public async Task ValidateAsync(HttpValidationContext context, IEnumerable<HttpValidationTag> activeTags)
    {
        var tagSet = new HashSet<HttpValidationTag>(activeTags);

        foreach (var validator in validators)
            if (validator.Tags.Overlaps(tagSet))
                await validator.ValidateAsync(context);
    }
}