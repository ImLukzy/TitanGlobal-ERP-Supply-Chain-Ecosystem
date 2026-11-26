namespace TitanGlobal.Application.DTOs.Modulo29_AjustesInventario;

public class ResumenAjustesInventarioDto
{
    public int TotalAjustes { get; set; }

    public int TotalIncrementos { get; set; }

    public int TotalDecrementos { get; set; }

    public decimal ValorIncrementos { get; set; }

    public decimal ValorDecrementos { get; set; }

    public decimal ValorNeto { get; set; }
}
