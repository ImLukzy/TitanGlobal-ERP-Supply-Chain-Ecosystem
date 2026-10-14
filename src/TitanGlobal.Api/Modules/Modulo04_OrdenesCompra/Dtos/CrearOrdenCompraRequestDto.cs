namespace TitanGlobal.Api.Modules.Modulo04_OrdenesCompra.Dtos;

public class CrearOrdenCompraRequestDto
{
    public string NumeroOrden { get; set; } = string.Empty;

    public int ClienteId { get; set; }

    public decimal Total { get; set; }

    public string? Descripcion { get; set; }
}
