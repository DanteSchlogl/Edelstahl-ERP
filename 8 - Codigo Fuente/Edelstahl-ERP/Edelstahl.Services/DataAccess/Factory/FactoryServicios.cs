using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using Edelstahl.Services.DataAccess.Configuration;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DomainModel.Exceptions;
using Edelstahl.Services.Logic.Infrastructure;

namespace Edelstahl.Services.DataAccess.Factory
{
    /// <summary>
    /// Fábrica de los repositorios de la parte de Sistema: usuarios, roles, permisos
    /// individuales, bitácora y respaldos.
    ///
    /// Igual que la fábrica del negocio, la clase concreta de cada repositorio no se escribe
    /// con "new": se busca por nombre mediante reflexión según el modo de ejecución y se valida
    /// antes de crearla. Los nombres pueden reemplazarse en App.config con la clave
    /// "Repositorio.[Modo].[Interfaz]".
    /// </summary>
    public static class FactoryServicios
    {
        private const string Sql = "Edelstahl.Services.DataAccess.Implementations.SqlServer.";
        private const string Mem = "Edelstahl.Services.DataAccess.Implementations.Memory.";

        private static readonly Dictionary<ExecutionMode, Dictionary<Type, string>> Clases =
            new Dictionary<ExecutionMode, Dictionary<Type, string>>
            {
                {
                    ExecutionMode.SqlServer,
                    new Dictionary<Type, string>
                    {
                        { typeof(IUsuarioRepository), Sql + "UsuarioRepositorySqlServer" },
                        { typeof(IRolRepository), Sql + "RolRepositorySqlServer" },
                        { typeof(IPermisoUsuarioRepository), Sql + "PermisoUsuarioRepositorySqlServer" },
                        { typeof(IBitacoraRepository), Sql + "BitacoraRepositorySqlServer" },
                        { typeof(IRespaldoRepository), Sql + "RespaldoRepositorySqlServer" },
                        { typeof(IRespaldoDatabase), "Edelstahl.Services.DataAccess.Respaldo.RespaldoDatabaseService" }
                    }
                },
                {
                    ExecutionMode.Demo,
                    new Dictionary<Type, string>
                    {
                        { typeof(IUsuarioRepository), Mem + "UsuarioRepositoryMemory" },
                        { typeof(IRolRepository), Mem + "RolRepositoryMemory" },
                        { typeof(IPermisoUsuarioRepository), Mem + "PermisoUsuarioRepositoryMemory" },
                        { typeof(IBitacoraRepository), Mem + "BitacoraRepositoryMemory" },
                        { typeof(IRespaldoRepository), Mem + "RespaldoRepositoryMemory" },
                        { typeof(IRespaldoDatabase), Mem + "RespaldoDatabaseMemory" }
                    }
                }
            };

        private static bool _inicializada;

        public static IUsuarioRepository UsuarioRepository { get; private set; }

        public static IRolRepository RolRepository { get; private set; }

        /// <summary>Permisos que el administrador agregó o quitó a un usuario puntual.</summary>
        public static IPermisoUsuarioRepository PermisoUsuarioRepository { get; private set; }

        public static IBitacoraRepository BitacoraRepository { get; private set; }

        public static IRespaldoRepository RespaldoRepository { get; private set; }

        /// <summary>Crear, comprobar y restaurar respaldos de las bases.</summary>
        public static IRespaldoDatabase RespaldoDatabase { get; private set; }

        /// <summary>
        /// Verificación de los dígitos verificadores de las tablas protegidas. Lo publica
        /// FactoryDataAccess al inicializarse, porque la implementación concreta vive en la DAL
        /// (necesita consultar las tablas del negocio) y Services no puede depender de la DAL.
        /// </summary>
        public static IVerificadorIntegridad VerificadorIntegridad { get; set; }

        /// <summary>
        /// Recrea las tablas y columnas que falten después de restaurar un respaldo viejo. La
        /// publica FactoryDataAccess por el mismo motivo que VerificadorIntegridad.
        /// </summary>
        public static Action PrepararBaseDeDatos { get; set; }

        public static bool EstaInicializada
        {
            get { return _inicializada; }
        }

