using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo24_ZonasAlmacenaje;

internal static class PersistenciaZonaAlmacenaje
{
    public static bool EsViolacionIndiceUnico(ViolacionRestriccionException excepcion)
    {
        return excepcion.Tipo == TipoViolacionRestriccion.Unicidad;
    }
}
