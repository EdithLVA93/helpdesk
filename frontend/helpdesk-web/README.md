# HelpDesk Web

Frontend Angular 21 de la mesa de ayuda. La documentación completa del proyecto
(arquitectura, base de datos, API y decisiones técnicas) está en el
[README principal](../../README.md).

## Comandos

| Comando | Qué hace |
| --- | --- |
| `npm install` | Instala las dependencias |
| `npm start` | Servidor de desarrollo en `http://localhost:4200`, con proxy de `/api` a `https://localhost:7080` |
| `npm test -- --watch=false` | Ejecuta las pruebas unitarias (Vitest) una vez |
| `npm run build` | Compilación de producción en `dist/helpdesk-web/browser` |

Para usar otra dirección de la API en desarrollo:

```powershell
$env:HELPDESK_API_URL = "http://localhost:5080"; npm start
```

## Estructura

```text
src/app/
├── core/       # Interceptor de errores, ApiError, notificaciones, i18n (paginador, fechas)
├── shared/     # Badges de estado y prioridad, validadores de formulario, utilidades
├── layout/     # Shell con menú lateral adaptable
└── features/
    ├── tickets/  # Panel (filtros, indicadores, cambio de estado/prioridad) y formulario de creación
    └── users/    # Alta y listado de usuarios
```
