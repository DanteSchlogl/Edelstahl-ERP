using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.Services.Integrity
{
    /// <summary>
    /// Calcula y verifica el Dígito Verificador
    /// Horizontal de un cliente.
    ///
    /// El DVH se obtiene mediante SHA-256 sobre
    /// una representación determinística de los
    /// campos protegidos del registro.
    /// </summary>
    public static class ClienteDvhService
    {
        /// <summary>
        /// Calcula el DVH correspondiente al estado
        /// actual del cliente.
        ///
        /// Cuando se utiliza con Entity Framework,
        /// el CUIT debe contener exactamente el valor
        /// que será almacenado en la base de datos.
        /// </summary>
        public static string Calcular(
            Cliente cliente)
        {
            if (cliente == null)
            {
                throw new ArgumentNullException(
                    nameof(cliente));
            }

            string contenido =
                ConstruirContenido(
                    cliente);

            byte[] datos =
                Encoding.UTF8.GetBytes(
                    contenido);

            byte[] hash;

            using (SHA256 sha256 =
                SHA256.Create())
            {
                hash =
                    sha256.ComputeHash(
                        datos);
            }

            return ConvertirAHexadecimal(
                hash);
        }

        /// <summary>
        /// Comprueba que el DVH almacenado coincida
        /// con el valor calculado actualmente.
        /// </summary>
        public static bool EsValido(
            Cliente cliente)
        {
            if (cliente == null ||
                string.IsNullOrWhiteSpace(
                    cliente.DVH))
            {
                return false;
            }

            string dvhCalculado =
                Calcular(
                    cliente);

            return CompararTiempoConstante(
                cliente.DVH.Trim(),
                dvhCalculado);
        }

        /// <summary>
        /// Calcula y asigna el DVH al cliente.
        /// </summary>
        public static void Asignar(
            Cliente cliente)
        {
            if (cliente == null)
            {
                throw new ArgumentNullException(
                    nameof(cliente));
            }

            cliente.DVH =
                Calcular(
                    cliente);
        }

        private static string ConstruirContenido(
            Cliente cliente)
        {
            StringBuilder contenido =
                new StringBuilder();

            AgregarCampo(
                contenido,
                "Id",
                cliente.Id.ToString("D"));

            /*
             * En persistencia, este campo contendrá
             * el paquete AES cifrado exactamente como
             * se guarda en SQL Server.
             */
            AgregarCampo(
                contenido,
                "CUIT",
                NormalizarTexto(
                    cliente.CUIT));

            AgregarCampo(
                contenido,
                "RazonSocial",
                NormalizarTexto(
                    cliente.RazonSocial));

            AgregarCampo(
                contenido,
                "DireccionFacturacion",
                NormalizarTexto(
                    cliente.DireccionFacturacion));

            AgregarCampo(
                contenido,
                "DireccionEntrega",
                NormalizarTexto(
                    cliente.DireccionEntrega));

            AgregarCampo(
                contenido,
                "Localidad",
                NormalizarTexto(
                    cliente.Localidad));

            AgregarCampo(
                contenido,
                "Provincia",
                NormalizarTexto(
                    cliente.Provincia));

            AgregarCampo(
                contenido,
                "CodigoPostal",
                NormalizarTexto(
                    cliente.CodigoPostal));

            AgregarCampo(
                contenido,
                "Email",
                NormalizarTexto(
                    cliente.Email));

            AgregarCampo(
                contenido,
                "Telefono",
                NormalizarTexto(
                    cliente.Telefono));

            AgregarCampo(
                contenido,
                "LimiteCredito",
                cliente.LimiteCredito.ToString(
                    "G29",
                    CultureInfo.InvariantCulture));

            AgregarCampo(
                contenido,
                "DeudaActual",
                cliente.DeudaActual.ToString(
                    "G29",
                    CultureInfo.InvariantCulture));

            AgregarCampo(
                contenido,
                "Activo",
                cliente.Activo
                    ? "1"
                    : "0");

            AgregarCampo(
                contenido,
                "FechaAlta",
                cliente.FechaAlta.ToString(
                    "yyyy-MM-ddTHH:mm:ss.fffffff",
                    CultureInfo.InvariantCulture));

            return contenido.ToString();
        }

        private static void AgregarCampo(
            StringBuilder contenido,
            string nombre,
            string valor)
        {
            string valorSeguro =
                valor ??
                string.Empty;

            /*
             * Se incorpora la longitud para evitar
             * ambigüedades durante la concatenación.
             */
            contenido.Append(
                nombre);

            contenido.Append(
                ':');

            contenido.Append(
                valorSeguro.Length.ToString(
                    CultureInfo.InvariantCulture));

            contenido.Append(
                ':');

            contenido.Append(
                valorSeguro);

            contenido.Append(
                '|');
        }

        private static string NormalizarTexto(
            string valor)
        {
            return string.IsNullOrEmpty(
                valor)
                ? string.Empty
                : valor.Trim();
        }

        private static string ConvertirAHexadecimal(
            byte[] datos)
        {
            StringBuilder resultado =
                new StringBuilder(
                    datos.Length * 2);

            foreach (byte valor
                in datos)
            {
                resultado.Append(
                    valor.ToString(
                        "X2",
                        CultureInfo.InvariantCulture));
            }

            return resultado.ToString();
        }

        private static bool CompararTiempoConstante(
            string valorEsperado,
            string valorCalculado)
        {
            if (valorEsperado == null ||
                valorCalculado == null ||
                valorEsperado.Length !=
                valorCalculado.Length)
            {
                return false;
            }

            int diferencia =
                0;

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
    }
}