# OrderFlow — Arquitectura y plan de trabajo

Documento de referencia del proyecto. Lo leen los agentes al empezar cada fase y lo actualiza docs-keeper cuando una
decisión cambia. Versión 7 · 2026-11-08.

---

## 1. Punto de partida: lo que ya resuelve el proyecto *Shop*

Shop es la plantilla de calidad. Todo lo de esta tabla se **conserva** en OrderFlow, con la misma intención (y los
mismos comentarios de "por qué").

| Área | Concepto en Shop | Dónde |
| --- | --- | --- |
| Capas | Models / Repository / Service / Host, cada capa registra lo suyo con un `AddXxx()` | `DependencyInjection.cs` por proyecto |
| Fronteras | `DisableTransitiveProjectReferences`: el host no puede ver el repositorio | `Shop.csproj` |
| Contratos | DTO (`record`) distinto de la entidad; el mapeo ocurre en el servicio | `EmployeeDto`, `ShopService` |
| Validación de entrada | `[BindRequired]`, `[EnumDataType]`, `[Range]`; enum sin valor 0 a propósito | `ShopController`, `EmployeeType` |
| Paginación | límites de `page` y `pageSize`, filtro y paginación traducidos a SQL, `AsNoTracking` | `ShopRepository` |
| Cancelación | `CancellationToken` de punta a punta; timeout por request → 504 | `Program.cs`, `GlobalExceptionHandler` |
| Errores | `IExceptionHandler` único + ProblemDetails (RFC 9457) con `traceId`; sin try/catch en capas | `GlobalExceptionHandler` |
| Logging | middleware de request: método, ruta, status, duración, traceId; sin query string; nivel según status | `RequestLoggingMiddleware` |
| Autenticación | JWT Bearer (`dotnet user-jwts` en dev, Authority/Audience por configuración) | `Program.cs` |
| Autorización | **fallback policy**: todo protegido por defecto; políticas por rol | `Program.cs` |
| Abuso | rate limiting por usuario (ventana fija) | `Program.cs` |
| Navegador | CORS restrictivo por configuración, solo los verbos y headers necesarios | `Program.cs` |
| Transporte | HTTPS redirect + HSTS fuera de dev | `Program.cs` |
| Salud | `/health` anónimo; el check de la base lo registra la capa dueña de la base | `AddShopRepository` |
| Observabilidad | OpenTelemetry: traces, métricas, logs; exportador OTLP opcional por configuración | `Program.cs` |
| Contrato | OpenAPI nativo + Swagger UI solo en dev, esquema Bearer documentado; enums como texto | `BearerSecuritySchemeTransformer` |
| Arranque seguro | fuera de Development falla si falta la cadena de conexión (fail fast) | `Program.cs` |
| Pruebas manuales | `.http` con casos felices **y** negativos (400, 401, 403, 429, CORS) | `Shop.http` |

Observaciones sobre Shop (no se copian tal cual):
- `EnsureCreated()` + `HasData` no conviven con migraciones: en OrderFlow todo es **Code First con migraciones**.
- `ConfigureAwait(false)` no aporta nada en ASP.NET Core; se puede omitir.
- La respuesta paginada es una lista sin metadatos: en OrderFlow se usa un sobre de paginación.
- No hay pruebas automatizadas ni endpoints de escritura: son los huecos más grandes.
- Un solo `/health`: en OrderFlow se separan liveness, readiness y startup.

## 2. Conceptos que faltan para una API robusta (y en qué fase entran)

