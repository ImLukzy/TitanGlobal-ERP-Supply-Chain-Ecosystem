namespace TitanGlobal.Application.DTOs.Modulo11_Proveedores;

public class ResumenProveedoresDto
{
    public int TotalProveedores { get; set; }

    public int Activos { get; set; }

    public int Inactivos { get; set; }

    public int SinEmail { get; set; }

    public List<PaisProveedoresDto> PorPais { get; set; } = new List<PaisProveedoresDto>();
}
