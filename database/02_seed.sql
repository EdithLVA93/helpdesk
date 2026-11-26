/* =============================================================================
   HelpDesk - 02_seed.sql
   -----------------------------------------------------------------------------
   Carga datos de prueba para la demo y para ejecutar 03_queries.sql.
   Son datos de ejemplo identificados como tales (dominio reservado
   example.com); la aplicación funciona igual con la base vacía.

   Ejecución (después de 01_schema.sql):
       sqlcmd -S .\SQLEXPRESS -E -C -I -b -f 65001 -i database\02_seed.sql

   Es re-ejecutable: borra los datos existentes y vuelve a insertarlos.
   ========================================================================== */

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;   -- cualquier error revierte toda la transacción
GO

USE HelpDeskDb;
GO

BEGIN TRANSACTION;

DELETE FROM dbo.Tickets;
DELETE FROM dbo.Users;

/* -----------------------------------------------------------------------------
   Usuarios
   -------------------------------------------------------------------------- */
INSERT INTO dbo.Users (Name, Email)
VALUES
    (N'Ana Torres',       N'ana.torres@example.com'),
    (N'Carlos Méndez',    N'carlos.mendez@example.com'),
    (N'Lucía Ramírez',    N'lucia.ramirez@example.com'),
    (N'Jorge Castillo',   N'jorge.castillo@example.com'),
    (N'Valentina Rojas',  N'valentina.rojas@example.com');

/* -----------------------------------------------------------------------------
   Tickets
   - El usuario se resuelve por email: no depende de los Id generados.
   - CreatedAt se calcula hacia atrás desde la fecha actual (HoursAgo) para
     que el orden por fecha tenga sentido en cualquier momento.
   - Los tickets que ya no están en Open tienen UpdatedAt posterior a CreatedAt.
   -------------------------------------------------------------------------- */
DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

INSERT INTO dbo.Tickets (Title, Description, Status, AssignedUserId, CreatedAt, UpdatedAt, AdditionalData)
SELECT
    s.Title,
    s.Description,
    s.Status,
    u.Id,
    DATEADD(HOUR, -s.HoursAgo, @Now),
    CASE WHEN s.Status = N'Open' THEN NULL ELSE DATEADD(HOUR, -(s.HoursAgo / 2), @Now) END,
    s.AdditionalData
FROM
(
    VALUES
    (N'Servidor de archivos sin respuesta',
     N'El recurso compartido \\fs01\finanzas no responde desde las 8:00.',
     N'InProgress', N'ana.torres@example.com', 150,
     N'{"priority":"Critical","category":"Infrastructure","estimatedResolutionDate":"2026-11-27","tags":["network","server"],"metadata":{"department":"Finance","source":"Phone"}}'),

    (N'VPN desconecta cada 10 minutos',
     N'Varios comerciales reportan cortes de VPN trabajando desde casa.',
     N'Open', N'carlos.mendez@example.com', 26,
     N'{"priority":"High","category":"Network","estimatedResolutionDate":"2026-11-28","tags":["vpn","network"],"metadata":{"department":"Sales","source":"Email"}}'),

    (N'Solicitud de acceso a Power BI',
     N'Acceso de lectura al espacio de trabajo de reportes financieros.',
     N'Resolved', N'lucia.ramirez@example.com', 290,
     N'{"priority":"Low","category":"Access","estimatedResolutionDate":"2026-11-16","tags":["access","reporting"],"metadata":{"department":"Finance","source":"Portal"}}'),

    (N'Impresora del piso 3 atasca papel',
     NULL,
     N'Open', N'jorge.castillo@example.com', 50,
     N'{"priority":"Medium","category":"Hardware","estimatedResolutionDate":"2026-11-30","tags":["printer"],"metadata":{"department":"Operations","source":"Portal"}}'),

    (N'Error al generar facturas en el ERP',
     N'El proceso de facturación termina con el error FAC-102.',
     N'InProgress', N'valentina.rojas@example.com', 98,
     N'{"priority":"High","category":"Software","estimatedResolutionDate":"2026-11-26","tags":["erp","billing"],"metadata":{"department":"Finance","source":"Email"}}'),

    (N'Alta de correo para nuevo colaborador',
     N'Crear buzón y grupos de distribución para el ingreso del lunes.',
     N'Resolved', N'ana.torres@example.com', 360,
     N'{"priority":"Medium","category":"Access","estimatedResolutionDate":"2026-11-12","tags":["email","onboarding"],"metadata":{"department":"HR","source":"Portal"}}'),

    (N'Lentitud en la base de datos de reportes',
     N'Las consultas del cierre mensual tardan más de 5 minutos.',
     N'Open', N'ana.torres@example.com', 3,
     N'{"priority":"High","category":"Infrastructure","estimatedResolutionDate":"2026-11-29","tags":["database","performance","server"],"metadata":{"department":"IT","source":"Internal"}}'),

    (N'Portátil no enciende',
     N'Equipo con 3 años de uso; no muestra luces al conectar el cargador.',
     N'InProgress', N'jorge.castillo@example.com', 74,
     N'{"priority":"Medium","category":"Hardware","estimatedResolutionDate":"2026-12-02","tags":["laptop"],"metadata":{"department":"Marketing","source":"Phone"}}'),

    (N'Restablecer contraseña de SAP',
     NULL,
     N'Resolved', N'carlos.mendez@example.com', 220,
     N'{"priority":"Low","category":"Access","estimatedResolutionDate":"2026-11-17","tags":["password"],"metadata":{"department":"Logistics","source":"Phone"}}'),

    (N'Wi-Fi intermitente en la sala de juntas',
     N'La señal se pierde durante las videollamadas.',
     N'Open', N'lucia.ramirez@example.com', 20,
     N'{"priority":"Medium","category":"Network","estimatedResolutionDate":"2026-12-01","tags":["wifi","network"],"metadata":{"department":"Management","source":"Email"}}'),

    (N'Actualizar antivirus en equipos de RR. HH.',
     N'Cinco equipos con firmas de antivirus desactualizadas.',
     N'Open', N'valentina.rojas@example.com', 122,
     N'{"priority":"Low","category":"Security","estimatedResolutionDate":"2026-12-05","tags":["antivirus","maintenance"],"metadata":{"department":"HR","source":"Internal"}}'),

    (N'Copia de seguridad nocturna fallida',
     N'El job de backup de las 2:00 terminó con error de espacio en disco.',
     N'InProgress', N'ana.torres@example.com', 40,
     N'{"priority":"Critical","category":"Infrastructure","estimatedResolutionDate":"2026-11-26","tags":["backup","server"],"metadata":{"department":"IT","source":"Internal"}}')
) AS s (Title, Description, Status, AssignedEmail, HoursAgo, AdditionalData)
INNER JOIN dbo.Users AS u
    ON u.Email = s.AssignedEmail;

COMMIT TRANSACTION;
GO

SELECT
    (SELECT COUNT(*) FROM dbo.Users)   AS UsersLoaded,
    (SELECT COUNT(*) FROM dbo.Tickets) AS TicketsLoaded;
GO
