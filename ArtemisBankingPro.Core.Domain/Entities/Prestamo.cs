using System;
using System.Collections.Generic;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class Prestamo : BaseEntity, IAuditableEntity
{
    public string NumeroPrestamo { get; set; } = default!;
    public string ClienteId { get; set; } = default!;
    public decimal CapitalAprobado { get; set; }
    public decimal TasaInteresAnual { get; set; }
    public int PlazoMeses { get; set; }
    public EstadoPrestamo Estado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ApplicationUser Cliente { get; set; } = default!;
    public ICollection<CuotaAmortizacion> Cuotas { get; set; } = new List<CuotaAmortizacion>();
}
