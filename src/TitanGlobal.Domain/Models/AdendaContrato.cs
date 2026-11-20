// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class AdendaContrato
{
    public int Id { get; set; }

    public int ContratoLaboralId { get; set; }

    public DateOnly FechaVigencia { get; set; }

    public string Cargo { get; set; } = string.Empty;

    public decimal SueldoBasico { get; set; }

    public virtual ContratoLaboral ContratoLaboral { get; set; } = null!;
}
