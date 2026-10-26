using System;
using System.Security.Cryptography;

namespace Edelstahl.Services.Security
{
    /// <summary>
    /// Resultado seguro del procesamiento de una contraseña.
    /// Solo almacena el hash, la sal y la cantidad de iteraciones.
    /// </summary>
    public sealed class PasswordHashData
    {
        public string Hash { get; private set; }

        public string Salt { get; private set; }

        public int Iteraciones { get; private set; }

        public PasswordHashData(
            string hash,
            string salt,
            int iteraciones)
        {
            Hash = hash;
            Salt = salt;
            Iteraciones = iteraciones;
        }
    }

    /// <summary>
    /// Genera y verifica hashes de contraseñas mediante PBKDF2.
    /// Las contraseñas nunca se guardan ni se pueden recuperar.
    /// </summary>
    public static class PasswordHasher
    {
        public const int IteracionesPredeterminadas = 100000;

        private const int LongitudSalt = 32;
        private const int LongitudHash = 32;

        public static PasswordHashData CrearHash(
            string password)
        {
            return CrearHash(
                password,
                IteracionesPredeterminadas);
        }

        public static PasswordHashData CrearHash(
            string password,
            int iteraciones)
        {
            ValidarPassword(password);
            ValidarIteraciones(iteraciones);

            byte[] salt = new byte[LongitudSalt];

            using (RandomNumberGenerator generador =
                RandomNumberGenerator.Create())
            {
                generador.GetBytes(salt);
            }

            byte[] hash = DerivarClave(
                password,
                salt,
                iteraciones);

            return new PasswordHashData(
                Convert.ToBase64String(hash),
                Convert.ToBase64String(salt),
                iteraciones);
        }

        public static bool Verificar(
            string password,
            string hashGuardado,
            string saltGuardada,
            int iteraciones)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrWhiteSpace(hashGuardado) ||
                string.IsNullOrWhiteSpace(saltGuardada) ||
                iteraciones <= 0)
            {
                return false;
            }

            byte[] hashEsperado;
            byte[] salt;

            try
            {
                hashEsperado = Convert.FromBase64String(
                    hashGuardado.Trim());

                salt = Convert.FromBase64String(
                    saltGuardada.Trim());
            }
            catch (FormatException)
            {
                return false;
            }

            if (hashEsperado.Length == 0 ||
                salt.Length == 0)
            {
                return false;
            }

            byte[] hashCalculado = DerivarClave(
                password,
                salt,
                iteraciones,
                hashEsperado.Length);

            return ComparacionTiempoConstante(
                hashEsperado,
                hashCalculado);
        }

        private static byte[] DerivarClave(
            string password,
            byte[] salt,
            int iteraciones)
        {
            return DerivarClave(
                password,
                salt,
                iteraciones,
                LongitudHash);
        }

        private static byte[] DerivarClave(
            string password,
            byte[] salt,
            int iteraciones,
            int longitud)
        {
            using (Rfc2898DeriveBytes derivador =
                new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iteraciones))
            {
                return derivador.GetBytes(longitud);
            }
        }

        private static bool ComparacionTiempoConstante(
            byte[] valorEsperado,
            byte[] valorCalculado)
        {
            if (valorEsperado == null ||
                valorCalculado == null ||
                valorEsperado.Length != valorCalculado.Length)
            {
                return false;
            }

            int diferencia = 0;

            for (int indice = 0;
                indice < valorEsperado.Length;
                indice++)
            {
                diferencia |=
                    valorEsperado[indice] ^
                    valorCalculado[indice];
            }

            return diferencia == 0;
        }

        private static void ValidarPassword(
            string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(
                    Edelstahl.Services.Localization.LanguageService.Translate("The password cannot be empty."),
                    nameof(password));
            }

            if (password.Length < 8)
            {
                throw new ArgumentException(
                    Edelstahl.Services.Localization.LanguageService.Translate("The password must have at least 8 characters."),
                    nameof(password));
            }
        }

        private static void ValidarIteraciones(
            int iteraciones)
        {
            if (iteraciones < 10000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(iteraciones),
                    "La cantidad de iteraciones no puede ser menor a 10000.");
            }
        }
    }
}
