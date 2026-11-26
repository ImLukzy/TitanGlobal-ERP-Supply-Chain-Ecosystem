using TitanGlobal.Application.DTOs.Modulo64_TurnosHorarios;
using TitanGlobal.Application.UseCases.Modulo63_AsistenciaBiometrica;
using TitanGlobal.Application.UseCases.Modulo64_TurnosHorarios;
using TitanGlobal.Domain.Models;

namespace TitanGlobal.Api.Tests.Modulo63_AsistenciaBiometrica;

// Cálculo puro de la jornada: sin base de datos ni HTTP. Hora local = UTC-5.
public class CalculadoraJornadaTests
{
    private static readonly DateOnly Lunes = new DateOnly(2026, 1, 5);

    [Fact]
    public void PuntualConElTurnoCumplidoNoTieneTardanzaNiHorasExtra()
    {
        JornadaAsistencia jornada = Calcular(DiaFijo(), Entrada(8, 0), Salida(17, 0));
        Assert.Equal("Asistencia", jornada.Estado);
        Assert.Equal(540, jornada.MinutosTrabajados);
        Assert.Equal(0, jornada.MinutosTardanza);
        Assert.Equal(0, jornada.MinutosExtra);
        Assert.Equal("NoAplica", jornada.EstadoHorasExtra);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 5, 13, 0), jornada.Entrada);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 5, 22, 0), jornada.Salida);
        Assert.Equal(new TimeOnly(8, 0), jornada.HoraEntradaProgramada);
        Assert.Equal(new TimeOnly(17, 0), jornada.HoraSalidaProgramada);
        Assert.Equal(10, jornada.ToleranciaMinutos);
        Assert.Equal(7, jornada.TurnoId);
        Assert.Equal(Lunes, jornada.Fecha);
    }

    [Theory]
    [InlineData(8, 10, 0, "Asistencia")]
    [InlineData(8, 11, 1, "Tardanza")]
    [InlineData(8, 25, 15, "Tardanza")]
    public void LaTardanzaSeMideDespuesDeLaTolerancia(int hora, int minuto, int tardanza, string estado)
    {
        JornadaAsistencia jornada = Calcular(DiaFijo(), Entrada(hora, minuto), Salida(17, 0));
        Assert.Equal(tardanza, jornada.MinutosTardanza);
        Assert.Equal(estado, jornada.Estado);
    }

    [Fact]
    public void SinToleranciaCualquierMinutoTardeEsTardanza()
    {
        DiaProgramadoDto dia = DiaFijo();
        dia.ToleranciaMinutos = 0;
        JornadaAsistencia jornada = Calcular(dia, Entrada(8, 1), Salida(17, 0));
        Assert.Equal(1, jornada.MinutosTardanza);
        Assert.Equal("Tardanza", jornada.Estado);
        Assert.Equal(0, jornada.ToleranciaMinutos);
    }

    [Fact]
    public void LasHorasExtraSumanLoTrabajadoAntesDeLaEntradaYDespuesDeLaSalida()
    {
        JornadaAsistencia jornada = Calcular(DiaFijo(), Entrada(7, 30), Salida(18, 15));
        Assert.Equal(645, jornada.MinutosTrabajados);
        Assert.Equal(105, jornada.MinutosExtra);
        Assert.Equal("Pendiente", jornada.EstadoHorasExtra);
        Assert.Equal("Asistencia", jornada.Estado);
    }

    [Fact]
    public void LasHorasExtraTienenUnTopeDiarioDeDoscientosCuarentaMinutos()
    {
        JornadaAsistencia jornada = Calcular(DiaFijo(), Entrada(5, 0), Salida(20, 0));
        Assert.Equal(900, jornada.MinutosTrabajados);
        Assert.Equal(CalculadoraJornada.TopeMinutosExtraDiario, jornada.MinutosExtra);
        Assert.Equal(240, jornada.MinutosExtra);
        Assert.Equal("Pendiente", jornada.EstadoHorasExtra);
    }

    [Fact]
    public void UnTurnoNocturnoQueCruzaMedianocheSeCalculaEnLaFechaDeLaEntrada()
    {
        // Turno 22:00-06:00 con tolerancia de 5 minutos; el lunes 5 entra a las 22:03 local y sale el martes a las 06:00.
        Turno noche = DatosAsistencia.CrearTurnoNoche("NOCHE");
        noche.Id = 9;
        DiaProgramadoDto dia = CalculadoraTurnos.CalcularDia(noche, Lunes, null);
        JornadaAsistencia jornada = Calcular(dia, Entrada(22, 3), SalidaDiaSiguiente(6, 0));
        Assert.Equal("Asistencia", jornada.Estado);
        Assert.Equal(477, jornada.MinutosTrabajados);
        Assert.Equal(0, jornada.MinutosTardanza);
        Assert.Equal(0, jornada.MinutosExtra);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 6, 3, 3), jornada.Entrada);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 6, 11, 0), jornada.Salida);
        Assert.Equal(new TimeOnly(6, 0), jornada.HoraSalidaProgramada);

        JornadaAsistencia conExtra = Calcular(dia, Entrada(22, 10), SalidaDiaSiguiente(7, 30));
        Assert.Equal(5, conExtra.MinutosTardanza);
        Assert.Equal(90, conExtra.MinutosExtra);
        Assert.Equal("Tardanza", conExtra.Estado);
    }

    [Fact]
    public void SinLaSalidaOSinLaEntradaLaJornadaQuedaSinMarcacion()
    {
        JornadaAsistencia soloEntrada = Calcular(DiaFijo(), Entrada(8, 0));
        Assert.Equal("SinMarcacion", soloEntrada.Estado);
        Assert.NotNull(soloEntrada.Entrada);
        Assert.Null(soloEntrada.Salida);
        Assert.Equal(0, soloEntrada.MinutosTrabajados);
        Assert.Equal(0, soloEntrada.MinutosExtra);
        Assert.Equal("NoAplica", soloEntrada.EstadoHorasExtra);

        JornadaAsistencia soloSalida = Calcular(DiaFijo(), Salida(17, 0));
        Assert.Equal("SinMarcacion", soloSalida.Estado);
        Assert.Null(soloSalida.Entrada);
        Assert.NotNull(soloSalida.Salida);

        // Una salida anterior a la entrada no cuenta como salida de esa entrada.
        JornadaAsistencia salidaAntes = Calcular(DiaFijo(), Salida(6, 0), Entrada(8, 0));
        Assert.Equal("SinMarcacion", salidaAntes.Estado);
    }

    [Fact]
    public void UnDiaLaborableSinNingunaMarcacionEnLaVentanaEsFalta()
    {
        JornadaAsistencia sinMarcas = Calcular(DiaFijo());
        Assert.Equal("Falta", sinMarcas.Estado);
        Assert.Equal(0, sinMarcas.MinutosTrabajados);
        Assert.Equal("NoAplica", sinMarcas.EstadoHorasExtra);
        Assert.Null(sinMarcas.Entrada);
        // 03:00 local queda fuera de la ventana (desde las 04:00).
        JornadaAsistencia fueraDeVentana = Calcular(DiaFijo(), Entrada(3, 0), Salida(3, 30));
        Assert.Equal("Falta", fueraDeVentana.Estado);
    }

    [Fact]
    public void UnDiaDeDescansoTrabajadoCuentaTodoLoTrabajadoComoHoraExtra()
    {
        Turno fijo = DatosAsistencia.CrearTurnoDia("DIA");
        fijo.Id = 7;
        DateOnly sabado = new DateOnly(2026, 1, 10);
        DiaProgramadoDto dia = CalculadoraTurnos.CalcularDia(fijo, sabado, null);
        JornadaAsistencia jornada = CalculadoraJornada.Calcular(
            dia,
            new List<Marcacion> { Marca(1, "Entrada", 2026, 1, 10, 9, 0), Marca(1, "Salida", 2026, 1, 10, 12, 0) })!;
        Assert.Equal("DescansoTrabajado", jornada.Estado);
        Assert.Equal(180, jornada.MinutosTrabajados);
        Assert.Equal(180, jornada.MinutosExtra);
        Assert.Equal("Pendiente", jornada.EstadoHorasExtra);
        Assert.Equal(0, jornada.MinutosTardanza);
        Assert.Null(jornada.HoraEntradaProgramada);
        Assert.Null(jornada.HoraSalidaProgramada);
        Assert.Equal(7, jornada.TurnoId);
    }

    [Fact]
    public void UnDiaNoLaborableTambienTieneTopeYSinMarcacionesNoGeneraJornada()
    {
        DiaProgramadoDto domingo = CalculadoraTurnos.CalcularDia(DatosAsistencia.CrearTurnoDia("DIA"), new DateOnly(2026, 1, 11), null);
        Assert.Null(CalculadoraJornada.Calcular(domingo, new List<Marcacion>()));
        JornadaAsistencia jornada = CalculadoraJornada.Calcular(
            domingo,
            new List<Marcacion> { Marca(1, "Entrada", 2026, 1, 11, 6, 0), Marca(1, "Salida", 2026, 1, 11, 20, 0) })!;
        Assert.Equal(840, jornada.MinutosTrabajados);
        Assert.Equal(240, jornada.MinutosExtra);
        // Con una sola marca el día de descanso queda sin marcación completa.
        JornadaAsistencia incompleta = CalculadoraJornada.Calcular(domingo, new List<Marcacion> { Marca(1, "Entrada", 2026, 1, 11, 6, 0) })!;
        Assert.Equal("SinMarcacion", incompleta.Estado);
        // Una marcación de la noche anterior no entra en la ventana de 00:00 a 23:59 del domingo.
        Assert.Null(CalculadoraJornada.Calcular(domingo, new List<Marcacion> { Marca(1, "Entrada", 2026, 1, 10, 23, 30) }));
    }

    [Fact]
    public void UnTurnoRotativoEnSuDiaDeDescansoSeCalculaComoDescansoTrabajado()
    {
        Turno rotativo = DatosAsistencia.CrearTurnoRotativo("ROT4X2");
        rotativo.Id = 3;
        // El ciclo empieza el 2026-01-05: trabaja del 5 al 8 y descansa el 9 y el 10.
        DiaProgramadoDto trabajo = CalculadoraTurnos.CalcularDia(rotativo, new DateOnly(2026, 1, 8), null);
        JornadaAsistencia normal = CalculadoraJornada.Calcular(
            trabajo,
            new List<Marcacion> { Marca(1, "Entrada", 2026, 1, 8, 7, 0), Marca(1, "Salida", 2026, 1, 8, 19, 0) })!;
        Assert.Equal("Asistencia", normal.Estado);
        Assert.Equal(720, normal.MinutosTrabajados);

        DiaProgramadoDto descanso = CalculadoraTurnos.CalcularDia(rotativo, new DateOnly(2026, 1, 9), null);
        JornadaAsistencia trabajado = CalculadoraJornada.Calcular(
            descanso,
            new List<Marcacion> { Marca(1, "Entrada", 2026, 1, 9, 7, 0), Marca(1, "Salida", 2026, 1, 9, 19, 0) })!;
        Assert.Equal("DescansoTrabajado", trabajado.Estado);
        Assert.Equal(240, trabajado.MinutosExtra);
        Assert.Equal(720, trabajado.MinutosTrabajados);
        Assert.Equal(3, trabajado.TurnoId);
    }

    [Fact]
    public void ElEmpleadoSinTurnoQueMarcaGeneraDescansoTrabajadoSinTurno()
    {
        DiaProgramadoDto sinTurno = CalculadoraTurnos.CalcularDia(null, Lunes, null);
        JornadaAsistencia jornada = CalculadoraJornada.Calcular(
            sinTurno,
            new List<Marcacion> { Marca(1, "Entrada", 2026, 1, 5, 8, 0), Marca(1, "Salida", 2026, 1, 5, 10, 0) })!;
        Assert.Equal("DescansoTrabajado", jornada.Estado);
        Assert.Null(jornada.TurnoId);
        Assert.Equal(120, jornada.MinutosExtra);
    }

    [Fact]
    public void LaVentanaYLasConversionesUsanElDesfaseDeLimaYLaFechaLocalDeLaEntradaProgramada()
    {
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 5, 13, 0), CalculadoraJornada.LocalAUtc(new DateTime(2026, 1, 5, 8, 0, 0)));
        Assert.Equal(new DateOnly(2026, 1, 4), CalculadoraJornada.FechaLocal(DatosAsistencia.Utc(2026, 1, 5, 4, 59)));
        Assert.Equal(new DateOnly(2026, 1, 5), CalculadoraJornada.FechaLocal(DatosAsistencia.Utc(2026, 1, 5, 5, 0)));
        (DateTime inicio, DateTime fin) = CalculadoraJornada.Ventana(Lunes, new TimeOnly(22, 0), new TimeOnly(6, 0));
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 5, 23, 0), inicio);
        Assert.Equal(DatosAsistencia.Utc(2026, 1, 6, 17, 0), fin);
    }

    private static DiaProgramadoDto DiaFijo()
    {
        Turno fijo = DatosAsistencia.CrearTurnoDia("DIA");
        fijo.Id = 7;
        return CalculadoraTurnos.CalcularDia(fijo, Lunes, null);
    }

    private static JornadaAsistencia Calcular(DiaProgramadoDto dia, params Marcacion[] marcaciones)
    {
        JornadaAsistencia? jornada = CalculadoraJornada.Calcular(dia, marcaciones);
        Assert.NotNull(jornada);
        return jornada;
    }

    private static Marcacion Entrada(int hora, int minuto)
    {
        return Marca(1, "Entrada", 2026, 1, 5, hora, minuto);
    }

    private static Marcacion Salida(int hora, int minuto)
    {
        return Marca(1, "Salida", 2026, 1, 5, hora, minuto);
    }

    private static Marcacion SalidaDiaSiguiente(int hora, int minuto)
    {
        return Marca(1, "Salida", 2026, 1, 6, hora, minuto);
    }

    // Instante dado en hora local de Lima.
    private static Marcacion Marca(int empleadoId, string tipo, int anio, int mes, int dia, int hora, int minuto)
    {
        DateTime local = new DateTime(anio, mes, dia, hora, minuto, 0);
        return DatosAsistencia.CrearMarcacion(empleadoId, tipo, CalculadoraJornada.LocalAUtc(local));
    }
}
