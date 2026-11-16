namespace TitanGlobal.Application.DTOs.Modulo14_Licitaciones;

public class LicitacionDetalleDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public string Titulo { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public decimal PresupuestoReferencial { get; set; }

    public DateTime FechaCierre { get; set; }

    public int? ProveedorAdjudicadoId { get; set; }

    public string? ProveedorAdjudicadoNombre { get; set; }

    public decimal? MontoAdjudicado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public List<OfertaDto> Ofertas { get; set; } = new List<OfertaDto>();
}
