using System;
using System.Collections.Generic;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class TarjetaCredito : BaseEntity, IAuditableEntity
{
    public string NumeroTarjeta { get; set; } = default!;
    public string CvcHash { get; set; } = default!;
    public string ClienteId { get; set; } = default!;
    public decimal LimiteCredito { get; set; }
    public decimal MontoAdeudado { get; set; }
    public DateTime FechaExpiracion { get; set; }
    public EstadoTarjeta Estado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ApplicationUser Cliente { get; set; } = default!;
    public ICollection<Transaccion> Consumos { get; set; } = new List<Transaccion>();

    public decimal CreditoDisponible => LimiteCredito - MontoAdeudado;
}
