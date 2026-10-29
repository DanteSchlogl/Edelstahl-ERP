using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Exceptions.Commercial;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Comercial;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Importación de clientes y productos desde archivos CSV
    /// (formato de exportación de tienda en línea, separados por punto y coma).
    /// </summary>
    public class ImportacionService
    {
        private const decimal TipoCambioReferencia = 1515m;
        private const int ProductosPorLote = 25;

        private readonly INegocioStore _almacen;

        public ImportacionService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public ImportacionService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Clientes
        // ---------------------------------------------------------------

        public ResultadoImportacion ImportarClientes(string ruta, IProgress<int> progreso)
        {
            Autorizacion.Exigir(Permiso.ClientesEditar);

            List<string[]> filas = LectorCsv.Leer(ruta, ';');

            if (filas.Count < 2)
            {
                throw Mensajes.Regla("The file has no data rows.");
            }

            string[] cab = filas[0];
            int iNombre = LectorCsv.Columna(cab, "Nombre y Apellido", "Razon social", "Nombre");

            if (iNombre < 0)
            {
                throw Mensajes.Regla("The customer name column was not found in the file.");
            }

            int iEmail = LectorCsv.Columna(cab, "E-mail", "Email", "Correo");
            int iTelefono = LectorCsv.Columna(cab, "Telefono");
            int iDireccion = LectorCsv.Columna(cab, "Direccion");
            int iNumero = LectorCsv.Columna(cab, "Numero");
            int iCiudad = LectorCsv.Columna(cab, "Ciudad");
            int iLocalidad = LectorCsv.Columna(cab, "Localidad");
            int iProvincia = LectorCsv.Columna(cab, "Provincia / Estado", "Provincia");
            int iPostal = LectorCsv.Columna(cab, "Codigo postal");
            int iTotal = LectorCsv.Columna(cab, "Total consumido (USD)");
            int iCuit = LectorCsv.Columna(cab, "CUIT");

            ClienteService servicio = new ClienteService();
            ResultadoImportacion resultado = new ResultadoImportacion { Total = filas.Count - 1 };

            for (int i = 1; i < filas.Count; i++)
            {
                string[] fila = filas[i];
                string nombre = LectorCsv.Valor(fila, iNombre);

                if (nombre.Length < 3)
                {
                    resultado.Omitidos++;
                    resultado.Errores.Add(Mensajes.T("Row {0}: the name is missing or too short.", i + 1));
                    Informar(progreso, i, filas.Count);
                    continue;
                }

                string email = LectorCsv.Valor(fila, iEmail);
                string cuit = iCuit >= 0 ? LectorCsv.Valor(fila, iCuit) : string.Empty;

                if (SoloDigitos(cuit).Length != 11)
                {
                    cuit = GenerarCuit(nombre + "|" + email);
                }

                string direccion = (LectorCsv.Valor(fila, iDireccion) + " " + LectorCsv.Valor(fila, iNumero)).Trim();

                // Sin direccion se deja vacio: asi Logistica sabe que falta cargarla.

                string localidad = LectorCsv.Valor(fila, iCiudad);

                if (localidad.Length == 0)
                {
                    localidad = LectorCsv.Valor(fila, iLocalidad);
                }

                decimal totalUsd = 0m;
                decimal.TryParse(LectorCsv.Valor(fila, iTotal), NumberStyles.Any, CultureInfo.InvariantCulture, out totalUsd);

                Cliente cliente = new Cliente
                {
                    CUIT = cuit,
                    Tipo = cuit.StartsWith("30") || cuit.StartsWith("33") || cuit.StartsWith("34") ? TipoPersona.Empresa : TipoPersona.Particular,
                    RazonSocial = nombre,
                    Email = EmailValido(email) ? email : string.Empty,
                    Telefono = SoloDigitos(LectorCsv.Valor(fila, iTelefono)),
                    DireccionFacturacion = direccion,
                    DireccionEntrega = direccion,
                    Localidad = localidad,
                    Provincia = LectorCsv.Valor(fila, iProvincia),
                    CodigoPostal = LectorCsv.Valor(fila, iPostal),
                    LimiteCredito = LimiteSegunConsumo(totalUsd)
                };

                try
                {
                    servicio.Registrar(cliente);
                    resultado.Importados++;
                }
                catch (ClienteDuplicadoException)
                {
                    resultado.Omitidos++;
                }
                catch (Exception ex)
                {
                    resultado.Omitidos++;
                    resultado.Errores.Add(Mensajes.T("Row {0}: {1}", i + 1, ex.Message));
                }

                Informar(progreso, i, filas.Count);
            }

            new BitacoraService().RegistrarAuditoria(
                "Ventas", "ImportarClientes", "Cliente", null,
                resultado.Importados + " importados, " + resultado.Omitidos + " omitidos");

            return resultado;
        }

        private static decimal LimiteSegunConsumo(decimal totalUsd)
        {
            decimal bruto = totalUsd * TipoCambioReferencia * 5m;
            decimal redondeado = Math.Round(bruto / 10000m, 0) * 10000m;
            return Math.Min(5000000m, Math.Max(300000m, redondeado));
        }

        private static bool EmailValido(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            try
            {
                MailAddress direccion = new MailAddress(email.Trim());
                return string.Equals(direccion.Address, email.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string SoloDigitos(string valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Genera un CUIT de formato válido (dígito verificador correcto) y
        /// estable para una misma semilla. Se usa cuando el archivo no lo trae.
        /// </summary>
        public static string GenerarCuit(string semilla)
        {
            uint hash = 2166136261;

            foreach (char c in semilla ?? string.Empty)
            {
                hash ^= c;
                hash *= 16777619;
            }

            bool empresa = Regex.IsMatch(semilla ?? string.Empty,
                @"\b(s\.?r\.?l|s\.?a|sociedad|s\.?a\.?s)\b", RegexOptions.IgnoreCase);

            string prefijo = empresa ? "30" : (hash % 2 == 0 ? "20" : "27");
            long cuerpo = 10000000L + (hash % 89999999L);

            for (int intento = 0; intento < 20; intento++)
            {
                string parcial = prefijo + cuerpo.ToString(CultureInfo.InvariantCulture);
                int[] pesos = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
                int suma = 0;

                for (int i = 0; i < 10; i++)
                {
                    suma += (parcial[i] - '0') * pesos[i];
                }

                int resto = 11 - (suma % 11);

                if (resto == 11)
                {
                    resto = 0;
                }

                if (resto != 10)
                {
                    return prefijo + "-" + cuerpo.ToString(CultureInfo.InvariantCulture) + "-"
                        + resto.ToString(CultureInfo.InvariantCulture);
                }

                cuerpo++;
            }

            return prefijo + "-" + cuerpo.ToString(CultureInfo.InvariantCulture) + "-0";
        }

        // ---------------------------------------------------------------
        // Productos
        // ---------------------------------------------------------------

        private sealed class FilaProducto
        {
            public string Sku;
            public string Nombre;
            public string RutaCategoria;
            public string Medida;
            public string Descripcion;
            public decimal Precio;
            public decimal Costo;
            public decimal Peso;
            public decimal Stock;
        }

        public ResultadoImportacion ImportarProductos(string ruta, IProgress<int> progreso)
        {
            Autorizacion.Exigir(Permiso.CatalogoEditar);

            List<string[]> filas = LectorCsv.Leer(ruta, ';');

            if (filas.Count < 2)
            {
                throw Mensajes.Regla("The file has no data rows.");
            }

            string[] cab = filas[0];
            int iNombre = LectorCsv.Columna(cab, "Nombre");
            int iSku = LectorCsv.Columna(cab, "SKU");
            int iPrecio = LectorCsv.Columna(cab, "Precio");

            if (iNombre < 0 || iSku < 0 || iPrecio < 0)
            {
                throw Mensajes.Regla("The columns Name, SKU and Price were not found in the file.");
            }

            int iUrl = LectorCsv.Columna(cab, "Identificador de URL");
            int iCategorias = LectorCsv.Columna(cab, "Categorias");
            int iStock = LectorCsv.Columna(cab, "Stock");
            int iCosto = LectorCsv.Columna(cab, "Costo");
            int iPeso = LectorCsv.Columna(cab, "Peso (kg)");
            int iDescripcion = LectorCsv.Columna(cab, "Descripcion");

            int[] propiedades =
            {
                LectorCsv.Columna(cab, "Nombre de propiedad 1"), LectorCsv.Columna(cab, "Valor de propiedad 1"),
                LectorCsv.Columna(cab, "Nombre de propiedad 2"), LectorCsv.Columna(cab, "Valor de propiedad 2"),
                LectorCsv.Columna(cab, "Nombre de propiedad 3"), LectorCsv.Columna(cab, "Valor de propiedad 3")
            };

            List<FilaProducto> productos = new List<FilaProducto>();
            string nombrePadre = string.Empty;
            string categoriasPadre = string.Empty;
            string descripcionPadre = string.Empty;
            string urlPadre = string.Empty;

            for (int i = 1; i < filas.Count; i++)
            {
                string[] fila = filas[i];
                string nombre = LectorCsv.Valor(fila, iNombre);

                if (nombre.Length > 0)
                {
                    nombrePadre = nombre;
                    categoriasPadre = LectorCsv.Valor(fila, iCategorias);
                    descripcionPadre = LectorCsv.Valor(fila, iDescripcion);
                    urlPadre = LectorCsv.Valor(fila, iUrl);
                }
                else if (LectorCsv.Valor(fila, iUrl).Length > 0 && LectorCsv.Valor(fila, iUrl) != urlPadre)
                {
                    continue;
                }

                string sku = LectorCsv.Valor(fila, iSku);

                if (sku.Length == 0 || nombrePadre.Length == 0)
                {
                    continue;
                }

                List<string> partes = new List<string>();
                List<string> medidas = new List<string>();

                for (int p = 0; p < propiedades.Length; p += 2)
                {
                    string etiqueta = LectorCsv.Valor(fila, propiedades[p]);
                    string valor = LectorCsv.Valor(fila, propiedades[p + 1]);

                    if (valor.Length > 0)
                    {
                        partes.Add(valor);
                        medidas.Add((etiqueta.Length > 0 ? etiqueta + ": " : string.Empty) + valor);
                    }
                }

                string nombreFinal = partes.Count > 0 ? nombrePadre + " (" + string.Join(", ", partes) + ")" : nombrePadre;

                productos.Add(new FilaProducto
                {
                    Sku = sku.Length > 50 ? sku.Substring(0, 50) : sku,
                    Nombre = nombreFinal.Length > 250 ? nombreFinal.Substring(0, 250) : nombreFinal,
                    RutaCategoria = PrimeraRuta(categoriasPadre),
                    Medida = string.Join("; ", medidas),
                    Descripcion = LimpiarHtml(descripcionPadre),
                    Precio = Decimal(LectorCsv.Valor(fila, iPrecio)),
                    Costo = Decimal(LectorCsv.Valor(fila, iCosto)),
                    Peso = Decimal(LectorCsv.Valor(fila, iPeso)),
                    Stock = Math.Max(0m, Decimal(LectorCsv.Valor(fila, iStock)))
                });
            }

            ResultadoImportacion resultado = new ResultadoImportacion { Total = productos.Count };
            Dictionary<string, Guid> categorias = CrearCategorias(productos.Select(p => p.RutaCategoria).Distinct().ToList());

            HashSet<string> existentes = _almacen.Ejecutar(unidad =>
                new HashSet<string>(unidad.Conjunto<Producto>().Select(p => p.CodigoSku).ToList(),
                    StringComparer.OrdinalIgnoreCase));

            List<FilaProducto> nuevos = new List<FilaProducto>();

            foreach (FilaProducto fila in productos)
            {
                if (existentes.Add(fila.Sku))
                {
                    nuevos.Add(fila);
                }
                else
                {
                    resultado.Omitidos++;
                }
            }

            int procesados = 0;

            for (int inicio = 0; inicio < nuevos.Count; inicio += ProductosPorLote)
            {
                List<FilaProducto> lote = nuevos.Skip(inicio).Take(ProductosPorLote).ToList();

                try
                {
                    GuardarLote(lote, categorias);
                    resultado.Importados += lote.Count;
                }
                catch (Exception ex)
                {
                    resultado.Omitidos += lote.Count;
                    resultado.Errores.Add(Mensajes.T("Batch starting at {0}: {1}", inicio + 1, ex.Message));
                }

                procesados += lote.Count;
                Informar(progreso, procesados, Math.Max(1, nuevos.Count));
            }

            new BitacoraService().RegistrarAuditoria(
                "Catálogo", "ImportarProductos", "Producto", null,
                resultado.Importados + " importados, " + resultado.Omitidos + " omitidos");

            return resultado;
        }

        private void GuardarLote(List<FilaProducto> lote, Dictionary<string, Guid> categorias)
        {
            _almacen.Ejecutar(unidad =>
            {
                UbicacionDeposito terminados = InventarioMotor.UbicacionPredeterminada(unidad, false);
                UbicacionDeposito materiales = InventarioMotor.UbicacionPredeterminada(unidad, true);
                List<Producto> creados = new List<Producto>();

                foreach (FilaProducto fila in lote)
                {
                    Producto producto = new Producto
                    {
                        CodigoSku = fila.Sku,
                        Nombre = fila.Nombre,
                        Descripcion = fila.Descripcion,
                        Medida = fila.Medida,
                        CategoriaId = fila.RutaCategoria.Length > 0 ? (Guid?)categorias[fila.RutaCategoria] : null,
                        Tipo = TipoSegunCategoria(fila.RutaCategoria),
                        CalidadAisi = DetectarAisi(fila.Nombre),
                        PesoUnitarioKg = fila.Peso > 0m ? (decimal?)fila.Peso : null,
                        PrecioBase = fila.Precio,
                        CostoActual = fila.Costo > 0m ? fila.Costo : Math.Round(fila.Precio * 0.6m, 2),
                        PuntoReposicion = fila.Stock > 0m ? Math.Max(2m, Math.Round(fila.Stock * 0.15m, 0)) : 0m,
                        Activo = fila.Precio > 0m
                    };

                    producto.StockMinimo = Math.Round(producto.PuntoReposicion / 2m, 0);
                    producto.AjustarSegunTipo();
                    unidad.Conjunto<Producto>().Agregar(producto);
                    creados.Add(producto);
                }

                unidad.Guardar();

                for (int i = 0; i < creados.Count; i++)
                {
                    FilaProducto fila = lote[i];
                    Producto producto = creados[i];

                    if (fila.Stock <= 0m)
                    {
                        continue;
                    }

                    bool esMaterial = producto.Tipo == TipoProducto.MateriaPrima
                        || producto.Tipo == TipoProducto.InsumoConsumible;
                    UbicacionDeposito ubicacion = esMaterial ? materiales : terminados;

                    unidad.Conjunto<StockProducto>().Agregar(new StockProducto
                    {
                        ProductoId = producto.Id,
                        UbicacionId = ubicacion.Id,
                        CantidadFisica = fila.Stock,
                        CostoPromedio = producto.CostoActual
                    });

                    unidad.Conjunto<MovimientoStock>().Agregar(new MovimientoStock
                    {
                        ProductoId = producto.Id,
                        UbicacionId = ubicacion.Id,
                        Tipo = TipoMovimientoStock.AjustePositivo,
                        Usuario = SesionActual.NombreUsuario,
                        Cantidad = fila.Stock,
                        CostoUnitario = producto.CostoActual,
                        DocumentoOrigen = "CARGA-INICIAL",
                        Observaciones = "Stock inicial",
                        SaldoFisicoResultante = fila.Stock
                    });
                }
            });
        }

        private Dictionary<string, Guid> CrearCategorias(List<string> rutas)
        {
            return _almacen.Ejecutar(unidad =>
            {
                List<CategoriaProducto> existentes = unidad.Conjunto<CategoriaProducto>().ToList();
                Dictionary<string, Guid> porRuta = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

                foreach (KeyValuePair<Guid, string> par in CatalogoService.ConstruirRutas(existentes))
                {
                    porRuta[par.Value] = par.Key;
                }

                int correlativo = existentes.Count;

                foreach (string ruta in rutas.Where(r => r.Length > 0))
                {
                    string[] segmentos = ruta.Split('>').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
                    string acumulada = string.Empty;
                    Guid? padre = null;

                    foreach (string segmento in segmentos)
                    {
                        acumulada = acumulada.Length == 0 ? segmento : acumulada + " > " + segmento;
                        Guid id;

                        if (!porRuta.TryGetValue(acumulada, out id))
                        {
                            correlativo++;

                            CategoriaProducto categoria = new CategoriaProducto
                            {
                                Codigo = "CAT" + correlativo.ToString("D3", CultureInfo.InvariantCulture),
                                Nombre = segmento.Length > 250 ? segmento.Substring(0, 250) : segmento,
                                CategoriaPadreId = padre
                            };

                            unidad.Conjunto<CategoriaProducto>().Agregar(categoria);
                            unidad.Guardar();
                            id = categoria.Id;
                            porRuta[acumulada] = id;
                        }

                        padre = id;
                    }
                }

                return porRuta;
            });
        }

        private static string PrimeraRuta(string categorias)
        {
            string primera = (categorias ?? string.Empty).Split(',')[0];

            return string.Join(" > ",
                primera.Split('>').Select(s => s.Trim()).Where(s => s.Length > 0));
        }

        private static TipoProducto TipoSegunCategoria(string ruta)
        {
            if (ruta.StartsWith("Materia Prima", StringComparison.OrdinalIgnoreCase))
            {
                return TipoProducto.MateriaPrima;
            }

            if (ruta.StartsWith("Repuestos", StringComparison.OrdinalIgnoreCase))
            {
                return TipoProducto.Componente;
            }

            return TipoProducto.ProductoTerminado;
        }

        private static string DetectarAisi(string nombre)
        {
            Match coincidencia = Regex.Match(nombre ?? string.Empty, @"AISI\s*(304L?|316L?)", RegexOptions.IgnoreCase);
            return coincidencia.Success ? "AISI " + coincidencia.Groups[1].Value.ToUpperInvariant() : string.Empty;
        }

        private static string LimpiarHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            string sinEtiquetas = Regex.Replace(html, "<[^>]+>", " ");
            string decodificado = WebUtility.HtmlDecode(sinEtiquetas);
            string compacto = Regex.Replace(decodificado, @"\s+", " ").Trim();

            return compacto.Length > 500 ? compacto.Substring(0, 497) + "..." : compacto;
        }

        private static decimal Decimal(string texto)
        {
            decimal valor;

            return decimal.TryParse(
                (texto ?? string.Empty).Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out valor) ? valor : 0m;
        }

        private static void Informar(IProgress<int> progreso, int actual, int total)
        {
            if (progreso != null && total > 0)
            {
                progreso.Report(Math.Min(100, actual * 100 / total));
            }
        }
    }
}
