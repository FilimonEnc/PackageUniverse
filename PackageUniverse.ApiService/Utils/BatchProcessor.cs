namespace PackageUniverse.ApiService.Utils;

public static class BatchProcessor
{
    public static async Task ForEachAsync<T>(
        IEnumerable<T> source,
        int maxDegreeOfParallelism,
        Func<T, Task> processor,
        ILogger? logger = null)
    {
        using var throttle = new SemaphoreSlim(maxDegreeOfParallelism);
        var tasks = source.Select(async item =>
        {
            await throttle.WaitAsync();
            try
            {
                await processor(item);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Ошибка при обработке элемента: {Item}", item);
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}