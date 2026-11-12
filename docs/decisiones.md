# Registro de decisiones de arquitectura (ADR)

Cada decisión importante queda aquí con su contexto y su porqué. Las decisiones **no se borran**: si una cambia, se
escribe un ADR nuevo que la reemplaza y el anterior se marca como *Reemplazado por ADR-N*.

Estados: **Propuesto** · **Aceptado** · **Reemplazado** · **Descartado**.

## Índice

| ADR | Título | Estado | Fecha |
| --- | --- | --- | --- |
| 001 | Capas y fronteras (sin proyecto Contracts) | Aceptado | 2026-11-06 |
| 002 | Persistencia: EF Core Code First; la base solo protege integridad | Aceptado | 2026-11-06 |
| 003 | Tenancy híbrido introducido en dos pasos | Aceptado | 2026-11-06 |
| 004 | Dos identidades de base de datos | Aceptado | 2026-11-06 |
| 005 | Contratos Request/Response y sobre de paginación | Aceptado | 2026-11-06 |
| 006 | Errores como ProblemDetails tipados | Aceptado | 2026-11-06 |
| 007 | Seguridad: fallback policy, roles y permisos por recurso | Aceptado | 2026-11-06 |
| 008 | Migraciones en producción fuera del arranque | Aceptado | 2026-11-06 |
| 009 | Operaciones largas con 202 Accepted | Aceptado | 2026-11-06 |
| 010 | API sin estado, preparada para N instancias | Aceptado | 2026-11-06 |
| 011 | Lo que NO hacemos | Aceptado | 2026-11-06 |
| 012 | El dominio es dueño de las reglas | Aceptado | 2026-11-06 |
| 013 | Identidad: proveedor externo (elección en F3) | Aceptado; proveedor **pendiente** | 2026-11-06 |
| 014 | Plataforma Azure | Aceptado | 2026-11-06 |
| 015 | Base de datos en Azure SQL desde F1 (free tier, solo Entra, SKU explícito para dedicadas) | Aceptado | 2026-11-08 |
| 016 | Mantener .NET 10 (LTS); no migrar a .NET 11 por ahora | Descartado (la migración) | 2026-11-10 |

El texto de los ADR 001–015 está resumido en `docs/ARQUITECTURA.md`, sección 3; el ADR 016 está completo abajo. Al cerrar cada fase se completan aquí
los que esa fase haya puesto a prueba, con lo aprendido.

---

## Plantilla

```
## ADR-NNN · Título corto en forma de decisión
Estado: Propuesto | Aceptado | Reemplazado por ADR-XXX | Descartado
Fecha: AAAA-MM-DD · Fase: Fx

### Contexto
Qué problema o situación obliga a decidir. Hechos, restricciones, números si los hay.

### Decisión
Lo que se eligió, en una o dos frases claras.

### Alternativas consideradas
- Opción A: por qué no.
- Opción B: por qué no.

### Consecuencias
- Lo que se gana.
- Lo que cuesta o complica.
- Lo que cambia en el código, las pruebas o la operación.
```

## ADR-016 · Mantener .NET 10 (LTS); no migrar a .NET 11 por ahora
Estado: Descartado (la migración) · Fecha: 2026-11-10 · Fase: F0

### Contexto
.NET 11 salió el 10 de noviembre de 2026 (STS, soporte hasta noviembre de 2028, igual que .NET 10 LTS). OrderFlow
estaba en F0, el momento más barato para cambiar: sin EF Core, sin migraciones, sin Azure. Gracias a `global.json`,
`Directory.Build.props` y CPM, el cambio era de pocas líneas.

### Decisión
Se intentó y se revirtió. OrderFlow se queda en **.NET 10** hasta que todas las dependencias tengan versión compatible.

### Alternativas consideradas
- Migrar a .NET 11 completo: `Microsoft.AspNetCore.OpenApi 11.0.0` exige `Microsoft.OpenApi` 3.x y
  `Asp.Versioning.OpenApi 10.2.3` (la última) exige 2.x → conflicto transitivo **NU1107** sin versión que cumpla ambos.
- Forzar `Microsoft.OpenApi` 3.x directo: compila, pero rompe `Asp.Versioning.OpenApi` en runtime.
- .NET 11 con `Microsoft.AspNetCore.OpenApi` 10.x: mezcla de versiones de ASP.NET Core sin soporte.
- .NET 11 sin `Asp.Versioning.OpenApi`, con transformers propios: cambio de alcance en la tarea 5.

### Consecuencias
- Hallazgo útil del intento: en `net11.0`, `Microsoft.Extensions.DependencyInjection.Abstractions` y
  `Microsoft.Extensions.Configuration.Abstractions` vienen con el framework (**NU1510**, package pruning); al migrar se
  eliminan sus `PackageReference`.
- **Condición para reabrir:** que exista `Asp.Versioning.OpenApi` 11.x (dependiente de `Microsoft.OpenApi` 3.x). Para
  .NET 10 la versión estable llegó ocho meses después del framework.
- La migración, cuando toque, es: `global.json`, `Directory.Build.props`, `Microsoft.*` en `Directory.Packages.props`,
  quitar las dos referencias podadas, y revisar los analizadores nuevos (warnings como errores).

---

## Bitácora de fases

Una entrada corta al cerrar cada fase: qué se construyó, qué decisión se tomó o cambió, qué se aprendió.

### F0 — Esqueleto del host
_(se condensa al cerrar la fase)_ Notas en curso:
- Ajuste al ADR 001: proyecto `OrderFlow.Composition` como composition root; `Api` solo llama `AddOrderFlow()` y no ve
  Infrastructure (`DisableTransitiveProjectReferences`).
- `ProblemTypes`: la base del URI del catálogo vive en código, no en appsettings (es parte del contrato, no del entorno).
- Errores del framework (404, 405, 401, 429) con el `type` del catálogo vía `UseStatusCodePages` + `CustomizeProblemDetails`.
- Seguridad: JWT de `dotnet user-jwts`, fallback policy (autenticado) y política `PlatformAdmin`.
- Rate limiting por usuario con `Retry-After` tomado del lease; health live/ready/startup; timeouts configurables;
  OpenTelemetry con exportación opt-in (perfil `https-telemetry` + dashboard de Aspire); `TimeProvider`.
- Intento de migrar a .NET 11 revertido (ADR 016).
