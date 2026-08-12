using System;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class Transaccion : BaseEntity
{
    public int? CuentaAhorroId { get; set; }
    public int? TarjetaCreditoId { get; set; }
    public int? PrestamoId { get; set; }

    public TipoOperacion TipoOperacion { get; set; }
    public TipoTransaccion Tipo { get; set; }
    public decimal Monto { get; set; }
    public string Origen { get; set; } = default!;
    public string Destino { get; set; } = default!;
    public EstadoTransaccion Estado { get; set; }
    public string? MotivoRechazo { get; set; }
    public Guid? GrupoOperacionId { get; set; }
    public DateTime Fecha { get; set; }

    public CuentaAhorro? CuentaAhorro { get; set; }
    public TarjetaCredito? TarjetaCredito { get; set; }
    public Prestamo? Prestamo { get; set; }
}
