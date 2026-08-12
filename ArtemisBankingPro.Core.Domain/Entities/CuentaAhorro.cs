using System;
using System.Collections.Generic;
using ArtemisBankingPro.Core.Domain.Common;
using ArtemisBankingPro.Core.Domain.Enums;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class CuentaAhorro : BaseEntity, IAuditableEntity
{
    public string NumeroCuenta { get; set; } = default!;
    public string ClienteId { get; set; } = default!;
    public decimal Balance { get; set; }
    public TipoCuenta TipoCuenta { get; set; }
    public EstadoCuenta Estado { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ApplicationUser Cliente { get; set; } = default!;
    public ICollection<Transaccion> Transacciones { get; set; } = new List<Transaccion>();
    public ICollection<Beneficiario> RegistradaComoBeneficiariaEn { get; set; } = new List<Beneficiario>();
}
