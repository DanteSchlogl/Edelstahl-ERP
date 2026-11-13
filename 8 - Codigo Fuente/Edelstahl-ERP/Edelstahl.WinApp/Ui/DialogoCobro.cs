using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Edelstahl.BLL.Services;
using Edelstahl.Domain.Finanzas;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// Registra un cobro de factura. Con efectivo no se pide banco ni referencia:
    /// esos datos se piden solo si el medio de pago los requiere (cheque, transferencia, tarjeta).
    /// </summary>
    public static class DialogoCobro
    {
        public static CobroCliente Registrar(IWin32Window padre, FinanzasService finanzas, Guid clienteId, Guid facturaId, decimal saldo)
        {
            List<MedioPago> medios = finanzas.ObtenerMediosPago(true);

            Respuesta paso1 = Dialogo.Pedir(padre, "Register collection",
                Campo.Combo("medio", "Payment method", medios.Select(m => new Opcion(m.Nombre, m.Id)).ToList()),
                Campo.Numero("importe", "Amount", saldo, 0.01m, 1000000000m));

            if (paso1 == null)
            {
                return null;
            }

            MedioPago medio = medios.First(m => m.Id == paso1.GuidDe("medio"));
            string referencia = string.Empty;
            string entidad = string.Empty;
            string obs = string.Empty;

            if (medio.RequiereReferencia)
            {
                Respuesta paso2 = Dialogo.Pedir(padre, "Payment details",
                    Campo.Texto("referencia", "Reference", "", true, 60),
                    Campo.Texto("entidad", "Bank", "", false, 60),
                    Campo.Texto("obs", "Notes", "", false, 200));

                if (paso2 == null)
                {
                    return null;
                }

                referencia = paso2.Texto("referencia");
                entidad = paso2.Texto("entidad");
                obs = paso2.Texto("obs");
            }

            return finanzas.RegistrarCobro(clienteId, facturaId, medio.Id, paso1.Numero("importe"), referencia, entidad, obs);
        }
    }
}
