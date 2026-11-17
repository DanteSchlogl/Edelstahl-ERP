using System;
using System.Collections.Generic;
using System.Data;
using Edelstahl.Services.DataAccess.Implementations.Adapters;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DataAccess.Tools;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.DataAccess.Implementations
{
    internal class PatenteRepository : IPatenteRepository
    {
        private const string Columnas = "IdPatente, DataKey, Nombre, TipoAcceso";

        public void Add(Patente entity)
        {
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "INSERT INTO dbo.Patente (IdPatente, DataKey, Nombre, TipoAcceso) VALUES (@IdPatente, @DataKey, @Nombre, @TipoAcceso)",
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, entity.Id),
                SqlHelper.Parametro("@DataKey", SqlDbType.NVarChar, 100, entity.DataKey),
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 150, entity.Nombre ?? string.Empty),
                SqlHelper.Parametro("@TipoAcceso", SqlDbType.Int, (int)entity.TipoAcceso));
        }

        public void Update(Patente entity)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                "UPDATE dbo.Patente SET DataKey = @DataKey, Nombre = @Nombre, TipoAcceso = @TipoAcceso WHERE IdPatente = @IdPatente",
                SqlHelper.Parametro("@IdPatente", SqlDbType.UniqueIdentifier, entity.Id),
                SqlHelper.Parametro("@DataKey", SqlDbType.NVarChar, 100, entity.DataKey),
                SqlHelper.Parametro("@Nombre", SqlDbType.NVarChar, 150, entity.Nombre ?? string.Empty),
                SqlHelper.Parametro("@TipoAcceso", SqlDbType.Int, (int)entity.TipoAcceso));
        }

        public void Delete(Guid id)
        {
            SqlHelper.EjecutarNoQuery(BaseDeDatos.Servicios, 
                @"DELETE FROM dbo.FamiliaPatente WHERE IdPatente = @Id;
                  DELETE FROM dbo.UsuarioPatente WHERE IdPatente = @Id;
                  DELETE FROM dbo.Patente WHERE IdPatente = @Id;",
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));
        }

        public Patente GetById(Guid id)
        {
            object[] fila = SqlHelper.ConsultarUno(BaseDeDatos.Servicios, 
                "SELECT " + Columnas + " FROM dbo.Patente WHERE IdPatente = @Id",
                SqlHelper.Valores,
                SqlHelper.Parametro("@Id", SqlDbType.UniqueIdentifier, id));

            return fila == null ? null : PatenteAdapter.Current.Get(fila);
        }

        public Patente GetByDataKey(string dataKey)
        {
            object[] fila = SqlHelper.ConsultarUno(BaseDeDatos.Servicios, 
                "SELECT " + Columnas + " FROM dbo.Patente WHERE DataKey = @DataKey",
                SqlHelper.Valores,
                SqlHelper.Parametro("@DataKey", SqlDbType.NVarChar, 100, dataKey));

            return fila == null ? null : PatenteAdapter.Current.Get(fila);
        }

        public List<Patente> GetAll()
        {
            List<object[]> filas = SqlHelper.Consultar(BaseDeDatos.Servicios, 
                "SELECT " + Columnas + " FROM dbo.Patente ORDER BY DataKey",
                SqlHelper.Valores);

            List<Patente> patentes = new List<Patente>();

            foreach (object[] fila in filas)
            {
                patentes.Add(PatenteAdapter.Current.Get(fila));
            }

            return patentes;
        }
    }
}
