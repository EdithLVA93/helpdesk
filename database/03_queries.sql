/* =============================================================================
   HelpDesk - 03_queries.sql
   -----------------------------------------------------------------------------
   Consultas de demostración, todas ejecutables, sobre los datos de 02_seed.sql.

   PARTE A - Consultas relacionales
     A1. Tickets por usuario
     A2. Filtrar por estado
     A3. Ordenar por fecha de creación
     A4. Consulta combinada con filtros opcionales y paginación
     A5. Conteo por estado (tarjetas de indicadores)
     A6. Verificación de integridad (FK, CHECK, UNIQUE)

   PARTE B - JSON nativo en SQL Server
     B1. ISJSON       - validar JSON
     B2. JSON_VALUE   - leer valores escalares y filtrar por ellos
     B3. JSON_QUERY   - leer objetos y arreglos
     B4. OPENJSON     - convertir JSON en filas
     B5. JSON_MODIFY  - actualizar una propiedad sin reescribir el documento
     B6. Consulta combinada relacional + JSON

   Ejecución:
       sqlcmd -S .\SQLEXPRESS -E -C -I -f 65001 -i database\03_queries.sql

   El script no deja cambios: todo lo que escribe se hace dentro de
   transacciones que terminan en ROLLBACK.
   ========================================================================== */

SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
GO

USE HelpDeskDb;
GO

/* =============================================================================
   PARTE A - CONSULTAS RELACIONALES
   ========================================================================== */

/* A1. Tickets por usuario ---------------------------------------------------
   Usa IX_Tickets_AssignedUserId_Status_CreatedAt (búsqueda por AssignedUserId).
   El JOIN trae el nombre del responsable, como lo muestra la API.            */
DECLARE @UserId INT = (SELECT Id FROM dbo.Users WHERE Email = N'ana.torres@example.com');

SELECT
    t.Id,
    t.Title,
    t.Status,
    u.Name AS AssignedUserName,
    t.CreatedAt
FROM dbo.Tickets AS t
INNER JOIN dbo.Users AS u ON u.Id = t.AssignedUserId
WHERE t.AssignedUserId = @UserId
ORDER BY t.CreatedAt DESC;
GO

/* A2. Filtrar por estado ----------------------------------------------------
   Usa IX_Tickets_Status_CreatedAt.                                           */
SELECT t.Id, t.Title, t.Status, t.CreatedAt
FROM dbo.Tickets AS t
WHERE t.Status = N'Open'
ORDER BY t.CreatedAt DESC;
GO

/* A3. Ordenar por fecha de creación -----------------------------------------
   Listado general del dashboard: los más recientes primero.
   Usa IX_Tickets_CreatedAt.                                                  */
SELECT t.Id, t.Title, t.Status, t.CreatedAt
FROM dbo.Tickets AS t
ORDER BY t.CreatedAt DESC;
GO

/* A4. Consulta combinada: usuario + estado + orden + paginación ------------
   Es la forma de la consulta que genera la API (GET /api/tickets).
   Cada filtro es opcional: si el parámetro es NULL, no filtra.
   OPTION (RECOMPILE) permite que el optimizador elija el mejor índice para
   la combinación concreta de filtros recibida, en vez de reutilizar un plan
   pensado para otra combinación.                                             */
DECLARE @FilterUserId INT          = (SELECT Id FROM dbo.Users WHERE Email = N'ana.torres@example.com');
DECLARE @FilterStatus NVARCHAR(20) = N'InProgress';
DECLARE @PageNumber   INT          = 1;
DECLARE @PageSize     INT          = 10;

SELECT
    t.Id,
    t.Title,
    t.Status,
    u.Name AS AssignedUserName,
    t.CreatedAt,
    COUNT(*) OVER () AS TotalCount     -- total sin paginar, para el paginador
