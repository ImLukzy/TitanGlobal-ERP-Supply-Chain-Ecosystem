-- Módulo 41: conserva el formato de la placa y rechaza duplicados sin distinguir mayúsculas.
-- Se conserva IX_VehiculosFlota_Placa, utilizado por el ON CONFLICT del seed.
-- Si existen duplicados normalizados, la creación falla sin borrar ni combinar vehículos.
CREATE UNIQUE INDEX "UX_VehiculosFlota_PlacaNormalizada"
    ON "VehiculosFlota" (lower(btrim("Placa")));
