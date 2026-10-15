namespace TitanGlobal.Api.Modules.Modulo13_SolicitudesCompra.Dtos;

public class ProductoSolicitadoDto
{
    public int ProductoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public int Unidades { get; set; }
}
