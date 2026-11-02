namespace TitanGlobal.Application.DTOs.Modulo11_Proveedores;

public class PaginaProveedoresDto
{
    public List<ProveedorListadoDto> Items { get; set; } = new List<ProveedorListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
