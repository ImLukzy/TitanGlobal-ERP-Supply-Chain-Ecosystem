using MediatR;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo33_CentrosTrabajo;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo33_CentrosTrabajo.Commands;

public class CrearCentroTrabajoCommand : IRequest<int>
{
    public required GuardarCentroTrabajoRequestDto Solicitud { get; set; }
}

public class CrearCentroTrabajoCommandHandler : IRequestHandler<CrearCentroTrabajoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public CrearCentroTrabajoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CrearCentroTrabajoCommand request, CancellationToken cancellationToken)
    {
        string codigo = ValidacionCentroTrabajo.NormalizarCodigo(request.Solicitud.Codigo);
        string nombre = ValidacionCentroTrabajo.NormalizarNombre(request.Solicitud.Nombre);
        string tipo = ValidacionCentroTrabajo.NormalizarTipo(request.Solicitud.Tipo);
        string planta = ValidacionCentroTrabajo.NormalizarPlanta(request.Solicitud.Planta);
        decimal capacidadHorasDia = ValidacionCentroTrabajo.ValidarCapacidad(request.Solicitud.CapacidadHorasDia);
        decimal costoHora = ValidacionCentroTrabajo.ValidarCostoHora(request.Solicitud.CostoHora);
        string? responsable = ValidacionCentroTrabajo.NormalizarResponsable(request.Solicitud.Responsable);
        bool codigoExiste = await _unitOfWork.Repository<CentroTrabajo>()
            .AsQueryable()
            .AnyAsync(centro => centro.Codigo == codigo);
        if (codigoExiste)
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        CentroTrabajo centroNuevo = new CentroTrabajo
        {
            Codigo = codigo,
            Nombre = nombre,
            Tipo = tipo,
            Planta = planta,
            CapacidadHorasDia = capacidadHorasDia,
            CostoHora = costoHora,
            Responsable = responsable,
            Activo = request.Solicitud.Activo
        };
        await _unitOfWork.Repository<CentroTrabajo>().AddAsync(centroNuevo);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (ViolacionRestriccionException excepcion) when (PersistenciaCentroTrabajo.EsViolacionCodigoUnico(excepcion))
        {
            throw PersistenciaCentroTrabajo.CrearConflictoCodigo(codigo);
        }

        return centroNuevo.Id;
    }
}
