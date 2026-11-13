using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Edelstahl.DAL.Tools;

namespace Edelstahl.DAL.EntityFramework
{
    /// <summary>
    /// Crea las bases de datos y las tablas que falten, sin modificar las
    /// que ya existen ni sus datos.
    ///
    /// Las tablas se obtienen del modelo de Entity Framework, de modo que
    /// el esquema siempre coincide con las clases del dominio. Las
    /// referencias entre módulos (claves foráneas) y los índices se
    /// declaran en este archivo.
    /// </summary>
    internal static class EsquemaNegocio
    {
        /// <summary>Tabla, columna y tabla referenciada.</summary>
        private static readonly string[][] Referencias =
        {
            new[] { "Productos", "CategoriaId", "CategoriasProducto" },
            new[] { "StockProductos", "ProductoId", "Productos" },
            new[] { "StockProductos", "UbicacionId", "UbicacionesDeposito" },
            new[] { "MovimientosStock", "ProductoId", "Productos" },
            new[] { "MovimientosStock", "UbicacionId", "UbicacionesDeposito" },
            new[] { "ReservasStock", "ProductoId", "Productos" },
            new[] { "ReservasStock", "UbicacionId", "UbicacionesDeposito" },
            new[] { "ReservasStock", "OrdenFabricacionId", "OrdenesFabricacion" },
            new[] { "OrdenesFabricacion", "ProductoId", "Productos" },
            new[] { "OrdenesFabricacion", "PedidoId", "Pedidos" },
            new[] { "ConsumosMaterial", "ProductoMaterialId", "Productos" },
            new[] { "OrdenesCompra", "ProveedorId", "Proveedores" },
            new[] { "DetallesOrdenCompra", "ProductoId", "Productos" },
            new[] { "RecepcionesCompra", "OrdenCompraId", "OrdenesCompra" },
            new[] { "DetallesRecepcionCompra", "DetalleOrdenCompraId", "DetallesOrdenCompra" },
            new[] { "DetallesRecepcionCompra", "ProductoId", "Productos" },
            new[] { "Facturas", "PedidoId", "Pedidos" },
            new[] { "Facturas", "ClienteId", "Clientes" },
            new[] { "CobrosCliente", "ClienteId", "Clientes" },
            new[] { "CobrosCliente", "FacturaId", "Facturas" },
            new[] { "CobrosCliente", "MedioPagoId", "MediosPago" },
            new[] { "PagosProveedor", "ProveedorId", "Proveedores" },
            new[] { "PagosProveedor", "OrdenCompraId", "OrdenesCompra" },
            new[] { "PagosProveedor", "MedioPagoId", "MediosPago" },
            new[] { "MovimientosCuentaCliente", "ClienteId", "Clientes" },
            new[] { "MovimientosCuentaCliente", "FacturaId", "Facturas" },
            new[] { "MovimientosCuentaCliente", "CobroId", "CobrosCliente" },
            new[] { "MovimientosCuentaCliente", "NotaCreditoId", "NotasCredito" },
            new[] { "MovimientosCuentaProveedor", "ProveedorId", "Proveedores" },
            new[] { "MovimientosCuentaProveedor", "OrdenCompraId", "OrdenesCompra" },
            new[] { "MovimientosCuentaProveedor", "PagoId", "PagosProveedor" },
            new[] { "NotasCredito", "FacturaId", "Facturas" },
            new[] { "NotasCredito", "ClienteId", "Clientes" },
            new[] { "NotasCredito", "DevolucionId", "Devoluciones" },
            new[] { "Remitos", "PedidoId", "Pedidos" },
            new[] { "Remitos", "ClienteId", "Clientes" },
            new[] { "HojasRuta", "VehiculoId", "Vehiculos" },
            new[] { "HojasRuta", "ChoferId", "Choferes" },
            new[] { "DetallesHojaRuta", "RemitoId", "Remitos" },
            new[] { "ComprobantesEntrega", "DetalleHojaRutaId", "DetallesHojaRuta" },
            new[] { "Devoluciones", "RemitoId", "Remitos" },
            new[] { "Devoluciones", "PedidoId", "Pedidos" },
            new[] { "Devoluciones", "ClienteId", "Clientes" }
        };

