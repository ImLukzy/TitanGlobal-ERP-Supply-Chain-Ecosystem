-- Módulo 42: unicidad sin distinguir mayúsculas, conservando el documento solicitado.
-- El índice original se mantiene por compatibilidad con el modelo generado.
-- Los duplicados existentes hacen fallar la creación; no se borran ni combinan conductores.
CREATE UNIQUE INDEX "UX_Conductores_DocumentoLicenciaNormalizado"
    ON "Conductores" (lower(btrim("DocumentoLicencia")));
