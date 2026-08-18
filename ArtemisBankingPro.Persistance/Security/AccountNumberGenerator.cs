using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Security
{
    public class AccountNumberGenerator : IAccountNumberGenerator
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountNumberGenerator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateUniqueAccountNumberAsync()
        {
            var random = new Random();
            string accountNumber;
            bool isUnique = false;

            do
            {
                accountNumber = random.Next(100000000, 999999999).ToString();

                var existingAccount = await _unitOfWork.Repository<CuentaAhorro>().FindAsync(x => x.NumeroCuenta == accountNumber);
                var existingLoan = await _unitOfWork.Repository<Prestamo>().FindAsync(x => x.NumeroPrestamo == accountNumber);

                if (existingAccount.Count == 0 && existingLoan.Count == 0)
                {
                    isUnique = true;
                }

            } while (!isUnique);

            return accountNumber;
        }
    }
}