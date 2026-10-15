namespace TitanGlobal.Api.Models;

public class ListaPrecios
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int? SegmentoClienteId { get; set; }

    public int Prioridad { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activa { get; set; } = true;

    public virtual SegmentoCliente? SegmentoCliente { get; set; }

    public virtual ICollection<ItemListaPrecios> Items { get; set; } = new List<ItemListaPrecios>();
}
