-- Módulo 22: unicidad de nombre de categoría sin distinguir mayúsculas ni espacios exteriores
CREATE UNIQUE INDEX IF NOT EXISTS "IX_CategoriasProducto_Nombre_Normalizado"
    ON "CategoriasProducto" (lower(btrim("Nombre")));
