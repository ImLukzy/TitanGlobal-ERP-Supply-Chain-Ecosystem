namespace TitanGlobal.Application.DTOs.Modulo01_Clientes;

public class GuardarClienteRequestDto
{
    public string Nombre { get; set; } = string.Empty;

    public string Ruc { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}
