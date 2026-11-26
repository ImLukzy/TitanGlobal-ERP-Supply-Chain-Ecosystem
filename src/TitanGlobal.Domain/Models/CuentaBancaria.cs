// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class CuentaBancaria
{
    public int Id { get; set; }

    public string Banco { get; set; } = string.Empty;

    public string Numero { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Moneda { get; set; } = string.Empty;

    public DateTime FechaRegistro { get; set; }

    public virtual ICollection<MovimientoBancario> MovimientosBancarios { get; set; } = new List<MovimientoBancario>();

    public virtual ICollection<TransferenciaBancaria> TransferenciasBancariaCuentaDestino { get; set; } = new List<TransferenciaBancaria>();

    public virtual ICollection<TransferenciaBancaria> TransferenciasBancariaCuentaOrigen { get; set; } = new List<TransferenciaBancaria>();
}
