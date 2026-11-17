using System.Data;

namespace Edelstahl.Services.DataAccess.Tools
{
    /// <summary>Crea las bases de datos que faltan en el servidor SQL.</summary>
    public static class AdministradorDeBases
    {
        /// <summary>Crea la base de datos indicada si todavía no existe.</summary>
        public static void AsegurarBase(string nombre)
        {
            int existe = SqlHelper.EjecutarEscalar<int>(
                BaseDeDatos.Master,
                "SELECT COUNT(*) FROM sys.databases WHERE name = @nombre",
                SqlHelper.Parametro("@nombre", SqlDbType.NVarChar, 128, nombre));

            if (existe > 0)
            {
                return;
            }

            string seguro = nombre.Replace("]", "]]");

            SqlHelper.EjecutarNoQuery(BaseDeDatos.Master, 120, "CREATE DATABASE [" + seguro + "]");
        }
    }
}