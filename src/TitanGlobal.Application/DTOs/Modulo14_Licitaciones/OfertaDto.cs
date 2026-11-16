namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class OfertaDto
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string ProveedorNombre { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public int PlazoEntregaDias { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaRegistro { get; set; }

    public bool EsGanadora { get; set; }
}
