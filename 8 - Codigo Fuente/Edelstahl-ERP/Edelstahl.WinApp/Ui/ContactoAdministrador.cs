using System.Collections.Generic;
using System.Configuration;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Datos de contacto del administrador del sistema. Se leen de App.config
    /// (claves ContactoNombre, ContactoRecuperacion y ContactoCorreo).
    /// </summary>
    public static class ContactoAdministrador
    {
        public static string Nombre
        {
            get { return Leer("ContactoNombre"); }
        }

        public static string Telefono
        {
            get { return Leer("ContactoRecuperacion"); }
        }

        public static string Correo
        {
            get { return Leer("ContactoCorreo"); }
        }

        /// <summary>Nombre, WhatsApp y correo, uno por linea; omite los que no estan configurados.</summary>
        public static string Texto()
        {
            List<string> lineas = new List<string>();

            if (Nombre.Length > 0)
            {
                lineas.Add(Nombre);
            }

            if (Telefono.Length > 0)
            {
                lineas.Add("WhatsApp: " + Telefono);
            }

            if (Correo.Length > 0)
            {
                lineas.Add("Email: " + Correo);
            }

            return string.Join("\n", lineas);
        }

        private static string Leer(string clave)
        {
            string valor = ConfigurationManager.AppSettings[clave];
            return valor == null ? string.Empty : valor.Trim();
        }
    }
}
