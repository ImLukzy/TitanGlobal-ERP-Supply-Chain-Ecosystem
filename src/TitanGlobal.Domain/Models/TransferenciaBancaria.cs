// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class TransferenciaBancaria
{
    public int Id { get; set; }

    public int CuentaOrigenId { get; set; }

    public int CuentaDestinoId { get; set; }

    public decimal Importe { get; set; }

    public DateOnly FechaOperacion { get; set; }

    public string? Referencia { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual CuentaBancaria CuentaDestino { get; set; } = null!;

    public virtual CuentaBancaria CuentaOrigen { get; set; } = null!;

    public virtual ICollection<MovimientoBancario> MovimientosBancarios { get; set; } = new List<MovimientoBancario>();
}
