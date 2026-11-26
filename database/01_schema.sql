/* =============================================================================
   HelpDesk - 01_schema.sql
   -----------------------------------------------------------------------------
   Crea la base de datos HelpDeskDb, sus tablas, relaciones, restricciones e
   índices. Es la ÚNICA fuente de verdad del esquema: EF Core lo mapea sin
   migraciones.

   Ejecución (desde la raíz del repositorio):
       sqlcmd -S .\SQLEXPRESS -E -C -I -b -f 65001 -i database\01_schema.sql

   -I        activa QUOTED_IDENTIFIER (sqlcmd lo desactiva por defecto y es
             obligatorio para los índices sobre columnas calculadas).
   -b        detiene la ejecución ante el primer error.
   -f 65001  lee el archivo como UTF-8 (tildes y eñes correctas).

   ATENCIÓN: el script es re-ejecutable. Si las tablas ya existen, las elimina
   y las vuelve a crear, por lo que se pierden los datos.
   ========================================================================== */

-- Opciones requeridas para crear índices sobre columnas calculadas y para
-- insertar o actualizar filas de tablas que los tienen.
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
GO

USE master;
GO

IF DB_ID(N'HelpDeskDb') IS NULL
BEGIN
    CREATE DATABASE HelpDeskDb;
    PRINT 'Base de datos HelpDeskDb creada.';
END
GO

USE HelpDeskDb;
GO

/* -----------------------------------------------------------------------------
   Limpieza para re-ejecución (orden inverso a las dependencias)
   -------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Tickets', N'U') IS NOT NULL DROP TABLE dbo.Tickets;
IF OBJECT_ID(N'dbo.Users',   N'U') IS NOT NULL DROP TABLE dbo.Users;
GO

/* -----------------------------------------------------------------------------
   Tabla dbo.Users
   - Email único: la intercalación de la base (CI = case-insensitive) hace que
     'Ana@x.com' y 'ana@x.com' se consideren el mismo correo.
   - El CHECK de formato es una barrera mínima; la validación completa del
     correo se hace en la API.
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.Users
(
    Id        INT IDENTITY(1, 1) NOT NULL,
    Name      NVARCHAR(100)      NOT NULL,
    Email     NVARCHAR(254)      NOT NULL,
    CreatedAt DATETIME2(3)       NOT NULL
        CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UX_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Name_NotBlank CHECK (LEN(TRIM(Name)) > 0),
    CONSTRAINT CK_Users_Email_Format  CHECK (Email LIKE N'%_@_%._%' AND Email NOT LIKE N'% %')
);
GO

/* -----------------------------------------------------------------------------
   Tabla dbo.Tickets
   - Status se guarda como texto legible, restringido por CHECK.
   - AdditionalData guarda información adicional en JSON. El CHECK con ISJSON
     (tipo OBJECT, disponible desde SQL Server 2022) garantiza que siempre es
     un objeto JSON válido, aunque se escriba sin pasar por la API.
   - Priority y Category son columnas calculadas con JSON_VALUE: no ocupan
     espacio (no son PERSISTED), pero se pueden indexar porque JSON_VALUE es
     determinista. Así el filtro por un valor del JSON usa un índice.
   - ON DELETE NO ACTION: no se puede borrar un usuario con tickets asignados.
   -------------------------------------------------------------------------- */
CREATE TABLE dbo.Tickets
(
    Id             INT IDENTITY(1, 1) NOT NULL,
    Title          NVARCHAR(150)      NOT NULL,
    Description    NVARCHAR(2000)     NULL,
    Status         NVARCHAR(20)       NOT NULL
        CONSTRAINT DF_Tickets_Status DEFAULT (N'Open'),
    AssignedUserId INT                NOT NULL,
    CreatedAt      DATETIME2(3)       NOT NULL
        CONSTRAINT DF_Tickets_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt      DATETIME2(3)       NULL,
    AdditionalData NVARCHAR(MAX)      NOT NULL
        CONSTRAINT DF_Tickets_AdditionalData DEFAULT (N'{}'),

    -- Columnas calculadas a partir del JSON
    Priority AS CAST(JSON_VALUE(AdditionalData, N'$.priority') AS NVARCHAR(20)),
    Category AS CAST(JSON_VALUE(AdditionalData, N'$.category') AS NVARCHAR(50)),

    CONSTRAINT PK_Tickets PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Tickets_Users_AssignedUserId
        FOREIGN KEY (AssignedUserId) REFERENCES dbo.Users (Id)
        ON DELETE NO ACTION
        ON UPDATE NO ACTION,
    CONSTRAINT CK_Tickets_Title_NotBlank CHECK (LEN(TRIM(Title)) > 0),
    CONSTRAINT CK_Tickets_Status CHECK (Status IN (N'Open', N'InProgress', N'Resolved')),
    CONSTRAINT CK_Tickets_AdditionalData_IsJson CHECK (ISJSON(AdditionalData, OBJECT) = 1),
    -- Un CHECK no puede usar una columna calculada no PERSISTED, por eso se
    -- repite aquí la expresión JSON_VALUE en lugar de referenciar Priority.
    CONSTRAINT CK_Tickets_Priority CHECK (
        JSON_VALUE(AdditionalData, N'$.priority') IS NULL
        OR JSON_VALUE(AdditionalData, N'$.priority') IN (N'Low', N'Medium', N'High', N'Critical')
    ),
    CONSTRAINT CK_Tickets_UpdatedAt CHECK (UpdatedAt IS NULL OR UpdatedAt >= CreatedAt)
);
GO

/* -----------------------------------------------------------------------------
   Índices
   Cada uno responde a una consulta concreta de la API:
   - IX_Tickets_CreatedAt: listado general ordenado por fecha (dashboard).
   - IX_Tickets_Status_CreatedAt: filtro por estado ordenado por fecha.
   - IX_Tickets_AssignedUserId_Status_CreatedAt: tickets por usuario, solo o
     combinado con estado. También da soporte a la FK (SQL Server no indexa las
     FK automáticamente).
   - IX_Tickets_Priority / IX_Tickets_Category: filtros sobre valores del JSON.
   -------------------------------------------------------------------------- */
CREATE NONCLUSTERED INDEX IX_Tickets_CreatedAt
    ON dbo.Tickets (CreatedAt DESC);

CREATE NONCLUSTERED INDEX IX_Tickets_Status_CreatedAt
    ON dbo.Tickets (Status, CreatedAt DESC)
    INCLUDE (Title, AssignedUserId);

CREATE NONCLUSTERED INDEX IX_Tickets_AssignedUserId_Status_CreatedAt
    ON dbo.Tickets (AssignedUserId, Status, CreatedAt DESC);

CREATE NONCLUSTERED INDEX IX_Tickets_Priority
    ON dbo.Tickets (Priority);

CREATE NONCLUSTERED INDEX IX_Tickets_Category
    ON dbo.Tickets (Category);
GO

PRINT 'Esquema de HelpDeskDb creado correctamente.';
GO
