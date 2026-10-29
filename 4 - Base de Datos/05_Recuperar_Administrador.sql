-- Edelstahl ERP - recuperacion de acceso de un usuario (por ejemplo, el unico administrador)
-- Usar solo cuando nadie puede entrar al programa para restablecer la contraseña desde Administracion.
-- Debe ejecutarlo quien administra el servidor SQL Server (se necesita acceso a la base).
--
-- Que hace:
--   1. Pone la contraseña provisoria:  Recuperar-2026
--   2. Reactiva el usuario y le quita el bloqueo y los intentos fallidos.
--   3. Marca que debe elegir una contraseña propia: al iniciar sesion el programa se la pide.
--
-- Como usarlo:
--   Cambiar el nombre de usuario en la linea SET @Usuario, ejecutar el script completo
--   e ingresar al programa con la contraseña provisoria. Elegir enseguida la contraseña propia.
USE EdelstahlServicios;
GO
SET NOCOUNT ON;
GO
DECLARE @Usuario NVARCHAR(50) = N'admin';

UPDATE [dbo].[Usuarios]
SET [PasswordHash] = N'jM1vnjRhZqY5Ollf8sELvoiRoEDhiNemZyOhd0kGB4w=',
    [PasswordSalt] = N'e5IsKxi8Ht6dlvcyxMAt0FgpysKlLiGriq6dSp42HYI=',
    [Iteraciones] = 100000,
    [Activo] = 1,
    [DebeCambiarPassword] = 1,
    [IntentosFallidos] = 0,
    [BloqueadoHasta] = NULL
WHERE [NombreUsuario] = @Usuario;

IF @@ROWCOUNT = 1
    PRINT N'Listo. El usuario ' + @Usuario + N' entra con la contraseña provisoria y debe elegir una propia.';
ELSE
    PRINT N'No se encontro el usuario ' + @Usuario + N'. Revisar el nombre en la linea SET @Usuario.';
GO