        /// <summary>
        /// Inicializa los repositorios de sistema según el modo de ejecución elegido al comenzar.
        /// </summary>
        public static void Inicializar(ExecutionMode modo)
        {
            if (modo != ExecutionMode.SqlServer && modo != ExecutionMode.Demo)
            {
                throw new ConfiguracionInvalidaException(
                    LanguageService.Translate("No valid run mode was selected."));
            }

            // Se crea todo primero y se publica al final: si una clase falla la validación,
            // la fábrica no queda a medio configurar.
            IUsuarioRepository usuario = Crear<IUsuarioRepository>(modo);
            IRolRepository rol = Crear<IRolRepository>(modo);
            IPermisoUsuarioRepository permisoUsuario = Crear<IPermisoUsuarioRepository>(modo);
            IBitacoraRepository bitacora = Crear<IBitacoraRepository>(modo);
            IRespaldoRepository respaldo = Crear<IRespaldoRepository>(modo);
            IRespaldoDatabase respaldoDatabase = Crear<IRespaldoDatabase>(modo);

            UsuarioRepository = usuario;
            RolRepository = rol;
            PermisoUsuarioRepository = permisoUsuario;
            BitacoraRepository = bitacora;
            RespaldoRepository = respaldo;
            RespaldoDatabase = respaldoDatabase;
            _inicializada = true;
        }

        private static TInterfaz Crear<TInterfaz>(ExecutionMode modo)
            where TInterfaz : class
        {
            return CrearDesdeNombre<TInterfaz>(ObtenerNombreDeClase(typeof(TInterfaz), modo));
        }

        /// <summary>
        /// Crea una instancia de la clase indicada por su nombre completo, comprobando antes que exista,
        /// que sea concreta, que implemente TInterfaz y que tenga un constructor sin parámetros.
        /// </summary>
        public static TInterfaz CrearDesdeNombre<TInterfaz>(string nombreClase)
            where TInterfaz : class
        {
            if (string.IsNullOrWhiteSpace(nombreClase))
            {
                throw new ConfiguracionInvalidaException(
                    LanguageService.Format(
                        "The data access configuration for '{0}' is empty.",
                        typeof(TInterfaz).Name));
            }

            nombreClase = nombreClase.Trim();
            Type tipo = BuscarTipo(nombreClase);

            ValidarTipo(tipo, nombreClase, typeof(TInterfaz));

            ConstructorInfo constructor = tipo.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (constructor == null)
            {
                throw new ConfiguracionInvalidaException(
                    LanguageService.Format(
                        "The class '{0}' has no constructor without parameters.",
                        nombreClase));
            }

            try
            {
                return (TInterfaz)constructor.Invoke(null);
            }
            catch (TargetInvocationException ex)
            {
                Exception causa = ex.InnerException ?? ex;

                throw new ConfiguracionInvalidaException(
                    LanguageService.Format(
                        "The class '{0}' could not be created: {1}",
                        nombreClase,
                        causa.Message),
                    causa);
            }
        }

        private static string ObtenerNombreDeClase(Type interfaz, ExecutionMode modo)
        {
            string clave = "Repositorio." + modo + "." + interfaz.Name;
            string configurado = ConfigurationManager.AppSettings[clave];

            if (!string.IsNullOrWhiteSpace(configurado))
            {
                return configurado.Trim();
            }

            string porDefecto;

            if (Clases.ContainsKey(modo) && Clases[modo].TryGetValue(interfaz, out porDefecto))
            {
                return porDefecto;
            }

            throw new ConfiguracionInvalidaException(
                LanguageService.Format(
                    "No data access class is configured for '{0}'.",
                    interfaz.Name));
        }

        private static Type BuscarTipo(string nombreClase)
        {
            Type tipo = null;

            try
            {
                tipo = typeof(FactoryServicios).Assembly.GetType(nombreClase, false);

                if (tipo == null)
                {
                    tipo = Type.GetType(nombreClase, false);
                }
            }
            catch (Exception ex) when (ex is ArgumentException
                || ex is FileLoadException
                || ex is TypeLoadException)
            {
                tipo = null;
            }

            if (tipo == null)
            {
                throw new ConfiguracionInvalidaException(
                    LanguageService.Format(
                        "The class '{0}' was not found in the data access assembly.",
                        nombreClase));
            }

            return tipo;
        }

        private static void ValidarTipo(Type tipo, string nombreClase, Type interfaz)
        {
            if (!tipo.IsClass || tipo.IsAbstract)
            {
                throw new ConfiguracionInvalidaException(
                    LanguageService.Format(
                        "The class '{0}' cannot be created because it is not a concrete class.",
                        nombreClase));
            }

            if (!interfaz.IsAssignableFrom(tipo))
            {
                throw new ConfiguracionInvalidaException(
                    LanguageService.Format(
                        "The class '{0}' does not implement the interface '{1}'.",
                        nombreClase,
                        interfaz.Name));
            }
        }
    }
}
