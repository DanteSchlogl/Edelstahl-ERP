USE EdelstahlNegocio;
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