using ArtemisBankingPro.Core.Domain.Entities;
using ArtemisBankingPro.Core.Domain.Enums;
using ArtemisBankingPro.Persistance.Context;
using ArtemisBankingPro.Persistance.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ArtemisBankingPro.Tests.IntegrationTests
{
    public class RepositoryIntegrationTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public RepositoryIntegrationTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ApplicationDbContext(_options);
            context.Database.EnsureCreated();
            context.Users.AddRange(
                new ApplicationUser { Id = "client-1", UserName = "client-1", NormalizedUserName = "CLIENT-1", Nombre = "Juan", Apellido = "Perez", Cedula = "00112345678" },
                new ApplicationUser { Id = "client-2", UserName = "client-2", NormalizedUserName = "CLIENT-2", Nombre = "Ana", Apellido = "Lopez", Cedula = "00112345679" },
                new ApplicationUser { Id = "client-3", UserName = "client-3", NormalizedUserName = "CLIENT-3", Nombre = "Luis", Apellido = "Diaz", Cedula = "00112345680" });
            context.SaveChanges();
        }

        public void Dispose() => _connection.Dispose();

        private ApplicationDbContext NewContext() => new(_options);

        [Fact]
        public async Task RepositorioCuentaAhorro_PersisteYConsulta()
        {
            using var context = NewContext();
            var repo = new GenericRepository<CuentaAhorro>(context);
            await repo.AddAsync(new CuentaAhorro
            {
                NumeroCuenta = "100000001",
                ClienteId = "client-1",
                Balance = 500m,
                TipoCuenta = TipoCuenta.Principal,
                Estado = EstadoCuenta.Activa,
                FechaCreacion = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            using var context2 = NewContext();
            var repo2 = new GenericRepository<CuentaAhorro>(context2);
            var encontrada = await repo2.FindAsync(c => c.NumeroCuenta == "100000001");
            var cuenta = Assert.Single(encontrada);
            Assert.Equal("client-1", cuenta.ClienteId);
            Assert.Equal(500m, cuenta.Balance);
        }

        [Fact]
        public async Task UnitOfWork_CommitPersiste()
        {
            using var context = NewContext();
            var uow = new UnitOfWork(context);
            await uow.BeginTransactionAsync();
            await uow.Repository<CuentaAhorro>().AddAsync(new CuentaAhorro
            {
                NumeroCuenta = "100000002",
                ClienteId = "client-2",
                Balance = 100m,
                TipoCuenta = TipoCuenta.Secundaria,
                Estado = EstadoCuenta.Activa,
                FechaCreacion = DateTime.UtcNow
            });
            await uow.SaveChangesAsync();
            await uow.CommitTransactionAsync();

            using var context2 = NewContext();
            var existe = await context2.CuentasAhorro.AnyAsync(c => c.NumeroCuenta == "100000002");
            Assert.True(existe);
        }

        [Fact]
        public async Task UnitOfWork_RollbackNoPersiste()
        {
            using var context = NewContext();
            var uow = new UnitOfWork(context);
            await uow.BeginTransactionAsync();
            await uow.Repository<CuentaAhorro>().AddAsync(new CuentaAhorro
            {
                NumeroCuenta = "100000003",
                ClienteId = "client-3",
                Balance = 100m,
                TipoCuenta = TipoCuenta.Secundaria,
                Estado = EstadoCuenta.Activa,
                FechaCreacion = DateTime.UtcNow
            });
            await uow.RollbackTransactionAsync();

            using var context2 = NewContext();
            var existe = await context2.CuentasAhorro.AnyAsync(c => c.NumeroCuenta == "100000003");
            Assert.False(existe);
        }

        [Fact]
        public async Task RepositorioPrestamo_PersisteConCuotas()
        {
            using var context = NewContext();
            var prestamo = new Prestamo
            {
                NumeroPrestamo = "200000001",
                ClienteId = "client-1",
                CapitalAprobado = 100000m,
                TasaInteresAnual = 12m,
                PlazoMeses = 12,
                Estado = EstadoPrestamo.Activo,
                FechaCreacion = DateTime.UtcNow
            };
            context.Prestamos.Add(prestamo);
            await context.SaveChangesAsync();

            context.CuotasAmortizacion.Add(new CuotaAmortizacion
            {
                PrestamoId = prestamo.Id,
                NumeroCuota = 1,
                FechaVencimiento = DateTime.UtcNow.AddMonths(1),
                ValorCuota = 8884.88m,
                MontoInteres = 1000m,
                MontoCapital = 7884.88m,
                SaldoPendiente = 92115.12m,
                EstadoPago = EstadoCuota.Pendiente,
                Atrasada = false
            });
            await context.SaveChangesAsync();

            using var context2 = NewContext();
            var cuota = await context2.CuotasAmortizacion.Include(c => c.Prestamo).FirstAsync(c => c.NumeroCuota == 1);
            Assert.Equal("200000001", cuota.Prestamo.NumeroPrestamo);
            Assert.Equal(8884.88m, cuota.ValorCuota);
        }

        [Fact]
        public async Task RepositorioTarjeta_PersisteConsumo()
        {
            using var context = NewContext();
            var tarjeta = new TarjetaCredito
            {
                NumeroTarjeta = "1234567890123456",
                CvcHash = "HASH",
                ClienteId = "client-1",
                LimiteCredito = 15000m,
                MontoAdeudado = 0m,
                FechaExpiracion = DateTime.UtcNow.AddYears(3),
                Estado = EstadoTarjeta.Activa,
                FechaCreacion = DateTime.UtcNow
            };
            context.TarjetasCredito.Add(tarjeta);
            await context.SaveChangesAsync();

            context.Transacciones.Add(new Transaccion
            {
                TarjetaCreditoId = tarjeta.Id,
                TipoOperacion = TipoOperacion.PagoHermesPay,
                Tipo = TipoTransaccion.Debito,
                Monto = 2500m,
                Origen = "Supermercado",
                Destino = "3456",
                Estado = EstadoTransaccion.Aprobada,
                GrupoOperacionId = Guid.NewGuid(),
                Fecha = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            using var context2 = NewContext();
            var tx = await context2.Transacciones.Include(t => t.TarjetaCredito).FirstAsync(t => t.TipoOperacion == TipoOperacion.PagoHermesPay);
            Assert.Equal("1234567890123456", tx.TarjetaCredito.NumeroTarjeta);
            Assert.Equal(2500m, tx.Monto);
        }

        [Fact]
        public async Task RepositorioComercio_VerificaUnicidadDeRnc()
        {
            using var context = NewContext();
            var repo = new GenericRepository<Comercio>(context);
            await repo.AddAsync(new Comercio
            {
                Name = "Supermercado",
                Email = "sup@test.com",
                PhoneNumber = "8095551234",
                Rnc = "130123456",
                IsActive = true,
                FechaCreacion = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            using var context2 = NewContext();
            var repo2 = new GenericRepository<Comercio>(context2);
            var porRnc = await repo2.FindAsync(c => c.Rnc == "130123456");
            Assert.Single(porRnc);
        }
    }
}