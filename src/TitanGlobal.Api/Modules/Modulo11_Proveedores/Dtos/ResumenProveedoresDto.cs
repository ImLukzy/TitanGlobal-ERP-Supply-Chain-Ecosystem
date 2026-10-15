namespace TitanGlobal.Api.Modules.Modulo11_Proveedores.Dtos;

public class ResumenProveedoresDto
{
    public int TotalProveedores { get; set; }

    public int Activos { get; set; }

    public int Inactivos { get; set; }

    public int SinEmail { get; set; }

    public List<PaisProveedoresDto> PorPais { get; set; } = new List<PaisProveedoresDto>();
}
