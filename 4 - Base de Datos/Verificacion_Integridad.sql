/* =====================================================================
   VERIFICACION DE INTEGRIDAD - Edelstahl ERP  (solo lectura, salvo la prueba 6, que hace ROLLBACK)
   Replica en T-SQL los algoritmos de C#:
     DVH -> Edelstahl.Services\Integrity\ClienteDvhService.cs
     DVV -> Edelstahl.Domain\Integrity\ClienteDvvService.cs
   Ejecutar en SSMS conectado a localhost\SQLEXPRESS (SQL Server 2019+ por el collation UTF8).
   ===================================================================== */
USE EdelstahlNegocio;
GO

/* ---------- 1) Estado general: cantidad de clientes y DVH vacios ---------- */
SELECT COUNT(*) AS TotalClientes,
       SUM(CASE WHEN DVH IS NULL OR LTRIM(RTRIM(DVH)) = '' THEN 1 ELSE 0 END) AS ClientesSinDVH
FROM dbo.Clientes;
GO

/* ---------- 2) DVH: recalculado vs. almacenado, fila por fila ---------- */
;WITH Base AS (
    SELECT c.Id, c.RazonSocial, c.DVH AS DVH_Almacenado,
        CAST(N'' AS nvarchar(max))
        + N'Id:36:'   + LOWER(CONVERT(nvarchar(36), c.Id)) + N'|'
        + N'CUIT:'                 + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.CUIT)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.CUIT)),N'') + N'|'
        + N'RazonSocial:'          + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.RazonSocial)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.RazonSocial)),N'') + N'|'
        + N'DireccionFacturacion:' + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.DireccionFacturacion)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.DireccionFacturacion)),N'') + N'|'
        + N'DireccionEntrega:'     + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.DireccionEntrega)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.DireccionEntrega)),N'') + N'|'
        + N'Localidad:'            + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.Localidad)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.Localidad)),N'') + N'|'
        + N'Provincia:'            + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.Provincia)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.Provincia)),N'') + N'|'
        + N'CodigoPostal:'         + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.CodigoPostal)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.CodigoPostal)),N'') + N'|'
        + N'Email:'                + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.Email)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.Email)),N'') + N'|'
        + N'Telefono:'             + CAST(DATALENGTH(ISNULL(LTRIM(RTRIM(c.Telefono)),N''))/2 AS nvarchar(10)) + N':' + ISNULL(LTRIM(RTRIM(c.Telefono)),N'') + N'|'
        + N'LimiteCredito:' + CAST(LEN(n.Lim) AS nvarchar(10)) + N':' + n.Lim + N'|'
        + N'DeudaActual:'   + CAST(LEN(n.Deu) AS nvarchar(10)) + N':' + n.Deu + N'|'
        + N'Activo:1:' + CASE WHEN c.Activo = 1 THEN N'1' ELSE N'0' END + N'|'
        + N'FechaAlta:27:' + CONVERT(nvarchar(27), c.FechaAlta, 126) + N'|' AS Contenido
    FROM dbo.Clientes c
    -- C# usa ToString("G29"): quita los ceros decimales finales (300000.00 -> 300000, 203.40 -> 203.4)
    CROSS APPLY (SELECT
        Lim = REPLACE(REPLACE(REVERSE(SUBSTRING(REVERSE(CONVERT(varchar(40), c.LimiteCredito)), PATINDEX('%[^0]%', REVERSE(CONVERT(varchar(40), c.LimiteCredito))), 40)) + '|', '.|', ''), '|', ''),
        Deu = REPLACE(REPLACE(REVERSE(SUBSTRING(REVERSE(CONVERT(varchar(40), c.DeudaActual)), PATINDEX('%[^0]%', REVERSE(CONVERT(varchar(40), c.DeudaActual))), 40)) + '|', '.|', ''), '|', '')
    ) n
)
SELECT Id, RazonSocial, DVH_Almacenado,
       CONVERT(varchar(64), HASHBYTES('SHA2_256',
           CONVERT(varchar(max), Contenido COLLATE Latin1_General_100_CI_AS_SC_UTF8)), 2) AS DVH_Calculado,
       CASE WHEN DVH_Almacenado = CONVERT(varchar(64), HASHBYTES('SHA2_256',
           CONVERT(varchar(max), Contenido COLLATE Latin1_General_100_CI_AS_SC_UTF8)), 2)
            THEN 'OK' ELSE 'ALTERADO' END AS Estado
