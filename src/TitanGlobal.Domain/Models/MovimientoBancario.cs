// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class MovimientoBancario
{
    public int Id { get; set; }

    public int CuentaBancariaId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public decimal Importe { get; set; }

    public DateOnly FechaOperacion { get; set; }

    public string? Referencia { get; set; }

    public int? TransferenciaBancariaId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public DateTime? FechaReversion { get; set; }

    public string? MotivoReversion { get; set; }

    public virtual CuentaBancaria CuentaBancaria { get; set; } = null!;

    public virtual TransferenciaBancaria? TransferenciaBancaria { get; set; }
}
