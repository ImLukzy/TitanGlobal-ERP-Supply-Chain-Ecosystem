using TitanGlobal.Application.DTOs.Modulo73_Sucursales;
using TitanGlobal.Domain.Models;
namespace TitanGlobal.Application.UseCases.Modulo73_Sucursales;

internal static class PresentacionSucursal
{
    public static SucursalListadoDto Listado(Sucursal sucursal)
    {
        SucursalListadoDto dto = new SucursalListadoDto();
        Asignar(sucursal, dto);
        return dto;
    }
    public static SucursalDetalleDto Detalle(Sucursal sucursal)
    {
        SucursalDetalleDto dto = new SucursalDetalleDto
        {
            Direccion = sucursal.Direccion,
            Contacto = sucursal.Contacto,
            Email = sucursal.Email,
            Telefono = sucursal.Telefono,
            Observaciones = sucursal.Observaciones,
            FechaApertura = sucursal.FechaApertura,
            FechaRegistro = sucursal.FechaRegistro,
            FechaCambioEstado = sucursal.FechaCambioEstado
        };
        Asignar(sucursal, dto);
        return dto;
    }
    private static void Asignar(Sucursal sucursal, SucursalListadoDto dto)
    {
        dto.Id = sucursal.Id;
        dto.Codigo = sucursal.Codigo;
        dto.Nombre = sucursal.Nombre;
        dto.Tipo = sucursal.Tipo;
        dto.Pais = sucursal.Pais;
        dto.Ciudad = sucursal.Ciudad;
        dto.Activa = sucursal.Activa;
    }
}
