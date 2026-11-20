namespace TitanGlobal.Application.DTOs.Modulo62_ContratosLaborales;

public class ContratoLaboralListadoDto
{
    public int Id { get; set; }

    public string Numero { get; set; } = string.Empty;

    public int EmpleadoId { get; set; }

    public string EmpleadoLegajo { get; set; } = string.Empty;

    public string EmpleadoNombre { get; set; } = string.Empty;

    public string Modalidad { get; set; } = string.Empty;

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public decimal SueldoBasico { get; set; }

    public string Cargo { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
}
