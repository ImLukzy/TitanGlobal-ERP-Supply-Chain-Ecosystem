using TitanGlobal.Domain.Models;

namespace TitanGlobal.Application.UseCases.Modulo61_Empleados;

// Lógica compartida por los commands y queries de Empleados.
internal static class OperacionesEmpleados
{
    internal static void AplicarDatos(Empleado empleado, ValidacionEmpleado.DatosEmpleado datos)
    {
        empleado.TipoDocumento = datos.TipoDocumento;
        empleado.NumeroDocumento = datos.NumeroDocumento;
        empleado.Nombres = datos.Nombres;
        empleado.Apellidos = datos.Apellidos;
        empleado.FechaNacimiento = datos.FechaNacimiento;
        empleado.FechaIngreso = datos.FechaIngreso;
        empleado.Cargo = datos.Cargo;
        empleado.Area = datos.Area;
        empleado.Email = datos.Email;
        empleado.Telefono = datos.Telefono;
    }
}
