using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TitanGlobal.Domain.Models;

[Index(nameof(Codigo), IsUnique = true)]
public class Impuesto
{
    public int Id { get; set; }

    [Required]
    [MaxLength(30)]
    public string Codigo { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Tipo { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Descripcion { get; set; }

    [Required]
    [MaxLength(3)]
    public string Pais { get; set; } = "PER";

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public virtual ICollection<TasaImpuesto> Tasas { get; set; } =
        new List<TasaImpuesto>();
}
