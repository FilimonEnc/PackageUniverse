using Microsoft.AspNetCore.Mvc;
using PackageUniverse.Application.CQRS.Packages.Queries.GetPackages;
using PackageUniverse.Application.Models;

namespace PackageUniverse.ApiService.Controllers;

public class PackagesController : BaseController
{
    private readonly IConfiguration _config;

    /// <summary>
    ///     Конструктор
    /// </summary>
    /// <param name="config"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public PackagesController(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    ///     Возвращает все пакеты
    /// </summary>
    /// <returns></returns>
    [HttpGet("GetAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IEnumerable<PackageModel>> GetAll()
    {
        GetPackagesQuery query = new();
        return await Mediator.Send(query);
    }
}