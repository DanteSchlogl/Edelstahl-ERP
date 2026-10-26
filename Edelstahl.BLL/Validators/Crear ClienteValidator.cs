using System;
using System.Net.Mail;
using Edelstahl.BLL.Exceptions.Commercial;
using Edelstahl.Domain.Comercial;
using Edelstahl.BLL.Infrastructure;

namespace Edelstahl.BLL.Validators
{
    /// <summary>
    /// Centraliza las validaciones aplicables a los clientes.
    /// </summary>
    public static class ClienteValidator
    {
        public static void Validate(Cliente cliente)
        {
            if (cliente == null)
            {
                throw new ArgumentNullException(
                    nameof(cliente),
                    "El cliente no puede ser nulo.");
            }

            ValidateCUIT(cliente.CUIT);
            ValidateRazonSocial(cliente.RazonSocial);
            ValidateLimiteCredito(cliente.LimiteCredito);
            ValidateEmail(cliente.Email);
        }

        public static void ValidateCUIT(string cuit)
        {
            if (string.IsNullOrWhiteSpace(cuit))
            {
                throw new CUITInvalidoException(cuit);
            }

            string cuitNormalizado = cuit
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);

            if (cuitNormalizado.Length != 11)
            {
                throw new CUITInvalidoException(cuit);
            }

            foreach (char caracter in cuitNormalizado)
            {
                if (!char.IsDigit(caracter))
                {
                    throw new CUITInvalidoException(cuit);
                }
            }
        }

        public static string NormalizeCUIT(string cuit)
        {
            if (string.IsNullOrWhiteSpace(cuit))
            {
                return string.Empty;
            }

            return cuit
                .Trim()
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty);
        }

        private static void ValidateRazonSocial(string razonSocial)
        {
            if (string.IsNullOrWhiteSpace(razonSocial))
            {
                throw Mensajes.Regla("The business name is required.");
            }

            if (razonSocial.Trim().Length < 3)
            {
                throw Mensajes.Regla("The business name must have at least 3 characters.");
            }

            if (razonSocial.Trim().Length > 150)
            {
                throw Mensajes.Regla("The business name cannot exceed 150 characters.");
            }
        }

        private static void ValidateLimiteCredito(decimal limiteCredito)
        {
            if (limiteCredito < 0)
            {
                throw Mensajes.Regla("The credit limit cannot be negative.");
            }
        }

        private static void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            try
            {
                MailAddress direccion = new MailAddress(email);

                if (!string.Equals(
                    direccion.Address,
                    email.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new FormatException();
                }
            }
            catch (FormatException)
            {
                throw Mensajes.Regla("The email address entered is not valid.");
            }
        }
    }
}