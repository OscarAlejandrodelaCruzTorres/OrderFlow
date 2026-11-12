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
**Q:**
**A:**

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
