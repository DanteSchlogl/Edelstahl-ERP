namespace Edelstahl.Services.Cryptography
{
    /// <summary>
    /// Proporciona una fachada para las operaciones
    /// criptográficas utilizadas por Edelstahl.
    /// </summary>
    public static class CryptographyService
    {
        /// <summary>
        /// Cifra un texto mediante el servicio AES
        /// configurado para la aplicación.
        /// </summary>
        public static string Encrypt(
            string plainText)
        {
            AesEncryptionService encryptionService =
                CreateEncryptionService();

            return encryptionService.Encrypt(
                plainText);
        }

        /// <summary>
        /// Descifra un texto previamente generado
        /// mediante el servicio AES.
        /// </summary>
        public static string Decrypt(
            string encryptedText)
        {
            AesEncryptionService encryptionService =
                CreateEncryptionService();

            return encryptionService.Decrypt(
                encryptedText);
        }

        /// <summary>
        /// Calcula por adelantado la clave maestra. Se invoca en segundo
        /// plano al iniciar para que el primer cifrado no demore.
        /// </summary>
        public static void Precalentar()
        {
            CreateEncryptionService().Precalentar();
        }

        private static AesEncryptionService
            CreateEncryptionService()
        {
            string secret =
                CryptographySettings
                    .GetEncryptionSecret();

            return new AesEncryptionService(
                secret);
        }
    }
}