| # | Concepto | Por qué importa en OrderFlow | Fase |
| --- | --- | --- | --- |
| 1 | Migraciones Code First, dos contextos (catálogo y tenant) | alta de tenants y evolución del esquema por código | F1–F2 |
| 2 | Multi-tenancy: resolución del tenant, enrutamiento de conexión, `SESSION_CONTEXT`, filtros globales | aislamiento de datos, base compartida + dedicadas (en dos pasos) | F3 (compartida), F4 (dedicadas) |
| 3 | Aprovisionamiento y migración de tenants por endpoint | nadie vuelve a ejecutar SQL a mano | F4 |
| 37 | Base de datos en **Azure SQL desde F1** (ADR 15): servidor solo Entra, free tier, firewall, reintentos, SKU explícito para dedicadas | experiencia real de nube desde el inicio, con costo controlado | F1, F4 |
| 4 | Versionado de API (`/api/v1`) y headers de deprecación (`Sunset`) | cambiar contratos sin romper clientes | F0 |
| 5 | Options pattern con validación al arrancar (`ValidateOnStart`) | configuración errónea = la app no arranca | F0 |
| 6 | Fundamentos de código: `Directory.Build.props`, `Directory.Packages.props`, analizadores, `.editorconfig`, advertencias como errores | calidad uniforme desde el día uno; después cuesta mucho meterlo | F0 |
| 7 | Health checks separados: liveness, readiness, startup | el orquestador sabe si reiniciar o solo sacar de rotación | F0, F9 |
| 8 | Tiempo y dinero: `DateTimeOffset` UTC, `TimeProvider` inyectado, `decimal` con redondeo explícito | vencimientos probables en pruebas, montos exactos | F2 |
| 9 | Request/Response DTOs por operación + FluentValidation | escrituras seguras, mensajes 400/422 consistentes | F6 |
| 10 | Errores de dominio → ProblemDetails tipados (404, 409, 412, 422) con catálogo de `type` documentado | el cliente distingue "no existe" de "conflicto" | F6 |
| 11 | Autorización a nivel de recurso (permisos, no solo roles) — OWASP API #1 (BOLA) | dentro del tenant, no todos pueden todo | F3, F6 |
| 12 | Concurrencia optimista (`rowversion` → `ETag` / `If-Match` → 412) | dos usuarios editando la misma orden | F6 |
| 13 | Idempotencia en POST (`Idempotency-Key`) | reintentos de red no duplican órdenes | F6 |
| 14 | Transacciones y reglas de negocio en el **dominio** (máquina de estados, total de la orden, ledger de inventario) | una regla vive en un solo lugar y se prueba sin base de datos | F2 (modelo), F6 |
| 15 | Paginación con sobre, filtros y orden por lista blanca, búsqueda "contiene" escapando `% _ [` | listas grandes, sin inyección por LIKE | F5 |
| 16 | Caché (output caching / HybridCache) para catálogos y resolución de tenants | latencia y carga en la base | F5 |
| 17 | Operaciones largas con `202 Accepted` + endpoint de estado | alta de tenant dedicado y migraciones no bloquean el request | F4 |
| 18 | Ciclo de vida del tenant: baja, exportación de datos, archivado/borrado, respaldo y restauración por tenant | dar de alta es solo la mitad del problema | F4, F9 |
| 19 | Auditoría con `SaveChangesInterceptor` (quién, cuándo, qué, JSON) | trazabilidad por tenant | F7 |
| 20 | Outbox + `BackgroundService` | eventos confiables; prepara webhooks y Service Bus | F7 |
| 21 | Pruebas: unitarias (dominio sin base), integración con SQL Server real (Testcontainers), arquitectura | RLS, constraints y migraciones solo se prueban contra SQL Server real | **todas**, F8 |
| 22 | Resiliencia: estrategia de ejecución de EF, resilience para HTTP saliente | fallos transitorios | F9 |
| 23 | Escalar a varias instancias: caché distribuida, rate limiting distribuido, llaves de Data Protection persistidas, contador de órdenes seguro | con dos instancias, lo que vive en memoria deja de funcionar | F9 |
| 24 | Endurecimiento: logins de mínimo privilegio, secretos fuera del repo, headers de seguridad, límites de tamaño, redacción de datos sensibles en logs | superficie de ataque | F9 |
| 25 | Observabilidad de datos: instrumentación de EF/SqlClient, `TenantId` en logs y traces, métricas propias, health por tenant | diagnosticar por tenant | F3, F9 |
| 26 | Seguridad de la cadena de suministro: paquetes vulnerables, Dependabot, CodeQL, escaneo de secretos | la mayoría de los incidentes entran por dependencias | F10 |
| 27 | Entrega: Dockerfile, docker-compose o .NET Aspire, CI, `efbundle` para migrar tenants | despliegue repetible | F10 |
| 28 | Cliente TypeScript generado desde OpenAPI (NSwag o Kiota) | front y API nunca vuelven a desalinearse | carril Angular |
| 29 | IA: asistente con function calling por tenant | paso 11 acordado | F11 |
| 30 | Azure (ADR 14): Container Apps, Azure SQL, Service Bus, Functions isolated, Blob Storage, Key Vault, App Insights, Managed Identity + RBAC, Bicep, CI/CD con OIDC | desplegar sin secretos ni connection strings, con infraestructura como código | F12 |
| 31 | Casos de uso del producto con criterios de aceptación (`docs/casos-de-uso.md`) | sin reglas de negocio escritas, F6 se inventa sobre la marcha | F0.5 |
| 32 | Guía de estilo de la API (`docs/guia-api.md`) | cada fase sigue la misma convención; los agentes no se desvían | F0 |
| 33 | Generador de datos de demostración realistas (Bogus), por tenant | probar paginación y rendimiento con volumen, pruebas y demos | F2, F5 |
| 34 | El proyecto como portafolio: README con diagrama, decisiones, arranque con un comando, demo | un reclutador lee el README, no el código | todas |
| 35 | Proveedor de identidad real y mapeo usuario → tenant → permisos (ADR 13) | sin él no hay login real ni claims confiables | F3, A1 |
| 36 | GraphQL de solo lectura (Hot Chocolate) como laboratorio | saber cuándo conviene GraphQL frente a REST, con la evidencia de haberlo implementado | Lab GraphQL |

## 3. Decisiones de arquitectura (ADR resumidos)

1. **Capas.** `OrderFlow.Domain` (entidades con comportamiento, enums, reglas e invariantes; sin EF) ·
   `OrderFlow.Application` (servicios, validadores, interfaces como `ITenantContext`, `IPermissionService`, y los
   Request/Response en su carpeta `Contracts/`) ·
   `OrderFlow.Infrastructure` (DbContexts, configuraciones, migraciones, repositorios, tenancy, aprovisionamiento) ·
   `OrderFlow.Api` (host). Cada capa expone un `AddXxx()`; el host llama uno solo y tiene
   `DisableTransitiveProjectReferences`. Pruebas: `OrderFlow.UnitTests`, `OrderFlow.IntegrationTests`,
   `OrderFlow.ArchitectureTests`. Configuración común en `Directory.Build.props` y versiones en `Directory.Packages.props`.
   *No hay proyecto `Contracts` separado:* solo se justificaría si otro cliente .NET compartiera los DTOs; el front usa
   un cliente TypeScript generado del OpenAPI, así que vivirían duplicados sin beneficio.
2. **Persistencia.** EF Core 10 + SQL Server, **Code First con migraciones**. `CatalogDbContext` (base
   `OrderFlowCF_Catalog`) y `TenantDbContext` (base compartida `OrderFlowCF_Shared` y una `OrderFlowCF_<Slug>` por
   tenant dedicado), cada uno con sus migraciones. Lo que EF no modela va en migraciones con `migrationBuilder.Sql` y su
   `Down`, y se limita a **proteger integridad sin decidir negocio**: la política RLS, el procedimiento atómico del
   contador de órdenes y un trigger que impide UPDATE/DELETE en el ledger de inventario (declarado con `HasTrigger`).
   **No hay triggers que calculen totales, muevan stock ni escriban historial:** eso es del dominio (ADR 12).
   Fechas `datetime2`/`DateTimeOffset` en UTC; dinero `decimal(18,2)`.