        /// <summary>Tabla, columnas separadas por coma y si es único.</summary>
        private static readonly string[][] Indices =
        {
            new[] { "Productos", "CodigoSku", "U" },
            new[] { "CategoriasProducto", "Codigo", "U" },
            new[] { "Depositos", "Codigo", "U" },
            new[] { "UbicacionesDeposito", "DepositoId,Codigo", "U" },
            new[] { "StockProductos", "ProductoId,UbicacionId", "U" },
            new[] { "OrdenesFabricacion", "Numero", "U" },
            new[] { "Proveedores", "Cuit", "U" },
            new[] { "OrdenesCompra", "Numero", "U" },
            new[] { "RecepcionesCompra", "Numero", "U" },
            new[] { "Facturas", "Numero", "U" },
            new[] { "MediosPago", "Codigo", "U" },
            new[] { "CobrosCliente", "Numero", "U" },
            new[] { "PagosProveedor", "Numero", "U" },
            new[] { "NotasCredito", "Numero", "U" },
            new[] { "Vehiculos", "Patente", "U" },
            new[] { "Choferes", "Documento", "U" },
            new[] { "Choferes", "LicenciaConducir", "U" },
            new[] { "Remitos", "Numero", "U" },
            new[] { "HojasRuta", "Numero", "U" },
            new[] { "Devoluciones", "Numero", "U" },
            new[] { "MovimientosStock", "ProductoId,FechaMovimiento", "N" },
            new[] { "ReservasStock", "ProductoId", "N" },
            new[] { "ReservasStock", "DetallePedidoId", "N" },
            new[] { "OrdenesFabricacion", "DetallePedidoId", "N" },
            new[] { "OrdenesCompra", "ProveedorId", "N" },
            new[] { "RecepcionesCompra", "OrdenCompraId", "N" },
            new[] { "Facturas", "PedidoId", "N" },
            new[] { "Facturas", "ClienteId", "N" },
            new[] { "CobrosCliente", "ClienteId", "N" },
            new[] { "PagosProveedor", "ProveedorId", "N" },
            new[] { "MovimientosCuentaCliente", "ClienteId,Fecha", "N" },
            new[] { "MovimientosCuentaProveedor", "ProveedorId,Fecha", "N" },
            new[] { "NotasCredito", "FacturaId", "N" },
            new[] { "Remitos", "PedidoId", "N" },
            new[] { "DetallesRemito", "DetallePedidoId", "N" },
            new[] { "DetallesHojaRuta", "RemitoId", "N" },
            new[] { "ComprobantesEntrega", "DetalleHojaRutaId", "N" },
            new[] { "Devoluciones", "RemitoId", "N" },
            new[] { "DetallesDevolucion", "DetallePedidoId", "N" }
        };

        /// <summary>
        /// Crea la base si no existe y agrega las tablas, referencias e
        /// índices que falten.
        /// </summary>
        public static void Instalar()
        {
            AsegurarBase(SqlServerConnection.NombreBaseNegocio);

            List<string> sentencias = ObtenerSentenciasModelo();

            SqlHelper.EnTransaccion(BaseDeDatos.Negocio, transaccion =>
            {
                HashSet<string> creadas = CrearTablasFaltantes(transaccion, sentencias);
                AgregarColumnasFaltantes(transaccion, sentencias, creadas);
                CrearReferenciasFaltantes(transaccion);
                CrearIndicesFaltantes(transaccion);
            });
        }

        /// <summary>
        /// Devuelve el script completo (tablas, referencias e índices) para
        /// documentación o para crear la base manualmente.
        /// </summary>
        public static string GenerarScript()
        {
            StringBuilder texto = new StringBuilder();
            texto.AppendLine("-- Edelstahl ERP - base de negocio");
            texto.AppendLine("-- Script generado a partir del modelo de datos.");
            texto.AppendLine();

            foreach (string sentencia in ObtenerSentenciasModelo())
            {
                texto.AppendLine(sentencia + ";");
                texto.AppendLine("GO");
            }

            foreach (string[] referencia in Referencias)
            {
                texto.AppendLine(SentenciaReferencia(referencia) + ";");
                texto.AppendLine("GO");
            }

            foreach (string[] indice in Indices)
            {
                texto.AppendLine(SentenciaIndice(indice) + ";");
                texto.AppendLine("GO");
            }

            return texto.ToString();
        }

