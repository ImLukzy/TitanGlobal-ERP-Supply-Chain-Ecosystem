using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo66_Planillas;

// Días del mes natural que cubren los contratos de un empleado (decisión 1) y el sueldo vigente el último día cubierto (decisión 3).
internal static class CoberturaContratos
{
    public class Cobertura
    {
        public int DiasContrato { get; set; }

        public ContratoLaboral Contrato { get; set; } = null!;

        public decimal SueldoBasico { get; set; }

        public string Cargo { get; set; } = string.Empty;
    }

    // Un contrato cubre desde FechaInicio hasta FechaCierre ?? FechaFin ?? infinito, sin pasar de la fecha de cese del empleado.
    // Los contratos anulados no cubren nada. Devuelve null si ningún día del mes queda cubierto.
    public static Cobertura? Calcular(IEnumerable<ContratoLaboral> contratos, int anio, int mes, DateOnly? fechaCese)
    {
        List<ContratoLaboral> vigentes = contratos
            .Where(contrato => contrato.Estado != ValidacionPlanilla.EstadoContratoAnulado)
            .ToList();
        int diasDelMes = DateTime.DaysInMonth(anio, mes);
        int cubiertos = 0;
        DateOnly? ultimoDia = null;
        ContratoLaboral? ultimoContrato = null;
        for (int dia = 1; dia <= diasDelMes; dia++)
        {
            DateOnly fecha = new DateOnly(anio, mes, dia);
            if (fechaCese is not null && fecha > fechaCese.Value)
            {
                break;
            }

            ContratoLaboral? cubre = vigentes
                .Where(contrato => contrato.FechaInicio <= fecha && fecha <= (contrato.FechaCierre ?? contrato.FechaFin ?? DateOnly.MaxValue))
                .OrderByDescending(contrato => contrato.FechaInicio)
                .FirstOrDefault();
            if (cubre is null)
            {
                continue;
            }

            cubiertos++;
            ultimoDia = fecha;
            ultimoContrato = cubre;
        }

        if (cubiertos == 0 || ultimoDia is null || ultimoContrato is null)
        {
            return null;
        }

        AdendaContrato? adenda = ultimoContrato.AdendasContrato
            .Where(candidata => candidata.FechaVigencia <= ultimoDia.Value)
            .OrderBy(candidata => candidata.FechaVigencia)
            .LastOrDefault();
        return new Cobertura
        {
            DiasContrato = cubiertos == diasDelMes ? CalculadoraRemuneracion.DiasMesComercial : Math.Min(cubiertos, CalculadoraRemuneracion.DiasMesComercial),
            Contrato = ultimoContrato,
            SueldoBasico = adenda?.SueldoBasico ?? ultimoContrato.SueldoBasico,
            Cargo = adenda?.Cargo ?? ultimoContrato.Cargo
        };
    }
}
