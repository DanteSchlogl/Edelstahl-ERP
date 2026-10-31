using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.Services;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Cuadros de dialogo del propio usuario: cambiar contrasena, elegir la pregunta de seguridad,
    /// pedir ayuda al administrador y abrir la ayuda del programa.
    /// </summary>
    public static class DialogosUsuario
    {
        public static List<Opcion> PreguntasSeguridad(bool conOpcionVacia)
        {
            List<Opcion> opciones = new List<Opcion>();

            if (conOpcionVacia)
            {
                opciones.Add(new Opcion(Tr.T("(none)"), string.Empty));
            }

            foreach (string pregunta in UsuarioService.PreguntasSeguridad)
            {
                opciones.Add(new Opcion(Tr.T(pregunta), pregunta));
            }

            return opciones;
        }

        /// <summary>Devuelve true si la contrasena se cambio.</summary>
        public static bool CambiarPassword(IWin32Window padre)
        {
            Respuesta datos = Dialogo.Pedir(padre, "Change password",
                "The password must have at least 8 characters.",
                Campo.Clave("actual", "Current password"),
                Campo.Clave("nueva", "New password"),
                Campo.Clave("nueva2", "Confirm password").Confirmando("nueva"));

            if (datos == null)
            {
                return false;
            }

            bool listo = Estilo.Ejecutar(padre, () =>
                new UsuarioService().CambiarPassword(SesionActual.Usuario.Id, datos.Texto("actual"), datos.Texto("nueva")));

            if (listo)
            {
                Estilo.Informar(padre, Tr.T("Your password was changed."));
            }

            return listo;
        }

        /// <summary>Devuelve true si la pregunta se guardo.</summary>
        public static bool CambiarPregunta(IWin32Window padre)
        {
            Respuesta datos = Dialogo.Pedir(padre, "Security question",
                "Choose a question and an answer only you know. You will need the answer to recover your password.",
                Campo.Combo("pregunta", "Question", PreguntasSeguridad(false)),
                Campo.Clave("respuesta", "Answer"),
                Campo.Clave("respuesta2", "Confirm answer").Confirmando("respuesta"),
                Campo.Clave("actual", "Current password"));

            if (datos == null)
            {
                return false;
            }

            bool listo = Estilo.Ejecutar(padre, () =>
                new UsuarioService().CambiarPreguntaSeguridad(
                    SesionActual.Usuario.Id,
                    datos.Texto("actual"),
                    (string)datos.Seleccion("pregunta"),
                    datos.Texto("respuesta")));

            if (listo)
            {
                Estilo.Informar(padre, Tr.T("Your security question was saved."));
            }

            return listo;
        }

        /// <summary>El usuario elige un tema segun su perfil; el pedido queda en la bitacora y se muestran los datos del administrador.</summary>
        public static void PedirAyuda(IWin32Window padre)
        {
            List<Opcion> temas = AyudaService.TemasPara(SesionActual.NombreRol)
                .Select(t => new Opcion(Tr.T(t), t)).ToList();

            Respuesta datos = Dialogo.Pedir(padre, "Request help",
                Tr.T("Profile: {0}", SesionActual.NombreRol) + "\n" +
                Tr.T("Choose what you need. The request is recorded and your administrator will see it."),
                Campo.Combo("tema", "Topic", temas),
                Campo.Multilinea("detalle", "Details (optional)"));

            if (datos == null)
            {
                return;
            }

            bool listo = Estilo.Ejecutar(padre, () =>
                new AyudaService().RegistrarPedido((string)datos.Seleccion("tema"), datos.Texto("detalle")));

            if (listo)
            {
                Estilo.Informar(padre,
                    Tr.T("Your request was registered. If it is urgent, contact the administrator:") +
                    "\n\n" + ContactoAdministrador.Texto());
            }
        }

        /// <summary>Abre la ayuda en linea (archivo .chm que se instala junto al programa).</summary>
        public static void AbrirAyuda(IWin32Window padre)
        {
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Edelstahl-Ayuda.chm");

            if (!File.Exists(ruta))
            {
                Estilo.Avisar(padre, Tr.T("The help file was not found."));
                return;
            }

            Help.ShowHelp(padre as Control, ruta);
        }
    }
}
