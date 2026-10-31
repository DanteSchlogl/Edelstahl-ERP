using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Edelstahl.Domain.Common;

namespace Edelstahl.Domain.Security
{
    /// <summary>
    /// Representa un usuario autorizado para utilizar Edelstahl ERP.
    /// La contraseña nunca se almacena en texto plano.
    /// </summary>
    public class Usuario : Entity
    {
        public Guid RolId { get; set; }

        public string NombreUsuario { get; set; }

        public string NombreCompleto { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public string PasswordSalt { get; set; }

        public int Iteraciones { get; set; }

        public bool Activo { get; set; }

        public bool DebeCambiarPassword { get; set; }

        public int IntentosFallidos { get; set; }

        public DateTime? BloqueadoHasta { get; set; }

        public DateTime FechaAlta { get; set; }

        public DateTime? UltimoAcceso { get; set; }

        /// <summary>Pregunta de seguridad elegida por el usuario (texto en ingles de la lista del sistema).</summary>
        public string PreguntaSeguridad { get; set; }

        /// <summary>Hash y sal de la respuesta a la pregunta de seguridad; la respuesta nunca se guarda.</summary>
        public string RespuestaHash { get; set; }

        public string RespuestaSalt { get; set; }

        public Rol Rol { get; set; }

        public Usuario()
        {
            NombreUsuario = string.Empty;
            NombreCompleto = string.Empty;
            Email = string.Empty;
            PasswordHash = string.Empty;
            PasswordSalt = string.Empty;
            Iteraciones = 100000;
            Activo = true;
            DebeCambiarPassword = true;
            IntentosFallidos = 0;
            BloqueadoHasta = null;
            FechaAlta = DateTime.Now;
            UltimoAcceso = null;
            PreguntaSeguridad = string.Empty;
            RespuestaHash = string.Empty;
            RespuestaSalt = string.Empty;
            Rol = null;
        }

        public bool TienePreguntaSeguridad()
        {
            return !string.IsNullOrWhiteSpace(PreguntaSeguridad) &&
                   !string.IsNullOrWhiteSpace(RespuestaHash) &&
                   !string.IsNullOrWhiteSpace(RespuestaSalt);
        }

        public bool EstaBloqueado()
        {
            return BloqueadoHasta.HasValue &&
                   BloqueadoHasta.Value > DateTime.Now;
        }

        public bool PuedeIniciarSesion()
        {
            return Activo && !EstaBloqueado();
        }

        public void RegistrarAccesoCorrecto()
        {
            IntentosFallidos = 0;
            BloqueadoHasta = null;
            UltimoAcceso = DateTime.Now;
        }

        public void RegistrarAccesoFallido(
            int intentosMaximos,
            TimeSpan duracionBloqueo)
        {
            if (intentosMaximos <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(intentosMaximos));
            }

            if (duracionBloqueo <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duracionBloqueo));
            }

            IntentosFallidos++;

            if (IntentosFallidos >= intentosMaximos)
            {
                BloqueadoHasta =
                    DateTime.Now.Add(duracionBloqueo);
            }
        }
    }
}

