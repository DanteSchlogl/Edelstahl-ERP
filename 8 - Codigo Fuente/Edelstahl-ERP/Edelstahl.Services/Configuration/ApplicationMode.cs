using System;

namespace Edelstahl.Services.Configuration
{
    /// <summary>
    /// Mantiene el modo de ejecución seleccionado
    /// durante la sesión actual de Edelstahl ERP.
    /// </summary>
    public static class ApplicationMode
    {
        private static ExecutionMode _currentMode =
            ExecutionMode.None;

        public static ExecutionMode CurrentMode
        {
            get
            {
                return _currentMode;
            }
        }

        public static bool UsesSqlServer
        {
            get
            {
                return _currentMode ==
                    ExecutionMode.SqlServer;
            }
        }

        public static bool IsDemo
        {
            get
            {
                return _currentMode ==
                    ExecutionMode.Demo;
            }
        }

        public static bool IsSelected
        {
            get
            {
                return _currentMode !=
                    ExecutionMode.None;
            }
        }

        public static void Select(
            ExecutionMode mode)
        {
            if (mode == ExecutionMode.None)
            {
                throw new ArgumentException(
                    "Debe seleccionar un modo de ejecución válido.",
                    nameof(mode));
            }

            _currentMode =
                mode;
        }

        public static void Reset()
        {
            _currentMode =
                ExecutionMode.None;
        }
    }
}