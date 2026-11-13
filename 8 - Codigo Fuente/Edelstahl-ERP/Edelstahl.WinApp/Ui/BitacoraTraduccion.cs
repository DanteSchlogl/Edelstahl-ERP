using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Edelstahl.WinApp.Ui
{
    /// <summary>
    /// La bitácora guarda cada evento una sola vez, en el texto original del sistema (español), para que
    /// el registro de auditoría no dependa del idioma de quien lo generó. Esta clase lo traduce al idioma
    /// activo en el momento de mostrarlo: módulo, acción, entidad, resultado, tipo de evento y descripción.
    /// Lo que no reconoce (nombres de personas, números, códigos) se muestra tal como está guardado.
    /// </summary>
    public static class BitacoraTraduccion
    {
        private sealed class Regla
        {
            public readonly Regex Patron;
            public readonly string Clave;
            public readonly string Mapeos;

            /// <param name="patron">Expresión regular del texto en español que se guardó.</param>
            /// <param name="clave">Texto en inglés (clave de traducción) con {0}, {1}... para los datos capturados.</param>
            /// <param name="mapeos">
            /// Una letra por cada grupo del patrón: r = texto tal cual, p = palabra conocida (estado, resultado,
            /// tipo de movimiento...), d = descripción anidada.
            /// </param>
            public Regla(string patron, string clave, string mapeos)
            {
                Patron = new Regex("^" + patron + "$", RegexOptions.Singleline | RegexOptions.CultureInvariant);
                Clave = clave;
                Mapeos = mapeos;
            }
        }

        private static readonly Dictionary<string, string> Modulos = Mapa(
            "Seguridad", "Security", "Producción", "Production", "Ventas", "Sales", "Logística", "Logistics",
            "Inventario", "Inventory", "Finanzas", "Finance", "Catálogo", "Catalog", "Respaldo", "Backup",
            "Compras", "Purchasing", "Inicio", "Startup", "Integridad", "Integrity", "Interfaz", "Interface",
            "Reportes", "Reports", "Sistema", "System", "Soporte", "Support");

        private static readonly Dictionary<string, string> Acciones = Mapa(
            "Inicio de sesion", "Sign in", "Cierre de sesion", "Sign out", "Alta de usuario", "User created",
            "Modificar usuario", "User modified", "Cambio de rol", "Role change", "Activar rol", "Role activated",
            "Desactivar rol", "Role deactivated", "Restablecer contrasena", "Password reset",
            "Cambio de contrasena", "Password change", "Definicion de pregunta de seguridad", "Security question defined",
            "Cambio de pregunta de seguridad", "Security question change", "Recuperacion de contrasena", "Password recovery",
            "Desbloqueo de usuario", "User unlocked", "Activacion de usuario", "User activated",
            "Desactivacion de usuario", "User deactivated", "Bloqueo de usuario", "User locked",
            "AccesoDenegado", "Access denied", "Pedido de ayuda", "Help request", "Pedido de ayuda atendido", "Help request handled",
            "Digitos verificadores recalculados", "Check digits recalculated", "Alteracion detectada", "Alteration detected",
            "Acceso a la recuperacion", "Recovery access", "Acceso a la recuperacion denegado", "Recovery access denied",
            "Crear respaldo completo", "Create full backup", "Restaurar respaldo", "Restore backup",
            "Copia previa a restaurar", "Safety copy before restore", "Respaldo automatico", "Automatic backup",
            "Inicializacion de la aplicacion", "Application startup", "Operacion", "Operation",
            "CargaDatosEjemplo", "Sample data load",
            "GuardarCategoria", "Save category", "GuardarProducto", "Save product", "ActivarProducto", "Activate product",
            "DesactivarProducto", "Deactivate product", "AumentoMasivoPrecios", "Mass price increase",
            "ImportarProductos", "Import products",
            "GuardarProveedor", "Save supplier", "EmitirOrdenCompra", "Issue purchase order",
            "CrearOrdenCompra", "Create purchase order", "CancelarOrdenCompra", "Cancel purchase order",
            "RegistrarRecepcion", "Register receipt",
            "RegistrarDevolucion", "Register return", "RecibirDevolucion", "Receive return",
            "CancelarDevolucion", "Cancel return", "ResolverDevolucion", "Resolve return",
            "GuardarVehiculo", "Save vehicle", "GuardarChofer", "Save driver", "GenerarRemito", "Generate delivery note",
            "CancelarRemito", "Cancel delivery note", "ReprogramarRemito", "Reschedule delivery note",
            "CrearHojaRuta", "Create route sheet", "IniciarHojaRuta", "Start route sheet",
            "RegistrarEntrega", "Register delivery", "VerificarCargaRemito", "Verify delivery note load",
            "Preparar pedido", "Prepare order",
            "EmitirFactura", "Issue invoice", "EmitirNotaCredito", "Issue credit note",
            "FormaDePagoPrevista", "Planned payment method", "RegistrarCobro", "Register collection",
            "RegistrarPagoProveedor", "Register supplier payment",
            "ImportarClientes", "Import customers", "CancelarPedido", "Cancel sales order",
            "CrearPresupuesto", "Create quotation", "CambiarEstadoPresupuesto", "Change quotation status",
            "ExtenderVigencia", "Extend validity", "AutorizarCredito", "Authorize credit", "RevocarCredito", "Revoke credit",
            "GuardarDeposito", "Save warehouse", "GuardarUbicacion", "Save warehouse location",
            "RegistrarMovimiento", "Register stock movement", "Transferir", "Transfer stock",
            "LiberarReserva", "Release reservation", "Ingreso de stock", "Stock entry",
            "GenerarOrdenFabricacion", "Generate manufacturing order", "IniciarProduccion", "Start production",
            "RegistrarAvance", "Register progress", "RegistrarConsumo", "Register consumption",
            "EnviarACalidad", "Send to quality control", "ControlCalidad", "Quality control",
            "CancelarOrden", "Cancel manufacturing order", "Exportar", "Export",
            "RegistrarCliente", "Register customer", "ModificarCliente", "Modify customer", "EliminarCliente", "Delete customer",
            "ConfirmarPedido", "Confirm sales order", "ReservarMateriales", "Reserve materials",
            "ImprimirFactura", "Print invoice", "ImprimirNotaCredito", "Print credit note", "ImprimirRemito", "Print delivery note",
            "ImprimirOrdenCompra", "Print purchase order", "ImprimirPresupuesto", "Print quotation", "ImprimirComprobantePedido", "Print order receipt",
            "Permiso agregado", "Permission granted", "Permiso quitado", "Permission revoked",
            "Permiso restablecido", "Permission restored", "Permisos restablecidos", "Permissions restored");

        private static readonly Dictionary<string, string> Entidades = Mapa(
            "Usuario", "User", "Rol", "Role", "Permiso", "Permission", "Pedido", "Sales order", "Presupuesto", "Quotation",
            "Cliente", "Customer", "Clientes", "Customers", "Producto", "Product", "CategoriaProducto", "Product category",
            "Proveedor", "Supplier", "OrdenCompra", "Purchase order", "RecepcionCompra", "Purchase receipt",
            "Devolucion", "Return", "RemitoEntrega", "Delivery note", "HojaRuta", "Route sheet",
            "DetalleHojaRuta", "Route sheet stop", "Vehiculo", "Vehicle", "Chofer", "Driver", "Factura", "Invoice",
            "FacturaDto", "Invoice", "NotaCredito", "Credit note", "CobroCliente", "Customer collection",
            "PagoProveedor", "Supplier payment", "MovimientoStock", "Stock movement", "ReservaStock", "Stock reservation",
            "Deposito", "Warehouse", "UbicacionDeposito", "Warehouse location", "OrdenFabricacion", "Manufacturing order",
            "Bases de datos", "Databases", "VentaDto", "Sale", "VentaProductoDto", "Product sale");

        private static readonly Dictionary<string, string> Resultados = Mapa(
            "Correcto", "Success", "Fallido", "Failed", "Advertencia", "Warning");

        private static readonly Dictionary<string, string> Eventos = Mapa(
            "Auditoria", "Audit", "Seguridad", "Security", "Error", "Error", "Informacion", "Information");

        /// <summary>Valores que aparecen dentro de las descripciones (estados, resultados, tipos de movimiento...).</summary>
        private static readonly Dictionary<string, string> Palabras = Mapa(
            "Borrador", "Draft", "Enviado", "Sent", "Aceptado", "Accepted", "Vencido", "Expired", "Rechazado", "Rejected",
            "ConvertidoEnPedido", "Converted to order",
            "EntregadoConforme", "Delivered in good order", "EntregaParcial", "Partial delivery", "Ausente", "Recipient absent",
            "DomicilioIncorrecto", "Wrong address", "Aprobado", "Approved", "Reproceso", "Rework",
            "RecepcionCompra", "Purchase receipt", "ProductoTerminado", "Finished product", "DevolucionCliente", "Customer return",
            "AjustePositivo", "Positive adjustment", "TransferenciaEntrada", "Transfer in", "ConsumoProduccion", "Production consumption",
            "MermaProduccion", "Production waste", "Despacho", "Dispatch", "AjusteNegativo", "Negative adjustment",
            "TransferenciaSalida", "Transfer out", "ReservaPedido", "Order reservation", "LiberacionReserva", "Reservation release",
            "FacturaA", "Invoice A", "FacturaB", "Invoice B", "Factura A", "Invoice A", "Factura B", "Invoice B",
            "Manual", "Manual", "Automatico", "Automatic", "sano", "verified healthy", "sin garantia de integridad", "no integrity guarantee",
            "datos sanos", "healthy data", "los datos NO pasaron la verificacion de integridad", "the data did NOT pass the integrity check",
            "inicio del sistema", "system startup",
            "Tarjeta", "Card", "Efectivo", "Cash", "Transferencia bancaria", "Bank transfer", "Cheque", "Check",
            "Contra entrega", "Cash on delivery", "Fabricación", "Manufacturing");

        private static readonly Regla[] Reglas = new Regla[]
        {
            new Regla("Inicio de sesion correcto\\.", "Sign-in successful.", ""),
            new Regla("Se intento iniciar sesion sin completar el usuario o la contrasena\\.", "A sign-in was attempted without entering the user name or the password.", ""),
            new Regla("Intento de acceso con un usuario inexistente\\.", "Access attempt with a user that does not exist.", ""),
            new Regla("Intento de acceso con un usuario inactivo\\.", "Access attempt with an inactive user.", ""),
            new Regla("Intento de acceso con un usuario bloqueado hasta (.+)\\.", "Access attempt with a user locked until {0}.", "r"),
            new Regla("El usuario fue bloqueado durante 15 minutos al alcanzar el maximo de intentos fallidos\\.", "The user was locked for 15 minutes after reaching the maximum number of failed attempts.", ""),
            new Regla("Contrasena incorrecta\\. Intentos restantes: (\\d+)\\.", "Incorrect password. Remaining attempts: {0}.", "r"),
            new Regla("El usuario cerro su sesion\\.", "The user signed out.", ""),
            new Regla("Se registro el usuario (.+) con el rol (.+)\\.", "User {0} was registered with the role {1}.", "rr"),
            new Regla("No fue posible registrar el usuario\\.", "The user could not be registered.", ""),
            new Regla("La contrasena fue modificada correctamente\\.", "The password was changed successfully.", ""),
            new Regla("La contrasena actual ingresada es incorrecta\\.", "The current password entered is incorrect.", ""),
            new Regla("El usuario cambio su pregunta de seguridad\\.", "The user changed their security question.", ""),
            new Regla("Se definio la pregunta de seguridad de (.+)\\.", "The security question of {0} was defined.", "r"),
            new Regla("Intento de recuperacion de un usuario inexistente, inactivo o sin pregunta de seguridad\\.", "Recovery attempt for a user that does not exist, is inactive or has no security question.", ""),
            new Regla("Respuesta incorrecta a la pregunta de seguridad\\.", "Wrong answer to the security question.", ""),
            new Regla("La contrasena se restablecio con la pregunta de seguridad\\.", "The password was reset with the security question.", ""),
            new Regla("El usuario (.+) fue desbloqueado\\.", "User {0} was unlocked.", "r"),
            new Regla("El usuario (.+) fue activado\\.", "User {0} was activated.", "r"),
            new Regla("El usuario (.+) fue desactivado\\.", "User {0} was deactivated.", "r"),
            new Regla("Se agregó el permiso (.+) a (.+)", "The permission {0} was granted to {1}", "nr"),
            new Regla("Se quitó el permiso (.+) a (.+)", "The permission {0} was revoked from {1}", "nr"),
            new Regla("Se restableció el permiso (.+) de (.+) al valor de su rol", "The permission {0} of {1} was set back to the value of its role", "nr"),
            new Regla("Se restablecieron los permisos de (.+) a los de su rol\\.", "The permissions of {0} were set back to those of their role.", "r"),
            new Regla("Se restableció la contraseña de (.+)", "The password of {0} was reset.", "r"),
            new Regla("Se modificaron los datos de (.+)", "The data of {0} was modified.", "r"),
            new Regla("(.+) pasó al rol (.+)", "{0} changed to the role {1}", "rr"),
            new Regla("Permiso requerido: (.+)", "Required permission: {0}", "r"),
            new Regla("El usuario no tiene permiso para administrar respaldos y no puede usar la recuperacion de datos\\.", "The user does not have permission to manage backups and cannot use the data recovery.", ""),
            new Regla("El usuario se identifico en la pantalla de recuperacion de datos\\.", "The user identified themselves on the data recovery screen.", ""),
            new Regla("Se detecto una alteracion de los datos de la tabla Clientes el (\\d{2}/\\d{2}/\\d{4} \\d{2}:\\d{2}:\\d{2})(?: \\((.+?)\\))?: (\\d+) registro\\(s\\) con el DVH incorrecto( y el DVV de la tabla no coincide)?\\. El acceso quedo bloqueado hasta restaurar un respaldo sano o recalcular los digitos\\.",
                "An alteration of the data of the Customers table was detected on {0}{1}: {2} record(s) with an incorrect DVH{3}. Access was blocked until a healthy backup is restored or the digits are recalculated.", "rrrr"),
            new Regla("El administrador recalculo los digitos verificadores \\(DVH y DVV\\) de la tabla Clientes con los datos actuales\\. Registros recalculados: (\\d+)\\.", "The administrator recalculated the check digits (DVH and DVV) of the Customers table with the current data. Records recalculated: {0}.", "r"),
            new Regla("Se crearon los respaldos de (.+) y (.+) \\((.+?), (.+)\\) en (.+)\\.", "The backups of {0} and {1} were created ({2}, {3}) in {4}.", "rrppr"),
            new Regla("Se crearon correctamente los respaldos de (.+) y (.+) en (.+)\\.", "The backups of {0} and {1} were created successfully in {2}.", "rrr"),
            new Regla("No fue posible completar el respaldo de las bases de datos\\.", "The backup of the databases could not be completed.", ""),
            new Regla("No fue posible crear el respaldo automatico\\.", "The automatic backup could not be created.", ""),
            new Regla("Se restauro la base (.+) desde el (.+)\\. Los digitos verificadores son correctos\\.", "The database {0} was restored from {1}. The check digits are correct.", "rd"),
            new Regla("Se restauro la base (.+) desde el (.+)\\. Atencion: los datos restaurados siguen sin pasar la verificacion de integridad\\.", "The database {0} was restored from {1}. Warning: the restored data still does not pass the integrity check.", "rd"),
            new Regla("No fue posible restaurar la base (.+) desde el (.+)\\.", "The database {0} could not be restored from {1}.", "rd"),
            new Regla("respaldo del (.+) \\((.+)\\)", "the backup of {0} ({1})", "rp"),
            new Regla("archivo (.+)", "the file {0}", "r"),
            new Regla("No se pudo guardar una copia del estado previo a la restauracion; se continua con la restauracion\\.", "A copy of the state before the restore could not be saved; the restore continues.", ""),
            new Regla("Se produjo un error no controlado\\.", "An unhandled error occurred.", ""),
            new Regla("Se produjo un error en la operación\\.", "An error occurred in the operation.", ""),
            new Regla("Pedido de ayuda de (.+) \\((.+)\\): (.+)", "Help request from {0} ({1}): {2}", "rrd"),
            new Regla("El administrador atendió el pedido de ayuda de (.+?): (.+)\\.", "The administrator handled the help request from {0}: {1}.", "rd"),
            new Regla("Producto guardado: (.+)", "Product saved: {0}", "r"),
            new Regla("Categoría guardada: (.+)", "Category saved: {0}", "r"),
            new Regla("Ajuste de ([\\d.,\\-]+)% sobre (\\d+) productos\\.(?: (.*))?", "Adjustment of {0}% on {1} products. {2}", "rrr"),
            new Regla("(\\d+) registros importados", "{0} records imported", "r"),
            new Regla("(\\d+) importados, (\\d+) omitidos", "{0} imported, {1} skipped", "rr"),
            new Regla("Exportacion de (\\d+) registros a (.+)", "Export of {0} records to {1}", "rr"),
            new Regla("(.+) del pedido (.+)", "{0} of order {1}", "rr"),
            new Regla("(.+) con (\\d+) remitos", "{0} with {1} delivery notes", "rr"),
            new Regla("Pedido (.+): reservados (\\d+), ingresados (\\d+), en producción (\\d+)", "Order {0}: {1} reserved, {2} added, {3} in production", "rrrr"),
            new Regla("Ingreso de ([\\d.,]+) de (.+?): (.+)", "Stock entry of {0} of {1}: {2}", "rrd"),
            new Regla("Ingreso de stock para el presupuesto (.+)", "Stock entry for the quotation {0}", "r"),
            new Regla("Ingreso de stock", "Stock entry", ""),
            new Regla("Ingreso de materiales para la orden de fabricación", "Entry of materials for the manufacturing order", ""),
            new Regla("Transferencia de (.+)", "Transfer of {0}", "r"),
            new Regla("(.+) para el pedido (.+)", "{0} for order {1}", "rr"),
            new Regla("(.+): carga verificada \\((\\d+) líneas\\)", "{0}: load verified ({1} lines)", "rr"),
            new Regla("(.+): (Aprobado|Rechazado|Reproceso)", "{0}: {1}", "rp"),
            new Regla("(.+): consumo (.+), merma (.+)", "{0}: consumption {1}, waste {2}", "rrr"),
            new Regla("(.+): (Tarjeta|Efectivo|Transferencia bancaria|Cheque|Contra entrega)", "{0}: {1}", "rp"),
            new Regla("(.+): (.+) ([\\d.,]+)%", "{0}: {1} {2}%", "rpr"),
            new Regla("(.+) para (.+)", "{0} for {1}", "rr"),
            new Regla("(.+) -> (.+)", "{0} -> {1}", "rp"),
            new Regla("(.+) \\((.+)\\) por (.+)", "{0} ({1}) for {2}", "rpr"),
            new Regla("(.+) por (.+)", "{0} for {1}", "rr"),
            new Regla("(.+) x ([\\d.,]+)", "{0} x {1}", "pr")
        };

        public static string Modulo(string valor) { return Buscar(Modulos, valor); }

        public static string Accion(string valor) { return Buscar(Acciones, valor); }

        public static string Entidad(string valor) { return Buscar(Entidades, valor); }

        public static string Resultado(string valor) { return Buscar(Resultados, valor); }

        public static string TipoEvento(string valor) { return Buscar(Eventos, valor); }

        /// <summary>Traduce la descripción de un evento; si no reconoce el texto lo devuelve sin cambios.</summary>
        public static string Descripcion(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                return descripcion;
            }

            string texto = descripcion.Trim();

            foreach (Regla regla in Reglas)
            {
                Match m = regla.Patron.Match(texto);

                if (!m.Success)
                {
                    continue;
                }

                object[] argumentos = new object[Math.Max(m.Groups.Count - 1, 0)];

                for (int i = 1; i < m.Groups.Count; i++)
                {
                    string valor = m.Groups[i].Value;
                    char modo = i - 1 < regla.Mapeos.Length ? regla.Mapeos[i - 1] : 'r';

                    if (modo == 'p')
                    {
                        valor = Buscar(Palabras, valor);
                    }
                    else if (modo == 'd')
                    {
                        valor = Descripcion(valor);
                    }
                    else if (modo == 'n')
                    {
                        valor = Tr.T(Nombres.Permiso(valor));
                    }

                    argumentos[i - 1] = valor;
                }

                if (regla.Clave.StartsWith("An alteration", StringComparison.Ordinal))
                {
                    argumentos[1] = m.Groups[2].Success ? " (" + Buscar(Palabras, m.Groups[2].Value) + ")" : string.Empty;
                    argumentos[3] = m.Groups[4].Success ? " " + Tr.T("and the table DVV does not match") : string.Empty;
                }

                return Tr.T(regla.Clave, argumentos);
            }

            // Un valor suelto (por ejemplo el resultado de una entrega: "Ausente").
            string suelto = Buscar(Palabras, texto);

            return ReferenceEquals(suelto, texto) || suelto == texto ? descripcion : suelto;
        }

        private static string Buscar(Dictionary<string, string> mapa, string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return valor;
            }

            string clave;

            return mapa.TryGetValue(Normalizar(valor), out clave) ? Tr.T(clave) : valor;
        }

        private static Dictionary<string, string> Mapa(params string[] pares)
        {
            Dictionary<string, string> mapa = new Dictionary<string, string>();

            for (int i = 0; i + 1 < pares.Length; i += 2)
            {
                mapa[Normalizar(pares[i])] = pares[i + 1];
            }

            return mapa;
        }

        /// <summary>Sin tildes y en minúsculas, para que "Inicio de sesión" y "Inicio de sesion" sean lo mismo.</summary>
        private static string Normalizar(string texto)
        {
            string descompuesto = texto.Trim().Normalize(NormalizationForm.FormD);
            StringBuilder resultado = new StringBuilder(descompuesto.Length);

            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    resultado.Append(c);
                }
            }

            return resultado.ToString().ToLowerInvariant();
        }
    }
}
