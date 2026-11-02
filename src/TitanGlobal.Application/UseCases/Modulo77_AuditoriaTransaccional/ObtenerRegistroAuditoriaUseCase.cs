using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional;

public class ObtenerRegistroAuditoriaUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerRegistroAuditoriaUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RegistroDetalleDto> ExecuteAsync(long id)
    {
        RegistroAuditoria? registro = await _unitOfWork.Repository<RegistroAuditoria>().AsQueryable()
            .FirstOrDefaultAsync(auditoria => auditoria.Id == id);
        if (registro == null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "El registro de auditoría " + id + " no existe.");
        }

        return new RegistroDetalleDto
        {
            Id = registro.Id,
            Fecha = registro.Fecha,
            Entidad = registro.Entidad,
            EntidadId = registro.EntidadId,
            Accion = registro.Accion,
            Usuario = registro.Usuario,
            Ruta = registro.Ruta,
            CamposCambiados = ListarAuditoriaUseCase.LeerCambios(registro.Cambios).Count,
            Cambios = ListarAuditoriaUseCase.LeerCambios(registro.Cambios)
        };
    }
}
