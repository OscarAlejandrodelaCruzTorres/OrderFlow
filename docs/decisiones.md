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

El texto de los ADR 001–014 está resumido en `docs/ARQUITECTURA.md`, sección 3. Al cerrar cada fase se completan aquí
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

---

## Bitácora de fases

Una entrada corta al cerrar cada fase: qué se construyó, qué decisión se tomó o cambió, qué se aprendió.

### F0 — Esqueleto del host
_(pendiente)_
