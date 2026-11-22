namespace TitanGlobal.Application.DTOs.Modulo73_Sucursales;

public class SucursalListadoDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Pais { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public bool Activa { get; set; }
}
