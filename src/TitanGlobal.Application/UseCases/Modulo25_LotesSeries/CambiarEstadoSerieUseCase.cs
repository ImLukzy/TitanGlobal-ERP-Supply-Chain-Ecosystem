using Microsoft.EntityFrameworkCore;
using TitanGlobal.Application.DTOs.Modulo25_LotesSeries;
using TitanGlobal.Domain.Models;
using TitanGlobal.Domain.Repositories;

namespace TitanGlobal.Application.UseCases.Modulo25_LotesSeries;

public class CambiarEstadoSerieUseCase
{
    private readonly IUnitOfWork _unitOfWork;

    public CambiarEstadoSerieUseCase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> ExecuteAsync(int id, CambiarEstadoSerieRequestDto solicitud)
    {
        UnidadSerie? serie = await _unitOfWork.Repository<UnidadSerie>()
            .AsQueryable()
            .SingleOrDefaultAsync(registro => registro.Id == id);
        if (serie is null)
        {
            throw ValidacionLoteSerie.CrearSerieNoEncontrada(id);
        }

        string estadoNuevo = ValidacionLoteSerie.NormalizarEstadoSerie(solicitud.Estado);
        ValidacionLoteSerie.ValidarTransicionSerie(serie.Estado, estadoNuevo);

        serie.Estado = estadoNuevo;
        await _unitOfWork.CompleteAsync();

        return serie.Id;
    }
}
