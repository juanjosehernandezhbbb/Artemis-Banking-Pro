using ArtemisBankingPro.Core.Application.Common.Interfaces;
using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Infrastructure.Security
{
    public class CreditCardService : ICreditCardService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreditCardService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateUniqueCardNumberAsync()
        {
            string cardNumber = string.Empty;
            bool isUnique = false;
            const int maxAttempts = 1000;

            for (int attempt = 0; attempt < maxAttempts && !isUnique; attempt++)
            {
                var sb = new StringBuilder(16);
                for (int i = 0; i < 16; i++)
                {
                    sb.Append(RandomNumberGenerator.GetInt32(0, 10));
                }

                cardNumber = sb.ToString();

                var existingCard = await _unitOfWork.Repository<TarjetaCredito>()
                    .FindAsync(x => x.NumeroTarjeta == cardNumber);

                if (existingCard.Count == 0)
                {
                    isUnique = true;
                }
            }

            if (!isUnique)
            {
                throw new InvalidOperationException("No fue posible generar un número de tarjeta único.");
            }

            return cardNumber;
        }

        public string HashCvc(string cvc)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(cvc));
            return Convert.ToHexString(bytes);
        }
    }
}