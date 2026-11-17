using System;
using System.Collections.Generic;
using Edelstahl.Services.DomainModel.Exceptions;
using Edelstahl.Services.DataAccess.Implementations;
using Edelstahl.Services.DataAccess.Interfaces;
using Edelstahl.Services.DomainModel.Composite;

namespace Edelstahl.Services.Logic
{
    /// <summary>Reglas de las familias (roles y grupos de permisos).</summary>
    internal static class FamiliaLogic
    {
        private static readonly IFamiliaRepository _repo = new FamiliaRepository();
        private static readonly FamiliaPatenteRepository _familiaPatente = new FamiliaPatenteRepository();
        private static readonly FamiliaFamiliaRepository _familiaFamilia = new FamiliaFamiliaRepository();

        public static void Add(Familia familia)
        {
            if (familia == null)
            {
                throw new ArgumentNullException("familia");
            }

            if (string.IsNullOrWhiteSpace(familia.Nombre))
            {
                throw new DatoInvalidoException("The family name is required.");
            }

            familia.Nombre = familia.Nombre.Trim();

            if (_repo.GetByNombre(familia.Nombre) != null)
            {
                throw new RegistroDuplicadoException("A family with the entered name already exists.");
            }

            _repo.Add(familia);
        }

        public static void AgregarPatente(Patente patente, Familia familia)
        {
            Validar(patente, familia);

            if (!_familiaPatente.Existe(patente, familia))
            {
                _familiaPatente.Add(patente, familia);
            }
        }

        public static void QuitarPatente(Patente patente, Familia familia)
        {
            Validar(patente, familia);
            _familiaPatente.Delete(patente, familia);
        }

        public static void AgregarFamilia(Familia hijo, Familia padre)
        {
            Validar(hijo, padre);

            if (Component.EsElMismo(hijo, padre))
            {
                throw new ReglaDeNegocioException("Una familia no puede contenerse a sí misma.");
            }

            // Se compara con lo que hay guardado: si el padre ya está dentro del hijo, quedaría un ciclo.
            Familia hijoGuardado = _repo.GetById(hijo.Id);
            Familia padreGuardado = _repo.GetById(padre.Id);

            if (hijoGuardado == null || padreGuardado == null)
            {
                throw new RegistroNoEncontradoException("The family was not found.");
            }

            if (hijoGuardado.Contiene(padreGuardado))
            {
                throw new ReglaDeNegocioException(
                    "La relación no puede realizarse porque generaría un ciclo entre familias ('"
                    + hijo.Nombre + "' dentro de '" + padre.Nombre + "').");
            }

            if (!_familiaFamilia.Existe(hijo, padre))
            {
                _familiaFamilia.Add(hijo, padre);
            }
        }

        public static void QuitarFamilia(Familia hijo, Familia padre)
        {
            Validar(hijo, padre);
            _familiaFamilia.Delete(hijo, padre);
        }

        public static Familia GetById(Guid id)
        {
            return _repo.GetById(id);
        }

        public static Familia GetByNombre(string nombre)
        {
            return string.IsNullOrWhiteSpace(nombre) ? null : _repo.GetByNombre(nombre.Trim());
        }

        public static List<Familia> GetAll()
        {
            return _repo.GetAll();
        }

        public static void Delete(Guid id)
        {
            _repo.Delete(id);
        }

        private static void Validar(Component hijo, Familia familia)
        {
            if (hijo == null || familia == null)
            {
                throw new ArgumentNullException(hijo == null ? "hijo" : "familia");
            }

            if (hijo.Id == Guid.Empty || familia.Id == Guid.Empty)
            {
                throw new DatoInvalidoException("Both components must be saved before they can be related.");
            }
        }
    }
}
