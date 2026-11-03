using System.ComponentModel.DataAnnotations;

namespace TitanGlobal.Domain.Models;

public class TasaImpuesto
{
    public int Id { get; set; }

    public int ImpuestoId { get; set; }

    public virtual Impuesto Impuesto { get; set; } = null!;
    public decimal Porcentaje { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activo { get; set; } = true;

    [MaxLength(250)]
    public string? Observacion { get; set; }
}
