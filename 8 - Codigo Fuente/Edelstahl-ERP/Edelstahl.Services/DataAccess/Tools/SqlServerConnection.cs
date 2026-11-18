using System;
using System.Configuration;
using System.Data.SqlClient;

namespace Edelstahl.Services.DataAccess.Tools
{
    /// <summary>
    /// Centraliza las conexiones con las bases de datos de Edelstahl ERP.
    ///
    /// Es un Singleton: existe una sola instancia para todo el programa (Instance), porque la
    /// instancia de SQL Server detectada (SQL Server Express, LocalDB, etc.) es un dato que se
    /// resuelve una sola vez y se comparte desde ahí en adelante, en vez de volver a detectarla
    /// cada vez. Cada llamada a CreateXxxConnection sigue devolviendo un SqlConnection nuevo
    /// (así ADO.NET puede usar su pool de conexiones): lo único único es el objeto que sabe
    /// resolver la instancia y armar las cadenas de conexión, no la conexión en sí.
    ///
    /// La instancia de SQL Server puede fijarse en App.config con las claves SqlServerInstancia,
    /// BaseNegocio y BaseServicios. También admiten variables de entorno
    /// (EDELSTAHL_SQL_SERVER, EDELSTAHL_NEGOCIO_DB, EDELSTAHL_SERVICIOS_DB),
    /// útiles para ejecutar pruebas sobre bases separadas.
    /// </summary>
    public sealed class SqlServerConnection
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

        private static readonly Lazy<SqlServerConnection> UnicaInstancia =
            new Lazy<SqlServerConnection>(() => new SqlServerConnection());

        private readonly object Candado = new object();
        private string _instancia;

        private SqlServerConnection()
        {
        }

        /// <summary>La única instancia de la clase (patrón Singleton).</summary>
        public static SqlServerConnection Instance
        {
            get { return UnicaInstancia.Value; }
        }

        public string Instancia
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

        public string NombreBaseNegocio
        {
            get { return Leer("EDELSTAHL_NEGOCIO_DB", "BaseNegocio", BaseNegocioPorDefecto); }
        }

        public string NombreBaseServicios
        {
            get { return Leer("EDELSTAHL_SERVICIOS_DB", "BaseServicios", BaseServiciosPorDefecto); }
        }

        public string CadenaNegocio
        {
            get { return Construir(NombreBaseNegocio, true); }
        }

        public string CadenaServicios
        {
            get { return Construir(NombreBaseServicios, false); }
        }

        public string CadenaMaster
        {
            get { return Construir("master", false); }
        }

        public SqlConnection CreateConnection()
        {
            return CreateBusinessConnection();
        }

        public SqlConnection CreateBusinessConnection()
        {
            return new SqlConnection(CadenaNegocio);
        }

        public SqlConnection CreateServicesConnection()
        {
            return new SqlConnection(CadenaServicios);
        }

        public SqlConnection CreateMasterConnection()
        {
            return new SqlConnection(CadenaMaster);
        }

        /// <summary>
        /// Descarta la instancia detectada para volver a buscarla.
        /// </summary>
        public void Reiniciar()
        {
            lock (Candado)
            {
                _instancia = null;
            }
        }

        private string Construir(string baseDeDatos, bool variasConsultasActivas)
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
