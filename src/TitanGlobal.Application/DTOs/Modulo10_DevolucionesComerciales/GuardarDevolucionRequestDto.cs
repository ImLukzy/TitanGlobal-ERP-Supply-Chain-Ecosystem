namespace TitanGlobal.Application.DTOs.Modulo10_DevolucionesComerciales;

public class GuardarDevolucionRequestDto
{
    public int ClienteId { get; set; }

    public int OrdenCompraId { get; set; }

    public int? ReclamoPostventaId { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public List<LineaGuardarDevolucionDto> Lineas { get; set; } = new List<LineaGuardarDevolucionDto>();
}

public class LineaGuardarDevolucionDto
{
    public int ProductoId { get; set; }

    public int Cantidad { get; set; }
}
