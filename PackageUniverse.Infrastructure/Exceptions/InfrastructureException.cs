namespace PackageUniverse.Infrastructure.Exceptions;

public class InfrastructureException : Exception
{
    public InfrastructureException(string businessMessage)
        : base(businessMessage)
    {
    }

    public InfrastructureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}