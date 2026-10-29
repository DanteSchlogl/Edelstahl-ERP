using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Edelstahl.DAL.Interfaces;
using Edelstahl.DAL.Tools;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Implementations.SqlServer
{
    public class BitacoraRepositorySqlServer : IBitacoraRepository
    {
        private const string Columnas =
            @"Id, FechaHora, UsuarioId, NombreUsuario, Modulo, Accion,
              Entidad, EntidadId, Descripcion, Resultado, DireccionEquipo,
              NombreEquipo, TipoEvento, DetalleTecnico";

        public void Add(RegistroBitacora entity)
        {
            Validar(entity);

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            const string sql =
                @"INSERT INTO dbo.Bitacora
                  (Id, FechaHora, UsuarioId, NombreUsuario, Modulo, Accion,
                   Entidad, EntidadId, Descripcion, Resultado, DireccionEquipo,
                   NombreEquipo, TipoEvento, DetalleTecnico)
                  VALUES
                  (@Id, @FechaHora, @UsuarioId, @NombreUsuario, @Modulo, @Accion,
                   @Entidad, @EntidadId, @Descripcion, @Resultado, @DireccionEquipo,
                   @NombreEquipo, @TipoEvento, @DetalleTecnico)";

            using (SqlConnection connection = SqlServerConnection.CreateServicesConnection()) 
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    AgregarParametros(command, entity);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void Update(RegistroBitacora entity)
        {
            throw new InvalidOperationException(
                "Los registros de bitácora no pueden modificarse.");
        }

        public void Delete(Guid id)
        {
            throw new InvalidOperationException(
                "Los registros de bitácora no pueden eliminarse.");
        }

        public RegistroBitacora GetById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return null;
            }

            string sql = "SELECT " + Columnas +
                " FROM dbo.Bitacora WHERE Id = @Id";

            List<RegistroBitacora> lista = EjecutarConsulta(
                sql,
                command => command.Parameters.Add(
                    "@Id", SqlDbType.UniqueIdentifier).Value = id);

            return lista.Count == 0 ? null : lista[0];
        }

        public List<RegistroBitacora> GetAll()
        {
            return EjecutarConsulta(
                "SELECT " + Columnas +
                " FROM dbo.Bitacora ORDER BY FechaHora DESC",
                null);
        }

        public List<RegistroBitacora> GetByUsuarioId(Guid usuarioId)
        {
            if (usuarioId == Guid.Empty)
            {
                return new List<RegistroBitacora>();
            }

            return EjecutarConsulta(
                "SELECT " + Columnas +
                " FROM dbo.Bitacora WHERE UsuarioId = @UsuarioId ORDER BY FechaHora DESC",
                command => command.Parameters.Add(
                    "@UsuarioId", SqlDbType.UniqueIdentifier).Value = usuarioId);
        }

        public List<RegistroBitacora> GetByFecha(DateTime desde, DateTime hasta)
        {
            if (hasta < desde)
            {
                throw new ArgumentException(
                    "La fecha final no puede ser anterior a la fecha inicial.");
            }

            return EjecutarConsulta(
                "SELECT " + Columnas +
                " FROM dbo.Bitacora WHERE FechaHora >= @Desde AND FechaHora <= @Hasta " +
                "ORDER BY FechaHora DESC",
                command =>
                {
                    command.Parameters.Add("@Desde", SqlDbType.DateTime2).Value = desde;
                    command.Parameters.Add("@Hasta", SqlDbType.DateTime2).Value = hasta;
                });
        }

        public List<RegistroBitacora> GetByModulo(string modulo)
        {
            if (string.IsNullOrWhiteSpace(modulo))
            {
                return new List<RegistroBitacora>();
            }

            return EjecutarConsulta(
                "SELECT " + Columnas +
                " FROM dbo.Bitacora WHERE Modulo = @Modulo ORDER BY FechaHora DESC",
                command => command.Parameters.Add(
                    "@Modulo", SqlDbType.NVarChar, 100).Value = modulo.Trim());
        }

        public List<RegistroBitacora> GetByTipoEvento(string tipoEvento)
        {
            if (string.IsNullOrWhiteSpace(tipoEvento))
            {
                return new List<RegistroBitacora>();
            }

            return EjecutarConsulta(
                "SELECT " + Columnas +
                " FROM dbo.Bitacora WHERE TipoEvento = @TipoEvento ORDER BY FechaHora DESC",
                command => command.Parameters.Add(
                    "@TipoEvento", SqlDbType.NVarChar, 50).Value = tipoEvento.Trim());
        }

        private static List<RegistroBitacora> EjecutarConsulta(
            string sql,
            Action<SqlCommand> configurar)
        {
            List<RegistroBitacora> registros = new List<RegistroBitacora>();

            using (SqlConnection connection = SqlServerConnection.CreateServicesConnection())
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    if (configurar != null)
                    {
                        configurar(command);
                    }

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            registros.Add(Mapear(reader));
                        }
                    }
                }
            }

            return registros;
        }

        private static RegistroBitacora Mapear(SqlDataReader reader)
        {
            return new RegistroBitacora
            {
                Id = reader.GetGuid(reader.GetOrdinal("Id")),
                FechaHora = Convert.ToDateTime(reader["FechaHora"]),
                UsuarioId = reader["UsuarioId"] == DBNull.Value
                    ? (Guid?)null : (Guid)reader["UsuarioId"],
                NombreUsuario = Texto(reader, "NombreUsuario"),
                Modulo = Texto(reader, "Modulo"),
                Accion = Texto(reader, "Accion"),
                Entidad = Texto(reader, "Entidad"),
                EntidadId = reader["EntidadId"] == DBNull.Value
                    ? (Guid?)null : (Guid)reader["EntidadId"],
                Descripcion = Texto(reader, "Descripcion"),
                Resultado = Texto(reader, "Resultado"),
                DireccionEquipo = Texto(reader, "DireccionEquipo"),
                NombreEquipo = Texto(reader, "NombreEquipo"),
                TipoEvento = Texto(reader, "TipoEvento"),
                DetalleTecnico = Texto(reader, "DetalleTecnico")
            };
        }

        private static void AgregarParametros(
            SqlCommand command,
            RegistroBitacora registro)
        {
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = registro.Id;
            command.Parameters.Add("@FechaHora", SqlDbType.DateTime2).Value = registro.FechaHora;
            command.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value =
                registro.UsuarioId.HasValue ? (object)registro.UsuarioId.Value : DBNull.Value;
            command.Parameters.Add("@NombreUsuario", SqlDbType.NVarChar, 100).Value =
                Limpiar(registro.NombreUsuario);
            command.Parameters.Add("@Modulo", SqlDbType.NVarChar, 100).Value =
                Limpiar(registro.Modulo);
            command.Parameters.Add("@Accion", SqlDbType.NVarChar, 150).Value =
                Limpiar(registro.Accion);
            command.Parameters.Add("@Entidad", SqlDbType.NVarChar, 100).Value =
                Limpiar(registro.Entidad);
            command.Parameters.Add("@EntidadId", SqlDbType.UniqueIdentifier).Value =
                registro.EntidadId.HasValue ? (object)registro.EntidadId.Value : DBNull.Value;
            command.Parameters.Add("@Descripcion", SqlDbType.NVarChar, 1000).Value =
                Limpiar(registro.Descripcion);
            command.Parameters.Add("@Resultado", SqlDbType.NVarChar, 50).Value =
                Limpiar(registro.Resultado);
            command.Parameters.Add("@DireccionEquipo", SqlDbType.NVarChar, 100).Value =
                Limpiar(registro.DireccionEquipo);
            command.Parameters.Add("@NombreEquipo", SqlDbType.NVarChar, 150).Value =
                Limpiar(registro.NombreEquipo);
            command.Parameters.Add("@TipoEvento", SqlDbType.NVarChar, 50).Value =
                Limpiar(registro.TipoEvento);
            command.Parameters.Add("@DetalleTecnico", SqlDbType.NVarChar, -1).Value =
                Limpiar(registro.DetalleTecnico);
        }

        private static string Texto(SqlDataReader reader, string columna)
        {
            return reader[columna] == DBNull.Value
                ? string.Empty
                : reader[columna].ToString();
        }

        private static string Limpiar(string valor)
        {
            return string.IsNullOrWhiteSpace(valor) ? string.Empty : valor.Trim();
        }

        private static void Validar(RegistroBitacora registro)
        {
            if (registro == null)
            {
                throw new ArgumentNullException(nameof(registro));
            }

            if (registro.FechaHora == default(DateTime))
            {
                registro.FechaHora = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(registro.Modulo) ||
                string.IsNullOrWhiteSpace(registro.Accion))
            {
                throw new InvalidOperationException(
                    "El módulo y la acción de la bitácora son obligatorios.");
            }

            string[] resultados = { "Correcto", "Fallido", "Advertencia" };
            string[] tipos = { "Informacion", "Seguridad", "Error", "Auditoria" };

            if (Array.IndexOf(resultados, registro.Resultado) < 0)
            {
                throw new InvalidOperationException("El resultado de la bitácora no es válido.");
            }

            if (Array.IndexOf(tipos, registro.TipoEvento) < 0)
            {
                throw new InvalidOperationException("El tipo de evento no es válido.");
            }
        }
    }
}
