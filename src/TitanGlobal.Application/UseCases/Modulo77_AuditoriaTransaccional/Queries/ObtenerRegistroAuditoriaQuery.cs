using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo77_AuditoriaTransaccional;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo77_AuditoriaTransaccional.Queries;

public class ObtenerRegistroAuditoriaQuery : IRequest<RegistroDetalleDto>
{
    public long Id { get; set; }
}

public class ObtenerRegistroAuditoriaQueryHandler : IRequestHandler<ObtenerRegistroAuditoriaQuery, RegistroDetalleDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public ObtenerRegistroAuditoriaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<RegistroDetalleDto> Handle(ObtenerRegistroAuditoriaQuery request, CancellationToken cancellationToken)
    {
        RegistroAuditoria? registro = await _unitOfWork.Repository<RegistroAuditoria>().AsQueryable()
            .FirstOrDefaultAsync(auditoria => auditoria.Id == request.Id);
        if (registro == null)
        {
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "El registro de auditoría " + request.Id + " no existe.");
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
            CamposCambiados = OperacionesAuditoria.LeerCambios(registro.Cambios).Count,
            Cambios = OperacionesAuditoria.LeerCambios(registro.Cambios)
        };
    }
}
