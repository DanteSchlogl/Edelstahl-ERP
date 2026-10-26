using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Infrastructure
{
    /// <summary>
    /// Control de acceso basado en roles. Las operaciones sensibles llaman a
    /// Exigir antes de modificar datos; los intentos denegados quedan en la
    /// bitácora.
    /// </summary>
    public static class Autorizacion
    {
        public static bool Tiene(Permiso permiso)
        {
            return SesionActual.Tiene(permiso);
        }

        public static void Exigir(Permiso permiso)
        {
            if (SesionActual.Tiene(permiso))
            {
                return;
            }

            string mensaje = Mensajes.T("You do not have permission to perform this operation.");

            try
            {
                new BitacoraService().Registrar(
                    "Seguridad",
                    "AccesoDenegado",
                    "Permiso",
                    null,
                    "Permiso requerido: " + permiso,
                    "Fallido",
                    "Seguridad");
            }
            catch
            {
                // La bitácora nunca debe ocultar el motivo de la denegación.
            }

            throw new AccesoNoAutorizadoException(mensaje);
        }
    }
}
