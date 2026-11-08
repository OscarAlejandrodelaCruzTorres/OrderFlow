# OrderFlow — contexto para agentes

API multi-tenant de órdenes, inventario y clientes en .NET 10 + SQL Server, construida fase por fase como proyecto de
arquitectura y portafolio. **Antes de cualquier tarea lee `docs/ARQUITECTURA.md`** (decisiones ADR 1–14 y plan F0–F12).

## Modo de trabajo
- Alejandro escribe el código para aprenderlo. El agente diseña, explica, da tareas con pistas y revisa.
  **Solo genera código completo si Alejandro escribe `genera`.** Los comandos de andamiaje (`dotnet new`, `dotnet add`,
  `dotnet ef`) sí se dan completos.
- No se avanza de fase sin su `ok`. Cada fase cierra con su "Listo cuando", pruebas verdes, `.http`, nota en
  `docs/decisiones.md`, README actualizado y cierre de entrevista en inglés (`docs/entrevista.md`).
- Sin commit ni push: los hace Alejandro.

## Estructura del repo
    src/                     OrderFlow.Domain · OrderFlow.Application · OrderFlow.Infrastructure · OrderFlow.Api
    tests/                   OrderFlow.UnitTests · OrderFlow.IntegrationTests · OrderFlow.ArchitectureTests
    docs/                    ARQUITECTURA · decisiones · guia-api · casos-de-uso · errores · entrevista · azure
    reference/Shop/          proyecto de referencia de calidad (solo lectura; no copiar su EnsureCreated)

## Comandos (se completan conforme existan)
    dotnet build OrderFlow.slnx
    dotnet test OrderFlow.slnx
    dotnet run --project src/OrderFlow.Api                                    # solo Alejandro
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

## Agentes disponibles (en `~/.claude/agents`)
Útiles aquí: `api-reviewer`, `db-reviewer` (contra las bases `OrderFlowCF_*` y para comparar con las de referencia),
`verifier`, `docs-keeper`. **`db-fixer` no se usa en este repo:** los cambios de esquema van siempre por migración.

## Prohibido
- Modificar las bases de referencia o ejecutar DDL fuera de migraciones.
- Paquetes fuera del stack del ADR sin proponerlos antes (con alternativas y licencia).
- MediatR, CQRS completo, microservicios, HATEOAS, repositorio genérico, GraphQL como API principal (ADR 11).
- Secretos en el repo.
