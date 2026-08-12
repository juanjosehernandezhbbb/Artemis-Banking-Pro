using System;
using System.Collections.Generic;
using ArtemisBankingPro.Core.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class ApplicationUser : IdentityUser, IAuditableEntity
{
    public string Nombre { get; set; } = default!;
    public string Apellido { get; set; } = default!;
    public string Cedula { get; set; } = default!;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }

    public ICollection<CuentaAhorro> CuentasAhorro { get; set; } = new List<CuentaAhorro>();
    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
    public ICollection<TarjetaCredito> TarjetasCredito { get; set; } = new List<TarjetaCredito>();
    public ICollection<Beneficiario> Beneficiarios { get; set; } = new List<Beneficiario>();
    public Comercio? Comercio { get; set; }
}
