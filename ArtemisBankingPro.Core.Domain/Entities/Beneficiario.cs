using System;
using ArtemisBankingPro.Core.Domain.Common;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class Beneficiario : BaseEntity, IAuditableEntity
{
    public string ClienteId { get; set; } = default!;
    public int CuentaAhorroId { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ApplicationUser Cliente { get; set; } = default!;
    public CuentaAhorro CuentaAhorro { get; set; } = default!;
}
