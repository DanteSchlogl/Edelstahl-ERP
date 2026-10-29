using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Edelstahl.BLL.DTOs;
using Edelstahl.BLL.Exceptions.Base;
using Edelstahl.BLL.Services;
using Edelstahl.DAL.EntityFramework;
using Edelstahl.DAL.Factory;
using Edelstahl.Domain.Catalogo;
using Edelstahl.Domain.Compras;
using Edelstahl.Domain.Security;
using Edelstahl.Services.Configuration;

namespace Edelstahl.Test
{
    /// <summary>
    /// Prueba el circuito del catalogo de productos: carga masiva desde CSV, alta y edicion,
    /// reglas de compra, aumento masivo de precios y sugerencias de reposicion.
    /// Para no tocar las bases reales usar EDELSTAHL_NEGOCIO_DB y EDELSTAHL_SERVICIOS_DB.
    /// </summary>
    public static class PruebaCatalogo
    {
        public static void Ejecutar(ExecutionMode modo, string rutaCsv)
        {
            Verificador.Seccion("Catalogo de productos en modo " + modo);

            ApplicationMode.Reset();
            ApplicationMode.Select(modo);
            FactoryDataAccess.Inicializar(modo);

            if (modo == ExecutionMode.SqlServer)
            {
                InicializadorNegocio.Inicializar();
            }

            SesionActual.Iniciar(new Usuario
            {
                NombreUsuario = "catalogo",
                NombreCompleto = "Prueba de catalogo",
                Rol = new Rol { Nombre = NombresRol.Administrador }
            });

            new CargaInicialService().AsegurarDatosBase();
            CatalogoService catalogo = new CatalogoService();

            // 1. Carga masiva desde el archivo de productos.
            ResultadoImportacion primera = new ImportacionService().ImportarProductos(rutaCsv, null);
            Console.WriteLine("   importados={0} omitidos={1} total={2} errores={3}", primera.Importados, primera.Omitidos, primera.Total, primera.Errores.Count);
            Verificador.Verificar(primera.Total >= 500, "El archivo trae unos 500 productos (" + primera.Total + ")");
            Verificador.Verificar(primera.Importados > 0, "Se importaron productos (" + primera.Importados + ")");
            Verificador.Verificar(primera.Importados + primera.Omitidos + primera.Errores.Count >= primera.Total - 1, "Cada fila se importa, se omite o se informa con error");

            // 2. Volver a importar no duplica nada.
            ResultadoImportacion segunda = new ImportacionService().ImportarProductos(rutaCsv, null);
            Verificador.Verificar(segunda.Importados == 0, "Importar de nuevo no duplica productos (importados=" + segunda.Importados + ")");
            Verificador.Verificar(segunda.Omitidos >= primera.Importados, "Los ya cargados se omiten (" + segunda.Omitidos + ")");

            // 3. El producto importado se puede buscar y leer.
            Producto muestra = catalogo.ObtenerPorSku("MDR25-C50");
            Verificador.Verificar(muestra != null, "El producto MDR25-C50 existe despues de la carga");
            if (muestra != null)
            {
                Verificador.Verificar(muestra.PrecioBase > 0m, "El producto importado tiene precio (" + muestra.PrecioBase + ")");
            }

            // 4. Alta de un producto nuevo: materia prima, comprable por tipo.
            Producto nuevo = new Producto
            {
                CodigoSku = "PRUEBA-MP-01",
                Nombre = "Chapa AISI 304 2B 1,5 mm - prueba",
                Tipo = TipoProducto.MateriaPrima,
                UnidadMedida = UnidadesMedida.Todas[0],
                CalidadAisi = "304",
                EspesorMm = 1.5m,
                Acabado = "2B",
                PrecioBase = 0m,
                CostoActual = 4500m,
                StockMinimo = 10m,
                PuntoReposicion = 20m
            };
            nuevo.AjustarSegunTipo();
            Verificador.Verificar(nuevo.EsComprable && !nuevo.EsVendible && !nuevo.EsFabricable, "Una materia prima se compra, no se vende ni se fabrica");
            Producto guardado = catalogo.GuardarProducto(nuevo);
            Verificador.Verificar(catalogo.ObtenerPorSku("PRUEBA-MP-01") != null, "El producto nuevo queda guardado");

            // 5. Reglas del alta.
            Producto repetido = new Producto { CodigoSku = "PRUEBA-MP-01", Nombre = "Repetido", Tipo = TipoProducto.MateriaPrima, UnidadMedida = UnidadesMedida.Todas[0] };
            Verificador.Falla<ReglaDeNegocioException>(() => catalogo.GuardarProducto(repetido), "Un codigo repetido se rechaza");
            Producto sinNombre = new Producto { CodigoSku = "PRUEBA-X", Nombre = " ", Tipo = TipoProducto.MateriaPrima, UnidadMedida = UnidadesMedida.Todas[0] };
            Verificador.Falla<ReglaDeNegocioException>(() => catalogo.GuardarProducto(sinNombre), "Un producto sin nombre se rechaza");
            Producto negativo = new Producto { CodigoSku = "PRUEBA-Y", Nombre = "Negativo", Tipo = TipoProducto.MateriaPrima, UnidadMedida = UnidadesMedida.Todas[0], PrecioBase = -1m };
            Verificador.Falla<ReglaDeNegocioException>(() => catalogo.GuardarProducto(negativo), "Un precio negativo se rechaza");

            // 6. Edicion de la ficha tecnica.
            Producto editar = catalogo.ObtenerPorSku("PRUEBA-MP-01");
            editar.CalidadAisi = "316L";
            editar.EspesorMm = 2m;
            catalogo.GuardarProducto(editar);
            Producto releido = catalogo.ObtenerPorSku("PRUEBA-MP-01");
            Verificador.Verificar(releido.CalidadAisi == "316L" && releido.EspesorMm == 2m, "La ficha tecnica editada se guarda");

            // 7. Orden de compra: un producto no comprable se rechaza, uno comprable se acepta.
            ComprasService compras = new ComprasService();
            Proveedor proveedor = compras.GuardarProveedor(new Proveedor
            {
                RazonSocial = "Proveedor de prueba S.A.",
                Cuit = ImportacionService.GenerarCuit("proveedor-prueba"),
                Activo = true
            });

            Producto noComprable = catalogo.ObtenerPorSku("MDR25-C50");
            if (noComprable != null && !noComprable.EsComprable)
            {
                Verificador.Falla<ReglaDeNegocioException>(
                    () => compras.CrearOrden(proveedor.Id, null, "Contado", "", new List<ItemCompra> { new ItemCompra { ProductoId = noComprable.Id, Cantidad = 1m, Costo = 10m } }, false),
                    "No se puede comprar un producto que no esta marcado como comprable");
            }

            OrdenCompra orden = compras.CrearOrden(proveedor.Id, null, "Contado", "", new List<ItemCompra> { new ItemCompra { ProductoId = guardado.Id, Cantidad = 5m, Costo = 4500m } }, true);
            Verificador.Verificar(orden != null && orden.Id != Guid.Empty, "Una materia prima comprable permite crear y emitir la orden");

            // 8. Producto inactivo: no se compra.
            catalogo.CambiarEstadoProducto(guardado.Id, false);
            Verificador.Falla<ReglaDeNegocioException>(
                () => compras.CrearOrden(proveedor.Id, null, "Contado", "", new List<ItemCompra> { new ItemCompra { ProductoId = guardado.Id, Cantidad = 1m, Costo = 1m } }, false),
                "Un producto inactivo no se puede comprar");
            catalogo.CambiarEstadoProducto(guardado.Id, true);

            // 9. Aumento masivo de precios.
            ProductoListaDto antes = catalogo.BuscarProductos("MDR25-C50", null, null, true, false).FirstOrDefault();
            if (antes != null)
            {
                int tocados = catalogo.AumentoMasivoPrecios(null, 10m, "Prueba automatica");
                Verificador.Verificar(tocados > 0, "El aumento masivo alcanza productos (" + tocados + ")");
                Producto despues = catalogo.ObtenerPorSku("MDR25-C50");
                Verificador.Verificar(Math.Abs(despues.PrecioBase - Math.Round(antes.PrecioBase * 1.10m, 2)) <= 0.02m, "El precio sube el 10% (" + antes.PrecioBase + " a " + despues.PrecioBase + ")");
                catalogo.AumentoMasivoPrecios(null, -9.0909090909m, "Prueba automatica: deshacer");
            }

            Verificador.Falla<ReglaDeNegocioException>(() => catalogo.AumentoMasivoPrecios(null, 0m, "x"), "Un aumento de 0% se rechaza");
            Verificador.Falla<ReglaDeNegocioException>(() => catalogo.AumentoMasivoPrecios(null, 900m, "x"), "Un aumento de 900% se rechaza");

            // 10. Sugerencias de reposicion: la materia prima nueva con stock cero aparece.
            List<SugerenciaCompraDto> sugerencias = compras.SugerirCompras();
            Verificador.Verificar(sugerencias.Any(s => s.Sku == "PRUEBA-MP-01"), "La materia prima sin stock aparece en las sugerencias de reposicion");
            Verificador.Verificar(new FaltantesService().Generales() != null, "La lista de productos faltantes se arma");
        }
    }
}
