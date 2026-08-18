namespace ArtemisBankingPro.Core.Domain.Common.Exceptions
{
    public class ForbiddenAccessException : DomainException
    {
        public ForbiddenAccessException(string message) : base(message)
        {
        }
    }
}