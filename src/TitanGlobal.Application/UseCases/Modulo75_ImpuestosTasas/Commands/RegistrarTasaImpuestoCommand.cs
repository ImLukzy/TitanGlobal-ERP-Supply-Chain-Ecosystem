using MediatR;
using System;
using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo75_ImpuestosTasas;
using TitanGlobal.Domain.Repositories;
using TitanGlobal.Domain.Models;
using TitanGlobal.Application.Exceptions;

namespace TitanGlobal.Application.UseCases.Modulo75_ImpuestosTasas.Commands;

public class RegistrarTasaImpuestoCommand : IRequest<int>
{
    public int ImpuestoId { get; set; }

    public required GuardarTasaImpuestoRequestDto Dto { get; set; }
}

public class RegistrarTasaImpuestoCommandHandler : IRequestHandler<RegistrarTasaImpuestoCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public RegistrarTasaImpuestoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(RegistrarTasaImpuestoCommand request, CancellationToken cancellationToken)
    {
        Impuesto? impuesto = await _unitOfWork.Repository<Impuesto>()
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == request.ImpuestoId);

        if (impuesto == null || !impuesto.Activo)
            throw new ReglaNegocioException(
                CodigosEstado.Status404NotFound,
                "El impuesto no existe o está inactivo.");

        ValidacionImpuesto.ValidarTasa(
            request.Dto.Porcentaje,
            request.Dto.FechaInicio,
            request.Dto.FechaFin,
            request.Dto.Observacion);

        DateTime fechaInicio = ValidacionImpuesto.NormalizarDia(request.Dto.FechaInicio);
        DateTime? fechaFinGuardada = request.Dto.FechaFin.HasValue
            ? ValidacionImpuesto.NormalizarDia(request.Dto.FechaFin.Value)
            : null;
        DateTime fechaFin = fechaFinGuardada
            ?? ValidacionImpuesto.NormalizarDia(DateTime.MaxValue);

        bool existeCruce = await _unitOfWork.Repository<TasaImpuesto>()
            .AsQueryable()
            .AnyAsync(t =>
                t.ImpuestoId == request.ImpuestoId &&
                t.Activo &&
                t.FechaInicio <= fechaFin &&
                (!t.FechaFin.HasValue || t.FechaFin.Value >= fechaInicio));

        if (existeCruce)
            throw new ReglaNegocioException(
                CodigosEstado.Status400BadRequest,
                "Ya existe una tasa activa para ese periodo.");

        TasaImpuesto tasa = new TasaImpuesto
        {
            ImpuestoId = request.ImpuestoId,
            Porcentaje = request.Dto.Porcentaje,
            FechaInicio = fechaInicio,
            FechaFin = fechaFinGuardada,
            Activo = true,
            Observacion = request.Dto.Observacion
        };

        await _unitOfWork.Repository<TasaImpuesto>().AddAsync(tasa);
        await _unitOfWork.CompleteAsync();

        return tasa.Id;
    }
}
