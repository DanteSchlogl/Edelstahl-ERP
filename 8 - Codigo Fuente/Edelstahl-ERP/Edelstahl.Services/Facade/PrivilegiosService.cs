using System;
using Edelstahl.Services.DomainModel.Composite;
using Edelstahl.Services.Logic;

namespace Edelstahl.Services.Facade
{
    /// <summary>Punto de entrada para asignar y recuperar los privilegios de un usuario.</summary>
    public static class PrivilegiosService
    {
        public static void AgregarFamilia(Familia familia, Guid idUsuario)
        {
            PrivilegiosLogic.AgregarFamilia(familia, idUsuario);
        }

        public static void AgregarPatente(Patente patente, Guid idUsuario)
        {
            PrivilegiosLogic.AgregarPatente(patente, idUsuario);
        }

        public static void QuitarFamilia(Familia familia, Guid idUsuario)
        {
            PrivilegiosLogic.QuitarFamilia(familia, idUsuario);
        }

        public static void QuitarPatente(Patente patente, Guid idUsuario)
        {
            PrivilegiosLogic.QuitarPatente(patente, idUsuario);
        }

        /// <summary>Recupera los privilegios del usuario con todo el árbol de familias armado.</summary>
        public static UsuarioPrivilegios Obtener(Guid idUsuario, string nombreUsuario)
        {
            return PrivilegiosLogic.Obtener(idUsuario, nombreUsuario);
        }
    }
}