3. **Tenancy.** El tenant viaja en el JWT (claim `tenant`), nunca en la ruta ni en el body. Un middleware lo resuelve
   contra el catálogo (con caché) y llena `ITenantContext`. `TenantDbContext` toma la cadena según el tenant; un
   `DbConnectionInterceptor` fija `SESSION_CONTEXT('TenantId')` con `@read_only = 1`. Defensa en profundidad: RLS en
   SQL **y** filtro global de EF. **Se introduce en dos pasos:** F3 resuelve solo tenants de la base compartida;
   F4 agrega el enrutamiento a bases dedicadas junto con su alta. El modelo híbrido es la decisión más costosa del
   diseño (duplica la matriz de pruebas y el enrutamiento), así que entra cuando lo compartido ya está probado.
4. **Dos identidades de base.** La app usa una identidad de mínimo privilegio (lectura/escritura y ejecución en su
   base); el aprovisionamiento usa una identidad con permiso para crear bases (en Azure SQL, el rol de servidor
   `##MS_DatabaseManager##`) que solo inyecta el servicio de alta. Con Azure SQL (ADR 15) ambas son **identidades de
   Entra ID**, sin contraseñas: en desarrollo tu cuenta (`az login`), en Azure Managed Identities. Nunca `sa` ni
   logins SQL con contraseña; Docker local solo para pruebas y referencia.
5. **Contratos.** Entra un `XxxRequest`, sale un `XxxResponse`; las entidades no cruzan el servicio. Enums como texto.
   Listas en un sobre `{ items, page, pageSize, totalCount }`. El OpenAPI es la fuente del cliente TypeScript.
6. **Errores.** Un único manejador global; el dominio lanza excepciones tipadas (`NotFound`, `Conflict`,
   `BusinessRule`, `Forbidden`) que se traducen a ProblemDetails con `type` (URI documentada en `docs/errores.md`),
   `title`, `status`, `traceId`. Ningún try/catch que oculte fallos.
7. **Seguridad.** Fallback policy (todo autenticado). Roles para el grueso (`TenantUser`, `TenantAdmin`,
   `PlatformAdmin`) y **permisos por recurso** para acciones sensibles (cancelar orden, ver costos, ajustar inventario),
   evaluados en el servicio, no en el controlador. Rate limiting por usuario y por tenant.
8. **Migraciones en producción.** La app no migra sola al arrancar. Se migra con el endpoint de administración
   (operación larga con 202) o con un `efbundle` en CI, tenant por tenant, registrando el resultado.
9. **Operaciones largas.** Todo lo que pueda tardar más de unos segundos responde `202 Accepted` con `Location` a
   `/api/v1/operations/{id}`, se ejecuta en un `BackgroundService` y su estado queda consultable.
10. **Escalabilidad.** La API es sin estado: caché, rate limiting y llaves de Data Protection deben funcionar con N
    instancias. Mientras corra una sola instancia se usan las implementaciones en memoria, detrás de abstracciones que
    permitan cambiarlas por Redis sin tocar el resto.
11. **Lo que NO hacemos** (y por qué):
    - **Microservicios:** a esta escala un monolito modular bien separado es más robusto; partir agrega red,
      despliegues y consistencia eventual sin beneficio.
    - **CQRS completo con MediatR o event sourcing:** resuelven problemas que OrderFlow no tiene; MediatR además cambió
      a licencia comercial. Separar servicios de lectura y escritura da el 80% del beneficio.
    - **GraphQL como API principal:** la API pública es REST (cachable, simple de autorizar por endpoint, cliente
      generado del OpenAPI). GraphQL se explora en un **laboratorio de solo lectura** (sección 4, Lab GraphQL) para
      decidir con evidencia; si el laboratorio demuestra que conviene para algún consumidor, se propone como nuevo ADR.
    - **HATEOAS:** casi ningún cliente lo usa.
    - **Repositorio genérico sobre EF:** EF ya es repositorio y unidad de trabajo; repositorios específicos por
      agregado sí, genérico no.
    - **Lógica de negocio en procedimientos almacenados o triggers:** en la base solo queda lo que protege integridad
      sin decidir negocio (constraints, FKs, RLS, contador atómico, ledger inmutable). Una regla en dos lugares
      termina contradiciéndose, y con EF un trigger que cambia columnas deja la entidad en memoria desactualizada.
    - **Proyecto `Contracts` separado:** ver ADR 1.
12. **El dominio es dueño de las reglas.** Las entidades tienen comportamiento, no solo propiedades: `Order.AddItem()`
    recalcula el total y valida existencia y duplicados; `Order.ChangeStatus()` valida la transición contra la máquina
    de estados y registra el `OrderStatusHistory`; los movimientos de inventario se crean desde el dominio y el
    servicio los persiste con la orden en una sola transacción. Setters privados y constructores que garantizan un
    estado válido. Cada regla RN-xx de los casos de uso se prueba con pruebas unitarias del dominio, sin base de
    datos; las pruebas de integración verifican que la persistencia no la rompe.
13. **Identidad.** La API no emite tokens: confía en un proveedor de identidad. Desarrollo: `dotnet user-jwts`.
    Para login real (A1 en adelante) se elige en F3 entre **Keycloak en Docker** (recomendado para el portafolio:
    local, gratuito, multi-realm), **Microsoft Entra External ID** (encaja con el camino a Azure de F12) o un emisor
    propio mínimo solo para la demo. El token lleva `sub`, `tenant` (slug) y roles; los **permisos finos** se
    resuelven en la API contra el catálogo (usuario ↔ tenant ↔ permisos) con caché, para no inflar el token ni
    depender de que el proveedor los conozca. Un usuario pertenece a un solo tenant (salvo `PlatformAdmin`).
