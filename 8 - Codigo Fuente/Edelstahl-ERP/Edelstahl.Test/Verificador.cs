using System;
using System.Collections.Generic;

namespace Edelstahl.Test
{
    /// <summary>
    /// Registro simple de comprobaciones para las pruebas de consola.
    /// </summary>
    public static class Verificador
    {
        private static int _correctas;
        private static readonly List<string> Fallas = new List<string>();

        public static int Correctas
        {
            get { return _correctas; }
        }

        public static IReadOnlyList<string> ListaDeFallas
        {
            get { return Fallas; }
        }

        public static void Seccion(string titulo)
        {
            Console.WriteLine();
            Console.WriteLine("== " + titulo);
        }

        public static void Verificar(bool condicion, string descripcion)
        {
            if (condicion)
            {
                _correctas++;
                Console.WriteLine("  bien   " + descripcion);
            }
            else
            {
                Fallas.Add(descripcion);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  FALLA  " + descripcion);
                Console.ResetColor();
            }
        }

        public static void Igual<T>(T esperado, T real, string descripcion)
        {
            bool igual = EqualityComparer<T>.Default.Equals(esperado, real);
            Verificar(igual, igual ? descripcion : descripcion + " (esperado " + esperado + ", real " + real + ")");
        }

        /// <summary>
        /// Comprueba que la acción falle con una excepción del tipo indicado.
        /// </summary>
        public static void Falla<TExcepcion>(Action accion, string descripcion) where TExcepcion : Exception
        {
            try
            {
                accion();
                Verificar(false, descripcion + " (no se produjo ningún error)");
            }
            catch (TExcepcion)
            {
                Verificar(true, descripcion);
            }
            catch (Exception ex)
            {
                Verificar(false, descripcion + " (error distinto: " + ex.GetType().Name + ": " + ex.Message + ")");
            }
        }

        public static int Resumen()
        {
            Console.WriteLine();
            Console.WriteLine("Resultado: " + _correctas + " bien, " + Fallas.Count + " con falla");

            foreach (string falla in Fallas)
            {
                Console.WriteLine("  - " + falla);
            }

            return Fallas.Count == 0 ? 0 : 1;
        }
    }
}
