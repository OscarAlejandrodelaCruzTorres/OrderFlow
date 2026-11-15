# Interview guide — OrderFlow

Study guide built phase by phase. Each phase opens with the architect's interview questions (answered **out loud, in
English**, before writing code) and closes with a mini interview. Only the best version of each answer is kept here.

## My 3-minute story
_(Written at the end of F3 and refined at the end of the Block 1. What OrderFlow is, the hardest decision, what I would
do differently, and the MCP + agents workflow.)_

---

## F0 — Host skeleton

### Questions
Closing mini interview, 2026-11-14 (my score in brackets):
1. You enforce boundaries with `DisableTransitiveProjectReferences` and an ArchUnitNET test. Isn't one redundant? [2/5]
2. Liveness vs readiness vs startup: what goes wrong if liveness checks Azure SQL? [3/5]
3. Why a fallback policy instead of `[Authorize]` everywhere? How do you keep `[AllowAnonymous]` from being abused? [3/5]
4. Why not just let the Api reference Infrastructure, like most templates do? [2/5]
5. A client sends you a 504 with a `traceId`. How do you find out what happened? [3/5]
6. Why URL segment versioning and not a header or a media type? [2/5]

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

**Q:** Compile-time boundary plus an architecture test — isn't one redundant?
**A:** No, they catch two different failures. `DisableTransitiveProjectReferences` works at compile time:
Infrastructure reaches the API transitively through Composition, and without it any controller could quietly use a
DbContext. With it, that code doesn't compile — that stops the *accidental* dependency. But a developer can get past
the compiler on purpose by adding a direct ProjectReference; the ArchUnitNET test catches that, because it inspects
the compiled types and fails in CI the first time an API type uses an Infrastructure type. Caveat: an unused
reference isn't in the assembly metadata, so the test only fires once something uses it. I verified both by
breaking the boundary on purpose: it compiled, and the test went red.

**Q:** Liveness vs readiness vs startup — what goes wrong if liveness checks the database?
**A:** Each probe triggers a different action. Liveness asks "is the process stuck?" — failing restarts the
container. Readiness asks "can I send traffic?" — failing takes the instance out of rotation but keeps it alive.
Startup holds the other two back until a slow start finishes, so liveness can stay strict afterwards. If liveness
checks Azure SQL, a 30-second failover makes every instance fail at once, the orchestrator restarts all of them,
and restarting doesn't fix the database: a restart loop and a full outage from a dependency blip. Rule: liveness
only checks what a restart can fix. In OrderFlow `/health/live` runs no checks; Azure SQL goes in readiness.

**Q:** Fallback policy vs `[Authorize]` everywhere; how do you keep `[AllowAnonymous]` from being abused?
**A:** With a fallback policy the API is secure by default: any endpoint with no authorization metadata requires an
authenticated user, so a forgotten `[Authorize]` exposes nothing. (The default policy is different: it applies to
`[Authorize]` without a named policy.) The risk moves to `[AllowAnonymous]`, which overrides everything, even
`[Authorize]` on the class. So I guard it with a test that enumerates every endpoint and fails if one is anonymous
and not in an explicit allowlist — the health probes and the OpenAPI document. A new anonymous endpoint breaks CI
until someone adds it to the list, and that change is visible in review.

**Q:** Why not let the Api reference Infrastructure, like most templates?
**A:** It works, but then the boundary is a convention: any controller can inject the DbContext and skip the
application service. In OrderFlow that's a security issue, not style — from F3 the services enforce resource
permissions and the tenant context. With a Composition project the host only references Composition and
Application, and Infrastructure types don't compile in the API. Composition only runs at startup, wiring interfaces
to implementations; at runtime a request goes controller → application service → interface, and the implementation
is injected. The cost is one project with no logic — cheap for a boundary the compiler enforces.

