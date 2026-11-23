// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class NotaCreditoDebito
{
    public int Id { get; set; }

    public int FacturaId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Serie { get; set; } = string.Empty;

    public int Numero { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public string? MotivoAnulacion { get; set; }

    public virtual Factura Factura { get; set; } = null!;
}
