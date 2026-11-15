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
| 017 | Composition root en su propio proyecto (`OrderFlow.Composition`); ajusta el ADR 001 | Aceptado | 2026-11-14 |

El texto de los ADR 001–015 está resumido en `docs/ARQUITECTURA.md`, sección 3; los ADR 016 y 017 están completos abajo. Al cerrar cada fase se completan aquí
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

## ADR-017 · Composition root en su propio proyecto
Estado: Aceptado (ajusta el ADR 001) · Fecha: 2026-11-14 · Fase: F0

### Contexto
El ADR 001 pide dos cosas que chocan entre sí. Por un lado, el host compone con **una sola** llamada `AddXxx()` y no
ve `Infrastructure`. Por otro, las interfaces (`ITenantContext`, repositorios) viven en `Application` y se
implementan en `Infrastructure`, así que **Infrastructure → Application**. En consecuencia, `Application` no puede
llamar a `AddInfrastructure()`, porque sería una referencia circular. En Shop esto funcionaba porque la dependencia
iba al revés (Service → Repository). Sin un cambio, el host tendría que referenciar `Infrastructure` para componer, y
la prueba de arquitectura del "Listo cuando" de F0 fallaría.

### Decisión
Un proyecto mínimo `OrderFlow.Composition` referencia `Application` e `Infrastructure` y expone `AddOrderFlow()`.
`Api` referencia solo `Composition` y `Application`, con `DisableTransitiveProjectReferences`. Ningún tipo de `Api`
puede nombrar un tipo de `Infrastructure`: el compilador lo impide y `ApiDoesNotDependOnInfrastructure` (ArchUnitNET)
lo vigila.

### Alternativas consideradas
- El host referencia `Infrastructure` solo para componer (`AddApplication().AddInfrastructure()`). Es lo más común,
  pero la frontera queda en convención: cualquier controlador podría usar un `DbContext` y solo lo detectaría una
  regla por tipos, no el compilador.
- Invertir la dependencia como en Shop (Application → Infrastructure). Obliga a sacar los puertos de `Application` y
  contradice el ADR 001.

### Consecuencias
- La frontera se cumple en dos niveles: el compilador (no compila) y la prueba (no se agrega la referencia sin
  que falle). Verificado al cerrar F0: agregar la referencia y usar `AddInfrastructure` desde `Api` hace fallar la prueba.
- Cuesta un proyecto más, que no tiene lógica, solo registro. `TimeProvider` también se registra ahí.
- Límite conocido: un `ProjectReference` que nadie usa no aparece en los metadatos y no dispara la prueba; la
  primera vez que se use un tipo, sí.

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
**Cierre: 2026-11-14 (`ok` de Alejandro).** Deuda anotada al cerrar: prueba `AnonymousEndpointsTests` (lista
blanca de `[AllowAnonymous]`, propuesta como live coding), comprobar `dotnet run` + Swagger siguiendo solo el README, y
documentar qué responde `/api/v2/me`. Construido: solución con 5 proyectos de `src` y 3 de pruebas; build
común (`Directory.Build.props`, CPM, analizadores del SDK, advertencias como errores); host con todo lo transversal
de Shop más versionado, options validadas al arrancar y tres sondas de salud. 18 pruebas (3 de arquitectura y 15 de
integración). Verificado en un clon limpio: 0 advertencias y 18/18 pruebas.
Decisiones y hallazgos:
- **ADR-017:** el proyecto `OrderFlow.Composition` es el composition root; `Api` solo llama `AddOrderFlow()` y no
  ve Infrastructure.
- Paquetes nuevos, propuestos y aceptados: `Asp.Versioning.Mvc`/`.ApiExplorer`/`.OpenApi` (MIT) y
  `TngTech.ArchUnitNET.xUnitV3` (Apache-2.0). Analizadores: solo los del SDK.
- Los paquetes `Microsoft.Extensions.*` deben ir en la misma versión mayor que el framework: en 11.0.0 reemplazaban
  en runtime a los de ASP.NET Core 10 (detectado en la revisión; regla en `CLAUDE.md`).
- CORS solo con lo que existe hoy (GET + `Authorization`); los verbos de escritura se abren en F6.
- Rate limiting particionado por `sub`, no por nombre; en F3 se combina con el tenant.

### F0.5 — Casos de uso del producto
**Cierre: 2026-11-14.** Sin código. `docs/casos-de-uso.md` aprobado: 16 decisiones de producto, matriz de actores y
permisos, 11 casos de uso con criterios *Dado / Cuando / Entonces*, 29 reglas (RN-01 a RN-29) con su origen, y 6
supuestos confirmados.
Decisiones que más impactan el diseño:
- **Núcleo común para B2B, B2C y negocio simple;** crédito y listas de precios fuera del Bloque 1 (P1b).
- **Máquina de estados lineal** con pago antes de envío: enviar sin cobrar sería crédito (P3).
- **Inventario descontado al crear la orden;** la cancelación devuelve con movimientos de reversa (ledger inmutable).
- **Impuesto por línea con redondeo comercial** (`AwayFromZero`), como CFDI 4.0; precio y tasa congelados en la orden.
- **Permiso por recurso según el estado:** cancelar una orden `Paid` solo TenantAdmin (primer caso de 403 dependiente
  del dato, ADR 7).
- **PlatformAdmin no ve datos de negocio** de los tenants.
- Datos nuevos para F2 que no estaban en el plan: costo y punto de reorden del producto, moneda y tasa de impuesto del
  tenant, subtotal e impuesto por línea y por orden.
- `ProblemTypes`: la base del URI del catálogo vive en código, no en appsettings (es parte del contrato, no del entorno).
- Errores del framework (404, 405, 401, 429) con el `type` del catálogo vía `UseStatusCodePages` + `CustomizeProblemDetails`.
- Seguridad: JWT de `dotnet user-jwts`, fallback policy (autenticado) y política `PlatformAdmin`.
- Rate limiting por usuario con `Retry-After` tomado del lease; health live/ready/startup; timeouts configurables;
  OpenTelemetry con exportación opt-in (perfil `https-telemetry` + dashboard de Aspire); `TimeProvider`.
- Intento de migrar a .NET 11 revertido (ADR 016).
