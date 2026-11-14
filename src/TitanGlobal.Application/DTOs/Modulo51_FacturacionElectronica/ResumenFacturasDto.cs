namespace TitanGlobal.Application.DTOs.Modulo51_FacturacionElectronica;

public class ResumenFacturasDto
{
    public int TotalComprobantes { get; set; }

    public int TotalFacturas { get; set; }

    public int TotalBoletas { get; set; }

    public int TotalBorradores { get; set; }

    public int TotalEmitidas { get; set; }

    public int TotalAnuladas { get; set; }

    public string Moneda { get; set; } = "PEN";

    public decimal SubtotalFacturado { get; set; }

    public decimal TotalDescuentos { get; set; }

    public decimal TotalImpuestos { get; set; }

    public decimal TotalFacturado { get; set; }
}
