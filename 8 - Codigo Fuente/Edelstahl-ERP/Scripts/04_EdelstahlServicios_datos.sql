-- Edelstahl ERP - datos de la base EdelstahlServicios
-- Ejecutar DESPUES del script de esquema, sobre la base ya creada y vacia.
USE EdelstahlServicios;
GO
SET NOCOUNT ON;
GO
-- Roles (7 filas)
INSERT INTO [dbo].[Roles] ([Id], [Nombre], [Descripcion], [Activo], [FechaAlta]) VALUES ('0285E4F4-F543-4B0C-85A5-064648BD0DA4', N'Responsable de Depósito', N'Inventario, reservas y movimientos de stock.', 1, '2026-10-20T23:17:00.1735647');
INSERT INTO [dbo].[Roles] ([Id], [Nombre], [Descripcion], [Activo], [FechaAlta]) VALUES ('D54AE1C0-B833-4A7B-B3F8-0B2422D5F04B', N'Responsable de Logística', N'Remitos, hojas de ruta, entregas y devoluciones.', 1, '2026-10-20T23:17:00.1746338');
INSERT INTO [dbo].[Roles] ([Id], [Nombre], [Descripcion], [Activo], [FechaAlta]) VALUES ('591C4E95-1DB5-4CC7-8AF2-365DFD2F7E24', N'Administrador', N'Acceso completo al sistema', 1, '2026-09-13T17:41:17.8828631');
INSERT INTO [dbo].[Roles] ([Id], [Nombre], [Descripcion], [Activo], [FechaAlta]) VALUES ('4F41FC39-6CCF-4B43-970B-76B3DB91BF45', N'Supervisor', N'Consulta, supervisión y aprobación de operaciones', 1, '2026-09-13T17:41:17.8888522');
INSERT INTO [dbo].[Roles] ([Id], [Nombre], [Descripcion], [Activo], [FechaAlta]) VALUES ('E638E992-348F-43DE-8578-816C545FE569', N'Responsable de Producción', N'Órdenes de fabricación y control de calidad.', 1, '2026-10-20T23:17:00.1710624');
INSERT INTO [dbo].[Roles] ([Id], [Nombre], [Descripcion], [Activo], [FechaAlta]) VALUES ('4041733E-AA19-4176-8657-9CAE8D8E0754', N'Responsable de Compras', N'Proveedores, órdenes de compra y recepciones.', 1, '2026-10-20T23:17:00.1735647');
INSERT INTO [dbo].[Roles] ([Id], [Nombre], [Descripcion], [Activo], [FechaAlta]) VALUES ('490F4F17-7D5A-4BA0-92A4-E57BB0EBCF46', N'Vendedor', N'Gestión de clientes, presupuestos y pedidos', 1, '2026-09-13T17:41:17.8878477');
GO
