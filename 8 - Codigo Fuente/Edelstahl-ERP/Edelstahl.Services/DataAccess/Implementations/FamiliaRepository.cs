using System;
using System.Collections.Generic;
using System.Data;
using Edelstahl.Services.DataAccess.Implementations.Adapters;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DataAccess.Tools;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Implementations
{
    internal class FamiliaRepository : IFamiliaRepository
    {
        public void Add(Familia entity)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "INSERT INTO dbo.Familia (IdFamilia, Nombre) VALUES (@IdFamilia, @Nombre)",
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, entity.Id),
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 100, entity.Nombre));
        }

        public void Update(Familia entity)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "UPDATE dbo.Familia SET Nombre = @Nombre WHERE IdFamilia = @IdFamilia",
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, entity.Id),
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 100, entity.Nombre));
        }

        public void Delete(Guid id)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                @"DELETE FROM dbo.FamiliaFamilia WHERE IdFamiliaPadre = @Id OR IdFamiliaHijo = @Id;
                  DELETE FROM dbo.FamiliaPatente WHERE IdFamilia = @Id;
                  DELETE FROM dbo.UsuarioFamilia WHERE IdFamilia = @Id;
                  DELETE FROM dbo.Familia WHERE IdFamilia = @Id;",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));
        }

        public Familia GetById(Guid id)
        {
            // Primero se lee la fila y se cierra la lectura; recién después se arma el árbol.
            object[] fila = SqlHelper.ConsultarUno(BaseDeDatos.Servicios, 
                "SELECT IdFamilia, Nombre FROM dbo.Familia WHERE IdFamilia = @IdFamilia",
                SqlHelper.Valores,
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, id));

            return fila == null ? null : FamiliaAdapter.Current.Get(fila);
        }

        public Familia GetByNombre(string nombre)
        {
            object[] fila = SqlHelper.ConsultarUno(BaseDeDatos.Servicios, 
                "SELECT IdFamilia, Nombre FROM dbo.Familia WHERE Nombre = @Nombre",
                SqlHelper.Valores,
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 100, nombre));

            return fila == null ? null : FamiliaAdapter.Current.Get(fila);
        }

        public List<Familia> GetAll()
        {
            List<object[]> filas = SqlHelper.Consultar(BaseDeDatos.Servicios, 
                "SELECT IdFamilia, Nombre FROM dbo.Familia ORDER BY Nombre",
                SqlHelper.Valores);

            List<Familia> familias = new List<Familia>();

            foreach (object[] fila in filas)
            {
                familias.Add(FamiliaAdapter.Current.Get(fila));
            }

            return familias;
        }
    }
}