FROM dbo.Tickets AS t
INNER JOIN dbo.Users AS u ON u.Id = t.AssignedUserId
WHERE (@FilterUserId IS NULL OR t.AssignedUserId = @FilterUserId)
  AND (@FilterStatus IS NULL OR t.Status = @FilterStatus)
ORDER BY t.CreatedAt DESC
OFFSET (@PageNumber - 1) * @PageSize ROWS
FETCH NEXT @PageSize ROWS ONLY
OPTION (RECOMPILE);
GO

/* A5. Conteo por estado ------------------------------------------------------
   Alimenta las tarjetas de indicadores sin traer los tickets a la aplicación.
   El LEFT JOIN contra la lista de estados devuelve 0 en los estados vacíos.  */
SELECT
    s.Status,
    COUNT(t.Id) AS TicketCount
FROM (VALUES (N'Open'), (N'InProgress'), (N'Resolved')) AS s (Status)
LEFT JOIN dbo.Tickets AS t ON t.Status = s.Status
GROUP BY s.Status
ORDER BY CASE s.Status WHEN N'Open' THEN 1 WHEN N'InProgress' THEN 2 ELSE 3 END;
GO

/* A6. Verificación de integridad --------------------------------------------
   Cada intento debe FALLAR. Se ejecutan en TRY/CATCH dentro de una
   transacción revertida, así que no dejan datos.                             */
BEGIN TRANSACTION;

-- A6.1 FK: usuario asignado inexistente
BEGIN TRY
    INSERT INTO dbo.Tickets (Title, AssignedUserId) VALUES (N'Ticket huérfano', -1);
    PRINT 'A6.1 ERROR: se insertó un ticket con usuario inexistente';
END TRY
BEGIN CATCH
    PRINT 'A6.1 OK (FK) -> ' + ERROR_MESSAGE();
END CATCH;

-- A6.2 CHECK: estado no permitido
BEGIN TRY
    INSERT INTO dbo.Tickets (Title, Status, AssignedUserId)
    SELECT TOP (1) N'Estado inválido', N'Closed', Id FROM dbo.Users;
    PRINT 'A6.2 ERROR: se insertó un estado no permitido';
END TRY
BEGIN CATCH
    PRINT 'A6.2 OK (CHECK Status) -> ' + ERROR_MESSAGE();
END CATCH;

-- A6.3 CHECK: título vacío
BEGIN TRY
    INSERT INTO dbo.Tickets (Title, AssignedUserId)
    SELECT TOP (1) N'   ', Id FROM dbo.Users;
    PRINT 'A6.3 ERROR: se insertó un título vacío';
END TRY
BEGIN CATCH
    PRINT 'A6.3 OK (CHECK Title) -> ' + ERROR_MESSAGE();
END CATCH;

-- A6.4 UNIQUE: correo repetido (la intercalación CI ignora mayúsculas)
BEGIN TRY
    INSERT INTO dbo.Users (Name, Email) VALUES (N'Duplicado', N'ANA.TORRES@example.com');
    PRINT 'A6.4 ERROR: se insertó un correo duplicado';
END TRY
BEGIN CATCH
    PRINT 'A6.4 OK (UNIQUE Email) -> ' + ERROR_MESSAGE();
END CATCH;

-- A6.5 FK: borrar un usuario con tickets asignados
BEGIN TRY
    DELETE FROM dbo.Users WHERE Email = N'ana.torres@example.com';
    PRINT 'A6.5 ERROR: se borró un usuario con tickets';
END TRY
BEGIN CATCH
    PRINT 'A6.5 OK (ON DELETE NO ACTION) -> ' + ERROR_MESSAGE();
END CATCH;

IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
GO

/* A6.6 Relaciones definidas en el catálogo del sistema */
SELECT
    fk.name                                             AS ForeignKey,
    OBJECT_NAME(fk.parent_object_id)                    AS ChildTable,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ChildColumn,
    OBJECT_NAME(fk.referenced_object_id)                AS ParentTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ParentColumn,
    fk.delete_referential_action_desc                   AS OnDelete
