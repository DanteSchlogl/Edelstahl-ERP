using System;
using System.Collections.Generic;
using Edelstahl.Services.DomainModel.Composite;
using Edelstahl.Services.Logic;

namespace Edelstahl.Services.Facade
{
    /// <summary>Punto de entrada para trabajar con patentes (permisos sueltos).</summary>
    public static class PatenteService
    {
        public static void Add(Patente patente)
        {
            PatenteLogic.Add(patente);
        }

        public static Patente GetById(Guid id)
        {
            return PatenteLogic.GetById(id);
        }

        public static Patente GetByDataKey(string dataKey)
        {
            return PatenteLogic.GetByDataKey(dataKey);
        }

        public static List<Patente> GetAll()
        {
            return PatenteLogic.GetAll();
        }

        public static void Delete(Guid id)
        {
            PatenteLogic.Delete(id);
        }
    }
}
