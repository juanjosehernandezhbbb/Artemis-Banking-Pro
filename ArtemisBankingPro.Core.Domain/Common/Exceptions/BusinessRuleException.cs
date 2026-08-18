namespace ArtemisBankingPro.Core.Domain.Common.Exceptions
{
    public class BusinessRuleException : DomainException
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}