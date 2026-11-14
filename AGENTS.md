# OrderFlow — contexto para agentes

API multi-tenant de órdenes, inventario y clientes en .NET 10 + SQL Server, construida fase por fase como proyecto de
arquitectura y portafolio. **Antes de cualquier tarea lee `docs/ARQUITECTURA.md`** (plan F0–F12) y el índice de
`docs/decisiones.md` (ADR 001–016).

## Modo de trabajo
- Alejandro escribe el código para aprenderlo. El agente diseña, explica, da tareas con pistas y revisa.
  **Solo genera código completo si Alejandro escribe `genera`.** Los comandos de andamiaje (`dotnet new`, `dotnet add`,
  `dotnet ef`) sí se dan completos.
- No se avanza de fase sin su `ok`. Cada fase cierra con su "Listo cuando", pruebas verdes, `.http`, nota en
  `docs/decisiones.md`, README actualizado y cierre de entrevista en inglés (`docs/entrevista.md`).
- Sin commit ni push: los hace Alejandro.

## Estructura del repo
    src/                     OrderFlow.Domain · OrderFlow.Application · OrderFlow.Infrastructure ·
                             OrderFlow.Composition (composition root: une Application + Infrastructure) · OrderFlow.Api
    tests/                   OrderFlow.UnitTests · OrderFlow.IntegrationTests · OrderFlow.ArchitectureTests
    docs/                    ARQUITECTURA · decisiones · guia-api · casos-de-uso · errores · entrevista · azure
    reference/Shop/          proyecto de referencia de calidad (solo lectura; no copiar su EnsureCreated).
                             **Solo local:** está en .gitignore y se purgó del historial; nunca se publica ni se cita
                             su código en issues, PRs o README.

## Comandos (se completan conforme existan)
    dotnet build OrderFlow.slnx
    dotnet test OrderFlow.slnx
    dotnet run --project src/OrderFlow.Api --launch-profile https             # solo Alejandro
    dotnet run --project src/OrderFlow.Api --launch-profile https-telemetry   # exporta OTLP a localhost:4317
    docker run --rm -d --name aspire-dashboard -p 18888:18888 -p 4317:18889 -e DASHBOARD__FRONTEND__AUTHMODE=Unsecured mcr.microsoft.com/dotnet/aspire-dashboard
    dotnet ef migrations add <Nombre> -p src/OrderFlow.Infrastructure -s src/OrderFlow.Api -c <Contexto> -o <carpeta>
    dotnet ef database update -p src/OrderFlow.Infrastructure -s src/OrderFlow.Api -c <Contexto>

## Entornos de base de datos (ADR 15)
- **Azure SQL (bases del proyecto):** servidor lógico en la suscripción personal, autenticación **solo Entra ID**.
  `OrderFlowCF_Catalog` y `OrderFlowCF_Shared` en free tier; `OrderFlowCF_<Slug>` (dedicadas, desde F4) con SKU Basic
  explícito, nunca creadas por EF con el SKU por defecto. Conexión `Authentication=Active Directory Default` (tu
  `az login`), sin contraseñas; reintentos de EF activos. Se crean y cambian **solo por migraciones de EF Core** y la
  infraestructura **solo por Bicep** (`infra/`). MCP: `sqlserver-azure`.
- **Docker local** (`orderflow-sql`, `localhost,14333`): pruebas de integración con Testcontainers (nunca contra Azure),
  base de comparación `OrderFlowCF_Compare`, y **bases de referencia de solo lectura** (`OrderFlowCatalog`,
  `OrderFlow`, `OrderFlow_*`). MCP: `sqlserver`. Credenciales locales solo en `user-secrets`.
- **Costo:** presupuesto de 10 USD/mes con alertas. No dejar conexiones abiertas a Azure SQL (impiden la pausa del
  free tier); los tenants dedicados de prueba se dan de baja al terminar cada sesión.

## Reglas no negociables (resumen; detalle en los ADR)
- Capas con fronteras: el host compone con un `AddXxx()` por capa y tiene `DisableTransitiveProjectReferences`.
- Entra `XxxRequest`, sale `XxxResponse`; las entidades no cruzan el servicio. DTOs en `Application/Contracts/`.
- **El dominio es dueño de las reglas** (ADR 12): nada de lógica de negocio en controladores, en servicios que solo
  orquestan, ni en triggers o procedimientos. Cada regla RN-xx tiene prueba unitaria del dominio.
- Todo protegido por defecto; el tenant sale del token, nunca de la ruta ni del body; RLS **y** filtro global.
- Tenancy en dos pasos: F3 solo base compartida; F4 agrega dedicadas.
- Cero SQL concatenado; búsquedas "contiene" con parámetros y escape de `%`, `_`, `[`.
- `CancellationToken` en todo I/O, `AsNoTracking` en lecturas, paginación siempre acotada.
- Errores solo por el manejador global como ProblemDetails tipados (`docs/errores.md`).
- Endpoints conforme a `docs/guia-api.md`. Reglas de negocio solo si están en `docs/casos-de-uso.md`; si falta una,
  se pregunta.
- Comentarios que expliquen el **porqué** de cada decisión no obvia, al estilo de `reference/Shop`.

