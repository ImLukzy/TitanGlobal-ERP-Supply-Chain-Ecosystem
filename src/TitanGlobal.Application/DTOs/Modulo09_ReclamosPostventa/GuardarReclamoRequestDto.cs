namespace TitanGlobal.Application.DTOs.Modulo09_ReclamosPostventa;

public class GuardarReclamoRequestDto
{
    public int ClienteId { get; set; }

    public int OrdenCompraId { get; set; }

    public int? ProductoId { get; set; }

    public string Tipo { get; set; } = string.Empty;

    public string Prioridad { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;
}
