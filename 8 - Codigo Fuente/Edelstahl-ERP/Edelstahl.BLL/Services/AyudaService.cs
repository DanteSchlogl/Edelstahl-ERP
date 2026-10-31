using System;
using System.Collections.Generic;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Pedidos de ayuda al administrador: el usuario elige un tema segun su perfil
    /// y el pedido queda registrado en la bitacora para que el administrador lo vea.
    /// </summary>
    public class AyudaService
    {
        /// <summary>Temas de ayuda que se ofrecen segun el perfil (texto en ingles; la pantalla los traduce).</summary>
        public static List<string> TemasPara(string rol)
        {
            List<string> temas = new List<string>();

            switch (rol)
            {
                case NombresRol.Vendedor:
                    temas.Add("I cannot create or confirm a quotation or an order");
                    temas.Add("A customer or a product is missing or wrong");
                    temas.Add("I cannot issue an invoice or register a collection");
                    break;
                case NombresRol.Produccion:
                    temas.Add("I cannot start or finish a manufacturing order");
                    temas.Add("Materials are missing for an order");
                    temas.Add("A quality check problem");
                    break;
                case NombresRol.Deposito:
                    temas.Add("The stock does not match");
                    temas.Add("I cannot receive a purchase or register a stock movement");
                    break;
                case NombresRol.Compras:
                    temas.Add("I cannot create a purchase order");
                    temas.Add("A supplier is missing or wrong");
                    break;
                case NombresRol.Logistica:
                    temas.Add("I cannot create a delivery note or a route");
                    temas.Add("A return needs approval");
                    break;
                case NombresRol.Supervisor:
                    temas.Add("I need a report or an access I do not have");
                    temas.Add("A number in a report looks wrong");
                    break;
                default:
                    temas.Add("I need a permission or a role change");
                    break;
            }

            temas.Add("I cannot sign in or my user is locked");
            temas.Add("Other");

            return temas;
        }

        /// <summary>Deja el pedido en la bitacora con el usuario, su perfil y el tema elegido.</summary>
        public void RegistrarPedido(string tema, string detalle)
        {
            if (!SesionActual.EstaIniciada)
            {
                throw Mensajes.Regla("You must sign in first.");
            }

            if (string.IsNullOrWhiteSpace(tema))
            {
                throw Mensajes.Regla("Choose a topic.");
            }

            string descripcion = "Pedido de ayuda de " + SesionActual.NombreUsuario +
                " (" + SesionActual.NombreRol + "): " + tema.Trim();

            if (!string.IsNullOrWhiteSpace(detalle))
            {
                descripcion += ". " + detalle.Trim();
            }

            new BitacoraService().Registrar(
                "Soporte", "Pedido de ayuda", "Usuario", SesionActual.Usuario.Id,
                descripcion, "Pendiente", "Soporte", string.Empty, SesionActual.NombreUsuario);
        }
    }
}
