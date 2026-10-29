using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Edelstahl.Domain.Comercial;

namespace Edelstahl.Services.Integrity
{
    /// <summary>
    /// Calcula y verifica el Dígito Verificador
    /// Vertical correspondiente a la tabla Clientes.
    ///
    /// El DVV se genera mediante SHA-256 a partir
    /// del Id y el DVH de todos los clientes,
    /// ordenados de forma determinística.
    /// </summary>
    public static class ClienteDvvService
    {
        public const string NombreTabla =
            "Clientes";

        /// <summary>
        /// Calcula el DVV del conjunto completo
        /// de clientes.
        /// </summary>
        public static string Calcular(
            IEnumerable<Cliente> clientes)
        {
            if (clientes == null)
            {
                throw new ArgumentNullException(
                    nameof(clientes));
            }

            List<Cliente> clientesOrdenados =
                clientes
                    .OrderBy(
                        cliente =>
                            cliente.Id)
                    .ToList();

            string contenido =
                ConstruirContenido(
                    clientesOrdenados);

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
        /// Compara el DVV almacenado con el DVV
        /// calculado para los clientes actuales.
        /// </summary>
        public static bool EsValido(
            IEnumerable<Cliente> clientes,
            string dvvAlmacenado)
        {
            if (clientes == null ||
                string.IsNullOrWhiteSpace(
                    dvvAlmacenado))
            {
                return false;
            }

            string dvvCalculado =
                Calcular(
                    clientes);

            return CompararTiempoConstante(
                dvvAlmacenado.Trim(),
                dvvCalculado);
        }

        private static string ConstruirContenido(
            IList<Cliente> clientes)
        {
            StringBuilder contenido =
                new StringBuilder();

            AgregarCampo(
                contenido,
                "Tabla",
                NombreTabla);

            AgregarCampo(
                contenido,
                "Cantidad",
                clientes.Count.ToString(
                    CultureInfo.InvariantCulture));

            foreach (Cliente cliente
                in clientes)
            {
                if (cliente == null)
                {
                    throw new InvalidOperationException(
                        "La colección contiene un cliente nulo.");
                }

                if (cliente.Id == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        "No es posible calcular el DVV " +
                        "porque existe un cliente sin " +
                        "identificador válido.");
                }

                if (string.IsNullOrWhiteSpace(
                    cliente.DVH))
                {
                    throw new InvalidOperationException(
                        "No es posible calcular el DVV " +
                        "porque el cliente '" +
                        cliente.RazonSocial +
                        "' no posee un DVH válido.");
                }

                AgregarCampo(
                    contenido,
                    "Id",
                    cliente.Id.ToString("D"));

                AgregarCampo(
                    contenido,
                    "DVH",
                    cliente.DVH.Trim()
                        .ToUpperInvariant());
            }

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
