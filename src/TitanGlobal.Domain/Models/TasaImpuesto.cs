using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TitanGlobal.Domain.Models;

[Index(nameof(ImpuestoId), nameof(FechaInicio))]
public class TasaImpuesto
{
    public int Id { get; set; }

    public int ImpuestoId { get; set; }

    public virtual Impuesto Impuesto { get; set; } = null!;

    [Precision(18, 6)]
    public decimal Porcentaje { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activo { get; set; } = true;

    [MaxLength(250)]
    public string? Observacion { get; set; }
}
