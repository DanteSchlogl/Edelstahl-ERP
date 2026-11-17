using System;
using System.Collections.Generic;
using System.Data;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DataAccess.Tools;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Implementations
{
    /// <summary>Familias (roles) asignadas a un usuario (tabla UsuarioFamilia).</summary>
    internal class UsuarioFamiliaRepository : IJoinRepository<Familia, UsuarioPrivilegios>
    {
        public List<Familia> GetByObject(UsuarioPrivilegios usuario)
        {
            List<Guid> ids = SqlHelper.Consultar(BaseDeDatos.Servicios, 
                "SELECT IdFamilia FROM dbo.UsuarioFamilia WHERE IdUsuario = @IdUsuario",
                fila => (Guid)fila[0],
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario));

            List<Familia> familias = new List<Familia>();

            foreach (Guid id in ids)
            {
                Familia familia = new FamiliaRepository().GetById(id);

                if (familia != null)
                {
                    familias.Add(familia);
                }
            }

            return familias;
        }

        public bool Existe(Familia familia, UsuarioPrivilegios usuario)
        {
            return SqlHelper.EjecutarEscalar<int>(BaseDeDatos.Servicios, 
                "SELECT COUNT(1) FROM dbo.UsuarioFamilia WHERE IdUsuario = @IdUsuario AND IdFamilia = @IdFamilia",
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario),
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, familia.Id)) > 0;
        }

        public void Add(Familia familia, UsuarioPrivilegios usuario)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "INSERT INTO dbo.UsuarioFamilia (IdUsuario, IdFamilia) VALUES (@IdUsuario, @IdFamilia)",
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario),
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, familia.Id));
        }

        public void AddRange(List<Familia> familias, UsuarioPrivilegios usuario)
        {
            foreach (Familia familia in familias)
            {
                if (!Existe(familia, usuario))
                {
                    Add(familia, usuario);
                }
            }
        }

        public void Delete(Familia familia, UsuarioPrivilegios usuario)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "DELETE FROM dbo.UsuarioFamilia WHERE IdUsuario = @IdUsuario AND IdFamilia = @IdFamilia",
                SqlHelper.Parametro("@IdUsuario", SqlDbType.UniqueIdentifier, usuario.IdUsuario),
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, familia.Id));
        }

        /// <summary>Rol de cada usuario existente: Id del usuario y nombre del rol.</summary>
        public List<KeyValuePair<Guid, string>> RolesDeLosUsuarios()
        {
            return SqlHelper.Consultar(BaseDeDatos.Servicios, 
                "SELECT u.Id, r.Nombre FROM dbo.Usuarios u JOIN dbo.Roles r ON r.Id = u.RolId",
                fila => new KeyValuePair<Guid, string>((Guid)fila[0], fila[1].ToString()));
        }
    }
}
