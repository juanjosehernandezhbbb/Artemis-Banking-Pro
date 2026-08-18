using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Infrastructure.Security;
using ArtemisBankingPro.Tests.Helpers;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace ArtemisBankingPro.Tests.UnitTests
{
    public class CreditCardServiceTests
    {
        [Fact]
        public void HashCvc_GeneraHashDeterminista()
        {
            var service = new CreditCardService(new FakeUnitOfWork());

            var hash1 = service.HashCvc("123");
            var hash2 = service.HashCvc("123");

            Assert.Equal(hash1, hash2);
            Assert.NotEqual("123", hash1);
        }

        [Fact]
        public void HashCvc_DistintosCvc_DanDistintosHash()
        {
            var service = new CreditCardService(new FakeUnitOfWork());

            Assert.NotEqual(service.HashCvc("123"), service.HashCvc("456"));
        }

        [Fact]
        public void HashCvc_TieneLongitudDeSha256()
        {
            var service = new CreditCardService(new FakeUnitOfWork());

            Assert.Equal(64, service.HashCvc("123").Length);
        }

        [Fact]
        public void GenerateUniqueCardNumberAsync_GeneraNumeroDe16Digitos()
        {
            var service = new CreditCardService(new FakeUnitOfWork());

            var numero = service.GenerateUniqueCardNumberAsync().GetAwaiter().GetResult();

            Assert.Matches(new Regex(@"^\d{16}$"), numero);
        }

        [Fact]
        public void GenerateUniqueCardNumberAsync_EvitaColisionConTarjetaExistente()
        {
            var uow = new FakeUnitOfWork();
            uow.Repo<TarjetaCredito>().Items.Add(new TarjetaCredito
            {
                Id = 1,
                NumeroTarjeta = "1111222233334444",
                CvcHash = "hash",
                ClienteId = "client-1",
                LimiteCredito = 10000m,
                MontoAdeudado = 0m,
                FechaExpiracion = System.DateTime.UtcNow.AddYears(3),
                Estado = ArtemisBankingPro.Core.Domain.Enums.EstadoTarjeta.Activa,
                FechaCreacion = System.DateTime.UtcNow
            });

            var service = new CreditCardService(uow);

            var numero = service.GenerateUniqueCardNumberAsync().GetAwaiter().GetResult();

            Assert.Matches(new Regex(@"^\d{16}$"), numero);
            Assert.Equal(1, uow.Repo<TarjetaCredito>().Items.Count);
        }
    }
}