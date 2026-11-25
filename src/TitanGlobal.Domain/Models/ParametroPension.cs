// Generado por scripts/scaffold.sh a partir de la base de datos (database/). No lo edites a mano:
// cambia el script SQL y vuelve a generar. El código propio de la entidad va en un archivo partial aparte.

namespace TitanGlobal.Domain.Models;

public partial class ParametroPension
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Sistema { get; set; } = string.Empty;

    public decimal TasaAporte { get; set; }

    public decimal TasaPrimaSeguro { get; set; }

    public decimal TasaComision { get; set; }

    public decimal? RemuneracionMaximaAsegurable { get; set; }

    public virtual ICollection<AfiliacionPension> AfiliacionesPension { get; set; } = new List<AfiliacionPension>();
}
