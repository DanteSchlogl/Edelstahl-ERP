using System.Configuration;

namespace Edelstahl.Services.Cryptography
{
    /// <summary>
    /// Obtiene la configuración utilizada por los
    /// servicios criptográficos de Edelstahl.
    /// </summary>
    public static class CryptographySettings
    {
        private const string SecretKeyName =
            "AesEncryptionSecret";

        /// <summary>
        /// Obtiene la clave configurada para el
        /// cifrado simétrico AES.
        /// </summary>
        public static string GetEncryptionSecret()
        {
            string secret =
                ConfigurationManager.AppSettings[
                    SecretKeyName];

            if (string.IsNullOrWhiteSpace(secret))
            {
                throw new ConfigurationErrorsException(
                    "No se encontró la configuración '" +
                    SecretKeyName +
                    "' en el archivo App.config.");
            }

            if (secret.Length < 16)
            {
                throw new ConfigurationErrorsException(
                    "La clave configurada para AES debe " +
                    "contener al menos 16 caracteres.");
            }

            return secret;
        }
    }
}
