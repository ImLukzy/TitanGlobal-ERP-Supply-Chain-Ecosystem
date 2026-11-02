namespace TitanGlobal.Application.DTOs.Modulo06_ListasPrecios;

public class GuardarListaRequestDto
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int? SegmentoClienteId { get; set; }

    public int Prioridad { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activa { get; set; } = true;

    public List<GuardarItemListaDto> Items { get; set; } = new List<GuardarItemListaDto>();
}

public class GuardarItemListaDto
{
    public int ProductoId { get; set; }

    public decimal Precio { get; set; }
}