**Q:** A client reports a 504 with a traceId. Walk me through it.
**A:** The traceId is the same in the response, the logs and the trace, so I search for it in the telemetry backend —
the Aspire dashboard locally, Application Insights in Azure, same id because it's all OpenTelemetry. The request log
line gives me endpoint, status and duration — about 30 s confirms it was our own request timeout. The exception
handler logged the cancellation with its stack trace, so I know which await was running. The trace timeline shows
which span consumed the time, typically a slow SQL query. I can't replay the request from the logs — I don't log
query strings or bodies on purpose — so I reproduce it with the client's input. Then I decide: optimise the slow
operation, or, if it's legitimately long, move it to a 202 Accepted operation instead of raising the timeout.

**Q:** Why URL segment versioning and not a header or a media type?
**A:** Visibility and simplicity for every consumer. The version is in the URL, so it shows up in logs, traces,
browser tabs, curl and `.http` files without inspecting headers. It's cache-friendly: the URL is the cache key,
while header versioning needs `Vary` and many proxies and CDNs handle that poorly. And it maps cleanly to one
OpenAPI document per version, which is what our generated TypeScript client consumes. The trade-off: purists argue
a URI should identify the resource, not its representation, which is why media-type versioning (like GitHub's
`Accept` header) is the "most RESTful" option, and it allows finer per-resource versioning. For a portfolio API
consumed by a generated client, discoverability wins. Changing the base URL in one place isn't the reason: a
default header in an HTTP interceptor is also set in one place.

### What I missed / to improve
- **Registration vs runtime flow (Q4):** said "the flow is api → composition → infrastructure". Composition only runs
  at startup; at runtime a request only sees interfaces. Most important lesson of F0.
- **Which tool acts when (Q1):** the architecture test never fails at compile time; that's the compiler's job.
- **Answer the question actually asked (Q2):** defined the probes but never said what goes wrong (restart loop).
- **Fallback vs default policy (Q3):** the fallback applies to endpoints with *no* authorization metadata.
- **Don't contradict my own design (Q5):** claimed the logs hold "the complete request"; they deliberately don't.
- **Why-not questions (Q4, Q6):** acknowledge the alternative is valid, then weigh costs. My Q6 argument (change
  the base URL in one place) is rebutted by a default header in an interceptor.
- **Lead with the thesis:** first sentence = the answer; then the why, an OrderFlow example, the trade-off.

### Live coding
- Task: error catalog (`ProblemTypes`), `GlobalExceptionHandler`, and framework errors (404, 405, 401, 429) as
  ProblemDetails with `UseStatusCodePages` + `CustomizeProblemDetails` (task 3).
- Time limit / actual time: target 60 min / actual: **~3–4 h wall-clock on Nov 8, 2026**, estimated from the commit
  times: steps 1–2 inside the 12:47–15:52 block (shared with task 2 and the user-jwts setup), step 3 and its cleanup
  15:52–17:50. Includes learning time (analyzers, ProblemDetails internals), not just typing.
- Notes:
  - Logger calls failed the build (CA1848/CA1873): learned `[LoggerMessage]` in a `partial` class.
  - First version of the status map used `_ => Internal`, which labeled a 405 as a server error; a manual test of a
    non-catalog status caught it. In an interview: write `_ => null` from the start and keep the framework's RFC link.
  - First version overwrote every `type`, including ours; fixed by only replacing null or `tools.ietf.org` types.
  - Next time: say the edge cases out loud before coding (non-catalog status, types set by our own code).

---

## F0.5 — Product use cases

### Questions
Closing mini interview, 2026-11-14 (my score in brackets):
1. You wrote 29 business rules before any code. Isn't that waterfall? How is RN-03 enforced and verified? [1/5]
2. Why calculate tax per line instead of on the total? What's the .NET rounding trap? [3/5]
3. Why decrement stock at order creation, not at shipping? Downside and mitigation? [3/5]
4. Why deactivate instead of delete, and why not an `IsDeleted` global query filter? [4/5]
5. PlatformAdmin can't see tenant business data: why, and how is it enforced? [3/5]

