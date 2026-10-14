namespace TitanGlobal.Api.Modules.Modulo01_Clientes.Dtos;

public class GuardarClienteRequestDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
