using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Common.Interfaces
{
    public interface ICreditCardService
    {
        Task<string> GenerateUniqueCardNumberAsync();
        string HashCvc(string cvc);
    }
}