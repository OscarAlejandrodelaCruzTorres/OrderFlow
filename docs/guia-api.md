# Guía de estilo de la API OrderFlow

Toda ruta, contrato y respuesta nueva sigue esta guía. Si un caso no está cubierto, se decide, se agrega aquí y se
registra en `docs/decisiones.md`.

## 1. Rutas
- Forma: `/api/v{version}/{recurso}`. Recursos en **plural**, minúsculas y con guiones:
  `/api/v1/orders`, `/api/v1/inventory-movements`.
- Subrecursos para relaciones fuertes: `/api/v1/orders/{orderId}/items`.
- Acciones que no son CRUD: subrecurso verbal con `POST`: `/api/v1/orders/{orderId}/cancel`,
  `/api/v1/orders/{orderId}/status-transitions`.
- Administración de plataforma: `/api/admin/...` (política `PlatformAdmin`).
- Operaciones largas: `/api/v1/operations/{operationId}`.
- Ids enteros en la ruta. Identificadores de negocio (`OrderNumber`, `Sku`) se buscan por filtro:
  `/api/v1/orders?orderNumber=ORD-20261108-000001`.

## 2. Verbos
| Verbo | Uso | Éxito |
| --- | --- | --- |
| GET | leer | 200 |
| POST | crear o ejecutar una acción | 201 con `Location` (creación) · 200/204 (acción) · 202 (larga) |
| PUT | reemplazar un recurso completo | 200 con el recurso o 204 |
| PATCH | cambio parcial (JSON Merge Patch) | 200 con el recurso |
| DELETE | eliminar o desactivar | 204 |

## 3. JSON
- Propiedades en **camelCase**. Enums como **texto** (`"status": "Pending"`).
- Fechas en ISO 8601 UTC con offset: `"createdAt": "2026-11-08T18:30:00Z"`.
- Dinero como número decimal con 2 decimales: `"totalAmount": 1250.50`. Sin símbolos ni separadores.
- Colecciones vacías como `[]`, nunca `null`. Campos opcionales ausentes se omiten o van en `null`, de forma consistente
  por contrato.
- Nombres de respuesta: `XxxResponse`; de entrada: `CreateXxxRequest`, `UpdateXxxRequest`.

## 4. Listas
Parámetros:
- `page` (desde 1, defecto 1), `pageSize` (defecto 20, máximo 100).
- `sort`: un campo de la lista blanca del recurso; `-campo` para descendente; varios separados por coma
  (`sort=-orderDate,orderNumber`). Un campo fuera de la lista blanca es **400**.
- `search`: texto que se busca con "contiene" en los campos definidos por el recurso.
- Filtros por nombre de campo: `status=Pending&customerId=42&from=2026-11-01&to=2026-11-30`.

Respuesta:
```json
{
  "items": [ { "id": 1, "orderNumber": "ORD-20261108-000001", "status": "Pending", "totalAmount": 1250.50 } ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 137
}
```

## 5. Códigos de estado
| Código | Cuándo |
| --- | --- |
| 200 | lectura o actualización que devuelve cuerpo |
| 201 | creación; header `Location` al recurso nuevo |
| 202 | operación larga aceptada; `Location` a `/api/v1/operations/{id}` |
| 204 | éxito sin cuerpo |
| 400 | entrada mal formada o inválida (validación de formato, parámetros fuera de rango) |
| 401 | sin token o token inválido |
| 403 | autenticado pero sin permiso, o tenant inactivo |
| 404 | el recurso no existe **o no pertenece a tu tenant** (nunca 403 en ese caso, para no revelar que existe) |
| 409 | conflicto de estado (transición inválida, duplicado de clave natural) |
| 412 | `If-Match` no coincide con el `ETag` actual |
| 422 | la entrada es válida en forma pero viola una regla de negocio (RN-xx) |
| 429 | límite de solicitudes excedido |
| 500 / 504 | fallo del servidor / timeout interno; nunca para errores del cliente |

## 6. Headers
- `ETag` en recursos editables (derivado de `rowversion`).
- `If-Match` **obligatorio** en `PUT`/`PATCH` de órdenes; sin él → 428 o 412 según se decida en F6.
- `Idempotency-Key` en `POST /api/v1/orders`: misma clave y mismo cuerpo → misma respuesta, sin duplicar.
- `Location` en 201 y 202.
- `Deprecation` y `Sunset` en endpoints de versiones en retiro.

## 7. Errores
Siempre ProblemDetails (RFC 9457), con el `type` del catálogo de `docs/errores.md`:
```json
{
  "type": "https://github.com/<tu-usuario>/OrderFlow/blob/main/docs/errores.md#business-rule",
  "title": "Business rule violated.",
  "status": 422,
  "detail": "Product SKU-001 has 5 units available; 6 were requested.",
  "rule": "RN-12",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```
Validación de campos (400) agrega `errors`:
```json
{ "type": ".../errores.md#validation", "title": "One or more validation errors occurred.", "status": 400,
  "errors": { "quantity": [ "Must be greater than 0." ] }, "traceId": "..." }
```

## 8. Versionado
- La versión va en la ruta (`/api/v1`). Agregar campos o endpoints **no** rompe; quitar o renombrar campos, cambiar
  tipos o significado **sí** rompe y exige `/api/v2`.
- La versión anterior se mantiene con `Deprecation` y `Sunset` hasta la fecha anunciada.

## 9. Seguridad en el contrato
- Ningún contrato recibe `tenantId`: el tenant sale del token.
- Ningún contrato de entrada recibe campos que el cliente no debe controlar (`createdAt`, `createdBy`, `totalAmount`,
  `status` en creación, `rowVersion` salvo vía `If-Match`).
- Datos personales solo en los contratos que los necesitan; nunca en logs.
