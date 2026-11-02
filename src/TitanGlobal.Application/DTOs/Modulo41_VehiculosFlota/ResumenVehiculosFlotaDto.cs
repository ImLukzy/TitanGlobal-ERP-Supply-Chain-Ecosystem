namespace TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;

public class ResumenVehiculosFlotaDto
{
    public int TotalVehiculos { get; set; }

    public int Activos { get; set; }

    public int Inactivos { get; set; }

    public decimal CapacidadTotalKg { get; set; }

    public List<TipoVehiculoDto> PorTipo { get; set; } = new List<TipoVehiculoDto>();
}
