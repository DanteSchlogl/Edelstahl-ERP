using System.Data.SqlClient;
using Edelstahl.DAL.Tools;

namespace Edelstahl.DAL.EntityFramework
{
    /// <summary>
    /// Crea la base de servicios (usuarios, roles y bitácora) cuando falta.
    /// Las tablas que ya existen no se modifican, salvo las columnas nuevas de Usuarios (pregunta de seguridad).
    /// </summary>
    public static class EsquemaServicios
    {
        private const string ScriptTablas = @"
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
CREATE TABLE dbo.Roles (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    Nombre nvarchar(100) NOT NULL,
    Descripcion nvarchar(300) NOT NULL,
    Activo bit NOT NULL,
    FechaAlta datetime2 NOT NULL
);

IF OBJECT_ID('dbo.Usuarios', 'U') IS NULL
CREATE TABLE dbo.Usuarios (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_Usuarios PRIMARY KEY,
    RolId uniqueidentifier NOT NULL CONSTRAINT FK_Usuarios_Roles REFERENCES dbo.Roles(Id),
    NombreUsuario nvarchar(100) NOT NULL,
    NombreCompleto nvarchar(250) NOT NULL,
    Email nvarchar(250) NOT NULL,
    PasswordHash nvarchar(500) NOT NULL,
    PasswordSalt nvarchar(500) NOT NULL,
    Iteraciones int NOT NULL,
    Activo bit NOT NULL,
    DebeCambiarPassword bit NOT NULL,
    IntentosFallidos int NOT NULL,
    BloqueadoHasta datetime2 NULL,
    FechaAlta datetime2 NOT NULL,
    UltimoAcceso datetime2 NULL,
    PreguntaSeguridad nvarchar(250) NOT NULL CONSTRAINT DF_Usuarios_PreguntaSeguridad DEFAULT '',
    RespuestaHash nvarchar(500) NOT NULL CONSTRAINT DF_Usuarios_RespuestaHash DEFAULT '',
    RespuestaSalt nvarchar(500) NOT NULL CONSTRAINT DF_Usuarios_RespuestaSalt DEFAULT ''
);

IF COL_LENGTH('dbo.Usuarios', 'PreguntaSeguridad') IS NULL
ALTER TABLE dbo.Usuarios ADD PreguntaSeguridad nvarchar(250) NOT NULL CONSTRAINT DF_Usuarios_PreguntaSeguridad DEFAULT '';

IF COL_LENGTH('dbo.Usuarios', 'RespuestaHash') IS NULL
ALTER TABLE dbo.Usuarios ADD RespuestaHash nvarchar(500) NOT NULL CONSTRAINT DF_Usuarios_RespuestaHash DEFAULT '';

IF COL_LENGTH('dbo.Usuarios', 'RespuestaSalt') IS NULL
ALTER TABLE dbo.Usuarios ADD RespuestaSalt nvarchar(500) NOT NULL CONSTRAINT DF_Usuarios_RespuestaSalt DEFAULT '';

IF OBJECT_ID('dbo.Bitacora', 'U') IS NULL
CREATE TABLE dbo.Bitacora (
    Id uniqueidentifier NOT NULL CONSTRAINT PK_Bitacora PRIMARY KEY,
    FechaHora datetime2 NOT NULL,
    UsuarioId uniqueidentifier NULL,
    NombreUsuario nvarchar(100) NOT NULL,
    Modulo nvarchar(100) NOT NULL,
    Accion nvarchar(150) NOT NULL,
    Entidad nvarchar(100) NOT NULL,
    EntidadId uniqueidentifier NULL,
    Descripcion nvarchar(1000) NOT NULL,
    Resultado nvarchar(50) NOT NULL,
    DireccionEquipo nvarchar(100) NOT NULL,
    NombreEquipo nvarchar(150) NOT NULL,
    TipoEvento nvarchar(50) NOT NULL,
    DetalleTecnico nvarchar(max) NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Roles_Nombre' AND object_id = OBJECT_ID('dbo.Roles'))
CREATE UNIQUE INDEX UQ_Roles_Nombre ON dbo.Roles (Nombre);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Usuarios_NombreUsuario' AND object_id = OBJECT_ID('dbo.Usuarios'))
CREATE UNIQUE INDEX UQ_Usuarios_NombreUsuario ON dbo.Usuarios (NombreUsuario);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Bitacora_FechaHora' AND object_id = OBJECT_ID('dbo.Bitacora'))
CREATE INDEX IX_Bitacora_FechaHora ON dbo.Bitacora (FechaHora DESC);
";

        public static void Instalar()
        {
            EsquemaNegocio.AsegurarBase(SqlServerConnection.NombreBaseServicios);

            using (SqlConnection conexion = SqlServerConnection.CreateServicesConnection())
            {
                conexion.Open();

                using (SqlCommand comando = new SqlCommand(ScriptTablas, conexion))
                {
                    comando.CommandTimeout = 120;
                    comando.ExecuteNonQuery();
                }
            }
        }

        public static string GenerarScript()
        {
            return "-- Edelstahl ERP - base de servicios (usuarios, roles y bitacora)\r\n"
                + ScriptTablas.Replace("\n", "\r\n");
        }
    }
}