FROM sys.foreign_keys AS fk
INNER JOIN sys.foreign_key_columns AS fkc ON fkc.constraint_object_id = fk.object_id;
GO


/* =============================================================================
   PARTE B - JSON NATIVO EN SQL SERVER

   ¿Qué función usar?
   - ISJSON      -> ¿este texto es JSON válido? (y desde 2022, ¿de qué tipo?)
   - JSON_VALUE  -> un valor ESCALAR (texto, número, fecha, booleano).
                    Con un objeto o un arreglo devuelve NULL.
   - JSON_QUERY  -> un OBJETO o un ARREGLO completo, como JSON.
                    Con un escalar devuelve NULL.
   - OPENJSON    -> convertir JSON en FILAS para hacer JOIN, EXISTS o GROUP BY.
   - JSON_MODIFY -> cambiar una propiedad y devolver el JSON actualizado.
   ========================================================================== */

/* B1. ISJSON - validar JSON ---------------------------------------------------
   En la tabla, el CHECK CK_Tickets_AdditionalData_IsJson usa
   ISJSON(AdditionalData, OBJECT) = 1, así que solo se aceptan objetos JSON.  */

-- B1.1 Qué devuelve ISJSON para distintos textos
SELECT
    v.Sample,
    ISJSON(v.Sample)         AS IsJson,
    ISJSON(v.Sample, OBJECT) AS IsJsonObject,
    ISJSON(v.Sample, ARRAY)  AS IsJsonArray
FROM (VALUES
        (N'{"priority":"High"}'),
        (N'["network","server"]'),
        (N'{"priority": "High",'),
        (N'texto plano')
     ) AS v (Sample);
GO

-- B1.2 Comprobación sobre los datos: todos los tickets deben tener JSON válido
SELECT
    COUNT(*)                                                 AS TotalTickets,
    SUM(CASE WHEN ISJSON(AdditionalData, OBJECT) = 1 THEN 1 ELSE 0 END) AS ValidJsonObjects
FROM dbo.Tickets;
GO

-- B1.3 La restricción rechaza JSON mal formado (debe FALLAR)
BEGIN TRANSACTION;
BEGIN TRY
    INSERT INTO dbo.Tickets (Title, AssignedUserId, AdditionalData)
    SELECT TOP (1) N'JSON inválido', Id, N'{"priority": "High",' FROM dbo.Users;
    PRINT 'B1.3 ERROR: se guardó un JSON inválido';
END TRY
BEGIN CATCH
    PRINT 'B1.3 OK (ISJSON) -> ' + ERROR_MESSAGE();
END CATCH;

-- B1.4 La restricción rechaza un arreglo: debe ser un objeto (debe FALLAR)
BEGIN TRY
    INSERT INTO dbo.Tickets (Title, AssignedUserId, AdditionalData)
    SELECT TOP (1) N'JSON arreglo', Id, N'["network"]' FROM dbo.Users;
    PRINT 'B1.4 ERROR: se guardó un arreglo como AdditionalData';
END TRY
BEGIN CATCH
    PRINT 'B1.4 OK (ISJSON OBJECT) -> ' + ERROR_MESSAGE();
END CATCH;

-- B1.5 Prioridad fuera de la lista permitida (debe FALLAR)
BEGIN TRY
    INSERT INTO dbo.Tickets (Title, AssignedUserId, AdditionalData)
    SELECT TOP (1) N'Prioridad inválida', Id, N'{"priority":"Urgent"}' FROM dbo.Users;
    PRINT 'B1.5 ERROR: se guardó una prioridad no permitida';
END TRY
BEGIN CATCH
    PRINT 'B1.5 OK (CHECK Priority) -> ' + ERROR_MESSAGE();
END CATCH;

IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
GO

/* B2. JSON_VALUE - valores escalares --------------------------------------- */

