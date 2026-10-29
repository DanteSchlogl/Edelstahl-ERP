using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Finanzas;
using Edelstahl.Domain.Logistica;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Datos mínimos para operar (roles, depósito y medios de pago) y datos
    /// de ejemplo (catálogo, clientes, proveedores, vehículos y choferes).
    /// </summary>
    public class CargaInicialService
    {
        private readonly INegocioStore _almacen;

        public CargaInicialService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public CargaInicialService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        public static string CarpetaDatos
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Datos"); }
        }

        /// <summary>
        /// Crea lo imprescindible si falta. Puede ejecutarse varias veces.
        /// </summary>
        public void AsegurarDatosBase()
        {
            AsegurarRoles();
            AsegurarDepositoYMediosDePago();
        }

        private static void AsegurarRoles()
        {
            Edelstahl.DAL.Interfaces.IRolRepository repositorio = FactoryDataAccess.RolRepository;

            if (repositorio == null)
            {
                return;
            }

            string[][] roles =
            {
                new[] { NombresRol.Administrador, "Acceso completo al sistema." },
                new[] { NombresRol.Vendedor, "Clientes, presupuestos, pedidos y facturación." },
                new[] { NombresRol.Produccion, "Órdenes de fabricación y control de calidad." },
                new[] { NombresRol.Deposito, "Inventario, reservas y movimientos de stock." },
                new[] { NombresRol.Compras, "Proveedores, órdenes de compra y recepciones." },
                new[] { NombresRol.Logistica, "Remitos, hojas de ruta, entregas y devoluciones." },
                new[] { NombresRol.Supervisor, "Consulta de todos los módulos, reportes y bitácora, sin modificar datos." }
            };

            foreach (string[] rol in roles)
            {
                if (!repositorio.ExistsByNombre(rol[0]))
                {
                    repositorio.Add(new Rol { Nombre = rol[0], Descripcion = rol[1] });
                }
            }
        }

        private void AsegurarDepositoYMediosDePago()
        {
            _almacen.Ejecutar(unidad =>
            {
                if (!unidad.Conjunto<Deposito>().Any())
                {
                    Deposito deposito = new Deposito
                    {
                        Codigo = "DEP-01",
                        Nombre = "Depósito Central",
                        Direccion = "Parque Industrial, nave 3",
                        Localidad = "Pilar",
                        Descripcion = "Depósito principal de materias primas y productos terminados",
                        Responsable = "Responsable de Depósito"
                    };

                    string[][] ubicaciones =
                    {
                        new[] { "A-01", "A", "Productos terminados, estantería 1" },
                        new[] { "A-02", "A", "Productos terminados, estantería 2" },
                        new[] { "B-01", "B", "Repuestos y componentes" },
                        new[] { "MP-01", "MP", "Chapas y planchuelas" },
                        new[] { "MP-02", "MP", "Tubos, barras e insumos" }
                    };

                    foreach (string[] u in ubicaciones)
                    {
                        deposito.Ubicaciones.Add(new UbicacionDeposito
                        {
                            DepositoId = deposito.Id,
                            Codigo = u[0],
                            Sector = u[1],
                            Descripcion = u[2]
                        });
                    }

                    unidad.Conjunto<Deposito>().Agregar(deposito);
                }

                if (!unidad.Conjunto<MedioPago>().Any())
                {
                    object[][] medios =
                    {
                        new object[] { "EFECTIVO", "Efectivo", false, false },
                        new object[] { "TRANSFERENCIA", "Transferencia bancaria", true, true },
                        new object[] { "DEPOSITO", "Depósito bancario", true, true },
                        new object[] { "CHEQUE", "Cheque", true, true },
                        new object[] { "ECHEQ", "Cheque electrónico (eCheq)", true, true },
                        new object[] { "TARJETA", "Tarjeta", true, false }
                    };

                    foreach (object[] m in medios)
                    {
                        unidad.Conjunto<MedioPago>().Agregar(new MedioPago
                        {
                            Codigo = (string)m[0],
                            Nombre = (string)m[1],
                            RequiereReferencia = (bool)m[2],
                            RequiereEntidadFinanciera = (bool)m[3]
                        });
                    }
                }
            });
        }

        public bool TieneCatalogo()
        {
            return _almacen.Ejecutar(unidad => unidad.Conjunto<Producto>().Any());
        }

        /// <summary>
        /// Carga catálogo, materias primas, clientes, proveedores, vehículos y
        /// choferes de ejemplo. Requiere un usuario con permisos de administración.
        /// </summary>
        public ResultadoImportacion CargarDatosDeEjemplo(IProgress<int> progreso)
        {
            AsegurarDatosBase();

            ResultadoImportacion total = new ResultadoImportacion();
            Informar(progreso, 2);

            CargarProveedores();
            Informar(progreso, 6);

            CargarFlota();
            Informar(progreso, 10);

            string archivoProductos = Path.Combine(CarpetaDatos, "productos.csv");
            string archivoClientes = Path.Combine(CarpetaDatos, "clientes.csv");

            if (File.Exists(archivoProductos))
            {
                ResultadoImportacion productos = new ImportacionService(_almacen).ImportarProductos(
                    archivoProductos, new Progress<int>(p => Informar(progreso, 10 + p * 60 / 100)));

                Acumular(total, productos);
            }

            CargarMateriasPrimasYFabricables();
            Informar(progreso, 75);

            if (File.Exists(archivoClientes))
            {
                ResultadoImportacion clientes = new ImportacionService(_almacen).ImportarClientes(
                    archivoClientes, new Progress<int>(p => Informar(progreso, 75 + p * 24 / 100)));

                Acumular(total, clientes);
            }

            Informar(progreso, 100);

            new BitacoraService().RegistrarAuditoria(
                "Sistema", "CargaDatosEjemplo", string.Empty, null,
                total.Importados + " registros importados");

            return total;
        }

        private static void Acumular(ResultadoImportacion destino, ResultadoImportacion origen)
        {
            destino.Total += origen.Total;
            destino.Importados += origen.Importados;
            destino.Omitidos += origen.Omitidos;
            destino.Errores.AddRange(origen.Errores);
        }

        private static void Informar(IProgress<int> progreso, int valor)
        {
            if (progreso != null)
            {
                progreso.Report(Math.Min(100, Math.Max(0, valor)));
            }
        }

        private static void CargarProveedores()
        {
            ComprasService compras = new ComprasService();

            string[][] proveedores =
            {
                new[] { "Aceros Inoxidables del Plata S.A.", "Av. Industrial 1450", "San Martín", "Buenos Aires" },
                new[] { "Tubos y Perfiles Industriales S.R.L.", "Calle 12 N° 880", "Campana", "Buenos Aires" },
                new[] { "Sellos y Juntas Sanitarias S.A.", "Ruta 9 km 52", "Escobar", "Buenos Aires" },
                new[] { "Gases Industriales del Sur S.A.", "Parque Industrial lote 14", "Zárate", "Buenos Aires" }
            };

            foreach (string[] datos in proveedores)
            {
                string cuit = ImportacionService.GenerarCuit(datos[0]);
                string digitos = new string(cuit.Where(char.IsDigit).ToArray());

                if (compras.ObtenerProveedores(digitos, false).Any(p => p.Cuit == digitos))
                {
                    continue;
                }

                compras.GuardarProveedor(new Proveedor
                {
                    Cuit = cuit,
                    RazonSocial = datos[0],
                    Direccion = datos[1],
                    Localidad = datos[2],
                    Provincia = datos[3],
                    Email = "ventas@proveedor-ejemplo.com.ar",
                    Telefono = "1140000000",
                    ContactoComercial = "Área comercial",
                    LimiteCredito = 8000000m,
                    PlazoEntregaDias = 10
                });
            }
        }

        private static void CargarFlota()
        {
            LogisticaService logistica = new LogisticaService();

            if (logistica.ObtenerVehiculos(false).Count == 0)
            {
                DateTime vence = DateTime.Today.AddMonths(8);

                object[][] vehiculos =
                {
                    new object[] { "AB123CD", "Iveco", "Daily 55C", 2021, 3500m },
                    new object[] { "AC456EF", "Mercedes-Benz", "Sprinter 515", 2020, 1800m },
                    new object[] { "AD789GH", "Ford", "Cargo 1722", 2019, 9000m }
                };

                foreach (object[] v in vehiculos)
                {
                    logistica.GuardarVehiculo(new Vehiculo
                    {
                        Patente = (string)v[0],
                        Marca = (string)v[1],
                        Modelo = (string)v[2],
                        Anio = (int)v[3],
                        CapacidadKg = (decimal)v[4],
                        VencimientoSeguro = vence,
                        VencimientoVtv = vence
                    });
                }
            }

            if (logistica.ObtenerChoferes(false).Count == 0)
            {
                DateTime vence = DateTime.Today.AddYears(2);

                string[][] choferes =
                {
                    new[] { "30123456", "Gómez", "Martín", "LIC-0001234", "C1" },
                    new[] { "28987654", "Pereyra", "Lucía", "LIC-0005678", "B2" },
                    new[] { "33456789", "Ruiz", "Damián", "LIC-0009012", "C2" }
                };

                foreach (string[] c in choferes)
                {
                    logistica.GuardarChofer(new Chofer
                    {
                        Documento = c[0],
                        Apellido = c[1],
                        Nombre = c[2],
                        LicenciaConducir = c[3],
                        CategoriaLicencia = c[4],
                        VencimientoLicencia = vence,
                        Telefono = "1155550000"
                    });
                }
            }
        }

        private void CargarMateriasPrimasYFabricables()
        {
            CatalogoService catalogo = new CatalogoService(_almacen);
            ProductoSemilla[] semillas = ProductoSemilla.Lista();

            foreach (ProductoSemilla semilla in semillas)
            {
                if (catalogo.ObtenerPorSku(semilla.Sku) != null)
                {
                    continue;
                }

                Producto producto = new Producto
                {
                    CodigoSku = semilla.Sku,
                    Nombre = semilla.Nombre,
                    Tipo = semilla.Tipo,
                    UnidadMedida = semilla.Unidad,
                    CalidadAisi = semilla.Aisi,
                    EspesorMm = semilla.EspesorMm,
                    PrecioBase = semilla.Precio,
                    CostoActual = semilla.Costo,
                    PuntoReposicion = semilla.PuntoReposicion,
                    StockMinimo = Math.Round(semilla.PuntoReposicion / 2m, 0)
                };

                producto.AjustarSegunTipo();
                catalogo.GuardarProducto(producto);

                if (semilla.Stock > 0m)
                {
                    Producto guardado = catalogo.ObtenerPorSku(semilla.Sku);

                    _almacen.Ejecutar(unidad =>
                    {
                        UbicacionDeposito ubicacion = InventarioMotor.UbicacionParaProducto(unidad, guardado);

                        InventarioMotor.Mover(
                            unidad, guardado.Id, ubicacion.Id, TipoMovimientoStock.AjustePositivo,
                            semilla.Stock, semilla.Costo, "CARGA-INICIAL", null,
                            string.Empty, string.Empty, "Stock inicial", false);
                    });
                }
            }
        }

        private sealed class ProductoSemilla
        {
            public string Sku;
            public string Nombre;
            public TipoProducto Tipo;
            public string Unidad;
            public string Aisi;
            public decimal? EspesorMm;
            public decimal Precio;
            public decimal Costo;
            public decimal Stock;
            public decimal PuntoReposicion;

            private static ProductoSemilla Nueva(
                string sku, string nombre, TipoProducto tipo, string unidad, string aisi,
                decimal? espesor, decimal precio, decimal costo, decimal stock, decimal punto)
            {
                return new ProductoSemilla
                {
                    Sku = sku, Nombre = nombre, Tipo = tipo, Unidad = unidad, Aisi = aisi,
                    EspesorMm = espesor, Precio = precio, Costo = costo, Stock = stock, PuntoReposicion = punto
                };
            }

            public static ProductoSemilla[] Lista()
            {
                string mp = UnidadesMedida.MetroCuadrado;

                return new[]
                {
                    Nueva("MP-304-2.0", "Chapa AISI 304 de 2,0 mm", TipoProducto.MateriaPrima, mp, "AISI 304", 2.0m, 0m, 38m, 120m, 40m),
                    Nueva("MP-304-3.0", "Chapa AISI 304 de 3,0 mm", TipoProducto.MateriaPrima, mp, "AISI 304", 3.0m, 0m, 56m, 90m, 30m),
                    Nueva("MP-316L-2.0", "Chapa AISI 316L de 2,0 mm", TipoProducto.MateriaPrima, mp, "AISI 316L", 2.0m, 0m, 62m, 70m, 25m),
                    Nueva("MP-316L-3.0", "Chapa AISI 316L de 3,0 mm", TipoProducto.MateriaPrima, mp, "AISI 316L", 3.0m, 0m, 91m, 45m, 20m),
                    Nueva("MP-TUBO-304-1.5", "Tubo sanitario AISI 304 de 1 1/2 pulgada (6 m)", TipoProducto.MateriaPrima, UnidadesMedida.Unidad, "AISI 304", null, 0m, 74m, 60m, 20m),
                    Nueva("MP-TUBO-304-2", "Tubo sanitario AISI 304 de 2 pulgadas (6 m)", TipoProducto.MateriaPrima, UnidadesMedida.Unidad, "AISI 304", null, 0m, 96m, 40m, 15m),
                    Nueva("MP-BARRA-304-10", "Barra redonda AISI 304 de 10 mm", TipoProducto.MateriaPrima, UnidadesMedida.Kilogramo, "AISI 304", null, 0m, 4.8m, 300m, 100m),
                    Nueva("MP-BARRA-316L-12", "Barra redonda AISI 316L de 12 mm", TipoProducto.MateriaPrima, UnidadesMedida.Kilogramo, "AISI 316L", null, 0m, 7.2m, 220m, 80m),
                    Nueva("MP-MALLA-316L-05", "Malla ranurada AISI 316L de 0,5 mm", TipoProducto.MateriaPrima, mp, "AISI 316L", 0.5m, 0m, 130m, 18m, 8m),
                    Nueva("IN-TIG-308L", "Varilla de aporte TIG 308L", TipoProducto.InsumoConsumible, UnidadesMedida.Kilogramo, string.Empty, null, 0m, 21m, 35m, 12m),
                    Nueva("IN-ARGON", "Garrafa de argón industrial", TipoProducto.InsumoConsumible, UnidadesMedida.Unidad, string.Empty, null, 0m, 55m, 14m, 5m),
                    Nueva("IN-DISCO-PULIDO", "Disco de pulido para acero inoxidable", TipoProducto.InsumoConsumible, UnidadesMedida.Unidad, string.Empty, null, 0m, 3.2m, 150m, 50m),
                    Nueva("FILTRO-10", "Filtro de ranura continua de 10 pulgadas", TipoProducto.ProductoTerminado, UnidadesMedida.Unidad, "AISI 316L", null, 1240m, 760m, 0m, 0m),
                    Nueva("FILTRO-20", "Filtro de ranura continua de 20 pulgadas", TipoProducto.ProductoTerminado, UnidadesMedida.Unidad, "AISI 316L", null, 1492m, 910m, 0m, 0m),
                    Nueva("MP304-15", "Mirilla plana para soldar AISI 304", TipoProducto.ProductoTerminado, UnidadesMedida.Unidad, "AISI 304", null, 33.47m, 20m, 40m, 8m),
                    Nueva("MCPS304-15", "Manguito Clamp largo AISI 304", TipoProducto.ProductoTerminado, UnidadesMedida.Unidad, "AISI 304", null, 4.23m, 2.5m, 60m, 12m)
                };
            }
        }
    }
}