14. **Plataforma Azure (F12).** Detalle de servicios, costos y comandos en `docs/azure.md`.
    - **Cómputo:** la API en **Azure Container Apps** (misma imagen Docker de F10; escala a cero). Los consumidores de
      eventos en **Azure Functions, modelo isolated, .NET 10, plan Flex Consumption**. El modelo in-process está en
      retiro, el plan Linux Consumption también (sin funciones nuevas desde 2025, retiro en 2028) y el soporte de
      .NET 8 y 9 en Functions termina en noviembre de 2026: se arranca directo en .NET 10 y Flex.
    - **Datos:** **Azure SQL Database desde F1** (ADR 15): free tier para el catálogo y la base compartida; un tenant
      dedicado = una base creada con SKU explícito, con **elastic pool** como opción de producción cuando haya varios.
      **Blob Storage** para las exportaciones de tenant de F4. Cosmos DB descartado: el modelo es relacional y la RLS
      es de SQL Server.
    - **Mensajería:** el Outbox de F7 publica en **Service Bus** (topic por tipo de evento); la Function lo consume.
      Storage Queues solo si se necesitara algo más barato y sin topics.
    - **Identidad de servicios:** **Managed Identity con RBAC en todo**, nada de connection strings ni llaves en
      configuración. Roles mínimos por recurso (p. ej. `Key Vault Secrets User`, `Storage Blob Data Contributor`,
      `Azure Service Bus Data Sender/Receiver`, usuario de base de datos ligado a la identidad). En código,
      `DefaultAzureCredential`: en local usa tu sesión de Visual Studio o `az login`; en Azure, la Managed Identity.
    - **Secretos y configuración:** **Key Vault** leído como `IConfiguration`
      (`Azure.Extensions.AspNetCore.Configuration.Secrets`); solo para lo que no admita Managed Identity.
    - **Observabilidad:** **Application Insights** vía la distro `Azure.Monitor.OpenTelemetry.AspNetCore`. Como F0 ya
      instrumenta con OpenTelemetry, cambiar de exportador no toca el código de la aplicación.
    - **Infraestructura como código:** **Bicep** (nativo de Azure; Terraform como alternativa si hiciera falta
      multinube). La CLI sirve para aprender y explorar; lo que se queda, en Bicep.
    - **CI/CD:** GitHub Actions con **credenciales federadas OIDC** contra Entra ID (sin secretos de Azure en el
      repo); despliegue de la imagen a Container Apps y migraciones por tenant con `efbundle`.
    - **Costos:** **alerta de presupuesto** en la suscripción desde el primer día; un resource group por entorno para
      borrarlo completo al terminar de practicar.
    - **Opcional, fuera del núcleo:** API Management (el equivalente a lo que haces en MuleSoft; buen laboratorio de
      políticas, cuotas y versiones), Durable Functions (candidata natural para orquestar el alta de un tenant
      dedicado), App Service (alternativa más simple a Container Apps).
15. **La base de datos vive en Azure SQL desde F1.** Las bases `OrderFlowCF_*` no se crean en Docker sino en un
    servidor lógico de Azure SQL de la suscripción personal (cuenta gratuita creada el 2026-11-08, con presupuesto de
    10 USD/mes y alertas de costo real y pronóstico).
    - **Servidor:** autenticación **solo Entra ID** (sin logins SQL); administrador de Entra = tu cuenta; regla de
      firewall con tu IP (se actualiza si cambia). Definido en **Bicep** desde F1 (`infra/`), no a mano.
    - **Catálogo y compartida:** **free tier** (hasta 10 bases por suscripción, 100,000 vCore-segundos y 32 GB al mes
      cada una) con el comportamiento **"pausar hasta el mes siguiente"** al agotar la cuota: costo cero garantizado.
    - **Tenants dedicados (F4):** el servicio de alta crea la base con **SKU explícito y barato**
      (`CREATE DATABASE … (EDITION = 'Basic', SERVICE_OBJECTIVE = 'Basic', MAXSIZE = 2 GB)`) y solo después aplica
      `MigrateAsync`. **Nunca se deja que EF cree la base**: usaría el SKU por defecto (General Purpose provisionado,
      cientos de dólares al mes). Crear una base en Azure es asíncrono, lo que encaja con el `202 Accepted` del ADR 9.
      Los tenants de prueba se dan de baja al terminar cada sesión. En producción la respuesta sería un elastic pool.
    - **Conexión:** `Authentication=Active Directory Default` (tu `az login` en desarrollo, Managed Identity en
      Azure), `Encrypt=True`, `Connect Timeout=60` y **`EnableRetryOnFailure` desde F1**: la base free tier se pausa tras
      15 minutos sin uso y la primera conexión tarda en despertarla. Con reintentos activos, las transacciones explícitas
      se ejecutan dentro de `Database.CreateExecutionStrategy()` (F6).
    - **Restricciones de Azure SQL Database que el diseño ya respeta:** sin consultas entre bases (cada contexto se
      conecta a su base), sin `USE`, sin SQL Agent. RLS, `SESSION_CONTEXT`, tablas temporales y columnas calculadas
      funcionan igual que en SQL Server 2022.
    - **Lo que se queda en Docker local:** las **pruebas de integración** (Testcontainers; nunca contra Azure: rapidez,
      costo y aislamiento) y las **bases de referencia** (`OrderFlow`, `OrderFlow_*`, `OrderFlowCatalog`).
    - **MCP:** dos servidores en Claude Code: `sqlserver` (Docker local, referencia) y `sqlserver-azure` (servidor de
      Azure con Entra), para revisar las bases nuevas y compararlas con la referencia.
    - **Hábitos de costo:** cerrar SSMS y Azure Data Studio al terminar (una conexión abierta impide la pausa y consume
      cuota); alerta en la métrica *Free amount remaining*; borrar tenants dedicados de prueba.
    - **Alternativas descartadas:** todo en Docker hasta F12 (se pierde la práctica de nube real durante cuatro meses);
      pool de bases free precreadas para dedicadas (costo cero pero lógica de asignación que no aporta a F4); dedicadas
      en Docker y el resto en Azure (dos entornos mezclados).

## 4. Plan por fases

**Definición de terminado de cada fase:** compila sin advertencias, pruebas de la fase verdes (unitarias e
integración), `.http` con casos felices y negativos, nota en `docs/decisiones.md`, **README actualizado** (sección 7),
endpoints nuevos conformes a `docs/guia-api.md`, y **cierre de entrevista** (sección 6). No se empieza una fase sin
aprobar la anterior.

