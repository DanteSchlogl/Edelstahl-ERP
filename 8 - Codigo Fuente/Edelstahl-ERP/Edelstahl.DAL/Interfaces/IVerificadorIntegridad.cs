using Edelstahl.Domain.Integrity;

namespace Edelstahl.DAL.Interfaces
{
    /// <summary>
    /// Comprueba (y, a pedido de un administrador, recalcula) los dígitos
    /// verificadores de las tablas protegidas.
    /// </summary>
    public interface IVerificadorIntegridad
    {
        /// <summary>Revisa cada registro (DVH) y el conjunto de la tabla (DVV). Solo lee.</summary>
        ResultadoIntegridad Verificar();

        /// <summary>Vuelve a calcular los dígitos con los datos actuales; devuelve cuántos registros.</summary>
        int Recalcular();
    }
}
