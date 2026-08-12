using System;

namespace ArtemisBankingPro.Core.Domain.Common;

public interface IAuditableEntity
{
    DateTime FechaCreacion { get; set; }
}
