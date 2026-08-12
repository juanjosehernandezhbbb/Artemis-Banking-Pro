using System;
using ArtemisBankingPro.Core.Domain.Common;

namespace ArtemisBankingPro.Core.Domain.Entities;

public class Comercio : BaseEntity, IAuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string Rnc { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string? CreatedByUserId { get; set; }

    public string? ApplicationUserId { get; set; }
    public ApplicationUser? ApplicationUser { get; set; }
}