### Bloque 1 — Núcleo (objetivo: listo para entrevistas)

#### F0 — Esqueleto del host
Solución con los cuatro proyectos del ADR 1 más los de prueba; `Directory.Build.props` (nullable, analizadores, advertencias como errores), `Directory.Packages.props`,
`.editorconfig`; DI por capa; fronteras; ProblemDetails + manejador global; middleware de logging; JWT dev; fallback
policy; CORS; rate limiting; timeouts; health **liveness/readiness/startup**; OpenTelemetry; OpenAPI + Swagger UI;
versionado `/api/v1`; options con `ValidateOnStart`; `TimeProvider` registrado; prueba de arquitectura de fronteras.
Además: **`docs/guia-api.md`** (sección 8) y el **README inicial** del repositorio (qué es, cómo levantarlo, diagrama
de capas).
*Listo cuando:* `/health/live` y `/health/ready` responden, un endpoint protegido da 401/403/200 según el token, la
prueba de arquitectura falla si `Api` referencia `Infrastructure`, y alguien que clone el repo lo levanta siguiendo
solo el README.

#### F0.5 — Casos de uso del producto
`docs/casos-de-uso.md` (sección 9): actores y permisos, flujos de catálogo, clientes, órdenes e inventario, reglas de
negocio numeradas (RN-01, RN-02…) y criterios de aceptación en formato *Dado / Cuando / Entonces*. Sin código.
El esquema de F2 y las reglas de F6 se derivan de aquí; cada prueba de F6 cita la regla que verifica.
*Listo cuando:* cada caso de uso tiene actor, flujo principal, alternativos y criterios de aceptación, y las preguntas
abiertas están resueltas o marcadas como decisión pendiente.

#### F1 — Catálogo Code First en Azure SQL
**Arranque de Azure (ADR 15):** resource group `rg-orderflow-dev`; servidor lógico de Azure SQL con autenticación solo
Entra (administrador = tu cuenta); regla de firewall para tu IP; bases free tier `OrderFlowCF_Catalog` y
`OrderFlowCF_Shared` con "pausar al agotar"; todo definido en `infra/main.bicep` y desplegado con
`az deployment group create`. Alerta de pronóstico en el presupuesto y alerta en *Free amount remaining*. MCP
`sqlserver-azure` registrado en Claude Code.
**Código:** `CatalogTenant`, `CatalogDbContext` + configuración Fluent, cadena con `Authentication=Active Directory
Default`, `EnableRetryOnFailure` y timeout de 60 s; primera migración aplicada a `OrderFlowCF_Catalog` en Azure;
primera prueba de integración con Testcontainers (local).
*Listo cuando:* `catalog.Tenants` en Azure tiene PK, UNIQUE de slug, CHECK de formato, defaults y rowversion (verificado
con el MCP `sqlserver-azure`); la migración se aplicó desde tu PC **sin ninguna contraseña**; borrar el resource group y
volver a desplegar los Bicep recrea servidor y bases; la prueba de integración aplica la migración en un contenedor
vacío; y el costo del mes sigue en 0.
Estimado: 12–15 horas (incluye el arranque de Azure).

#### F2 — Esquema del tenant Code First
OrderStatuses, TenantOrderCounters, Categories, Customers, Products (temporal), Orders, OrderItems, OrderStatusHistory,
InventoryMovementTypes, InventoryMovements, AuditLog. FKs compuestas con `TenantId`, índices únicos y filtrados,
columnas calculadas persistidas, datos de referencia con `HasData`, tipos de fecha y dinero según ADR 2. Migración con
RLS, procedimiento del contador y trigger de inmutabilidad del ledger (`HasTrigger` solo ahí).
**Modelo de dominio rico (ADR 12):** `Order` con `AddItem`, `RemoveItem` y `ChangeStatus`; máquina de estados;
`Product` con ajuste de existencia que produce el movimiento de ledger; constructores y setters que impiden estados
inválidos. Pruebas unitarias del dominio para cada regla RN-xx disponible, sin base de datos.
Además: **generador de datos de demostración** con Bogus (proyecto o herramienta `OrderFlow.Seeder`): por tenant,
volumen configurable (p. ej. 500 clientes, 200 productos, 5 000 órdenes con líneas y movimientos coherentes), semilla
fija para que sea reproducible; nunca corre en producción.
*Listo cuando:* las migraciones están aplicadas a `OrderFlowCF_Shared` en Azure; aplicadas también a una base
local de comparación (`OrderFlowCF_Compare` en Docker), `compare_schemas` contra la `OrderFlow` actual muestra solo
diferencias explicadas (las esperadas:
los triggers de total, stock e historial ya no existen porque su lógica pasó al dominio); las pruebas de integración
verifican RLS, constraints y la inmutabilidad del ledger; las pruebas unitarias cubren total, transiciones y movimientos;
y el generador llena un tenant de prueba **usando los métodos del dominio**, respetando todas las reglas.
⚠ F2 y F3 son el tramo donde más fácil es perfeccionar en lugar de avanzar: si F2 pasa de 2.5 semanas, se recorta.

#### F3 — Tenancy en tiempo de ejecución (base compartida) e identidad
**Decisión de identidad (ADR 13):** elegir proveedor, levantarlo (si es Keycloak, en el docker-compose) y documentar
cómo se emite el claim `tenant` y los roles. Tablas de usuarios y permisos por tenant en el catálogo.
Claim `tenant`, middleware de resolución con caché, `ITenantContext`, interceptor de `SESSION_CONTEXT`, filtro global,
`TenantId` en logs y traces; `IPermissionService` contra el catálogo con caché. **Solo tenants de la base compartida;**
las dedicadas llegan en F4.
*Listo cuando:* pruebas con dos tenants de la base compartida demuestran aislamiento; un token emitido por el proveedor
elegido funciona de punta a punta; token sin tenant o con tenant inactivo → 403; un usuario sin el permiso requerido →
403 aunque el recurso sea de su tenant.

