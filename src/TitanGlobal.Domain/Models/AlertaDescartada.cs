namespace TitanGlobal.Domain.Models;

public class AlertaDescartada
{
    public int Id { get; set; }

    public string Clave { get; set; } = string.Empty;

    public DateTime FechaDescarte { get; set; }
}
