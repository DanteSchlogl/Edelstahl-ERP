using System;
using Edelstahl.WinApp.Ui;
using System.Threading;
using System.Windows.Forms;
using Edelstahl.BLL.Services;

namespace Edelstahl.WinApp.Infrastructure
{
    /// <summary>
    /// Captura excepciones no controladas, registra su detalle tecnico
    /// en la bitacora y muestra mensajes seguros al usuario.
    /// </summary>
    public static class GlobalExceptionHandler
    {
        private static readonly object Sincronizacion = new object();
        private static bool _registrado;
        private static bool _mostrandoError;

        public static void Registrar()
        {
            lock (Sincronizacion)
            {
                if (_registrado)
                {
                    return;
                }

                Application.SetUnhandledExceptionMode(
                    UnhandledExceptionMode.CatchException);

                Application.ThreadException +=
                    Application_ThreadException;

                AppDomain.CurrentDomain.UnhandledException +=
                    CurrentDomain_UnhandledException;

                _registrado = true;
            }
        }

        public static void ProcesarErrorInicio(
            Exception exception)
        {
            Procesar(
                exception,
                "Inicio",
                "Inicializacion de la aplicacion",
                Tr.T("Edelstahl ERP could not be started."),
                true);
        }

        public static void ProcesarErrorControlado(
            Exception exception,
            string modulo,
            string accion,
            string mensajeUsuario)
        {
            Procesar(
                exception,
                modulo,
                accion,
                mensajeUsuario,
                false);
        }

        private static void Application_ThreadException(
            object sender,
            ThreadExceptionEventArgs e)
        {
            Procesar(
                e.Exception,
                "Interfaz",
                "Excepcion no controlada",
                Tr.T("An unexpected error occurred. The operation could not be completed."),
                false);
        }

        private static void CurrentDomain_UnhandledException(
            object sender,
            UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;

            if (exception == null)
            {
                exception = new Exception(
                    "Se produjo una excepcion no controlada sin detalle disponible.");
            }

            RegistrarEnBitacora(
                exception,
                "Sistema",
                "Excepcion no controlada de dominio");

            if (!e.IsTerminating)
            {
                MostrarMensajeSeguro(
                    Tr.T("An unexpected error occurred in the system."), Tr.T("Unexpected error"));
            }
        }

        private static void Procesar(
            Exception exception,
            string modulo,
            string accion,
            string mensajeUsuario,
            bool errorInicio)
        {
            Exception error = exception ?? new Exception(
                "No se recibio informacion tecnica de la excepcion.");

            RegistrarEnBitacora(
                error,
                modulo,
                accion);

            string titulo = errorInicio
                ? Tr.T("Startup error") : Tr.T("Unexpected error");

            MostrarMensajeSeguro(
                mensajeUsuario,
                titulo);
        }

        private static void RegistrarEnBitacora(
            Exception exception,
            string modulo,
            string accion)
        {
            try
            {
                BitacoraService bitacoraService =
                    new BitacoraService();

                bitacoraService.RegistrarError(
                    modulo,
                    accion,
                    exception,
                    "Se produjo un error no controlado.");
            }
            catch
            {
                // Una falla al registrar la bitacora nunca debe provocar
                // una segunda excepcion ni ocultar el error original.
            }
        }

        private static void MostrarMensajeSeguro(
            string mensaje,
            string titulo)
        {
            lock (Sincronizacion)
            {
                if (_mostrandoError)
                {
                    return;
                }

                _mostrandoError = true;
            }

            try
            {
                MessageBox.Show(
                    mensaje +
                    Environment.NewLine +
                    Environment.NewLine +
                    Tr.T("The technical detail was recorded in the audit log."),
                    titulo,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                lock (Sincronizacion)
                {
                    _mostrandoError = false;
                }
            }
        }
    }
}