### Best answers
**Q:** Isn't writing the rules upfront waterfall? How is RN-03 enforced and verified?
**A:** Partly — it's upfront design, but deliberately small: rules only for the current block, everything else
parked as future use cases, and the document changes before the code does, so it's a living source of truth. The
payoff is traceability: decision → rule → code → test. RN-03, "no sale without stock", lives in the domain: the Order
aggregate rejects the whole order if any line lacks stock, so a partial order can never exist. A domain unit test
named after the rule proves it without a database, and an integration test checks that after the 422 nothing was
persisted. Without written rules I'd invent business behaviour on the fly, and nobody could tell me I got it wrong.

**Q:** Tax per line or on the total? The .NET rounding trap?
**A:** Per line, for two reasons. It matches the invoice — CFDI 4.0 computes tax per line item — so two lines of
$10.03 at 16% give 3.20, while on the total it would be 3.21, a cent off the invoice. And each line is
self-contained: removing a line subtracts exactly its tax. The trap: `Math.Round` defaults to banker's rounding —
midpoints go to the even number, 2.125 becomes 2.12. So the domain rounds money in one place with
`MidpointRounding.AwayFromZero` explicit, always on `decimal`, never `double`, and the $10.03 case is a unit test.

**Q:** Why decrement stock at creation? Downside and mitigation?
**A:** An accepted order should be a promise I can keep: I never sell what I don't have, and the customer learns
about a shortage immediately. Creation writes one outbound ledger movement per line; paying and shipping don't touch
stock again; cancelling writes reversal movements. The downside: unpaid pending orders hold stock indefinitely, so
availability looks lower than it is and someone could hoard items. Mitigation: manual cancellation now, then an
automatic expiry after N hours — a background service using `TimeProvider` so it's testable. Decrementing at payment
was rejected because the customer would learn about a shortage too late.

**Q:** Deactivate instead of delete — and why not an `IsDeleted` global query filter?
**A:** Deleting breaks history: orders reference the customer through a foreign key, so a hard delete either fails
or cascades into accounting data. Deactivation keeps past orders intact and lets me reactivate a returning customer.
A global filter hides inactive records from every query, including those that need them: an old order's detail
would lose its customer, and I couldn't list inactive customers to win them back. `IgnoreQueryFilters` exists, but
then every history query must remember to opt out, and forgetting is a silent bug. In OrderFlow the global filter
has a security job — tenant isolation, always on — while "active" is a business rule, applied explicitly where it
matters: new orders and default lists.

**Q:** Why can't the PlatformAdmin see tenant data, and how is it enforced?
**A:** Privacy and least privilege: the data belongs to the tenant, and managing tenants doesn't require reading
their sales. It also limits the blast radius: a stolen platform account can't exfiltrate every customer's data.
Enforcement is structural, not a hidden button. The tenant comes only from the token, and a PlatformAdmin token
carries no tenant, so business endpoints answer 403 before touching the database. Even if the API had a bug, row-level
security returns nothing without a TenantId in `SESSION_CONTEXT`: two independent layers. Platform operations live
apart, under `/api/admin` with their own policy.

### What I missed / to improve
- **Ask when a term is unclear (Q1):** misread "waterfall" as all-or-nothing orders. Say: "Do you mean upfront design?"
- **Know my own model (Q3):** said stock goes out at payment; it's decremented at creation (RN-04). Price is frozen,
  not cost.
- **Depth (Q2, Q5):** right ideas in one line sound memorised. Aim for 30–60 seconds: why, example, trade-off.
- **Defense in depth (Q5):** mention both layers — API (no tenant → 403) and database (RLS).
- **Precise vocabulary (Q4):** customers are *deactivated*, not *deleted*.

---

## Answer template
```
**Q:** question
**A:** short answer first (one sentence), then the why, then a concrete example from OrderFlow,
then the trade-off or the alternative I rejected.
```
