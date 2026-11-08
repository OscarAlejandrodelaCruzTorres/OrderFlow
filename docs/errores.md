# Catálogo de errores

Cada error de la API es un ProblemDetails cuyo `type` apunta a una sección de este archivo
(`https://github.com/OscarAlejandrodelaCruzTorres/OrderFlow/blob/main/docs/errores.md#<ancla>`). El `title` es estable; el `detail`
explica el caso concreto. Siempre incluye `traceId`. Los tipos se implementan en F0 (base) y F6 (dominio).

## validation
**400** · Entrada mal formada o que no cumple el formato (campos requeridos, rangos, longitudes, enums inválidos).
Incluye `errors` por campo.

## unauthorized
**401** · Falta el token o no es válido. Lo produce el middleware de autenticación.

## forbidden
**403** · El usuario no tiene el permiso requerido para la acción, o su tenant está inactivo.

## not-found
**404** · El recurso no existe **o no pertenece al tenant del usuario**. Nunca se distingue entre ambos casos.

## conflict
**409** · El estado actual no permite la operación: transición de estado inválida, clave natural duplicada (SKU,
email activo), operación ya ejecutada. Incluye `currentState` cuando aplica.

## precondition-failed
**412** · El `If-Match` no coincide con la versión actual del recurso: alguien más lo modificó. El cliente debe
recargar y reintentar.

## precondition-required
**428** · Falta `If-Match` en una operación que lo exige.

## business-rule
**422** · La entrada es válida en forma pero viola una regla de negocio. Incluye `rule` con el identificador RN-xx de
`docs/casos-de-uso.md`.

## idempotency-conflict
**422** · Se reutilizó una `Idempotency-Key` con un cuerpo distinto al de la solicitud original.

## too-many-requests
**429** · Se excedió el límite de solicitudes. Incluye `Retry-After`.

## timeout
**504** · La operación superó el tiempo máximo del servidor.

## internal
**500** · Error no esperado. El `detail` nunca expone información interna; el `traceId` permite encontrarlo en los logs.