#### F4 — Tenants dedicados y ciclo de vida por endpoint
**Enrutamiento a bases dedicadas:** la cadena de `TenantDbContext` se elige según el modelo del tenant en el catálogo;
las pruebas de aislamiento de F3 se repiten con un tenant dedicado.
**Creación de bases dedicadas en Azure (ADR 15):** el servicio de alta ejecuta `CREATE DATABASE` con SKU explícito
(Basic), espera a que la base esté en línea (operación asíncrona, de ahí el 202) y luego aplica `MigrateAsync` y la
semilla. La identidad de aprovisionamiento tiene `##MS_DatabaseManager##` en el servidor; la de la app no.
`POST /api/admin/tenants` (compartido o dedicado; dedicado responde **202** con operación consultable; idempotente por
slug), `POST /api/admin/tenants/migrate` (202, todas las bases pendientes), `GET /api/v1/operations/{id}`,
`GET /api/admin/tenants`, desactivar, **exportar datos del tenant**, **dar de baja** (archivar o eliminar su base con
confirmación explícita). Cadena de administración separada. Health por tenant.
*Listo cuando:* crear un tenant dedicado por la API deja una base idéntica a la plantilla (0 diferencias), un usuario
de ese tenant opera contra su propia base con el mismo comportamiento que uno compartido, repetir la llamada no duplica
nada, la base dedicada quedó en el SKU Basic (no en el de por defecto), y la baja de un tenant de prueba deja su
exportación, ninguna fila suya en la base compartida y su base eliminada de Azure.

#### F5 — Lecturas
Categorías, productos (incluido historial temporal), clientes, órdenes con detalle. Sobre de paginación, filtros, orden
por lista blanca, búsqueda "contiene" con escape, proyecciones, `AsNoTracking`, output caching en catálogos.
*Listo cuando:* `.http` cubre paginación, límites, filtros, orden y búsqueda; `explain_query` confirma uso de índices
**con el volumen del generador** (no con 10 filas).

#### F6 — Escrituras y reglas de negocio
Alta/edición de clientes, productos, categorías; orden con líneas en una transacción: el **servicio orquesta** (carga,
llama al dominio, obtiene el OrderNumber del contador, persiste orden + líneas + movimientos + historial en un solo
`SaveChanges`, dentro de la estrategia de ejecución de EF por los reintentos del ADR 15) y el **dominio decide**
(total, existencia, transiciones); máquina de estados (409); FluentValidation; errores tipados y `docs/errores.md`; permisos por recurso
en acciones sensibles; `ETag`/`If-Match` → 412; `Idempotency-Key` en `POST /orders`.
*Listo cuando:* pruebas cubren sobreventa (rechazada), transición inválida (409), edición concurrente (412), acción sin
permiso (403) y reintento con la misma `Idempotency-Key` (misma respuesta, sin duplicar); **cada regla RN-xx de
`docs/casos-de-uso.md` tiene al menos una prueba que la cita**.

### Bloque 2 — Profundización

#### F7 — Auditoría y eventos
`SaveChangesInterceptor` → `AuditLog`; Outbox en la misma transacción; `BackgroundService` que la despacha.
*Listo cuando:* crear y modificar una orden deja auditoría y un evento despachado.

#### F8 — Calidad continua
Completar la suite (las pruebas ya vienen de cada fase): huecos de cobertura en lo crítico, pruebas de endpoints con
`WebApplicationFactory`, prueba de humo de carga (k6 o NBomber) con presupuesto de tiempo por endpoint.
*Listo cuando:* `dotnet test` corre todo en limpio en una máquina sin bases previas.

#### F9 — Endurecimiento y escala
Estrategia de ejecución de EF con reintentos; resiliencia HTTP; headers de seguridad; límites de tamaño; compresión;
redacción de datos sensibles en logs; logins de mínimo privilegio; secretos fuera del repo; instrumentación de
EF/SqlClient; métricas propias; **caché y rate limiting distribuidos, llaves de Data Protection persistidas, contador de
órdenes seguro con varias instancias**; respaldo y restauración por tenant documentados.
*Listo cuando:* dos instancias de la API detrás de un balanceador local se comportan igual que una.

#### F10 — Entrega y cadena de suministro
Dockerfile multi-stage; docker-compose o **.NET Aspire** (API + SQL Server + dashboard); CI con build, pruebas con
Testcontainers, `efbundle`, **paquetes vulnerables, CodeQL, escaneo de secretos, Dependabot**; guía de despliegue y de
migración de tenants.

#### F11 — IA (paso 11 acordado)
`POST /api/v1/assistant/ask` con Microsoft.Extensions.AI y herramientas tenant-aware sobre los servicios de la
aplicación, nunca SQL generado por el modelo.

#### F12 — Azure (paso 12 acordado, ADR 14)
1. **Fundamentos:** el resource group, el presupuesto y el servidor de Azure SQL ya existen desde F1; los Bicep de
   `infra/` crecen con el resto de los recursos.
2. **Identidad:** Managed Identity para la API y la Function; asignaciones RBAC mínimas; `DefaultAzureCredential` en
   el código.
3. **Datos:** usuarios de base de datos de Azure SQL ligados a las Managed Identities (la de la app con mínimo
   privilegio, la de aprovisionamiento con `##MS_DatabaseManager##`); migraciones aplicadas con `efbundle` desde el
   pipeline; Blob Storage para exportaciones de tenant.
4. **Secretos:** Key Vault como fuente de configuración (solo para lo que no admite Managed Identity).
5. **API:** imagen de F10 en Container Apps, con los health probes de liveness/readiness/startup de F0.
6. **Eventos:** Outbox → Service Bus → **Function isolated .NET 10 en Flex Consumption** (trigger de Service Bus);
   opcional, una segunda Function con trigger de Blob para procesar exportaciones.
7. **Observabilidad:** App Insights con la distro de Azure Monitor para OpenTelemetry; dashboard con requests, errores
   y latencia por tenant; traza distribuida API → Service Bus → Function.
