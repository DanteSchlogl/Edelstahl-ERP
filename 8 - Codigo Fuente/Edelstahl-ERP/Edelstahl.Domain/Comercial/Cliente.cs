using System;
using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Comercial
{
    /// <summary>
    /// Una empresa se identifica con CUIT; un particular, con CUIL.
    /// </summary>
    public enum TipoPersona
    {
        Empresa = 0,
        Particular = 1
    }

    /// <summary>
    /// Representa a un cliente de Edelstahl.
    /// </summary>
    public class Cliente : Entity
    {
        public string CUIT
        {
            get;
            set;
        }

        /// <summary>
        /// Empresa (CUIT) o particular (CUIL). El número se guarda siempre en CUIT.
        /// </summary>
        public TipoPersona Tipo
        {
            get;
            set;
        }

        /// <summary>
        /// Texto del documento según el tipo de cliente: CUIT o CUIL.
        /// </summary>
        public string EtiquetaDocumento
        {
            get { return Tipo == TipoPersona.Particular ? "CUIL" : "CUIT"; }
        }

        public string RazonSocial
        {
            get;
            set;
        }

        public string DireccionFacturacion
        {
            get;
            set;
        }

        public string DireccionEntrega
        {
            get;
            set;
        }

        public string Localidad
        {
            get;
            set;
        }

        public string Provincia
        {
            get;
            set;
        }

        public string CodigoPostal
        {
            get;
            set;
        }

        public string Email
        {
            get;
            set;
        }

        public string Telefono
        {
            get;
            set;
        }

        /// <summary>
        /// Cliente autorizado a recibir la mercaderia sin haber pagado el 100% (cuenta corriente).
        /// </summary>
        public bool AutorizadoACredito
        {
            get;
            set;
        }

        public decimal LimiteCredito
        {
            get;
            set;
        }

        public decimal DeudaActual
        {
            get;
            set;
        }

        public bool Activo
        {
            get;
            set;
        }

        public DateTime FechaAlta
        {
            get;
            set;
        }

        /// <summary>
        /// Dígito Verificador Horizontal del registro.
        ///
        /// Contiene un hash SHA-256 representado mediante
        /// 64 caracteres hexadecimales.
        /// </summary>
        public string DVH
        {
            get;
            set;
        }

        public Cliente()
        {
            CUIT =
                string.Empty;

            RazonSocial =
                string.Empty;

            DireccionFacturacion =
                string.Empty;

            DireccionEntrega =
                string.Empty;

            Localidad =
                string.Empty;

            Provincia =
                string.Empty;

            CodigoPostal =
                string.Empty;

            Email =
                string.Empty;

            Telefono =
                string.Empty;

            Activo =
                true;

            DeudaActual =
                0m;

            FechaAlta =
                DateTime.Now;

            DVH =
                string.Empty;
        }

        public decimal CalcularCreditoDisponible()
        {
            return LimiteCredito -
                DeudaActual;
        }
    }
}