namespace TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;

public class PaginaDevolucionesDto
{
    public List<DevolucionListadoDto> Items { get; set; } = new List<DevolucionListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}

public class DevolucionListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public int ClienteId { get; set; }

    public string ClienteNombre { get; set; } = string.Empty;

    public int OrdenCompraId { get; set; }

    public string NumeroOrden { get; set; } = string.Empty;

    public string? ReclamoNumero { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public int Unidades { get; set; }

    public decimal Total { get; set; }
}

public class DevolucionDetalleDto : DevolucionListadoDto
{
    public string? Observaciones { get; set; }

    public string? MotivoRechazo { get; set; }

    public DateTime? FechaAprobacion { get; set; }

    public DateTime? FechaRecepcion { get; set; }

    public DateTime? FechaRechazo { get; set; }

    public List<LineaDevolucionDto> Lineas { get; set; } = new List<LineaDevolucionDto>();
}

public class LineaDevolucionDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Importe { get; set; }
}

public class ProductoDisponibleDto
{
    public int ProductoId { get; set; }

    public string ProductoNombre { get; set; } = string.Empty;

    public int Comprado { get; set; }

    public int Devuelto { get; set; }

    public int Disponible { get; set; }

    public decimal PrecioUnitario { get; set; }
}

public class ResumenDevolucionesDto
{
    public int Total { get; set; }

    public int Solicitadas { get; set; }

    public int Aprobadas { get; set; }

    public int Recibidas { get; set; }

    public int Rechazadas { get; set; }

    public decimal MontoReembolsado { get; set; }

    public List<MotivoDevolucionesDto> PorMotivo { get; set; } = new List<MotivoDevolucionesDto>();

    public decimal TasaDevolucion { get; set; }
}

public class MotivoDevolucionesDto
{
    public string Motivo { get; set; } = string.Empty;

    public int Devoluciones { get; set; }

    public int Unidades { get; set; }
}

public class DevolucionCreadaDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;
}
