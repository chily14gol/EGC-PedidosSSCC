-- Fecha y usuario comunes
DECLARE @Fecha DATETIME = GETDATE();
DECLARE @Usuario INT = 1

INSERT INTO dbo.Seguridad_Opciones (SOP_Id, SOP_Nombre, SOP_SOP_Id_Padre, FechaAlta, FechaModificacion, PER_Id_Modificacion)
VALUES
('1',     'Facturación',                   NULL,       @Fecha, @Fecha, @Usuario),
('1.1',   'Tareas',                        '1',         @Fecha, @Fecha, @Usuario),
('1.1.1', 'Datos generales',              '1.1',       @Fecha, @Fecha, @Usuario),
('1.1.2', 'Edición presupuestos',         '1.1',       @Fecha, @Fecha, @Usuario),
('1.2',   'Conceptos de facturación',     '1',         @Fecha, @Fecha, @Usuario),
('1.3',   'Pedidos',                       '1',         @Fecha, @Fecha, @Usuario),
('1.4',   'Enlaces contables',            '1',         @Fecha, @Fecha, @Usuario),
('1.5',   'Licencias Microsoft',          '1',         @Fecha, @Fecha, @Usuario),
('1.5.1', 'Entidades',                    '1.5',       @Fecha, @Fecha, @Usuario),
('1.6',   'CAU y soporte',                '1',         @Fecha, @Fecha, @Usuario),
('1.6.1', 'Contratos CAU',                '1.6',       @Fecha, @Fecha, @Usuario),
('1.6.2', 'Tickets',                      '1.6',       @Fecha, @Fecha, @Usuario),
('1.7',   'Soporte proveedores',          '1',         @Fecha, @Fecha, @Usuario),
('1.7.1', 'Asuntos',                      '1.7',       @Fecha, @Fecha, @Usuario),
('2',     'Mantenimientos',               NULL,        @Fecha, @Fecha, @Usuario),
('2.1',   'Seguridad',                    '2',         @Fecha, @Fecha, @Usuario),
('2.1.1', 'Usuarios',                     '2.1',       @Fecha, @Fecha, @Usuario),
('2.1.2', 'Perfiles',                     '2.1',       @Fecha, @Fecha, @Usuario),
('2.2',   'General',                 '2',    @Fecha, @Fecha, @Usuario),
('2.2.1', 'Configuración',           '2.2',  @Fecha, @Fecha, @Usuario),
('2.2.2', 'Productos D365',          '2.2',  @Fecha, @Fecha, @Usuario),
('2.2.3', 'Item numbers D365',       '2.2',  @Fecha, @Fecha, @Usuario),
('2.2.4', 'Empresas',                '2.2',  @Fecha, @Fecha, @Usuario),
('2.2.5', 'Departamentos',           '2.2',  @Fecha, @Fecha, @Usuario),
('2.3',   'Licencias Microsoft',     '2',    @Fecha, @Fecha, @Usuario),
('2.3.1', 'Tipos entidad',           '2.3',  @Fecha, @Fecha, @Usuario),
('2.3.2', 'Oficinas',                '2.3',  @Fecha, @Fecha, @Usuario),
('1.5.2', 'Licencias',               '1.5',  @Fecha, @Fecha, @Usuario),
('2.4',   'CAU y soporte',           '2',    @Fecha, @Fecha, @Usuario),
('2.4.1', 'Estados ticket',          '2.4',  @Fecha, @Fecha, @Usuario),
('2.4.2', 'Orígenes ticket',         '2.4',  @Fecha, @Fecha, @Usuario),
('2.4.3', 'Tipos ticket',            '2.4',  @Fecha, @Fecha, @Usuario),
('2.4.4', 'Validaciones ticket',     '2.4',  @Fecha, @Fecha, @Usuario),
('2.4.5', 'Grupos de guardia',       '2.4',  @Fecha, @Fecha, @Usuario),
('2.5',   'Soporte proveedores',     '2',    @Fecha, @Fecha, @Usuario),
('2.5.1', 'Proveedores',             '2.5',  @Fecha, @Fecha, @Usuario)

