namespace TitanGlobal.Application.DTOs.Modulo41_VehiculosFlota;

public class PaginaVehiculosFlotaDto
{
    public List<VehiculoFlotaListadoDto> Items { get; set; } = new List<VehiculoFlotaListadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
