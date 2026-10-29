using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Edelstahl.Domain.Comercial
{
    /// <summary>
    /// Representa los posibles estados comerciales
    /// de un presupuesto.
    /// </summary>
    public enum EstadoPresupuesto
    {
        Borrador = 1,
        Enviado = 2,
        Aceptado = 3,
        Vencido = 4,
        Rechazado = 5,
        ConvertidoEnPedido = 6
    }
}
