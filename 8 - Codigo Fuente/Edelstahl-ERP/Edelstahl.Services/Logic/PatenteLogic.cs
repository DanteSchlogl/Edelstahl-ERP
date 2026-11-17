using System;
using System.Collections.Generic;
using Edelstahl.Services.DomainModel.Exceptions;
using Edelstahl.Services.DataAccess.Implementations;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.Logic
{
    /// <summary>Reglas de las patentes (permisos sueltos).</summary>
    internal static class PatenteLogic
    {
        private static readonly IPatenteRepository _repo = new PatenteRepository();

        public static void Add(Patente patente)
        {
            if (patente == null)
            {
                throw new ArgumentNullException("patente");
            }

            if (string.IsNullOrWhiteSpace(patente.DataKey))
            {
                throw new DatoInvalidoException("The permission key (DataKey) is required.");
            }

            patente.DataKey = patente.DataKey.Trim();

            if (string.IsNullOrWhiteSpace(patente.Nombre))
            {
                patente.Nombre = patente.DataKey;
            }

            if (_repo.GetByDataKey(patente.DataKey) != null)
            {
                throw new RegistroDuplicadoException("A permission with the entered key already exists.");
            }

            _repo.Add(patente);
        }

        public static Patente GetById(Guid id)
        {
            return _repo.GetById(id);
        }

        public static Patente GetByDataKey(string dataKey)
        {
            return string.IsNullOrWhiteSpace(dataKey) ? null : _repo.GetByDataKey(dataKey.Trim());
        }

        public static List<Patente> GetAll()
        {
            return _repo.GetAll();
        }

        public static void Delete(Guid id)
        {
            _repo.Delete(id);
        }
    }
}
