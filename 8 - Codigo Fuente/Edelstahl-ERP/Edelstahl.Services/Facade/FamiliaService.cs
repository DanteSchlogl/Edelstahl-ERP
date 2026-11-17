using System;
using System.Collections.Generic;
using Edelstahl.Services.DomainModel.Composite;
using Edelstahl.Services.Logic;

namespace Edelstahl.Services.Facade
{
    /// <summary>Punto de entrada para trabajar con familias (roles y grupos de permisos).</summary>
    public static class FamiliaService
    {
        public static void Add(Familia familia)
        {
            FamiliaLogic.Add(familia);
        }

        public static void AgregarPatente(Patente patente, Familia familia)
        {
            FamiliaLogic.AgregarPatente(patente, familia);
        }

        public static void QuitarPatente(Patente patente, Familia familia)
        {
            FamiliaLogic.QuitarPatente(patente, familia);
        }

        public static void AgregarFamilia(Familia hijo, Familia padre)
        {
            FamiliaLogic.AgregarFamilia(hijo, padre);
        }

        public static void QuitarFamilia(Familia hijo, Familia padre)
        {
            FamiliaLogic.QuitarFamilia(hijo, padre);
        }

        public static Familia GetById(Guid id)
        {
            return FamiliaLogic.GetById(id);
        }

        public static Familia GetByNombre(string nombre)
        {
            return FamiliaLogic.GetByNombre(nombre);
        }

        public static List<Familia> GetAll()
        {
            return FamiliaLogic.GetAll();
        }

        public static void Delete(Guid id)
        {
            FamiliaLogic.Delete(id);
        }
    }
}
