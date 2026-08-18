using System.Threading.Tasks;

namespace ArtemisBankingPro.Core.Application.Common.Interfaces
{
    public interface IAccountNumberGenerator
    {

        Task<string> GenerateUniqueAccountNumberAsync();
    }
}