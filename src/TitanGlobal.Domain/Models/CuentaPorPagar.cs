// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class CuentaPorPagar
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public int? OrdenAbastecimientoId { get; set; }

    public string Serie { get; set; } = string.Empty;

    public string NumeroDocumento { get; set; } = string.Empty;

    public DateOnly FechaDocumento { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public string Moneda { get; set; } = string.Empty;

    public decimal BaseImponible { get; set; }

    public decimal ImporteImpuesto { get; set; }

    public decimal Total { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual ICollection<AplicacionPago> AplicacionesPago { get; set; } = new List<AplicacionPago>();

    public virtual OrdenAbastecimiento? OrdenAbastecimiento { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;
}
