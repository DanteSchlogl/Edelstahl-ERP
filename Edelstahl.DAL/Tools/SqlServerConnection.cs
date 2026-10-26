using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Edelstahl.DAL.Tools
{
    /// <summary>
    /// Centraliza las conexiones con las bases de datos de Edelstahl ERP.
    ///
    /// La instancia de SQL Server se detecta sola (SQL Server Express o
    /// LocalDB) y puede fijarse en App.config con las claves SqlServerInstancia,
    /// BaseNegocio y BaseServicios. También admiten variables de entorno
    /// (EDELSTAHL_SQL_SERVER, EDELSTAHL_NEGOCIO_DB, EDELSTAHL_SERVICIOS_DB),
    /// útiles para ejecutar pruebas sobre bases separadas.
    /// </summary>
    public static class SqlServerConnection
    {
        private const string BaseNegocioPorDefecto = "EdelstahlNegocio";
        private const string BaseServiciosPorDefecto = "EdelstahlServicios";

        private static readonly string[] InstanciasCandidatas =
        {
            @"localhost\SQLEXPRESS",
            @"(localdb)\MSSQLLocalDB",
            @".\SQLEXPRESS",
            "localhost"
        };

        private static readonly object Candado = new object();
        private static string _instancia;

        public static string Instancia
        {
            get
            {
                lock (Candado)
                {
                    if (_instancia == null)
                    {
                        _instancia = ResolverInstancia();
                    }

                    return _instancia;
                }
            }
        }

        public static string NombreBaseNegocio
        {
            get { return Leer("EDELSTAHL_NEGOCIO_DB", "BaseNegocio", BaseNegocioPorDefecto); }
        }

        public static string NombreBaseServicios
        {
            get { return Leer("EDELSTAHL_SERVICIOS_DB", "BaseServicios", BaseServiciosPorDefecto); }
        }

        public static string CadenaNegocio
        {
            get { return Construir(NombreBaseNegocio, true); }
        }

        public static string CadenaServicios
        {
            get { return Construir(NombreBaseServicios, false); }
        }

        public static string CadenaMaster
        {
            get { return Construir("master", false); }
        }

        public static SqlConnection CreateConnection()
        {
            return CreateBusinessConnection();
        }

        public static SqlConnection CreateBusinessConnection()
        {
            return new SqlConnection(CadenaNegocio);
        }

        public static SqlConnection CreateServicesConnection()
        {
            return new SqlConnection(CadenaServicios);
        }

        public static SqlConnection CreateMasterConnection()
        {
            return new SqlConnection(CadenaMaster);
        }

        /// <summary>
        /// Descarta la instancia detectada para volver a buscarla.
        /// </summary>
        public static void Reiniciar()
        {
            lock (Candado)
            {
                _instancia = null;
            }
        }

        private static string Construir(string baseDeDatos, bool variasConsultasActivas)
        {
            SqlConnectionStringBuilder cadena = new SqlConnectionStringBuilder
            {
                DataSource = Instancia,
                InitialCatalog = baseDeDatos,
                IntegratedSecurity = true,
                ConnectTimeout = 15
            };

            if (variasConsultasActivas)
            {
                cadena.MultipleActiveResultSets = true;
            }

            return cadena.ConnectionString;
        }

        private static string Leer(string variable, string clave, string valorPorDefecto)
        {
            string valor = Environment.GetEnvironmentVariable(variable);

            if (string.IsNullOrWhiteSpace(valor))
            {
                valor = ConfigurationManager.AppSettings[clave];
            }

            return string.IsNullOrWhiteSpace(valor) ? valorPorDefecto : valor.Trim();
        }

        private static string ResolverInstancia()
        {
            string indicada = Leer("EDELSTAHL_SQL_SERVER", "SqlServerInstancia", "auto");

            if (!string.Equals(indicada, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return indicada;
            }

            foreach (string candidata in InstanciasCandidatas)
            {
                if (PuedeConectar(candidata))
                {
                    return candidata;
                }
            }

            return InstanciasCandidatas[0];
        }

        private static bool PuedeConectar(string instancia)
        {
            SqlConnectionStringBuilder cadena = new SqlConnectionStringBuilder
            {
                DataSource = instancia,
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ConnectTimeout = 4
            };

            try
            {
                using (SqlConnection conexion = new SqlConnection(cadena.ConnectionString))
                {
                    conexion.Open();
                    return true;
                }
            }
            catch (SqlException)
            {
                return false;
            }
        }
    }
}
