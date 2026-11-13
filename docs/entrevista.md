# Interview guide — OrderFlow

Study guide built phase by phase. Each phase opens with the architect's interview questions (answered **out loud, in
English**, before writing code) and closes with a mini interview. Only the best version of each answer is kept here.

## My 3-minute story
_(Written at the end of F3 and refined at the end of the Block 1. What OrderFlow is, the hardest decision, what I would
do differently, and the MCP + agents workflow.)_

---

## F0 — Host skeleton

### Questions
1. _(from the architect)_

### Concepts to master (lived during F0; turn each into a Q/A)
- `Directory.Build.props`, Central Package Management and warnings as errors; the .NET 11 attempt (NU1510, NU1107).
- Composition root and `DisableTransitiveProjectReferences`: each layer registers its own services.
- ProblemDetails: `IExceptionHandler`, a `type` catalog, `UseStatusCodePages`, only replacing framework types.
- `[LoggerMessage]` and structured logging (templates, not interpolation).
- `AddXxx` vs `UseXxx`: DI registration vs middleware, and why pipeline order matters.
- Authentication vs authorization; fallback policy vs default policy.
- Policy-based vs role-based authorization; roles have no hierarchy; `RequireRole(a, b)` = OR, chained = AND.
- Rate limiting per user, `Retry-After` from the lease, 429 as ProblemDetails.
- Logs vs metrics vs traces; W3C `traceparent` and its flags; `ParentBased` sampling (why traces from the VS `.http`
  client were not recorded).
- Health checks: liveness vs readiness vs startup; why the database never goes in liveness.
- Cooperative request timeouts; layered timeouts (client 20 s, API, code); the `CancellationToken` down to I/O.
- Options pattern vs `IConfiguration`; defaults in code overridden by configuration.
- Primary constructors and their non-readonly captures.
- Repository per aggregate vs generic repository over EF Core.
- Deciding not to upgrade: evaluating a dependency conflict and writing it down as an ADR.

### Best answers
**Q:** How do you build an API before the first business endpoint? / What does "production-ready" mean to you?

**A:** Before the first business endpoint I build the host skeleton every endpoint will inherit, so nobody
has to remember cross-cutting concerns later. I think about it in five groups:

- **Foundations:** one place for the framework, analyzers and package versions (Directory.Build.props,
  central package management, warnings as errors) and a composition root, so the API can't even reference
  the infrastructure layer.
- **Failure contract:** every error, exception or not, comes back as typed ProblemDetails with a trace id,
  so clients handle errors one way and I can find any of them in the logs.
- **Security by default:** JWT validation against an external identity provider, a fallback policy so a
  forgotten [Authorize] can't expose an endpoint, named policies instead of roles, and CORS for browser
  clients only.
- **Protecting the server:** per-user rate limiting with Retry-After, cooperative request timeouts with the
  cancellation token flowing down to I/O, and configuration validated at startup, so a bad setting stops
  the deployment instead of breaking production.
- **Operability:** OpenTelemetry traces, metrics and logs; separate liveness, readiness and startup probes;
  URL versioning with one OpenAPI document per version.

The order matters: exception handling outermost, CORS before authentication so preflights and 401s work,
rate limiting after authorization because it partitions by user. With that in place, a new endpoint is
just a controller: protected, versioned, limited and observable without writing anything extra.

**Examples ready for follow-ups (from OrderFlow):**
- Errors: a `_ => Internal` default in the status-to-type map labeled a 405 as a server error; found with a test.
- Security: roles came back empty because JwtBearer maps `role` to `ClaimTypes.Role` on the way in.
- Protection: `Uri.TryCreate` accepted `localhost:4200` as a valid absolute URI (scheme "localhost"), so the
  CORS origin check needed a stricter rule; timeouts are layered (client 20 s, API, code).
- Operability: traces from the VS `.http` client weren't recorded because its `traceparent` carried flag `00`
  and the `ParentBased` sampler respected it.

**Trade-offs I can mention:** OpenAPI is not exposed in production (generated at build time for clients);
I kept .NET 10 instead of upgrading to .NET 11 and documented why in ADR 016.

### What I missed / to improve
-

### Live coding
- Task:
- Time limit / actual time:
- Notes:

---

## Answer template
```
**Q:** question
**A:** short answer first (one sentence), then the why, then a concrete example from OrderFlow,
then the trade-off or the alternative I rejected.
```
