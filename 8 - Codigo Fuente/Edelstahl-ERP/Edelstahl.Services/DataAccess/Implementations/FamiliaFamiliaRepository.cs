using System;
using System.Collections.Generic;
using System.Data;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DataAccess.Tools;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Implementations
{
    /// <summary>Familias que contiene una familia (tabla FamiliaFamilia).</summary>
    internal class FamiliaFamiliaRepository : IJoinRepository<Familia, Familia>
    {
        public List<Familia> GetByObject(Familia padre)
        {
            List<Guid> ids = SqlHelper.Consultar(BaseDeDatos.Servicios, 
                "SELECT IdFamiliaHijo FROM dbo.FamiliaFamilia WHERE IdFamiliaPadre = @IdFamiliaPadre ORDER BY IdFamiliaHijo",
                fila => (Guid)fila[0],
                SqlHelper.Parametro("@IdFamiliaPadre", SqlDbType.UniqueIdentifier, padre.Id));

            List<Familia> familias = new List<Familia>();

            foreach (Guid id in ids)
            {
                Familia hija = new FamiliaRepository().GetById(id);

                if (hija != null)
                {
                    familias.Add(hija);
                }
            }

            return familias;
        }

        public bool Existe(Familia hijo, Familia padre)
        {
            return SqlHelper.EjecutarEscalar<int>(BaseDeDatos.Servicios, 
                "SELECT COUNT(1) FROM dbo.FamiliaFamilia WHERE IdFamiliaPadre = @IdFamiliaPadre AND IdFamiliaHijo = @IdFamiliaHijo",
                SqlHelper.Parametro("@IdFamiliaPadre", SqlDbType.UniqueIdentifier, padre.Id),
                SqlHelper.Parametro("@IdFamiliaHijo", SqlDbType.UniqueIdentifier, hijo.Id)) > 0;
        }

        public void Add(Familia hijo, Familia padre)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "INSERT INTO dbo.FamiliaFamilia (IdFamiliaPadre, IdFamiliaHijo) VALUES (@IdFamiliaPadre, @IdFamiliaHijo)",
                SqlHelper.Parametro("@IdFamiliaPadre", SqlDbType.UniqueIdentifier, padre.Id),
                SqlHelper.Parametro("@IdFamiliaHijo", SqlDbType.UniqueIdentifier, hijo.Id));
        }

        public void AddRange(List<Familia> hijos, Familia padre)
        {
            foreach (Familia hijo in hijos)
            {
                if (!Existe(hijo, padre))
                {
                    Add(hijo, padre);
                }
            }
        }

        public void Delete(Familia hijo, Familia padre)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "DELETE FROM dbo.FamiliaFamilia WHERE IdFamiliaPadre = @IdFamiliaPadre AND IdFamiliaHijo = @IdFamiliaHijo",
                SqlHelper.Parametro("@IdFamiliaPadre", SqlDbType.UniqueIdentifier, padre.Id),
                SqlHelper.Parametro("@IdFamiliaHijo", SqlDbType.UniqueIdentifier, hijo.Id));
        }
    }
}
