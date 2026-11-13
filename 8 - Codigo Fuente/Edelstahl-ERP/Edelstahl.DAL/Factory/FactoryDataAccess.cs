using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Services.Configuration;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Factory
{
    /// <summary>
    /// Centraliza los repositorios utilizados por Edelstahl ERP.
    ///
    /// La clase concreta de cada repositorio no se escribe con "new": se
    /// busca por nombre mediante reflexión según el modo de ejecución y se
    /// valida antes de crearla (que exista, que sea una clase concreta, que
    /// implemente la interfaz pedida y que tenga un constructor sin
    /// parámetros). Los nombres salen de una tabla propia y pueden
    /// reemplazarse en App.config con la clave
    /// "Repositorio.[Modo].[Interfaz]" (por ejemplo
    /// "Repositorio.SqlServer.IClienteRepository").
    ///
    /// En modo SQL Server utiliza Entity Framework y ADO.NET.
    /// En modo demostración utiliza repositorios en memoria.
    /// </summary>
    public static class FactoryDataAccess
    {
        private const string Sql = "Edelstahl.DAL.Implementations.SqlServer.";
        private const string Ef = "Edelstahl.DAL.Implementations.EntityFramework.";
        private const string Mem = "Edelstahl.DAL.Implementations.Memory.";

        private static readonly Dictionary<ExecutionMode, Dictionary<Type, string>> Clases =
            new Dictionary<ExecutionMode, Dictionary<Type, string>>
            {
                {
                    ExecutionMode.SqlServer,
                    new Dictionary<Type, string>
                    {
                        { typeof(IClienteRepository), Ef + "ClienteRepositoryEntityFramework" },
                        { typeof(IPresupuestoRepository), Ef + "PresupuestoRepositoryEntityFramework" },
                        { typeof(IPedidoRepository), Ef + "PedidoRepositoryEntityFramework" },
                        { typeof(IUsuarioRepository), Sql + "UsuarioRepositorySqlServer" },
                        { typeof(IRolRepository), Sql + "RolRepositorySqlServer" },
                        { typeof(IPermisoUsuarioRepository), Sql + "PermisoUsuarioRepositorySqlServer" },
                        { typeof(IBitacoraRepository), Sql + "BitacoraRepositorySqlServer" },
                        { typeof(IRespaldoRepository), Sql + "RespaldoRepositorySqlServer" },
                        { typeof(IRespaldoDatabase), "Edelstahl.DAL.Backup.RespaldoDatabaseService" },
                        { typeof(IVerificadorIntegridad), "Edelstahl.DAL.Integrity.VerificadorIntegridad" },
                        { typeof(INegocioStore), "Edelstahl.DAL.Store.EfNegocioStore" }
                    }
                },
                {
                    ExecutionMode.Demo,
                    new Dictionary<Type, string>
                    {
                        { typeof(IClienteRepository), Mem + "ClienteRepositoryMemory" },
                        { typeof(IPresupuestoRepository), Mem + "PresupuestoRepositoryMemory" },
                        { typeof(IPedidoRepository), Mem + "PedidoRepositoryMemory" },
                        { typeof(IUsuarioRepository), Mem + "UsuarioRepositoryMemory" },
                        { typeof(IRolRepository), Mem + "RolRepositoryMemory" },
                        { typeof(IPermisoUsuarioRepository), Mem + "PermisoUsuarioRepositoryMemory" },
                        { typeof(IBitacoraRepository), Mem + "BitacoraRepositoryMemory" },
                        { typeof(IRespaldoRepository), Mem + "RespaldoRepositoryMemory" },
                        { typeof(IRespaldoDatabase), Mem + "RespaldoDatabaseMemory" },
                        { typeof(IVerificadorIntegridad), Mem + "VerificadorIntegridadMemory" },
                        { typeof(INegocioStore), "Edelstahl.DAL.Store.MemoryNegocioStore" }
                    }
                }
            };

        private static bool _inicializada;
        private static ExecutionMode _modo = ExecutionMode.None;

        public static IClienteRepository ClienteRepository { get; private set; }

        public static IPresupuestoRepository PresupuestoRepository { get; private set; }

        public static IPedidoRepository PedidoRepository { get; private set; }

        public static IUsuarioRepository UsuarioRepository { get; private set; }

        public static IRolRepository RolRepository { get; private set; }

        /// <summary>Permisos que el administrador agregó o quitó a un usuario puntual.</summary>
        public static IPermisoUsuarioRepository PermisoUsuarioRepository { get; private set; }

        public static IBitacoraRepository BitacoraRepository { get; private set; }

        public static IRespaldoRepository RespaldoRepository { get; private set; }

        /// <summary>
        /// Crear, comprobar y restaurar respaldos de las bases.
        /// </summary>
        public static IRespaldoDatabase RespaldoDatabase { get; private set; }

        /// <summary>
        /// Verificación de los dígitos verificadores (DVH y DVV).
        /// </summary>
        public static IVerificadorIntegridad VerificadorIntegridad { get; private set; }

        /// <summary>
        /// Almacén transaccional de los módulos de catálogo, inventario,
        /// producción, compras, finanzas y logística.
        /// </summary>
        public static INegocioStore NegocioStore { get; private set; }

        /// <summary>
        /// Inicializa los repositorios según el modo de ejecución
        /// seleccionado al comenzar la aplicación.
        /// </summary>
        public static void Inicializar(ExecutionMode modo)
        {
            if (modo != ExecutionMode.SqlServer && modo != ExecutionMode.Demo)
            {
                throw new ConfiguracionInvalidaException(
                    LanguageService.Translate("No valid run mode was selected."));
            }

            // Se crea todo primero y se publica al final: si una clase falla
            // la validación, la Factory no queda a medio configurar.
            IClienteRepository cliente = Crear<IClienteRepository>(modo);
            IPresupuestoRepository presupuesto = Crear<IPresupuestoRepository>(modo);
            IPedidoRepository pedido = Crear<IPedidoRepository>(modo);
            IUsuarioRepository usuario = Crear<IUsuarioRepository>(modo);
            IRolRepository rol = Crear<IRolRepository>(modo);
            IPermisoUsuarioRepository permisoUsuario = Crear<IPermisoUsuarioRepository>(modo);
            IBitacoraRepository bitacora = Crear<IBitacoraRepository>(modo);
            IRespaldoRepository respaldo = Crear<IRespaldoRepository>(modo);
            IRespaldoDatabase respaldoDatabase = Crear<IRespaldoDatabase>(modo);
            IVerificadorIntegridad integridad = Crear<IVerificadorIntegridad>(modo);
            INegocioStore almacen = Crear<INegocioStore>(modo);

            ClienteRepository = cliente;
            PresupuestoRepository = presupuesto;
            PedidoRepository = pedido;
            UsuarioRepository = usuario;
            RolRepository = rol;
            PermisoUsuarioRepository = permisoUsuario;
            BitacoraRepository = bitacora;
            RespaldoRepository = respaldo;
            RespaldoDatabase = respaldoDatabase;
            VerificadorIntegridad = integridad;
            _modo = modo;
            NegocioStore = almacen;
            _inicializada = true;
        }

        /// <summary>
        /// Prepara la base de datos para usarla: crea las bases y tablas que falten,
        /// migra los datos de versiones anteriores y genera los dígitos verificadores
        /// iniciales. En modo demostración no hay nada que preparar.
        /// </summary>
        public static void PrepararBaseDeDatos()
        {
            if (_modo == ExecutionMode.SqlServer)
            {
                InicializadorNegocio.Inicializar();
            }
        }

        /// <summary>
        /// Crea la implementación de la interfaz indicada para el modo
        /// dado, buscando la clase por reflexión.
        /// </summary>
        private static TInterfaz Crear<TInterfaz>(ExecutionMode modo)
            where TInterfaz : class
        {
            return CrearDesdeNombre<TInterfaz>(
                ObtenerNombreDeClase(typeof(TInterfaz), modo));
        }

        /// <summary>
        /// Crea una instancia de la clase indicada por su nombre completo,
        /// comprobando antes que exista, que sea concreta, que implemente
        /// TInterfaz y que tenga un constructor sin parámetros. Si algo no
        /// se cumple lanza ConfiguracionInvalidaException.
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

        /// <summary>
        /// Nombre de la clase: el de App.config si existe y, si no, el de la
        /// tabla propia de la Factory.
        /// </summary>
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

        /// <summary>
        /// Busca el tipo en este ensamblado o, si el nombre trae el
        /// ensamblado ("Clase, Ensamblado"), en el indicado.
        /// </summary>
        private static Type BuscarTipo(string nombreClase)
        {
            Type tipo = null;

            try
            {
                tipo = typeof(FactoryDataAccess).Assembly.GetType(nombreClase, false);

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

        public static bool EstaInicializada
        {
            get { return _inicializada; }
        }

        public static bool EsModoDemostracion
        {
            get { return ApplicationMode.IsDemo; }
        }

        public static bool UsaSqlServer
        {
            get { return ApplicationMode.UsesSqlServer; }
        }
    }
}
