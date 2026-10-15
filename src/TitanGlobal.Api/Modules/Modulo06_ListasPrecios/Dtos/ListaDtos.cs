namespace TitanGlobal.Api.Modules.Modulo06_ListasPrecios.Dtos;

public class PaginaListasDto
{
    public List<ListaListadoDto> Items { get; set; } = new List<ListaListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class ListaListadoDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int? SegmentoId { get; set; }

    public string? SegmentoCodigo { get; set; }

    public int Prioridad { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public bool Activa { get; set; }

    public bool Vigente { get; set; }

    public int Productos { get; set; }
}

public class ListaDetalleDto : ListaListadoDto
{
    public List<ItemListaDto> Items { get; set; } = new List<ItemListaDto>();
}

public class ItemListaDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public decimal PrecioBase { get; set; }

    public decimal Precio { get; set; }

    public decimal DiferenciaPorcentaje { get; set; }
}

public class PrecioResueltoDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public string? SegmentoCodigo { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Precio { get; set; }

    public decimal PrecioBase { get; set; }

    public string Origen { get; set; } = string.Empty;

    public int? ListaId { get; set; }

    public string? ListaCodigo { get; set; }
}

public class ResumenListasDto
{
    public int Total { get; set; }

    public int Activas { get; set; }

    public int Vigentes { get; set; }

    public int DeSegmento { get; set; }

    public int Generales { get; set; }

    public int ProductosConPrecioEspecial { get; set; }
}

public class ListaCreadaDto
{
    public int Id { get; set; }
}
