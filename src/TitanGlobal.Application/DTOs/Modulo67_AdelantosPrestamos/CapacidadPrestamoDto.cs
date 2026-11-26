namespace TitanGlobal.Application.DTOs.Modulo67_AdelantosPrestamos;

public class CapacidadPrestamoDto
{
    public int EmpleadoId { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal SueldoVigente { get; set; }

    public decimal NetoEstimado { get; set; }

    public decimal CuotaMaxima { get; set; }

    public List<CuotaComprometidaDto> CuotasComprometidasPorMes { get; set; } = new List<CuotaComprometidaDto>();
}
