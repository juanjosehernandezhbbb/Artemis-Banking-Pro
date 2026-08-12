using System;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class CuotaAmortizacion : BaseEntity
{
    public int PrestamoId { get; set; }
    public int NumeroCuota { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public decimal ValorCuota { get; set; }
    public decimal MontoInteres { get; set; }
    public decimal MontoCapital { get; set; }
    public decimal SaldoPendiente { get; set; }
    public EstadoCuota EstadoPago { get; set; }
    public bool Atrasada { get; set; }

    public Prestamo Prestamo { get; set; } = default!;
}