## Convenciones del host (aprendidas en F0; aplican a todo código nuevo)
- **Servicios del host** en `src/OrderFlow.Api/Extensions/`, un `AddApiXxx()` por tema (`AddApiErrors`, `AddApiSecurity`,
  `AddApiRateLimiting`, `AddApiObservability`, `AddApiRequestTimeouts`). El **orden del pipeline** (`app.UseXxx`) se
  queda visible en `Program.cs`, con un comentario del porqué de cada posición.
- **Nombres como constantes, nunca strings sueltos:** `Authorization/Policies`, `Authorization/Roles`,
  `RateLimiting/RateLimitPolicies`, `Application/Health/HealthTags` (en Application porque Api e Infrastructure lo
  comparten). Los valores de `Roles` son **literales** (`"PlatformAdmin"`), nunca `nameof`: son un contrato con el
  proveedor de identidad y un rename no debe cambiarlos.
- **Autorización por políticas:** los endpoints usan `[Authorize(Policy = Policies.X)]`, nunca `[Authorize(Roles = ...)]`.
  Los roles no tienen jerarquía. Los controladores de admin (`/api/admin/...`, `[ApiVersionNeutral]`) llevan
  `Policies.PlatformAdmin` a nivel de clase.
- **Errores:** ningún middleware ni `OnRejected` escribe body; pone el status y `UseStatusCodePages` genera el
  ProblemDetails. `CustomizeProblemDetails` solo reemplaza el `type` si es null o de `tools.ietf.org`; el que pone
  nuestro código se respeta. Catálogo en `Api/Errors/ProblemTypes.cs` (la base del URI vive en código).
- **Health checks:** `/health/live` sin checks (nunca la base); los checks de dependencias se registran en su capa con
  `HealthTags.Ready`. Los tres endpoints son anónimos y sin rate limiting.
- **Timeouts cooperativos:** el `CancellationToken` de la acción viaja hasta EF Core / `HttpClient`; sin él, el timeout
  no corta nada. Lo que tarda de verdad responde 202 (ADR 009); no se sube el timeout.
- **Hora:** siempre `TimeProvider` (registrado en Composition), nunca `DateTime.Now`/`UtcNow`.
- **Logging:** `[LoggerMessage]` en clases `partial` (CA1848/CA1873); plantillas con `{Nombre}`, nunca interpolación.
- **`.http`:** `{{host}}` sale de `http-client.env.json` (se versiona); los tokens de `dotnet user-jwts` en
  `http-client.env.json.user` (ignorado por `*.user`). Nunca un token en el `.http`.
- **Telemetría local:** el cliente `.http` de Visual Studio envía `traceparent` con bandera `00` y el muestreo
  `ParentBased` no graba esas trazas; para ver trazas en el dashboard usar navegador o `curl.exe`. Las métricas sí llegan.
- **Versiones:** .NET 10 fijado por `global.json` (ADR 016). Antes de subir una versión mayor de un paquete, verificar
  que todo el árbol sea compatible (NU1107); versiones solo en `Directory.Packages.props`.
- **Paquetes del framework:** los `Microsoft.Extensions.*` y `Microsoft.AspNetCore.*` van en la **misma versión mayor
  que el `TargetFramework`** (hoy 10.0.x) y se actualizan juntos, nunca sueltos desde el administrador de NuGet. Un
  paquete de versión mayor que el framework compartido gana en la resolución del host (gana la versión de assembly
  más alta) y reemplaza al del framework en tiempo de ejecución: compila, pero puede fallar en producción. Alternativa
  conocida: `<FrameworkReference Include="Microsoft.AspNetCore.App" />`, que usa siempre la versión del framework a
  cambio de ligar la capa a ASP.NET Core. Pasó en F0: los `Abstractions` quedaron en 11.0.0 tras el intento de .NET 11.
- **Commits** (los hace Alejandro): `tipo(Fx): descripción` con `feat`, `fix`, `refactor`, `style`, `chore`, `build`,
  `docs`, `test`.

## IA (F11)
No se implementa nada de IA antes de F11 (una versión mínima podría adelantarse después de F5 solo si una vacante lo
pide). Las ideas candidatas y las **reglas obligatorias** están en `docs/ARQUITECTURA.md`, sección F11: el modelo solo
usa herramientas sobre los services (nunca SQL), aislamiento por tenant, el dominio decide, humano en el ciclo para
escrituras, rate limiting y métricas de tokens, proveedor intercambiable con `Microsoft.Extensions.AI`. El alcance se
decide con un ADR al llegar a la fase.

## Agentes disponibles (en `~/.claude/agents`)
Útiles aquí: `api-reviewer`, `db-reviewer` (contra las bases `OrderFlowCF_*` y para comparar con las de referencia),
`verifier`, `docs-keeper`. **`db-fixer` no se usa en este repo:** los cambios de esquema van siempre por migración.

## Prohibido
- Modificar las bases de referencia o ejecutar DDL fuera de migraciones.
- Paquetes fuera del stack del ADR sin proponerlos antes (con alternativas y licencia).
- MediatR, CQRS completo, microservicios, HATEOAS, repositorio genérico, GraphQL como API principal (ADR 11).
- Secretos en el repo.
