using MediatR;

using Microsoft.AspNetCore.Mvc;
namespace PackageUniverse.ApiService.Controllers
{
    /// <summary>
    /// Базовый контроллер
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public abstract class BaseController : ControllerBase
    {
        // <summary>
        // Медиатор
        // </summary>
        protected IMediator Mediator
        {
            get
            {
                _mediator ??= HttpContext.RequestServices.GetService<IMediator>();
                if (_mediator == null) throw new ArgumentNullException(nameof(_mediator));

                return _mediator;
            }

        }
        private IMediator? _mediator;

        //internal int UserId => !User.Identity!.IsAuthenticated ? 0 : int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        //internal string UserEmail => !User.Identity!.IsAuthenticated ? string.Empty : User.FindFirst(ClaimTypes.Email)!.Value;
        //internal string UserName => !User.Identity!.IsAuthenticated ? string.Empty : User.FindFirst(ClaimTypes.GivenName)!.Value;
        //internal string UserSurname => !User.Identity!.IsAuthenticated ? string.Empty : User.FindFirst(ClaimTypes.Surname)!.Value;
    }
}
