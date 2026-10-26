using System;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Autentica al usuario temporal utilizado cuando
    /// Edelstahl funciona en modo demostración.
    ///
    /// No utiliza SQL Server y no conserva la contraseña
    /// en texto plano.
    /// </summary>
    public sealed class DemoAuthenticationService
    {
        private const string UsuarioDemostracion =
            "demo";

        private const string PasswordHashDemostracion =
            "RvscGhczkpckbkA31fGJDRPcOy/qi2eVxmaETGQH22I=";

        private const string PasswordSaltDemostracion =
            "zkiQx/AdCeqjnBNMvtWWGlOV8+UHoKqsu9tKI8oQE7U=";

        private const int IteracionesDemostracion =
            100000;

        public Usuario Autenticar(
            string nombreUsuario,
            string password)
        {
            string usuarioNormalizado =
                string.IsNullOrWhiteSpace(
                    nombreUsuario)
                    ? string.Empty
                    : nombreUsuario
                        .Trim()
                        .ToLowerInvariant();

            if (!string.Equals(
                usuarioNormalizado,
                UsuarioDemostracion,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "El usuario o la contraseña son incorrectos.");
            }

            bool passwordCorrecto =
                PasswordHasher.Verificar(
                    password,
                    PasswordHashDemostracion,
                    PasswordSaltDemostracion,
                    IteracionesDemostracion);

            if (!passwordCorrecto)
            {
                throw new InvalidOperationException(
                    "El usuario o la contraseña son incorrectos.");
            }

            Rol rolDemostracion =
                CrearRolDemostracion();

            Usuario usuarioDemostracion =
                new Usuario
                {
                    Id =
                        Guid.NewGuid(),

                    RolId =
                        rolDemostracion.Id,

                    NombreUsuario =
                        UsuarioDemostracion,

                    NombreCompleto =
                        "Usuario de demostración",

                    Email =
                        "demo@edelstahl.local",

                    PasswordHash =
                        PasswordHashDemostracion,

                    PasswordSalt =
                        PasswordSaltDemostracion,

                    Iteraciones =
                        IteracionesDemostracion,

                    Activo =
                        true,

                    DebeCambiarPassword =
                        false,

                    IntentosFallidos =
                        0,

                    BloqueadoHasta =
                        null,

                    FechaAlta =
                        DateTime.Now,

                    UltimoAcceso =
                        DateTime.Now,

                    Rol =
                        rolDemostracion
                };

            return usuarioDemostracion;
        }

        private static Rol CrearRolDemostracion()
        {
            return new Rol
            {
                Id =
                    Guid.NewGuid(),

                Nombre =
                    "DEMO",

                Descripcion =
                    "Perfil temporal para demostración sin SQL Server.",

                Activo =
                    true,

                FechaAlta =
                    DateTime.Now
            };
        }
    }
}