-- B2.1 Tickets con prioridad High.
--      Se filtra por la columna calculada Priority (= JSON_VALUE) y por eso
--      SQL Server puede usar el índice IX_Tickets_Priority.
SELECT t.Id, t.Title, t.Priority, t.Status
FROM dbo.Tickets AS t
WHERE t.Priority = N'High'
ORDER BY t.CreatedAt DESC;
GO

-- B2.2 La misma consulta escrita con JSON_VALUE explícito.
--      Devuelve lo mismo; SQL Server reconoce la expresión de la columna
--      calculada y también puede usar su índice.
SELECT t.Id, t.Title, JSON_VALUE(t.AdditionalData, N'$.priority') AS Priority
FROM dbo.Tickets AS t
WHERE JSON_VALUE(t.AdditionalData, N'$.priority') = N'High'
ORDER BY t.CreatedAt DESC;
GO

-- B2.3 Tickets de una categoría específica
SELECT t.Id, t.Title, t.Category
FROM dbo.Tickets AS t
WHERE t.Category = N'Infrastructure'
ORDER BY t.CreatedAt DESC;
GO

-- B2.4 Propiedad anidada y conversión de tipo:
--      departamento (dentro de metadata) y fecha estimada como DATE.
--      Tickets no resueltos cuya fecha estimada ya pasó o vence hoy.
SELECT
    t.Id,
    t.Title,
    JSON_VALUE(t.AdditionalData, N'$.metadata.department')                  AS Department,
    TRY_CONVERT(DATE, JSON_VALUE(t.AdditionalData, N'$.estimatedResolutionDate')) AS EstimatedResolutionDate
FROM dbo.Tickets AS t
WHERE t.Status <> N'Resolved'
  AND TRY_CONVERT(DATE, JSON_VALUE(t.AdditionalData, N'$.estimatedResolutionDate'))
      <= CAST(SYSUTCDATETIME() AS DATE)
ORDER BY EstimatedResolutionDate;
GO

/* B3. JSON_QUERY - objetos y arreglos ------------------------------------- */

-- B3.1 Etiquetas y metadatos de cada ticket.
--      JSON_VALUE sobre '$.tags' devuelve NULL porque es un arreglo:
--      por eso existe JSON_QUERY.
SELECT
    t.Id,
    t.Title,
    JSON_VALUE(t.AdditionalData, N'$.tags')     AS TagsWithJsonValue,  -- NULL
    JSON_QUERY(t.AdditionalData, N'$.tags')     AS Tags,
    JSON_QUERY(t.AdditionalData, N'$.metadata') AS Metadata
FROM dbo.Tickets AS t
ORDER BY t.Id;
GO

-- B3.2 Etiquetas de un ticket concreto
SELECT
    t.Id,
    t.Title,
    JSON_QUERY(t.AdditionalData, N'$.tags') AS Tags
FROM dbo.Tickets AS t
WHERE t.Title = N'Servidor de archivos sin respuesta';
GO

/* B4. OPENJSON - JSON a filas ------------------------------------------------ */

-- B4.1 Una fila por etiqueta (CROSS APPLY recorre el arreglo de cada ticket)
SELECT
    t.Id,
    t.Title,
    tag.[value] AS Tag
FROM dbo.Tickets AS t
CROSS APPLY OPENJSON(t.AdditionalData, N'$.tags') AS tag
ORDER BY t.Id, Tag;
GO

-- B4.2 Filtrar tickets que tienen una etiqueta.
--      Es la consulta que usará GET /api/tickets?tag=network.
DECLARE @Tag NVARCHAR(30) = N'network';

SELECT t.Id, t.Title, JSON_QUERY(t.AdditionalData, N'$.tags') AS Tags
FROM dbo.Tickets AS t
WHERE EXISTS (
    SELECT 1
    FROM OPENJSON(t.AdditionalData, N'$.tags') AS tag
    WHERE tag.[value] = @Tag
)
ORDER BY t.CreatedAt DESC;
GO