FROM Base
ORDER BY Estado DESC, RazonSocial;      -- las filas ALTERADO salen primero
GO

/* ---------- 3) DVV: recalculado (a partir de Id + DVH) vs. el de IntegridadTablas ---------- */
;WITH Orden AS (
    SELECT N'Id:36:' + LOWER(CONVERT(nvarchar(36), Id)) + N'|DVH:'
           + CAST(LEN(UPPER(LTRIM(RTRIM(DVH)))) AS nvarchar(10)) + N':' + UPPER(LTRIM(RTRIM(DVH))) + N'|' AS Fila,
           CONVERT(varchar(36), Id) AS Clave
    FROM dbo.Clientes
), Contenido AS (
    SELECT N'Tabla:8:Clientes|Cantidad:' + CAST(LEN(CAST(COUNT(*) AS varchar(10))) AS nvarchar(10)) + N':' + CAST(COUNT(*) AS nvarchar(10)) + N'|'
           + ISNULL(STRING_AGG(CAST(Fila AS nvarchar(max)), N'') WITHIN GROUP (ORDER BY Clave COLLATE Latin1_General_BIN2), N'') AS Texto
    FROM Orden
)
SELECT i.NombreTabla,
       i.DVV AS DVV_Almacenado,
       CONVERT(varchar(64), HASHBYTES('SHA2_256',
           CONVERT(varchar(max), c.Texto COLLATE Latin1_General_100_CI_AS_SC_UTF8)), 2) AS DVV_Calculado,
       CASE WHEN i.DVV = CONVERT(varchar(64), HASHBYTES('SHA2_256',
           CONVERT(varchar(max), c.Texto COLLATE Latin1_General_100_CI_AS_SC_UTF8)), 2)
            THEN 'OK' ELSE 'ALTERADO' END AS Estado,
       i.FechaActualizacion
FROM dbo.IntegridadTablas i CROSS JOIN Contenido c
WHERE i.NombreTabla = 'Clientes';
GO

/* ---------- 4) Resumen rapido de todas las tablas (cantidad de filas) ---------- */
SELECT t.name AS Tabla, SUM(p.rows) AS Filas
FROM sys.tables t JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
GROUP BY t.name ORDER BY t.name;
GO

/* ---------- 5) Servicios: usuarios, roles y ultima actividad de bitacora ---------- */
USE EdelstahlServicios;
GO
SELECT TOP 20 * FROM dbo.Usuarios;                 -- no muestra el hash completo si ajustas las columnas
SELECT * FROM dbo.Roles;
SELECT TOP 20 * FROM dbo.Bitacora ORDER BY 1 DESC;  -- ajustar el ORDER BY a la columna de fecha si existe
GO

/* ---------- 6) PRUEBA DE ALTERACION (simula un cliente manipulado y vuelve atras) ----------
   Modifica un dato sin recalcular el DVH, vuelve a comparar y hace ROLLBACK: no deja cambios.
   Despues de ejecutar el bloque, el cliente debe aparecer como ALTERADO solo dentro de la transaccion. */
USE EdelstahlNegocio;
GO
BEGIN TRAN;
    DECLARE @id uniqueidentifier = (SELECT TOP 1 Id FROM dbo.Clientes ORDER BY RazonSocial);
    UPDATE dbo.Clientes SET LimiteCredito = LimiteCredito + 1 WHERE Id = @id;
    SELECT 'Dentro de la transaccion' AS Momento, Id, RazonSocial, LimiteCredito FROM dbo.Clientes WHERE Id = @id;
    -- Ejecutar aqui la consulta 2 para ver ALTERADO (o abrir la app y verificar integridad).
ROLLBACK TRAN;
SELECT 'Despues del rollback' AS Momento, Id, RazonSocial, LimiteCredito FROM dbo.Clientes WHERE Id = (SELECT TOP 1 Id FROM dbo.Clientes ORDER BY RazonSocial);
GO
