namespace TitanGlobal.Application.DTOs.Modulo36_ControlCalidadInsumos;

public sealed record ProveedorInspeccionesInsumosDto(
    int ProveedorId,
    string ProveedorRazonSocial,
    int Aprobadas,
    int Rechazadas,
    decimal TasaRechazoPorcentaje);
