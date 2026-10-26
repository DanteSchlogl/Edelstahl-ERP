using System;
using System.Collections.Generic;
using System.Linq;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Infrastructure;
using Edelstahl.DAL.Factory;
using Edelstahl.DAL.Store;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Security;

namespace Edelstahl.BLL.Services
{
    /// <summary>
    /// Catálogo maestro: categorías, productos y materiales, con sus
    /// precios y existencias.
    /// </summary>
    public class CatalogoService
    {
        private readonly INegocioStore _almacen;

        public CatalogoService()
            : this(FactoryDataAccess.NegocioStore)
        {
        }

        public CatalogoService(INegocioStore almacen)
        {
            _almacen = almacen ?? throw new ArgumentNullException(nameof(almacen));
        }

        // ---------------------------------------------------------------
        // Categorías
        // ---------------------------------------------------------------

        public List<CategoriaProducto> ObtenerCategorias()
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<CategoriaProducto>().ToList()
                    .OrderBy(c => c.Nombre).ToList());
        }

        /// <summary>
        /// Devuelve el nombre completo de cada categoría (Padre > Hija).
        /// </summary>
        public Dictionary<Guid, string> ObtenerRutasDeCategoria()
        {
            List<CategoriaProducto> categorias = ObtenerCategorias();
            return ConstruirRutas(categorias);
        }

        public static Dictionary<Guid, string> ConstruirRutas(List<CategoriaProducto> categorias)
        {
            Dictionary<Guid, CategoriaProducto> porId = categorias.ToDictionary(c => c.Id);
            Dictionary<Guid, string> rutas = new Dictionary<Guid, string>();

            foreach (CategoriaProducto categoria in categorias)
            {
                List<string> partes = new List<string>();
                CategoriaProducto actual = categoria;
                int guarda = 0;

                while (actual != null && guarda++ < 10)
                {
                    partes.Insert(0, actual.Nombre);
                    CategoriaProducto padre = null;

                    if (actual.CategoriaPadreId.HasValue)
                    {
                        porId.TryGetValue(actual.CategoriaPadreId.Value, out padre);
                    }

                    actual = padre;
                }

                rutas[categoria.Id] = string.Join(" > ", partes);
            }

            return rutas;
        }

        public CategoriaProducto GuardarCategoria(CategoriaProducto categoria)
        {
            Autorizacion.Exigir(Permiso.CatalogoEditar);

            if (categoria == null)
            {
                throw new ArgumentNullException(nameof(categoria));
            }

            string codigo = (categoria.Codigo ?? string.Empty).Trim().ToUpperInvariant();
            string nombre = (categoria.Nombre ?? string.Empty).Trim();

            if (codigo.Length == 0 || nombre.Length == 0)
            {
                throw Mensajes.Regla("The category code and name are required.");
            }

            if (categoria.CategoriaPadreId.HasValue && categoria.CategoriaPadreId.Value == categoria.Id)
            {
                throw Mensajes.Regla("A category cannot be its own parent.");
            }

            CategoriaProducto resultado = _almacen.Ejecutar(unidad =>
            {
                IConjunto<CategoriaProducto> conjunto = unidad.Conjunto<CategoriaProducto>();
                CategoriaProducto existente = conjunto.FirstOrDefault(c => c.Id == categoria.Id);

                bool codigoRepetido = conjunto.Any(c => c.Codigo == codigo && c.Id != categoria.Id);

                if (codigoRepetido)
                {
                    throw Mensajes.Regla("There is already a category with the code {0}.", codigo);
                }

                if (existente == null)
                {
                    existente = new CategoriaProducto { Id = categoria.Id };
                    conjunto.Agregar(existente);
                }

                existente.Codigo = codigo;
                existente.Nombre = nombre;
                existente.Descripcion = categoria.Descripcion ?? string.Empty;
                existente.CategoriaPadreId = categoria.CategoriaPadreId;
                existente.Activo = categoria.Activo;

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Catálogo", "GuardarCategoria", "CategoriaProducto", resultado.Id,
                "Categoría guardada: " + resultado.Nombre);

            return resultado;
        }

        // ---------------------------------------------------------------
        // Productos
        // ---------------------------------------------------------------

        /// <summary>
        /// Productos activos y vendibles que se pueden fabricar a medida (base de una linea de fabricacion especial).
        /// </summary>
        public List<ProductoListaDto> ObtenerFabricables()
        {
            HashSet<Guid> ids = _almacen.Ejecutar(unidad => new HashSet<Guid>(
                unidad.Conjunto<Producto>().ToList().Where(p => p.Activo && p.EsVendible && p.EsFabricable).Select(p => p.Id)));

            return BuscarProductos(string.Empty, null, null, true, true).Where(p => ids.Contains(p.Id)).ToList();
        }

        public List<ProductoListaDto> BuscarProductos(
            string texto,
            Guid? categoriaId,
            TipoProducto? tipo,
            bool soloActivos,
            bool soloVendibles)
        {
            return _almacen.Ejecutar(unidad =>
            {
                List<Producto> productos = unidad.Conjunto<Producto>().ToList();
                List<CategoriaProducto> categorias = unidad.Conjunto<CategoriaProducto>().ToList();
                Dictionary<Guid, string> rutas = ConstruirRutas(categorias);

                Dictionary<Guid, List<StockProducto>> stocks = unidad.Conjunto<StockProducto>()
                    .ToList()
                    .GroupBy(s => s.ProductoId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                HashSet<Guid> idsCategoria = categoriaId.HasValue
                    ? DescendientesDe(categorias, categoriaId.Value)
                    : null;

                string buscado = (texto ?? string.Empty).Trim().ToLowerInvariant();

                IEnumerable<Producto> filtrados = productos;

                if (buscado.Length > 0)
                {
                    filtrados = filtrados.Where(p =>
                        p.CodigoSku.ToLowerInvariant().Contains(buscado)
                        || p.Nombre.ToLowerInvariant().Contains(buscado)
                        || (p.Medida ?? string.Empty).ToLowerInvariant().Contains(buscado));
                }

                if (idsCategoria != null)
                {
                    filtrados = filtrados.Where(p => p.CategoriaId.HasValue && idsCategoria.Contains(p.CategoriaId.Value));
                }

                if (tipo.HasValue)
                {
                    filtrados = filtrados.Where(p => p.Tipo == tipo.Value);
                }

                if (soloActivos)
                {
                    filtrados = filtrados.Where(p => p.Activo);
                }

                if (soloVendibles)
                {
                    filtrados = filtrados.Where(p => p.EsVendible);
                }

                return filtrados
                    .OrderBy(p => p.Nombre)
                    .Select(p => ArmarDto(p, rutas, stocks))
                    .ToList();
            });
        }

        private static ProductoListaDto ArmarDto(
            Producto p,
            Dictionary<Guid, string> rutas,
            Dictionary<Guid, List<StockProducto>> stocks)
        {
            List<StockProducto> propios;
            decimal fisica = 0m;
            decimal disponible = 0m;

            if (stocks.TryGetValue(p.Id, out propios))
            {
                fisica = propios.Sum(s => s.CantidadFisica);
                disponible = propios.Sum(s => s.CantidadDisponible);
            }

            string categoria = string.Empty;

            if (p.CategoriaId.HasValue)
            {
                rutas.TryGetValue(p.CategoriaId.Value, out categoria);
            }

            return new ProductoListaDto
            {
                Id = p.Id,
                Sku = p.CodigoSku,
                Nombre = p.Nombre,
                Categoria = categoria ?? string.Empty,
                Tipo = Etiquetas.De(p.Tipo),
                Unidad = p.UnidadMedida,
                PrecioBase = p.PrecioBase,
                CostoActual = p.CostoActual,
                Fisica = fisica,
                Disponible = disponible,
                StockMinimo = p.StockMinimo,
                Estado = EstadoStock(disponible, p.PuntoReposicion),
                Activo = p.Activo
            };
        }

        public static string EstadoStock(decimal disponible, decimal puntoReposicion)
        {
            if (disponible <= 0m)
            {
                return Mensajes.T("Out of stock");
            }

            if (puntoReposicion > 0m && disponible <= puntoReposicion)
            {
                return Mensajes.T("Reorder");
            }

            return Mensajes.T("In stock");
        }

        public static HashSet<Guid> DescendientesDe(List<CategoriaProducto> categorias, Guid raiz)
        {
            HashSet<Guid> resultado = new HashSet<Guid> { raiz };
            bool cambio = true;

            while (cambio)
            {
                cambio = false;

                foreach (CategoriaProducto categoria in categorias)
                {
                    if (categoria.CategoriaPadreId.HasValue
                        && resultado.Contains(categoria.CategoriaPadreId.Value)
                        && resultado.Add(categoria.Id))
                    {
                        cambio = true;
                    }
                }
            }

            return resultado;
        }

        public Producto ObtenerProducto(Guid id)
        {
            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Producto>().FirstOrDefault(p => p.Id == id));
        }

        public Producto ObtenerPorSku(string sku)
        {
            string buscado = (sku ?? string.Empty).Trim();

            return _almacen.Ejecutar(unidad =>
                unidad.Conjunto<Producto>().FirstOrDefault(p => p.CodigoSku == buscado));
        }

        public Producto GuardarProducto(Producto producto)
        {
            Autorizacion.Exigir(Permiso.CatalogoEditar);

            if (producto == null)
            {
                throw new ArgumentNullException(nameof(producto));
            }

            ValidarProducto(producto);

            string sku = producto.CodigoSku.Trim();

            Producto resultado = _almacen.Ejecutar(unidad =>
            {
                IConjunto<Producto> conjunto = unidad.Conjunto<Producto>();

                if (conjunto.Any(p => p.CodigoSku == sku && p.Id != producto.Id))
                {
                    throw Mensajes.Regla("There is already a product with the code {0}.", sku);
                }

                if (producto.CategoriaId.HasValue)
                {
                    Guid categoriaId = producto.CategoriaId.Value;
                    CategoriaProducto categoria = unidad.Conjunto<CategoriaProducto>()
                        .FirstOrDefault(c => c.Id == categoriaId);

                    if (categoria == null || !categoria.Activo)
                    {
                        throw Mensajes.Regla("The selected category is not available.");
                    }
                }

                Producto existente = conjunto.FirstOrDefault(p => p.Id == producto.Id);

                if (existente == null)
                {
                    existente = new Producto { Id = producto.Id };
                    conjunto.Agregar(existente);
                }
                else
                {
                    existente.FechaModificacion = DateTime.Now;
                }

                Copiar(producto, existente);
                existente.CodigoSku = sku;

                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Catálogo", "GuardarProducto", "Producto", resultado.Id,
                "Producto guardado: " + resultado.CodigoSku + " - " + resultado.Nombre);

            return resultado;
        }

        private static void Copiar(Producto origen, Producto destino)
        {
            destino.Nombre = origen.Nombre.Trim();
            destino.Descripcion = origen.Descripcion ?? string.Empty;
            destino.CategoriaId = origen.CategoriaId;
            destino.Tipo = origen.Tipo;
            destino.UnidadMedida = origen.UnidadMedida;
            destino.CalidadAisi = origen.CalidadAisi ?? string.Empty;
            destino.EspesorMm = origen.EspesorMm;
            destino.Acabado = origen.Acabado ?? string.Empty;
            destino.AnchoMm = origen.AnchoMm;
            destino.LargoMm = origen.LargoMm;
            destino.DiametroMm = origen.DiametroMm;
            destino.PesoUnitarioKg = origen.PesoUnitarioKg;
            destino.Medida = origen.Medida ?? string.Empty;
            destino.PrecioBase = origen.PrecioBase;
            destino.CostoActual = origen.CostoActual;
            destino.StockMinimo = origen.StockMinimo;
            destino.PuntoReposicion = origen.PuntoReposicion;
            destino.EsVendible = origen.EsVendible;
            destino.EsComprable = origen.EsComprable;
            destino.EsFabricable = origen.EsFabricable;
            destino.Activo = origen.Activo;
            destino.RutaImagen = origen.RutaImagen ?? string.Empty;
            destino.RutaPlanoTecnico = origen.RutaPlanoTecnico ?? string.Empty;
        }

        public static void ValidarProducto(Producto producto)
        {
            if (string.IsNullOrWhiteSpace(producto.CodigoSku))
            {
                throw Mensajes.Regla("The product code (SKU) is required.");
            }

            if (producto.CodigoSku.Trim().Length > 50)
            {
                throw Mensajes.Regla("The product code cannot exceed 50 characters.");
            }

            if (string.IsNullOrWhiteSpace(producto.Nombre))
            {
                throw Mensajes.Regla("The product name is required.");
            }

            if (producto.PrecioBase < 0m || producto.CostoActual < 0m)
            {
                throw Mensajes.Regla("Prices and costs cannot be negative.");
            }

            if (producto.StockMinimo < 0m || producto.PuntoReposicion < 0m)
            {
                throw Mensajes.Regla("The minimum stock and the reorder point cannot be negative.");
            }

            if (Array.IndexOf(UnidadesMedida.Todas, producto.UnidadMedida) < 0)
            {
                throw Mensajes.Regla("The unit of measure is not valid.");
            }
        }

        public void CambiarEstadoProducto(Guid productoId, bool activo)
        {
            Autorizacion.Exigir(Permiso.CatalogoEditar);

            Producto producto = _almacen.Ejecutar(unidad =>
            {
                Producto existente = InventarioMotor.ObtenerProducto(unidad, productoId);
                existente.Activo = activo;
                existente.FechaModificacion = DateTime.Now;
                return existente;
            });

            new BitacoraService().RegistrarAuditoria(
                "Catálogo", activo ? "ActivarProducto" : "DesactivarProducto", "Producto",
                producto.Id, producto.CodigoSku);
        }

        /// <summary>
        /// Aplica un ajuste porcentual a los precios de una categoría completa
        /// (con sus subcategorías) o de todo el catálogo.
        /// </summary>
        public int AumentoMasivoPrecios(Guid? categoriaId, decimal porcentaje, string motivo)
        {
            Autorizacion.Exigir(Permiso.CatalogoEditar);

            if (porcentaje < -90m || porcentaje > 500m || porcentaje == 0m)
            {
                throw Mensajes.Regla("The percentage must be between -90 and 500, and different from zero.");
            }

            int cantidad = _almacen.Ejecutar(unidad =>
            {
                List<CategoriaProducto> categorias = unidad.Conjunto<CategoriaProducto>().ToList();
                HashSet<Guid> ids = categoriaId.HasValue ? DescendientesDe(categorias, categoriaId.Value) : null;

                List<Producto> alcanzados = unidad.Conjunto<Producto>()
                    .Where(p => p.Activo && p.EsVendible)
                    .ToList()
                    .Where(p => ids == null || (p.CategoriaId.HasValue && ids.Contains(p.CategoriaId.Value)))
                    .ToList();

                foreach (Producto producto in alcanzados)
                {
                    producto.PrecioBase = Math.Round(producto.PrecioBase * (1m + porcentaje / 100m), 2);
                    producto.FechaModificacion = DateTime.Now;
                }

                return alcanzados.Count;
            });

            new BitacoraService().RegistrarAuditoria(
                "Catálogo", "AumentoMasivoPrecios", "Producto", null,
                "Ajuste de " + porcentaje.ToString("0.##") + "% sobre " + cantidad + " productos. " + motivo);

            return cantidad;
        }
    }
}
