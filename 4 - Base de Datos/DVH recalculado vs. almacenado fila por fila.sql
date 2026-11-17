USE EdelstahlNegocio;
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
ORDER BY Estado DESC, RazonSocial;   -- los ALTERADO salen primero