8. **CI/CD:** GitHub Actions con OIDC (sin secretos de Azure en el repo): build, pruebas, imagen, despliegue y
   migraciones por tenant.
*Listo cuando:* los Bicep levantan todo desde cero con un solo despliegue; no existe ningún connection string ni llave
en la configuración de la API ni de la Function; crear una orden en la API desplegada produce un evento que la Function
procesa, visible como una sola traza en App Insights; y borrar el resource group elimina todo el costo.
Estimado: 20–28 horas (la base de datos ya vive en Azure desde F1).

### Lab GraphQL (opcional; requiere F3 y F5)
Endpoint `/graphql` de **solo lectura** con **Hot Chocolate**, en el mismo host, reutilizando los servicios de
lectura de `Application` (no consulta el `DbContext` por su cuenta). Esquema: productos, categorías, clientes y
órdenes con sus líneas.
Conceptos que se practican: esquema y tipos, resolvers sobre servicios existentes, **DataLoader** para evitar N+1,
paginación por cursor (connections), filtrado y ordenamiento controlados, **autorización** con las mismas políticas
y permisos de la API REST, y **aislamiento por tenant** igual que en REST (mismo `ITenantContext`, RLS y filtro
global). Protección contra abuso: profundidad y complejidad máximas por consulta, timeout, sin introspección fuera de
desarrollo, y persisted queries opcionales.
*Listo cuando:* una consulta de órdenes con líneas, cliente y productos genera un número acotado de consultas SQL
(verificado con logs de EF o `explain_query`); un usuario de un tenant no puede obtener datos de otro por ningún camino
del esquema; una consulta demasiado profunda o compleja se rechaza; y `docs/decisiones.md` registra la comparación
REST vs GraphQL en OrderFlow (qué ganó cada uno, en qué caso adoptarías GraphQL y por qué no es la API principal).
Estimado: 12–15 horas. Se agenda después del Bloque 1, o antes si una vacante concreta lo pide.

### Bloque opcional (cuando el núcleo esté sólido)
- Cuotas por tenant (vecino ruidoso) y feature flags por tenant (Microsoft.FeatureManagement).
- Webhooks para tenants, firmados y con reintentos, alimentados por el Outbox.
- Exportaciones grandes en segundo plano (CSV/Excel) y streaming con `IAsyncEnumerable`.
- SLOs con alertas y correlación hacia servicios externos.

## 5. Carril Angular (en paralelo al backend)

| Paso | Contenido | Arranca después de |
| --- | --- | --- |
| A0 | Proyecto Angular (standalone, signals), estructura, ESLint, pruebas, proxy a la API | F0 |
| A1 | Login con el token, interceptor de autenticación, guard de rutas, manejo de ProblemDetails en un solo lugar | F3 |
| A2 | **Cliente TypeScript generado del OpenAPI** (NSwag o Kiota) en el build; nada de servicios HTTP escritos a mano | F5 |
| A3 | Listas con paginación del servidor, filtros, orden y búsqueda (catálogo, clientes, órdenes) | F5 |
| A4 | Formularios de alta/edición con las mismas validaciones que la API, manejo de 409/412/422 | F6 |
| A5 | Pantalla de admin de tenants: alta, seguimiento de la operación (202) y baja | F4 |
| A6 | Asistente de IA en la UI | F11 |

## 6. Calendario y práctica de entrevistas

**Ritmo: 10 horas por semana**, a partir del lunes 9 de noviembre de 2026. Sugerencia de reparto: dos sesiones
entre semana de 2 horas (una para escribir código, otra para revisión y práctica de entrevista) y un bloque de 6 horas
el fin de semana para lo más pesado de cada fase.

**Objetivo del Bloque 1:** F0–F6 (incluida F0.5), Angular A0–A2 y el README de portafolio con demo, **≈ 160–170 horas,
terminando a mediados de marzo de 2027**. A3–A4 pasan al inicio del Bloque 2. (Versión 7: el arranque de Azure en F1
suma unas 5 horas y las fiestas se calculan a medio ritmo; por eso el cierre se mueve dos semanas.)

| Semanas | Fechas | Fase | Entregable |
| --- | --- | --- | --- |
| 1–2 | 9 – 22 nov | F0 | host completo, guía de estilo de la API, README inicial, repo en GitHub |
| 3–4 | 23 nov – 6 dic | F0.5 + F1 | casos de uso con reglas RN-xx; Azure SQL con Bicep; catálogo creado por migración |
| 5–6 | 7 – 20 dic | F2 | esquema por migraciones en Azure, RLS probada, modelo de dominio con sus reglas, generador |
| 7–9 | 21 dic – 10 ene | F3 | proveedor de identidad, tenancy (base compartida) y permisos (semanas 7–8 a medio ritmo por fiestas) |
| 10 | 11 – 17 ene | A0 | proyecto Angular base |
| 11–12 | 18 – 31 ene | F4 | tenants dedicados en Azure con SKU explícito; alta, migración, exportación y baja |
| 13–14 | 1 – 14 feb | F5 + A1 | lecturas con volumen real; login y manejo de errores en Angular |
| 15–17 | 15 feb – 7 mar | F6 | escrituras, reglas de negocio, concurrencia, idempotencia |
| 18 | 8 – 14 mar | A2 + portafolio | cliente TypeScript generado y una lista; README final y video demo |

**Hitos:**
- **Fin de la semana 9 (≈ 10 ene): reactivar postulaciones.** Con F0–F3 terminadas ya hay una historia de entrevista
  fuerte (Code First, dominio rico con sus reglas probadas, multi-tenant con RLS, identidad real, pruebas contra SQL
  Server real, base en Azure SQL con Entra y Bicep, MCP y agentes). Actualizar CV y LinkedIn en la semana 10 y
  postular desde la semana 11, en paralelo al proyecto. Si una vacante buena aparece antes, se postula: la historia de
  F0–F2 ya es sólida.