        private static List<string> ObtenerSentenciasModelo()
        {
            string script;

            using (EdelstahlNegocioContext contexto = new EdelstahlNegocioContext())
            {
                script = ((IObjectContextAdapter)contexto).ObjectContext.CreateDatabaseScript();
            }

            List<string> sentencias = new List<string>();

            foreach (string trozo in Regex.Split(script, @";\s*\r?\n"))
            {
                string limpia = trozo.Trim().TrimEnd(';');

                if (limpia.Length > 0)
                {
                    sentencias.Add(limpia);
                }
            }

            return sentencias;
        }

        private static HashSet<string> CrearTablasFaltantes(
            SqlHelper.Transaccion transaccion,
            List<string> sentencias)
        {
            HashSet<string> creadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string sentencia in sentencias)
            {
                Match tabla = Regex.Match(
                    sentencia,
                    @"^create table \[dbo\]\.\[(?<t>[^\]]+)\]",
                    RegexOptions.IgnoreCase);

                if (!tabla.Success)
                {
                    continue;
                }

                string nombre = tabla.Groups["t"].Value;

                if (!ExisteTabla(transaccion, nombre))
                {
                    Ejecutar(transaccion, sentencia);
                    creadas.Add(nombre);
                }
            }

            // Las relaciones padre-hijo de EF solo se agregan a tablas nuevas.
            foreach (string sentencia in sentencias)
            {
                Match relacion = Regex.Match(
                    sentencia,
                    @"^alter table \[dbo\]\.\[(?<t>[^\]]+)\] add constraint \[(?<c>[^\]]+)\]",
                    RegexOptions.IgnoreCase);

                if (relacion.Success && creadas.Contains(relacion.Groups["t"].Value))
                {
                    Ejecutar(transaccion, sentencia);
                }
            }

            return creadas;
        }

