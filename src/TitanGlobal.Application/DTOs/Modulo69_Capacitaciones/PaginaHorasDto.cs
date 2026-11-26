namespace TitanGlobal.Application.DTOs.Modulo69_Capacitaciones;

public class PaginaHorasDto
{
    public int Anio { get; set; }

    public List<HorasEmpleadoDto> Items { get; set; } = new List<HorasEmpleadoDto>();

    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }
}
