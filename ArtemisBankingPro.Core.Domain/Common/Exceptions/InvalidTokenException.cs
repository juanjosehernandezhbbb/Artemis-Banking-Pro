namespace ArtemisBankingPro.Core.Domain.Common.Exceptions
{
    public class InvalidTokenException : DomainException
    {
        public InvalidTokenException(string message) : base(message)
        {
        }
    }
}