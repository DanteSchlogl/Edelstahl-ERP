namespace Edelstahl.Services.DataAccess.Tools
{
    /// <summary>
    /// Crea las tablas del Composite en la base de servicios cuando faltan:
    /// Familia, Patente, FamiliaFamilia, FamiliaPatente, UsuarioFamilia y UsuarioPatente.
    /// Las tablas que ya existen no se modifican.
    /// </summary>
    public static class EsquemaComposite
    {
        private const string Script = @"
IF OBJECT_ID('dbo.Familia', 'U') IS NULL
CREATE TABLE dbo.Familia (
    IdFamilia uniqueidentifier NOT NULL CONSTRAINT PK_Familia PRIMARY KEY,
    Nombre nvarchar(100) NOT NULL
);

IF OBJECT_ID('dbo.Patente', 'U') IS NULL
CREATE TABLE dbo.Patente (
    IdPatente uniqueidentifier NOT NULL CONSTRAINT PK_Patente PRIMARY KEY,
    DataKey nvarchar(100) NOT NULL,
    Nombre nvarchar(150) NOT NULL CONSTRAINT DF_Patente_Nombre DEFAULT '',
    TipoAcceso int NOT NULL
);

IF OBJECT_ID('dbo.FamiliaFamilia', 'U') IS NULL
CREATE TABLE dbo.FamiliaFamilia (
    IdFamiliaPadre uniqueidentifier NOT NULL CONSTRAINT FK_FamiliaFamilia_Padre REFERENCES dbo.Familia(IdFamilia),
    IdFamiliaHijo uniqueidentifier NOT NULL CONSTRAINT FK_FamiliaFamilia_Hijo REFERENCES dbo.Familia(IdFamilia),
    CONSTRAINT PK_FamiliaFamilia PRIMARY KEY (IdFamiliaPadre, IdFamiliaHijo),
    CONSTRAINT CK_FamiliaFamilia_Distintas CHECK (IdFamiliaPadre <> IdFamiliaHijo)
);

IF OBJECT_ID('dbo.FamiliaPatente', 'U') IS NULL
CREATE TABLE dbo.FamiliaPatente (
    IdFamilia uniqueidentifier NOT NULL CONSTRAINT FK_FamiliaPatente_Familia REFERENCES dbo.Familia(IdFamilia),
    IdPatente uniqueidentifier NOT NULL CONSTRAINT FK_FamiliaPatente_Patente REFERENCES dbo.Patente(IdPatente),
    CONSTRAINT PK_FamiliaPatente PRIMARY KEY (IdFamilia, IdPatente)
);

IF OBJECT_ID('dbo.UsuarioFamilia', 'U') IS NULL
CREATE TABLE dbo.UsuarioFamilia (
    IdUsuario uniqueidentifier NOT NULL CONSTRAINT FK_UsuarioFamilia_Usuarios REFERENCES dbo.Usuarios(Id),
    IdFamilia uniqueidentifier NOT NULL CONSTRAINT FK_UsuarioFamilia_Familia REFERENCES dbo.Familia(IdFamilia),
    CONSTRAINT PK_UsuarioFamilia PRIMARY KEY (IdUsuario, IdFamilia)
);

IF OBJECT_ID('dbo.UsuarioPatente', 'U') IS NULL
CREATE TABLE dbo.UsuarioPatente (
    IdUsuario uniqueidentifier NOT NULL CONSTRAINT FK_UsuarioPatente_Usuarios REFERENCES dbo.Usuarios(Id),
    IdPatente uniqueidentifier NOT NULL CONSTRAINT FK_UsuarioPatente_Patente REFERENCES dbo.Patente(IdPatente),
    CONSTRAINT PK_UsuarioPatente PRIMARY KEY (IdUsuario, IdPatente)
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Familia_Nombre' AND object_id = OBJECT_ID('dbo.Familia'))
CREATE UNIQUE INDEX UQ_Familia_Nombre ON dbo.Familia (Nombre);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Patente_DataKey' AND object_id = OBJECT_ID('dbo.Patente'))
CREATE UNIQUE INDEX UQ_Patente_DataKey ON dbo.Patente (DataKey);
";

        /// <summary>Crea las tablas que falten. Requiere que ya exista dbo.Usuarios.</summary>
        public static void Instalar()
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 120, Script);
        }

        public static string GenerarScript()
        {
            return "-- Edelstahl ERP - tablas del Composite (familias y patentes)\r\n"
                + Script.Replace("\n", "\r\n");
        }
    }
}
