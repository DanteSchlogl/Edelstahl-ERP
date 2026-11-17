using System;
using System.Collections.Generic;
using System.Data;
using Edelstahl.Services.DataAccess.Implementations.Adapters;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DataAccess.Tools;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Implementations
{
    /// <summary>Patentes que contiene una familia (tabla FamiliaPatente).</summary>
    internal class FamiliaPatenteRepository : IJoinRepository<Patente, Familia>
    {
        public List<Patente> GetByObject(Familia familia)
        {
            List<object[]> filas = SqlHelper.Consultar(BaseDeDatos.Servicios, 
                @"SELECT p.IdPatente, p.DataKey, p.Nombre, p.TipoAcceso
                  FROM dbo.FamiliaPatente fp
                  JOIN dbo.Patente p ON p.IdPatente = fp.IdPatente
                  WHERE fp.IdFamilia = @IdFamilia
                  ORDER BY p.DataKey",
                SqlHelper.Valores,
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, familia.Id));

            List<Patente> patentes = new List<Patente>();

            foreach (object[] fila in filas)
            {
                patentes.Add(PatenteAdapter.Current.Get(fila));
            }

            return patentes;
        }

        public bool Existe(Patente patente, Familia familia)
        {
            return SqlHelper.EjecutarEscalar<int>(BaseDeDatos.Servicios, 
                "SELECT COUNT(1) FROM dbo.FamiliaPatente WHERE IdFamilia = @IdFamilia AND IdPatente = @IdPatente",
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, familia.Id),
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, patente.Id)) > 0;
        }

        public void Add(Patente patente, Familia familia)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "INSERT INTO dbo.FamiliaPatente (IdFamilia, IdPatente) VALUES (@IdFamilia, @IdPatente)",
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, familia.Id),
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, patente.Id));
        }

        public void AddRange(List<Patente> patentes, Familia familia)
        {
            foreach (Patente patente in patentes)
            {
                if (!Existe(patente, familia))
                {
                    Add(patente, familia);
                }
            }
        }

        public void Delete(Patente patente, Familia familia)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "DELETE FROM dbo.FamiliaPatente WHERE IdFamilia = @IdFamilia AND IdPatente = @IdPatente",
                SqlHelper.Parametro("@IdFamilia", SqlDbType.UniqueIdentifier, familia.Id),
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, patente.Id));
        }
    }
}
