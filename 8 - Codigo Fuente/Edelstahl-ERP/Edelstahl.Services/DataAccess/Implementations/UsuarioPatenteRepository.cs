using System;
using System.Collections.Generic;
using System.Data;
using Edelstahl.Services.DataAccess.Implementations.Adapters;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DataAccess.Tools;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Implementations
{
    /// <summary>Patentes asignadas directamente a un usuario, sin familia (tabla UsuarioPatente).</summary>
    internal class UsuarioPatenteRepository : IJoinRepository<Patente, UsuarioPrivilegios>
    {
        public List<Patente> GetByObject(UsuarioPrivilegios usuario)
        {
            List<object[]> filas = SqlHelper.Consultar(BaseDeDatos.Servicios, 
                @"SELECT p.IdPatente, p.DataKey, p.Nombre, p.TipoAcceso
                  FROM dbo.UsuarioPatente up
                  JOIN dbo.Patente p ON p.IdPatente = up.IdPatente
                  WHERE up.IdUsuario = @IdUsuario
                  ORDER BY p.DataKey",
                SqlHelper.Valores,
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario));

            List<Patente> patentes = new List<Patente>();

            foreach (object[] fila in filas)
            {
                patentes.Add(PatenteAdapter.Current.Get(fila));
            }

            return patentes;
        }

        public bool Existe(Patente patente, UsuarioPrivilegios usuario)
        {
            return SqlHelper.EjecutarEscalar<int>(BaseDeDatos.Servicios, 
                "SELECT COUNT(1) FROM dbo.UsuarioPatente WHERE IdUsuario = @IdUsuario AND IdPatente = @IdPatente",
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario),
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, patente.Id)) > 0;
        }

        public void Add(Patente patente, UsuarioPrivilegios usuario)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "INSERT INTO dbo.UsuarioPatente (IdUsuario, IdPatente) VALUES (@IdUsuario, @IdPatente)",
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario),
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, patente.Id));
        }

        public void AddRange(List<Patente> patentes, UsuarioPrivilegios usuario)
        {
            foreach (Patente patente in patentes)
            {
                if (!Existe(patente, usuario))
                {
                    Add(patente, usuario);
                }
            }
        }

        public void Delete(Patente patente, UsuarioPrivilegios usuario)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "DELETE FROM dbo.UsuarioPatente WHERE IdUsuario = @IdUsuario AND IdPatente = @IdPatente",
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario),
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, patente.Id));
        }
    }
}
