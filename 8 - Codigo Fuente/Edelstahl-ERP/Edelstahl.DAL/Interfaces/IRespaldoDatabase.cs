using System;
using Edelstahl.Domain.Security;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Operaciones de respaldo sobre las bases de datos: crear el archivo,
    /// comprobarlo y restaurarlo. La capa de negocio solo conoce esta
    /// interfaz; la clase que la implementa es interna de la DAL y la
    /// entrega la Factory.
    /// </summary>
    public interface IRespaldoDatabase
    {
        /// <summary>Nombre de la base de negocio, la que se restaura ante una alteración.</summary>
        string BaseNegocio { get; }

        /// <summary>Nombre de la base de servicios (usuarios, bitácora y respaldos).</summary>
        string BaseServicios { get; }

        /// <summary>Carpeta de respaldos propia de SQL Server.</summary>
        string ObtenerCarpetaPredeterminada();

        /// <summary>Hace un respaldo completo y comprueba el archivo resultante.</summary>
        ResultadoRespaldo CrearRespaldo(string baseDatos, string carpetaDestino, DateTime fechaHora);

        /// <summary>Comprueba que el archivo sea un respaldo completo y legible.</summary>
        void VerificarArchivo(string ruta);

        /// <summary>Nombre de la base de la que se hizo el respaldo.</summary>
        string ObtenerBaseDelArchivo(string ruta);

        /// <summary>Reemplaza la base por el contenido del respaldo.</summary>
        void Restaurar(string baseDatos, string ruta);

        /// <summary>Copia un archivo con los permisos del usuario de Windows.</summary>
        string CopiarArchivo(string rutaOrigen, string carpetaDestino);

        /// <summary>Indica si el error se debe a que SQL Server no pudo abrir el archivo.</summary>
        bool EsErrorDeArchivo(Exception ex);
    }
}
