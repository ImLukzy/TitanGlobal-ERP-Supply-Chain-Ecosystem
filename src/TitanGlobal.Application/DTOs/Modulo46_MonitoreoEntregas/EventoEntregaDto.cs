namespace TitanGlobal.Application.DTOs.Modulo46_MonitoreoEntregas;

public class EventoEntregaDto
{
    public int Id { get; set; }

    public string Estado { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}