- **Fin de la semana 18 (≈ 14 mar): Bloque 1 cerrado** y portafolio publicado.
- **Bloque 2 + A3–A6:** ≈ 130–160 horas más; al mismo ritmo, de mediados de marzo a julio–agosto de 2027.

**Reglas del calendario:** si una fase se pasa más de una semana de lo previsto, se revisa el alcance de esa fase (no se
sacrifica el ritual de entrevista ni las pruebas para alcanzar la fecha). Una semana perdida por trabajo se recorre
completa; no se compensa con semanas de 20 horas.
- **Cada fase arranca** con las preguntas de entrevista que da el agente, contestadas **en voz alta y en inglés** antes
  de escribir código.
- **Cada fase cierra** con 30 minutos de "explica lo que construiste" en inglés, como en una entrevista: qué, por qué,
  qué alternativas descartaste. Las respuestas buenas se guardan en `docs/entrevista.md` como guía de estudio.
- **Al menos una tarea por fase se escribe sin ayuda y con reloj**, como práctica de live coding; después se revisa.

## 7. El proyecto como portafolio

El repositorio se publica en GitHub y el README es un entregable que **crece en cada fase**. Un reclutador lee el
README, no el código; un entrevistador lo usa para decidir qué preguntarte.

Contenido del README (al cierre del Bloque 1):
1. Qué es OrderFlow en tres líneas y para quién.
2. Diagrama de arquitectura (capas, catálogo, base compartida, bases dedicadas, flujo de un request con el tenant).
3. Decisiones clave con enlace a `docs/decisiones.md`, incluida la sección "lo que no hice y por qué".
4. Cómo levantarlo con **un solo comando** (docker-compose o Aspire) y cómo cargar datos de demostración.
5. Recorrido de la API: crear un tenant, crear una orden, ver aislamiento entre tenants.
6. Cómo se prueba (Testcontainers, pruebas de arquitectura) y estado del CI con badge.
7. Capturas o un video corto (3–5 min) de demo, en inglés.
8. Lo que sigue (Bloque 2) y qué aprendiste.

Reglas: README en **inglés** (el mercado al que apuntas), sin secretos ni datos reales, y verificado clonando el repo
en una carpeta vacía antes de cada actualización importante.

## 8. Guía de estilo de la API (resumen; detalle en `docs/guia-api.md`)

- **Rutas:** `/api/v{version}/{recurso}` en plural y en minúsculas con guiones (`/api/v1/orders`,
  `/api/v1/inventory-movements`); subrecursos para relaciones fuertes (`/api/v1/orders/{id}/items`); acciones que no
  son CRUD como subrecurso verbal en POST (`/api/v1/orders/{id}/cancel`). Administración de plataforma bajo
  `/api/admin/…`.
- **Ids:** enteros en la ruta; el `OrderNumber` es identificador de negocio y se busca por filtro, no por ruta.
- **JSON:** camelCase; enums como texto; fechas ISO 8601 en UTC con offset (`2026-11-06T18:30:00Z`); dinero como
  número decimal con 2 decimales; nada de `null` para colecciones vacías (`[]`).
- **Listas:** parámetros `page` (desde 1), `pageSize` (máx. 100, defecto 20), `sort` (`campo` o `-campo`, solo campos
  de la lista blanca), `search` (contiene); filtros por nombre de campo (`status=Pending`). Respuesta:
  `{ items, page, pageSize, totalCount }`.
- **Códigos:** 200 lectura/actualización con cuerpo; 201 creación con `Location`; 204 sin cuerpo; 202 operación larga
  con `Location` a `/api/v1/operations/{id}`; 400 entrada mal formada; 401 sin token; 403 sin permiso o tenant
  inactivo; 404 no existe (o no es de tu tenant: nunca 403 para no revelar existencia); 409 conflicto de estado;
  412 `If-Match` no coincide; 422 regla de negocio violada; 429 límite; 5xx solo fallos del servidor.
- **Headers:** `ETag` en recursos editables; `If-Match` obligatorio en PUT/PATCH de órdenes; `Idempotency-Key` en POST
  que crean órdenes; `traceId` siempre en errores.
- **Errores:** ProblemDetails con `type` = URI de `docs/errores.md`, `title` estable, `detail` legible, `errors` por
  campo en validación.
- **Versionado:** cambios que rompen → nueva versión; la vieja se marca con `Deprecation` y `Sunset`.

## 9. Plantilla de casos de uso (`docs/casos-de-uso.md`, se llena en F0.5)

**Actores y permisos** (tabla): PlatformAdmin, TenantAdmin, Vendedor, Almacén, Consulta — qué permiso tiene cada uno.

**Por cada caso de uso:**
```
CU-07 · Crear orden
Actor: Vendedor
Precondiciones: cliente activo del tenant; productos activos con existencia.
Flujo principal: 1) ... 2) ... 3) ...
Flujos alternativos: 3a) sin existencia suficiente → ...
Reglas: RN-12 (no se vende sin existencia), RN-13 (el total es la suma de las líneas), RN-14 (OrderNumber consecutivo por tenant)
Criterios de aceptación:
  Dado un producto con 5 unidades, cuando creo una orden por 6, entonces recibo 422 con la regla RN-12 y no se crea nada.
  Dado ... cuando ... entonces ...
```

**Preguntas que deben quedar resueltas antes de F2** (decisiones de producto, no técnicas):
- ¿Hay pago en el sistema o solo se registra que la orden se pagó?
- ¿Se permiten envíos parciales? ¿Devoluciones? ¿Afectan inventario?
- ¿Se puede cancelar una orden pagada o enviada? ¿Quién puede?
- ¿Los precios se congelan al crear la orden (sí, en `OrderItems.UnitPrice`) y qué pasa si cambia el precio del producto?
- ¿Hay descuentos o impuestos por línea u orden? ¿Una sola moneda por tenant?
- ¿Qué reportes necesita un TenantAdmin (ventas por periodo, productos más vendidos, inventario bajo)?
- ¿Un cliente puede pertenecer a varios tenants? (respuesta esperada: no; es otro cliente en cada tenant)
