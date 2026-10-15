namespace TitanGlobal.Api.Modules.Modulo23_Almacenes.Dtos;

public class ResumenAlmacenesDto
{
    public int TotalAlmacenes { get; set; }

    public int Activos { get; set; }

    public int Inactivos { get; set; }

    public decimal CapacidadTotalM3 { get; set; }

    public decimal CapacidadActivaM3 { get; set; }

    public List<TipoAlmacenesDto> PorTipo { get; set; } = new List<TipoAlmacenesDto>();

    public List<CiudadAlmacenesDto> PorCiudad { get; set; } = new List<CiudadAlmacenesDto>();
}
