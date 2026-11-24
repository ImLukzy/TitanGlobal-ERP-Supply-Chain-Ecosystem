// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ParametroPlanilla
{
    public int Id { get; set; }

    public string Clave { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}
