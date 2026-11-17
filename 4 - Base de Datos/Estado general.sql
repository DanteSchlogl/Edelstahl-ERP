USE EdelstahlNegocio;
SELECT COUNT(*) AS TotalClientes,
       SUM(CASE WHEN DVH IS NULL OR LTRIM(RTRIM(DVH)) = '' THEN 1 ELSE 0 END) AS ClientesSinDVH
FROM dbo.Clientes;