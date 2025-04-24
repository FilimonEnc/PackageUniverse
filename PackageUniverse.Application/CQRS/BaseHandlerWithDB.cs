using MapsterMapper;

using PackageUniverse.Application.Interfaces;

namespace PackageUniverse.Application.CQRS
{
    public abstract class BaseHandlerWithDB
    {
        protected readonly IPUContext Context;
        protected readonly IMapper Mapper;

        public BaseHandlerWithDB(IPUContext context, IMapper mapper)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

    }
}
