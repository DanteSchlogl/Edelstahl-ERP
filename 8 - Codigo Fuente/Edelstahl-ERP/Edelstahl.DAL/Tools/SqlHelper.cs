using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Tools
{
    /// <summary>
    /// Base de datos sobre la que se ejecuta una sentencia.
    /// </summary>
    internal enum BaseDeDatos
    {
        Negocio,
        Servicios,
        Master
    }

    /// <summary>
    /// Ejecuta sentencias SQL con parámetros. Todos los repositorios y
    /// servicios de la DAL que usan ADO.NET pasan por esta clase, de modo
    /// que abrir y cerrar la conexión, crear el comando, asignar los
    /// parámetros y traducir los errores de SQL Server se hace en un solo
    /// lugar.
    ///
    /// Un error de SQL Server (SqlException) sale como AccesoADatosException,
    /// con la excepción original como causa para la bitácora.
    /// </summary>
    internal static class SqlHelper
    {
        private const int TiempoPorDefecto = 30;

        // ------------------------------------------------------------------
        // Parámetros
        // ------------------------------------------------------------------

        public static SqlParameter Parametro(string nombre, SqlDbType tipo, object valor)
        {
            return Parametro(nombre, tipo, 0, valor);
        }

        public static SqlParameter Parametro(string nombre, SqlDbType tipo, int tamano, object valor)
        {
            SqlParameter parametro = tamano != 0
                ? new SqlParameter(nombre, tipo, tamano)
                : new SqlParameter(nombre, tipo);

            parametro.Value = valor ?? DBNull.Value;
            return parametro;
        }

        /// <summary>
        /// Valor para un parámetro de una columna que admite nulos.
        /// </summary>
        public static object ONulo<T>(T? valor) where T : struct
        {
            return valor.HasValue ? (object)valor.Value : DBNull.Value;
        }

        // ------------------------------------------------------------------
        // Ejecución sobre una conexión propia
        // ------------------------------------------------------------------

        public static int EjecutarNoQuery(
            BaseDeDatos baseDeDatos,
            string sql,
            params SqlParameter[] parametros)
        {
            return EjecutarNoQuery(baseDeDatos, TiempoPorDefecto, sql, parametros);
        }

        public static int EjecutarNoQuery(
            BaseDeDatos baseDeDatos,
            int segundosDeEspera,
            string sql,
            params SqlParameter[] parametros)
        {
            return Ejecutar(baseDeDatos, sql, segundosDeEspera, parametros,
                comando => comando.ExecuteNonQuery());
        }

        public static T EjecutarEscalar<T>(
            BaseDeDatos baseDeDatos,
            string sql,
            params SqlParameter[] parametros)
        {
            return Ejecutar(baseDeDatos, sql, TiempoPorDefecto, parametros,
                comando => Convertir<T>(comando.ExecuteScalar()));
        }

        /// <summary>
        /// Devuelve todas las filas, transformadas por la función que las
        /// convierte en objetos.
        /// </summary>
        public static List<T> Consultar<T>(
            BaseDeDatos baseDeDatos,
            string sql,
            Func<IDataRecord, T> mapear,
            params SqlParameter[] parametros)
        {
            return Ejecutar(baseDeDatos, sql, TiempoPorDefecto, parametros, comando =>
            {
                List<T> filas = new List<T>();

                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        filas.Add(mapear(lector));
                    }
                }

                return filas;
            });
        }

        /// <summary>
        /// Devuelve la primera fila o null si la consulta no trae ninguna.
        /// </summary>
        public static T ConsultarUno<T>(
            BaseDeDatos baseDeDatos,
            string sql,
            Func<IDataRecord, T> mapear,
            params SqlParameter[] parametros)
            where T : class
        {
            return Ejecutar(baseDeDatos, sql, TiempoPorDefecto, parametros, comando =>
            {
                using (SqlDataReader lector = comando.ExecuteReader(CommandBehavior.SingleRow))
                {
                    return lector.Read() ? mapear(lector) : null;
                }
            });
        }

        // ------------------------------------------------------------------
        // Transacciones: varias sentencias que se confirman o se deshacen juntas
        // ------------------------------------------------------------------

        /// <summary>
        /// Abre una conexión y una transacción, ejecuta el trabajo y la
        /// confirma; si el trabajo falla la deshace y repite el error.
        /// </summary>
        public static void EnTransaccion(
            BaseDeDatos baseDeDatos,
            Action<Transaccion> trabajo)
        {
            try
            {
                using (SqlConnection conexion = Crear(baseDeDatos))
                {
                    conexion.Open();

                    using (SqlTransaction transaccion = conexion.BeginTransaction())
                    {
                        try
                        {
                            trabajo(new Transaccion(conexion, transaccion));
                            transaccion.Commit();
                        }
                        catch
                        {
                            transaccion.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
        }

        /// <summary>
        /// Operaciones que se ejecutan dentro de la transacción abierta por
        /// EnTransaccion.
        /// </summary>
        internal sealed class Transaccion
        {
            private readonly SqlConnection _conexion;
            private readonly SqlTransaction _transaccion;

            internal Transaccion(SqlConnection conexion, SqlTransaction transaccion)
            {
                _conexion = conexion;
                _transaccion = transaccion;
            }

            public int EjecutarNoQuery(string sql, int segundosDeEspera, params SqlParameter[] parametros)
            {
                using (SqlCommand comando = Preparar(_conexion, _transaccion, sql, segundosDeEspera, parametros))
                {
                    return comando.ExecuteNonQuery();
                }
            }

            public T EjecutarEscalar<T>(string sql, params SqlParameter[] parametros)
            {
                using (SqlCommand comando = Preparar(_conexion, _transaccion, sql, TiempoPorDefecto, parametros))
                {
                    return Convertir<T>(comando.ExecuteScalar());
                }
            }

            public List<T> Consultar<T>(string sql, Func<IDataRecord, T> mapear, params SqlParameter[] parametros)
            {
                List<T> filas = new List<T>();

                using (SqlCommand comando = Preparar(_conexion, _transaccion, sql, TiempoPorDefecto, parametros))
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        filas.Add(mapear(lector));
                    }
                }

                return filas;
            }
        }

        // ------------------------------------------------------------------
        // Lectura de columnas
        // ------------------------------------------------------------------

        public static string Texto(IDataRecord fila, string columna)
        {
            object valor = fila[columna];
            return valor == DBNull.Value ? string.Empty : valor.ToString();
        }

        public static Guid? GuidONulo(IDataRecord fila, string columna)
        {
            object valor = fila[columna];
            return valor == DBNull.Value ? (Guid?)null : (Guid)valor;
        }

        public static DateTime? FechaONula(IDataRecord fila, string columna)
        {
            object valor = fila[columna];
            return valor == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(valor);
        }

        // ------------------------------------------------------------------
        // Interno
        // ------------------------------------------------------------------

        private static T Ejecutar<T>(
            BaseDeDatos baseDeDatos,
            string sql,
            int segundosDeEspera,
            SqlParameter[] parametros,
            Func<SqlCommand, T> accion)
        {
            try
            {
                using (SqlConnection conexion = Crear(baseDeDatos))
                {
                    conexion.Open();

                    using (SqlCommand comando = Preparar(conexion, null, sql, segundosDeEspera, parametros))
                    {
                        return accion(comando);
                    }
                }
            }
            catch (SqlException ex)
            {
                throw Traducir(ex);
            }
        }

        private static SqlCommand Preparar(
            SqlConnection conexion,
            SqlTransaction transaccion,
            string sql,
            int segundosDeEspera,
            SqlParameter[] parametros)
        {
            SqlCommand comando = new SqlCommand(sql, conexion, transaccion);
            comando.CommandTimeout = segundosDeEspera;

            if (parametros != null)
            {
                foreach (SqlParameter parametro in parametros)
                {
                    comando.Parameters.Add(parametro);
                }
            }

            return comando;
        }

        private static SqlConnection Crear(BaseDeDatos baseDeDatos)
        {
            switch (baseDeDatos)
            {
                case BaseDeDatos.Negocio:
                    return SqlServerConnection.CreateBusinessConnection();
                case BaseDeDatos.Servicios:
                    return SqlServerConnection.CreateServicesConnection();
                default:
                    return SqlServerConnection.CreateMasterConnection();
            }
        }

        private static T Convertir<T>(object valor)
        {
            if (valor == null || valor == DBNull.Value)
            {
                return default(T);
            }

            Type destino = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (destino.IsInstanceOfType(valor))
            {
                return (T)valor;
            }

            return (T)Convert.ChangeType(valor, destino);
        }

        private static AccesoADatosException Traducir(SqlException ex)
        {
            return new AccesoADatosException(
                LanguageService.Format(
                    "The database could not complete the operation: {0}",
                    ex.Message),
                ex);
        }
    }
}
