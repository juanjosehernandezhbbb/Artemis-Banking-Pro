using ArtemisBankingPro.Core.Domain.Entities;

namespace ArtemisBankingPro.WebApp.ViewModels
{
    public class ClientDashboardViewModel
    {
        public List<CuentaAhorro> CuentasAhorro { get; set; } = new();
        public List<Prestamo> Prestamos { get; set; } = new();
        public List<TarjetaCredito> TarjetasCredito { get; set; } = new();

        public bool HasProducts => CuentasAhorro.Any() || Prestamos.Any() || TarjetasCredito.Any();
    }
}