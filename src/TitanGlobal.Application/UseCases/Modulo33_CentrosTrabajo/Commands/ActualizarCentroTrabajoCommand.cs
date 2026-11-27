using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Commands;

public class ActualizarCentroTrabajoCommand : IRequest<int>
{
    public int Id { get; set; }

    public required GuardarCentroTrabajoRequestDto Solicitud { get; set; }
}

public class ActualizarCentroTrabajoCommandHandler : IRequestHandler<ActualizarCentroTrabajoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public ActualizarCentroTrabajoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(ActualizarCentroTrabajoCommand request, CancellationToken cancellationToken)
    {
        CentroTrabajo? centro = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .SingleOrDefaultAsync(entidad => entidad.Id == request.Id);
        if (centro is null)
        {
            throw PersistenciaCentroTrabajo.CrearNoEncontrado(request.Id);
        }

        string codigo = ValidacionCentroTrabajo.NormalizarCodigo(request.Solicitud.Codigo);
        string nombre = ValidacionCentroTrabajo.NormalizarNombre(request.Solicitud.Nombre);
        string tipo = ValidacionCentroTrabajo.NormalizarTipo(request.Solicitud.Tipo);
        string planta = ValidacionCentroTrabajo.NormalizarPlanta(request.Solicitud.Planta);
        decimal capacidadHorasDia = ValidacionCentroTrabajo.ValidarCapacidad(request.Solicitud.CapacidadHorasDia);
        decimal costoHora = ValidacionCentroTrabajo.ValidarCostoHora(request.Solicitud.CostoHora);
        string? responsable = ValidacionCentroTrabajo.NormalizarResponsable(request.Solicitud.Responsable);
        bool codigoExiste = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .AnyAsync(entidad => entidad.Id != request.Id && entidad.Codigo == codigo);
        if (codigoExiste)
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        centro.Codigo = codigo;
        centro.Nombre = nombre;
        centro.Tipo = tipo;
        centro.Planta = planta;
        centro.CapacidadHorasDia = capacidadHorasDia;
        centro.CostoHora = costoHora;
        centro.Responsable = responsable;
        centro.Activo = request.Solicitud.Activo;
        _unitOfWork.Repository<CentroTrabajo>().Update(centro);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaCentroTrabajo.EsViolacionCodigoUnico(excepcion))
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        return centro.Id;
    }
}
