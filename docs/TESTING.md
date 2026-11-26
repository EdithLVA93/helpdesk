# Pruebas

Cómo se verifica cada requisito de la mesa de ayuda: pruebas automáticas
(backend y frontend), el script SQL de demostración y pruebas manuales de
integración.

## Cómo ejecutarlas

```powershell
# Backend: 100 tests (xUnit + NSubstitute)
dotnet test backend\HelpDesk.sln

# Frontend: 40 tests (Vitest)
cd frontend\helpdesk-web
npm test -- --watch=false

# Base de datos: consultas y comprobaciones de integridad (8 comprobaciones "OK")
sqlcmd -S .\SQLEXPRESS -E -C -I -f 65001 -i database\03_queries.sql
```

## Qué cubre cada prueba

### Backend

| Requisito | Prueba automática | Resultado esperado |
| --- | --- | --- |
| Crear usuario correctamente | `UserServiceTests.CreateAsync_ValidUser_SavesNormalizedEmail` | Usuario guardado con correo normalizado |
| Crear usuario con información inválida | `CreateUserRequestValidatorTests` (nombre vacío, 6 formatos de correo inválidos, longitudes) | 400 con mensaje por campo |
| Correo duplicado | `UserServiceTests.CreateAsync_EmailAlreadyRegistered_ThrowsConflictAndDoesNotSave` | 409 y no se guarda |
| Crear ticket | `TicketServiceTests.CreateAsync_ValidRequest_SavesOpenTicketWithNormalizedJson` | Nace en `Open`, con fecha de creación y JSON normalizado |
| Crear ticket sin título | `CreateTicketRequestValidatorTests.Validate_WithoutTitle_FailsBecauseTitleIsRequired` (`null`, `""`, `"   "`) | 400 «El título del ticket es obligatorio.» |
| Crear ticket sin usuario asignado | `CreateTicketRequestValidatorTests.Validate_WithoutAssignedUser_FailsBecauseTicketMustBeAssigned` | 400 «El ticket debe tener un usuario asignado.» |
| Asignar un usuario inexistente | `TicketServiceTests.CreateAsync_AssignedUserDoesNotExist_ThrowsValidationErrorOnAssignedUserId` | 400 en `assignedUserId`, no se guarda |
| Cambiar estados | `TicketStatusTransitionPolicyTests` (las 9 combinaciones origen/destino), `TicketTests`, `TicketServiceTests.ChangeStatusAsync_OpenToInProgress_…` | Transiciones permitidas aplicadas |
| **Open → Resolved prohibido** | `TicketStatusTransitionPolicyTests`, `TicketTests.ChangeStatus_OpenToResolved_ThrowsAndKeepsTicketUnchanged`, `TicketServiceTests.ChangeStatusAsync_OpenToResolved_ThrowsAndDoesNotSave` | 409 y el ticket no cambia |
| Consultar un ticket inexistente | `TicketServiceTests.GetByIdAsync_…`, `ChangeStatusAsync_TicketDoesNotExist_…`, `ChangePriorityAsync_…` | 404 |
| Listar y filtrar tickets | `TicketRequestValidatorsTests` (paginación y filtros inválidos) + pruebas HTTP manuales (abajo) | 200 / 400 |

**Comprobación de mutación.** Se permitió a propósito Open → Resolved en
`TicketStatusTransitionPolicy` y se ejecutaron los tests: **8 fallaron**, en
las tres capas (política, entidad y servicio). La regla está protegida. El
cambio se revirtió después.

### Base de datos (`database/03_queries.sql`)

| Requisito | Bloque del script | Resultado esperado |
| --- | --- | --- |
| Verificar relaciones | A6.1 (FK), A6.5 (ON DELETE NO ACTION), A6.6 (catálogo `sys.foreign_keys`) | Inserción/borrado rechazados; FK listada |
| Restricciones | A6.2 (estado), A6.3 (título vacío), A6.4 (correo único) | Rechazados por su CHECK/UNIQUE |
| Validar JSON | B1.1–B1.5 (`ISJSON`, JSON mal formado, arreglo en lugar de objeto, prioridad no permitida) | Rechazados por `CK_Tickets_AdditionalData_IsJson` / `CK_Tickets_Priority` |
| Consultar propiedades JSON | B2.1–B2.4 (`JSON_VALUE`, incluida `$.metadata.department`) | Valores escalares |
| Filtrar por prioridad | B2.1 / B2.2 (columna calculada indexada) | 3 tickets `High` en el seed |
| Consultar arreglos con OPENJSON | B4.1–B4.5 (una fila por etiqueta, `EXISTS`, `GROUP BY`, `WITH`) | Filas por etiqueta y conteos |
| Objetos y arreglos con JSON_QUERY | B3.1, B3.2, B4.2, B6 | `tags` y `metadata` como JSON |
| Actualizar con JSON_MODIFY | B5.1 (prioridad), B5.2 (añadir etiqueta con `append`) | Solo cambia la propiedad indicada (con `ROLLBACK`) |

Todas las comprobaciones que deben fallar imprimen `OK (...)`. El script no
deja cambios: lo que escribe lo revierte con `ROLLBACK`.

### Frontend

| Requisito | Prueba automática | Resultado esperado |
| --- | --- | --- |
| Validar formularios | `ticket-create.spec.ts` (formulario vacío, título con espacios), `form-validators.spec.ts` | No se envía; mensajes por campo |
| Crear tickets | `ticket-create.spec.ts` «envía la petición con el formato de la API…» | Petición con fechas `yyyy-MM-dd`, etiquetas, `metadata.source = "Web"` |
| Mostrar errores de API | `http-error.interceptor.spec.ts` (400, 404, 409, 500, API caída, sin red), `ticket-create.spec.ts` «muestra en el campo…», `server-errors.spec.ts` | Mensaje en el campo o notificación |
| Filtrar / cambiar estado (llamadas HTTP) | `ticket.service.spec.ts` (sin parámetros vacíos, `PUT /status`, `PATCH /priority`) | Peticiones correctas |
| Fecha escrita a mano `dd/mm/aaaa` | `day-month-year-date-adapter.spec.ts` | `15/12/2026` válida, `31/02/2026` inválida |

## Pruebas manuales de integración

Realizadas con la API arrancada desde Visual Studio 2026 (perfil `https`) y
el frontend con `npm start`:

| Escenario | Resultado |
| --- | --- |
| Listar tickets, filtrar por estado (tarjetas y filtros), paginar | Datos reales del seed; paginador en español |
| Cambiar estado desde el menú de cada fila | Solo se ofrecen las transiciones permitidas; «Resuelto» aparece deshabilitado desde «Abierto» |
| Crear ticket con todos los campos | Ticket creado, notificación y regreso al listado |
| Crear usuario y repetir el correo en mayúsculas | Usuario creado; 409 mostrado en el campo correo |
| Cambio concurrente: otro usuario cambia el ticket mientras el menú está abierto | 409 «El ticket ya se encuentra en estado 'InProgress'», la vista se refresca |
| API detenida | Indicadores en «—», estado de error y «No se pudo conectar con el servidor» |
| API recuperada → «Reintentar» | Vuelven a cargarse tabla e indicadores |
| Vista móvil (375 px) | Menú lateral desplegable, indicadores 2×2, filtros apilados |

> Al depurar la API con **F5**, Visual Studio se detiene en las excepciones de
> negocio (400/404/409) aunque la API las maneje correctamente. Para usar la
> aplicación con normalidad, arrancarla con **Ctrl+F5** (Iniciar sin depurar).