        /// <summary>
        /// Las tablas que ya existian (por ejemplo la de clientes de una version
        /// anterior) reciben las columnas nuevas del modelo. Las columnas
        /// obligatorias se agregan con un valor inicial para no fallar si ya
        /// hay filas.
        /// </summary>
        private static void AgregarColumnasFaltantes(
            SqlHelper.Transaccion transaccion,
            List<string> sentencias,
            HashSet<string> creadas)
        {
            foreach (string sentencia in sentencias)
            {
                Match tabla = Regex.Match(
                    sentencia,
                    @"^create table \[dbo\]\.\[(?<t>[^\]]+)\]",
                    RegexOptions.IgnoreCase);

                if (!tabla.Success || creadas.Contains(tabla.Groups["t"].Value))
                {
                    continue;
                }

                string nombre = tabla.Groups["t"].Value;
                HashSet<string> existentes = ObtenerColumnas(transaccion, nombre);

                foreach (Match columna in Regex.Matches(
                    sentencia,
                    @"^\s*\[(?<c>[^\]]+)\]\s+(?<tipo>\[[a-z0-9]+\](?:\s*\([^)]*\))?)\s*(?<nulo>not null|null)?",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    string nombreColumna = columna.Groups["c"].Value;

                    if (existentes.Contains(nombreColumna))
                    {
                        continue;
                    }

                    string tipo = columna.Groups["tipo"].Value.Trim();
                    bool obligatoria = columna.Groups["nulo"].Value.Trim().Equals("not null", StringComparison.OrdinalIgnoreCase);

                    string sql = "ALTER TABLE [dbo].[" + nombre + "] ADD [" + nombreColumna + "] " + tipo;

                    if (obligatoria)
                    {
                        sql += " NOT NULL DEFAULT (" + ValorInicial(tipo) + ")";
                    }
                    else
                    {
                        sql += " NULL";
                    }

                    Ejecutar(transaccion, sql);
                }
            }
        }

        private static string ValorInicial(string tipo)
        {
            string t = tipo.ToLowerInvariant().Replace("[", string.Empty).Replace("]", string.Empty);

            if (t.StartsWith("uniqueidentifier"))
            {
                return "'00000000-0000-0000-0000-000000000000'";
            }

            if (t.Contains("char") || t.StartsWith("xml"))
            {
                return "''";
            }

            if (t.StartsWith("datetime") || t.StartsWith("date") || t.StartsWith("smalldatetime"))
            {
                return "'19000101'";
            }

            return "0";
        }

        private static HashSet<string> ObtenerColumnas(SqlHelper.Transaccion transaccion, string tabla)
        {
            return new HashSet<string>(
                transaccion.Consultar(
                    "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @t",
                    fila => fila.GetString(0),
                    SqlHelper.Parametro("@t", SqlDbType.NVarChar, 128, tabla)),
                StringComparer.OrdinalIgnoreCase);
        }

        private static string NombreReferencia(string[] referencia)
        {
            return "FK_" + referencia[0] + "_" + referencia[1];
        }

        private static string SentenciaReferencia(string[] referencia)
        {
            return "ALTER TABLE [dbo].[" + referencia[0] + "] WITH CHECK ADD CONSTRAINT ["
                + NombreReferencia(referencia) + "] FOREIGN KEY ([" + referencia[1]
                + "]) REFERENCES [dbo].[" + referencia[2] + "]([Id])";
        }

        private static string NombreIndice(string[] indice)
        {
            string prefijo = indice[2] == "U" ? "UQ_" : "IX_";
            return prefijo + indice[0] + "_" + indice[1].Replace(",", "_");
        }

        private static string SentenciaIndice(string[] indice)
        {
            string[] columnas = indice[1].Split(',');
            string lista = "[" + string.Join("], [", columnas) + "]";

            return "CREATE " + (indice[2] == "U" ? "UNIQUE " : string.Empty) + "INDEX ["
                + NombreIndice(indice) + "] ON [dbo].[" + indice[0] + "] (" + lista + ")";
        }

        private static void CrearReferenciasFaltantes(SqlHelper.Transaccion transaccion)
        {
            foreach (string[] referencia in Referencias)
            {
                if (!ExisteTabla(transaccion, referencia[0])
                    || !ExisteTabla(transaccion, referencia[2]))
                {
                    continue;
                }

                if (!ExisteRestriccion(transaccion, NombreReferencia(referencia)))
                {
                    Ejecutar(transaccion, SentenciaReferencia(referencia));
                }
            }
        }

        private static void CrearIndicesFaltantes(SqlHelper.Transaccion transaccion)
        {
            foreach (string[] indice in Indices)
            {
                if (!ExisteTabla(transaccion, indice[0]))
                {
                    continue;
                }

                if (!ExisteIndice(transaccion, indice[0], NombreIndice(indice)))
                {
                    Ejecutar(transaccion, SentenciaIndice(indice));
                }
            }
        }

        private static bool ExisteTabla(SqlHelper.Transaccion transaccion, string tabla)
        {
            return transaccion.EjecutarEscalar<int>(
                "SELECT COUNT(*) FROM sys.tables WHERE name = @nombre AND schema_id = SCHEMA_ID('dbo')",
                SqlHelper.Parametro("@nombre", SqlDbType.NVarChar, 128, tabla)) > 0;
        }

        private static bool ExisteRestriccion(SqlHelper.Transaccion transaccion, string nombre)
        {
            return transaccion.EjecutarEscalar<int>(
                "SELECT COUNT(*) FROM sys.foreign_keys WHERE name = @nombre",
                SqlHelper.Parametro("@nombre", SqlDbType.NVarChar, 128, nombre)) > 0;
        }

        private static bool ExisteIndice(SqlHelper.Transaccion transaccion, string tabla, string nombre)
        {
            return transaccion.EjecutarEscalar<int>(
                "SELECT COUNT(*) FROM sys.indexes WHERE name = @nombre AND object_id = OBJECT_ID(@tabla)",
                SqlHelper.Parametro("@nombre", SqlDbType.NVarChar, 128, nombre),
                SqlHelper.Parametro("@tabla", SqlDbType.NVarChar, 300, "dbo." + tabla)) > 0;
        }

        private static void Ejecutar(SqlHelper.Transaccion transaccion, string sql)
        {
            transaccion.EjecutarNoQuery(sql, 120);
        }

        /// <summary>
        /// Crea la base de datos indicada si todavía no existe.
        /// </summary>
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
