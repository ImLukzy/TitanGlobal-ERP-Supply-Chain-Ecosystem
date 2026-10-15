using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Api.Middlewares;
using TitanGlobal.Api.Models;
using TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.Dtos;
using TitanGlobal.Api.Repositories.Interfaces;

namespace TitanGlobal.Api.Modules.Modulo77_AuditoriaTransaccional.UseCases;

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
                StatusCodes.Status404NotFound,
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
