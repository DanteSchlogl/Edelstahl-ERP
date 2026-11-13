using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.DAL.Interfaces;
using Edelstahl.Domain.Exceptions;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Localization;

namespace Edelstahl.DAL.Implementations.Memory
{
    /// <summary>
    /// Historial de respaldos en memoria para el modo demostración y las
    /// pruebas. El acceso está protegido porque se usa desde varios hilos.
    /// </summary>
    internal sealed class RespaldoRepositoryMemory : IRespaldoRepository
    {
        private static readonly object Candado = new object();
        private static readonly List<RegistroRespaldo> Registros = new List<RegistroRespaldo>();

        public void Add(RegistroRespaldo entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            lock (Candado)
            {
                Registros.Add(entity);
            }
        }

        public void Update(RegistroRespaldo entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            lock (Candado)
            {
                int indice = Registros.FindIndex(r => r.Id == entity.Id);

                if (indice < 0)
                {
                    throw new RegistroNoEncontradoException(
                        LanguageService.Translate("The backup was not found."));
                }

                Registros[indice] = entity;
            }
        }

        public void Delete(Guid id)
        {
            lock (Candado)
            {
                Registros.RemoveAll(r => r.Id == id);
            }
        }

        public RegistroRespaldo GetById(Guid id)
        {
            lock (Candado)
            {
                return Registros.FirstOrDefault(r => r.Id == id);
            }
        }

        public List<RegistroRespaldo> GetAll()
        {
            lock (Candado)
            {
                return Registros.OrderByDescending(r => r.FechaHora).ToList();
            }
        }

        public List<RegistroRespaldo> GetByBaseDatos(string baseDatos)
        {
            lock (Candado)
            {
                return Registros
                    .Where(r => string.Equals(r.BaseDatos, baseDatos, StringComparison.Ordinal))
                    .OrderByDescending(r => r.FechaHora)
                    .ToList();
            }
        }
    }
}