-- B4.3 Etiquetas más usadas (agregación sobre el contenido del JSON)
SELECT
    tag.[value] AS Tag,
    COUNT(*)    AS TicketCount
FROM dbo.Tickets AS t
CROSS APPLY OPENJSON(t.AdditionalData, N'$.tags') AS tag
GROUP BY tag.[value]
ORDER BY TicketCount DESC, Tag;
GO

-- B4.4 Leer todas las propiedades con tipos, usando OPENJSON ... WITH.
--      AS JSON conserva tags y metadata como JSON.
SELECT
    t.Id,
    t.Title,
    j.Priority,
    j.Category,
    j.EstimatedResolutionDate,
    j.Department,
    j.Source,
    j.Tags
FROM dbo.Tickets AS t
CROSS APPLY OPENJSON(t.AdditionalData)
    WITH (
        Priority                NVARCHAR(20)  N'$.priority',
        Category                NVARCHAR(50)  N'$.category',
        EstimatedResolutionDate DATE          N'$.estimatedResolutionDate',
        Department              NVARCHAR(50)  N'$.metadata.department',
        Source                  NVARCHAR(50)  N'$.metadata.source',
        Tags                    NVARCHAR(MAX) N'$.tags' AS JSON
    ) AS j
ORDER BY t.Id;
GO

-- B4.5 Sin esquema: OPENJSON devuelve clave, valor y tipo de cada propiedad
--      (type: 1 = texto, 2 = número, 3 = booleano, 4 = arreglo, 5 = objeto)
SELECT p.[key], p.[value], p.[type]
FROM dbo.Tickets AS t
CROSS APPLY OPENJSON(t.AdditionalData) AS p
WHERE t.Title = N'Servidor de archivos sin respuesta';
GO

/* B5. JSON_MODIFY - actualizar una propiedad ---------------------------------
   Cambia solo '$.priority' sin reemplazar el resto del documento.
   Es lo que ejecutará PATCH /api/tickets/{id}/priority.
   Se hace dentro de una transacción con ROLLBACK para no alterar la demo.   */
BEGIN TRANSACTION;

DECLARE @TicketId INT = (SELECT Id FROM dbo.Tickets WHERE Title = N'Impresora del piso 3 atasca papel');

SELECT 'Antes' AS Moment, Id, Priority, AdditionalData
FROM dbo.Tickets WHERE Id = @TicketId;

-- B5.1 Actualizar la prioridad
UPDATE dbo.Tickets
SET AdditionalData = JSON_MODIFY(AdditionalData, N'$.priority', N'High'),
    UpdatedAt      = SYSUTCDATETIME()
WHERE Id = @TicketId;

-- B5.2 Agregar una etiqueta al final del arreglo
UPDATE dbo.Tickets
SET AdditionalData = JSON_MODIFY(AdditionalData, N'append $.tags', N'urgent')
WHERE Id = @TicketId;

SELECT 'Después' AS Moment, Id, Priority, AdditionalData
FROM dbo.Tickets WHERE Id = @TicketId;

ROLLBACK TRANSACTION;
GO

/* B6. Consulta combinada relacional + JSON ----------------------------------
   Tickets no resueltos de prioridad High o Critical con la etiqueta 'server',
   con su responsable, ordenados por fecha de creación.                        */
SELECT
    t.Id,
    t.Title,
    t.Status,
    t.Priority,
    u.Name AS AssignedUserName,
    JSON_QUERY(t.AdditionalData, N'$.tags') AS Tags,
    t.CreatedAt
FROM dbo.Tickets AS t
INNER JOIN dbo.Users AS u ON u.Id = t.AssignedUserId
WHERE t.Status <> N'Resolved'
  AND t.Priority IN (N'High', N'Critical')
  AND EXISTS (
        SELECT 1
        FROM OPENJSON(t.AdditionalData, N'$.tags') AS tag
        WHERE tag.[value] = N'server'
      )
ORDER BY t.CreatedAt DESC;
GO
