-- Restaura EdelstahlNegocio y EdelstahlServicios en otra PC.
-- 1) Poner los dos .bak en una carpeta que SQL Server pueda leer (por ejemplo C:\Respaldos).
-- 2) Cambiar @carpeta si hace falta y ejecutar este script en SSMS conectado a localhost\SQLEXPRESS.
DECLARE @carpeta nvarchar(260) = N'C:\Respaldos\';
DECLARE @datos nvarchar(260) = CONVERT(nvarchar(260), SERVERPROPERTY('InstanceDefaultDataPath'));
DECLARE @logs nvarchar(260) = CONVERT(nvarchar(260), SERVERPROPERTY('InstanceDefaultLogPath'));
DECLARE @bd nvarchar(128), @sql nvarchar(max);
DECLARE c CURSOR FOR SELECT N'EdelstahlNegocio' UNION ALL SELECT N'EdelstahlServicios';
OPEN c; FETCH NEXT FROM c INTO @bd;
WHILE @@FETCH_STATUS = 0
BEGIN
  SET @sql = N'RESTORE DATABASE [' + @bd + N'] FROM DISK = N''' + @carpeta + @bd + N'.bak'' WITH REPLACE, '
           + N'MOVE N''' + @bd + N''' TO N''' + @datos + @bd + N'.mdf'', '
           + N'MOVE N''' + @bd + N'_log'' TO N''' + @logs + @bd + N'_log.ldf'';';
  EXEC sys.sp_executesql @sql;
  FETCH NEXT FROM c INTO @bd;
END
CLOSE c; DEALLOCATE c;
