namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class CrearOfertaRequestDto
{
    public int ProveedorId { get; set; }

    public decimal Monto { get; set; }

    public int PlazoEntregaDias { get; set; }

    public string? Observaciones { get; set; }
}
