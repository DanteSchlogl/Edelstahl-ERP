using System;
using System.Data.Entity;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.Services.Configuration;

namespace Edelstahl.Test
{
    internal static class Program
    {
        /// <summary>
        /// Uso: Edelstahl.Test.exe [cifrado] [demo] [sql]
        /// Sin argumentos ejecuta el cifrado y el escenario en modo demostración.
        /// </summary>
        [STAThread]
        private static int Main(string[] args)
        {
            Database.SetInitializer<EdelstahlNegocioContext>(null);

            bool cifrado = args.Length == 0 || Array.IndexOf(args, "cifrado") >= 0;
            bool demo = args.Length == 0 || Array.IndexOf(args, "demo") >= 0;
            bool sql = Array.IndexOf(args, "sql") >= 0;
            bool pantallas = Array.IndexOf(args, "pantallas") >= 0;
            bool preparacion = Array.IndexOf(args, "prep") >= 0;
            bool pantallasSql = Array.IndexOf(args, "pantallassql") >= 0;

            try
            {
                if (cifrado)
                {
                    PruebaCifrado.Ejecutar();
                }

                if (demo)
                {
                    PruebaEscenarioCompleto.Ejecutar(ExecutionMode.Demo);
                    PruebaPreparacion.Ejecutar(ExecutionMode.Demo);
                }

                if (pantallas)
                {
                    PruebaPantallas.GeoErrata();
                }

                if (pantallas)
                {
                    PruebaPantallas.Ejecutar(ExecutionMode.Demo);
                }

                if (Array.IndexOf(args, "despacho") >= 0)
                {
                    PruebaPantallas.DiagnosticoDespacho();
                    return 0;
                }

                if (Array.IndexOf(args, "diag") >= 0)
                {
                    PruebaPantallas.Diagnostico();
                    return 0;
                }

                int iMat = Array.IndexOf(args, "materiales");
                if (iMat >= 0 && iMat + 1 < args.Length)
                {
                    PruebaPantallas.CapturarMateriales(args[iMat + 1]);
                    return Verificador.Resumen();
                }

                int iPdf = Array.IndexOf(args, "pdf");
                if (iPdf >= 0 && iPdf + 1 < args.Length)
                {
                    PruebaPantallas.CapturarPdf(args[iPdf + 1]);
                    return Verificador.Resumen();
                }

                int iVer = Array.IndexOf(args, "verificar");
                if (iVer >= 0 && iVer + 1 < args.Length)
                {
                    PruebaPantallas.CapturarVerificacion(args[iVer + 1]);
                    return Verificador.Resumen();
                }

                int iFa = Array.IndexOf(args, "faltantes");
                if (iFa >= 0 && iFa + 1 < args.Length)
                {
                    PruebaPantallas.CapturarFaltantes(args[iFa + 1]);
                    return Verificador.Resumen();
                }

                int iLogo = Array.IndexOf(args, "logo");
                if (iLogo >= 0 && iLogo + 1 < args.Length)
                {
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(args[iLogo + 1]));
                    Edelstahl.WinApp.Ui.Marca.Emblema(256).Save(args[iLogo + 1], System.Drawing.Imaging.ImageFormat.Png);
                    Verificador.Verificar(System.IO.File.Exists(args[iLogo + 1]), "Se genero el logo");
                    return Verificador.Resumen();
                }

                int iEd = Array.IndexOf(args, "editor");
                if (iEd >= 0 && iEd + 1 < args.Length)
                {
                    PruebaPantallas.CapturarEditorLineas(args[iEd + 1]);
                    return Verificador.Resumen();
                }

                int iMapa = Array.IndexOf(args, "mapa");
                if (iMapa >= 0 && iMapa + 1 < args.Length)
                {
                    PruebaPantallas.CapturarMapaDemo(args[iMapa + 1]);
                    return Verificador.Resumen();
                }

                int iCap = Array.IndexOf(args, "capturas");
                if (iCap >= 0 && iCap + 1 < args.Length)
                {
                    PruebaPantallas.Capturar(args[iCap + 1]);
                    return Verificador.Resumen();
                }

                if (pantallasSql)
                {
                    PruebaPantallas.Ejecutar(ExecutionMode.SqlServer);
                }

                if (sql)
                {
                    PruebaEscenarioCompleto.Ejecutar(ExecutionMode.SqlServer);
                    PruebaPreparacion.Ejecutar(ExecutionMode.SqlServer);
                }
            }
            catch (Exception ex)
            {
                Verificador.Verificar(false, "Error no controlado: " + ex);
            }

            return Verificador.Resumen();
        }
    }
}
