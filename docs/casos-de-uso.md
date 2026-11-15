# Casos de uso de OrderFlow

**Estado: aprobado en F0.5 (2026-11-14).** Toda regla de negocio del código sale de aquí:
el dominio la implementa (ADR 12) y cada regla RN-xx tiene al menos una prueba que la cita en su nombre o comentario.
Si una regla falta o cambia, se actualiza este archivo **antes** de tocar el código.

Convenciones: los códigos HTTP siguen `docs/guia-api.md` y los `type` de error, `docs/errores.md`. "Cliente" es el
comprador registrado en un tenant; "usuario" es quien opera OrderFlow.

## 0. Decisiones de producto

Respuestas del dueño del producto en F0.5. Las reglas de la sección 3 citan la decisión de la que salen.

| # | Pregunta | Decisión |
| --- | --- | --- |
| P1 | ¿Qué tipo de negocio es un tenant? | Los tres: distribuidor B2B, comercio minorista B2C y pequeño negocio genérico. |
| P1b | ¿Cómo se maneja la diferencia entre tipos? | **Núcleo común** igual para todos (catálogo, clientes, órdenes con estados, inventario sin sobreventa, permisos). Crédito al cliente y listas de precios por cliente quedan **fuera del Bloque 1** (CU-F1, CU-F2), activables por tenant con feature flags (bloque opcional). |
| P2 | ¿Hay pago dentro del sistema? | **Solo se registra que la orden se pagó.** El cobro ocurre fuera de OrderFlow; un usuario con permiso marca la orden como pagada y queda quién y cuándo. Sin montos ni pagos parciales. |
| P3 | ¿Estados y transiciones de la orden? | **Flujo lineal, se paga antes de enviar:** `Pending → Paid → Shipped → Delivered`; `Cancelled` solo desde `Pending` o `Paid`. `Delivered` y `Cancelled` son finales. Enviar sin cobrar sería dar crédito (fuera del Bloque 1, P1b). |
| P4 | ¿Cuándo se descuenta el inventario? | **Al crear la orden (`Pending`).** Sin existencia suficiente la orden se rechaza (422) y no se crea nada. Al cancelar, la existencia regresa con un **movimiento de reversa** en el ledger (el original nunca se borra). Órdenes `Pending` sin pagar: **sin cancelación automática** en el Bloque 1 (CU-F3); se cancelan a mano (opción recomendada, tomada por defecto). |
| P5 | ¿Quién puede cancelar? | **Depende del estado:** `Pending` → Vendedor o TenantAdmin; `Paid` → solo TenantAdmin (implica reembolso fuera del sistema). Permiso por recurso (ADR 7): mismo endpoint, 204 o 403 según estado y permiso. |
| P6 | ¿Precio si cambia el del producto? | **Se congela al crear la orden** en `OrderItems.UnitPrice`. Un cambio de precio solo afecta a órdenes nuevas; el historial temporal de `Products` permite consultar el precio en cualquier fecha. |
| P7 | ¿Impuestos o descuentos? | **Una tasa de impuesto por tenant, sin descuentos.** La orden guarda subtotal, impuesto y total; la tasa se congela al crear la orden, como el precio. |
| P8 | ¿Cómo se redondea el impuesto? | **Por línea:** cada línea guarda su impuesto redondeado a 2 decimales; el impuesto de la orden es la suma de las líneas (cuadra con CFDI 4.0, impuestos por concepto). Redondeo **comercial** (`MidpointRounding.AwayFromZero`), nunca el bancario por defecto de .NET. |
| P9 | ¿Se puede modificar una orden? | **Solo en `Pending`:** agregar o quitar líneas y cambiar cantidades; cada cambio ajusta existencia con su movimiento en el ledger y recalcula subtotal, impuesto y total. Exige `If-Match` (412 si no coincide). Desde `Paid` queda congelada: se cancela y se crea otra. |
| P10 | ¿Se borran clientes y productos? | **Solo se desactivan** (y se pueden reactivar). Inactivo = no se usa en órdenes nuevas (422) y no aparece en listas por defecto; sus órdenes pasadas quedan intactas. `DELETE` = desactivar, 204. |
| P11 | ¿Envíos parciales y devoluciones? | **Sin envíos parciales** (la orden se envía completa, P3). **La devolución es un movimiento de inventario** tipo `Return` (producto, cantidad y opcionalmente la orden de origen) que registra Almacén; la orden sigue en `Delivered` y el reembolso ocurre fuera del sistema. **Con orden de origen, no se devuelve más de lo vendido** menos lo ya devuelto (422); la orden debe estar `Delivered`. Sin orden de origen no hay tope. |
| P12 | ¿Qué moneda maneja un tenant? | **Una moneda por tenant**, elegida en el alta; no se puede cambiar después de la primera orden. Los montos se guardan sin moneda por fila (todo en la del tenant). |
| P13 | ¿Un cliente en varios tenants? | **No:** el mismo negocio es un cliente independiente en cada tenant, sin relación entre sí. Dentro de un tenant, **el email de un cliente activo es único** (409 si se repite). |
| P14 | ¿Qué reportes necesita un TenantAdmin? | **Tres:** ventas por periodo (órdenes y total por día o mes), productos más vendidos (los N primeros por cantidad) e inventario bajo (existencia por debajo del **punto de reorden** de cada producto, dato que define el tenant). **Cuenta como venta** una orden `Paid`, `Shipped` o `Delivered`; `Pending` y `Cancelled` no. |
| P15 | ¿Actores y permisos? | **Matriz aceptada tal cual** (sección 1). PlatformAdmin administra tenants y no ve datos de negocio. "Ver costos" agrega el **costo** al producto. Vendedor, Almacén y Consulta son perfiles de permisos resueltos contra el catálogo (ADR 13), no roles del token. |

**Supuestos confirmados** (derivados de las decisiones; sección 5): S1–S6.

## 1. Actores y permisos

| Actor | Rol en el token | Descripción |
| --- | --- | --- |
| PlatformAdmin | `PlatformAdmin` | Opera la plataforma: alta, migración, exportación y baja de tenants. **No ve datos de negocio de ningún tenant** (RN-27). |
| TenantAdmin | `TenantAdmin` | Dueño o gerente del tenant: todo lo del tenant, incluidos precios, costos, reportes y usuarios. |
| Vendedor | `TenantUser` + perfil | Captura clientes y órdenes, registra pagos, cancela órdenes `Pending`. |
| Almacén | `TenantUser` + perfil | Envía y entrega órdenes; registra entradas, devoluciones y ajustes de inventario. |
| Consulta | `TenantUser` + perfil | Solo lectura (sin costos ni reportes). |

Los perfiles Vendedor, Almacén y Consulta se resuelven en la API contra el catálogo (usuario ↔ tenant ↔ permisos, con
caché), no viajan en el token (ADR 13). Un usuario pertenece a un solo tenant, salvo PlatformAdmin.

| Acción (permiso) | TenantAdmin | Vendedor | Almacén | Consulta |
| --- | :-: | :-: | :-: | :-: |
| Ver catálogo, existencias, clientes y órdenes | ✔ | ✔ | ✔ | ✔ |
| Administrar categorías y productos (incluidos precios) | ✔ | | | |
| **Ver el costo** de los productos | ✔ | | | |
| Administrar clientes | ✔ | ✔ | | |
| Crear y modificar órdenes `Pending` | ✔ | ✔ | | |
| Marcar una orden como pagada | ✔ | ✔ | | |
| **Cancelar** una orden `Pending` / `Paid` | ✔ / ✔ | ✔ / — | | |
| Marcar enviada y entregada | ✔ | | ✔ | |
| Registrar entradas y devoluciones de inventario | ✔ | | ✔ | |
| **Ajustar inventario** (corrección por conteo) | ✔ | | ✔ | |
| Ver reportes | ✔ | | | |
| Asignar perfiles a los usuarios de su tenant | ✔ | | | |

En negrita, las acciones sensibles del ADR 7: se evalúan con permiso por recurso en el servicio, no en el controlador.

## 2. Casos de uso

### CU-01 · Dar de alta un tenant
**Actor:** PlatformAdmin · **Fase:** F4 (compartido desde F3 por semilla)
**Precondiciones:** el slug no existe.
**Flujo principal:**
1. El PlatformAdmin envía slug, nombre, modelo (`Shared` o `Dedicated`), moneda (ISO 4217) y tasa de impuesto.
2. Compartido: el sistema registra el tenant en el catálogo y responde 201.
3. Dedicado: el sistema responde 202 con `Location` a la operación; en segundo plano crea la base con SKU explícito,
   aplica migraciones y la semilla, y marca el tenant activo (ADR 9, ADR 15).

**Flujos alternativos:**
- 1a. Slug con formato inválido o moneda/tasa fuera de rango → 400.
- 1b. El mismo slug con los mismos datos se repite → misma respuesta, sin duplicar (idempotente por slug).
- 1c. El mismo slug con datos distintos → 409.
- 3a. Falla la creación de la base dedicada → la operación queda en estado fallido, consultable; el tenant no se activa.

**Reglas:** RN-24.
**Criterios de aceptación:**
- Dado un slug nuevo, cuando el PlatformAdmin crea un tenant compartido, entonces recibe 201 y sus usuarios pueden operar.
- Dado un tenant dedicado en creación, cuando se consulta la operación, entonces muestra su estado hasta terminar o fallar.
- Dado un tenant existente, cuando se repite la misma alta, entonces no se crea nada nuevo.

### CU-02 · Desactivar, exportar y dar de baja un tenant
**Actor:** PlatformAdmin · **Fase:** F4
**Flujo principal:**
1. El PlatformAdmin desactiva el tenant: sus usuarios reciben 403 desde ese momento.
2. Solicita la exportación de datos (operación larga, 202); el archivo queda disponible para descargar.
3. Solicita la baja con confirmación explícita: se elimina la base dedicada o las filas del tenant en la compartida.

**Flujos alternativos:**
- 3a. Baja sin confirmación explícita → 400, nada se borra.
- 3b. Baja de un tenant activo → 409: primero se desactiva.

**Reglas:** RN-28.
**Criterios de aceptación:**
- Dado un tenant desactivado, cuando uno de sus usuarios llama a cualquier endpoint, entonces recibe 403.
- Dado un tenant dado de baja, entonces existe su exportación y no queda ninguna fila suya en la base compartida.

### CU-03 · Administrar categorías
**Actor:** TenantAdmin
**Flujo principal:** crear, editar el nombre, desactivar y reactivar una categoría.
**Flujos alternativos:**
- Nombre duplicado entre categorías activas → 409.
- Producto nuevo en una categoría inactiva → 422.

**Reglas:** RN-23 (supuesto S1).
**Criterios de aceptación:**
- Dada una categoría inactiva, cuando el TenantAdmin crea un producto en ella, entonces recibe 422.

### CU-04 · Administrar productos y precios
**Actor:** TenantAdmin
**Datos:** SKU, nombre, categoría, precio de venta, costo, punto de reorden. La existencia **no** se edita aquí: solo
cambia por movimientos (CU-05, CU-07 a CU-09).
**Flujo principal:** crear, editar (incluido el precio), desactivar y reactivar un producto.
**Flujos alternativos:**
- SKU repetido en el tenant → 409.
- Precio ≤ 0, costo o punto de reorden negativos → 400 (supuesto S3).

**Reglas:** RN-05, RN-21, RN-23, RN-26.
**Criterios de aceptación:**
- Dada una orden `Pending` con un producto a $100, cuando el TenantAdmin cambia el precio a $120, entonces la orden
  conserva $100 y solo las órdenes nuevas usan $120 (RN-05).
- Dado un usuario Vendedor, cuando consulta un producto, entonces la respuesta no incluye el costo (RN-26).

### CU-05 · Registrar entrada, devolución o ajuste de inventario
**Actor:** Almacén o TenantAdmin
**Flujo principal:**
1. El usuario elige el tipo: **entrada** (mercancía recibida), **devolución** (`Return`) o **ajuste** (corrección por
   conteo físico, con motivo).
2. Indica producto, cantidad y, en devoluciones, opcionalmente la orden de origen.
3. El sistema agrega el movimiento al ledger y actualiza la existencia.

**Flujos alternativos:**
- 2a. Devolución con orden de origen que no está `Delivered` → 422 (RN-19).
- 2b. Devolución por más de lo vendido menos lo ya devuelto en esa orden → 422 (RN-19).
- 2c. Ajuste que dejaría la existencia negativa → 422 (RN-18).
- 2d. Usuario Vendedor o Consulta → 403 (RN-20).

**Reglas:** RN-17, RN-18, RN-19, RN-20.
**Criterios de aceptación:**
- Dada una orden `Delivered` con 5 unidades de un producto y 3 ya devueltas, cuando se registra una devolución de 3,
  entonces se rechaza con 422 (RN-19); con 2, se acepta y la existencia sube 2.
- Dado un movimiento registrado, cuando cualquiera intenta modificarlo o borrarlo, entonces no es posible: la
  corrección es otro movimiento (RN-17).

### CU-06 · Administrar clientes
**Actor:** Vendedor o TenantAdmin
**Flujo principal:** crear, editar, desactivar y reactivar un cliente.
**Flujos alternativos:**
- Email igual al de otro cliente activo del tenant → 409 (RN-22).
- Reactivar un cliente cuyo email ya usa otro cliente activo → 409 (RN-22).

**Reglas:** RN-22, RN-23.
**Criterios de aceptación:**
- Dado un cliente activo con email `a@x.com`, cuando se crea otro con el mismo email en el mismo tenant, entonces
  recibe 409; en otro tenant, se crea sin problema (P13).

### CU-07 · Crear orden
**Actor:** Vendedor o TenantAdmin
**Precondiciones:** cliente activo; productos activos con existencia.
**Flujo principal:**
1. El usuario envía el cliente y las líneas (producto y cantidad) con un `Idempotency-Key`.
2. El dominio valida cliente, productos, duplicados y existencia; congela precio y tasa; calcula impuesto por línea,
   subtotal, impuesto y total.
3. El sistema asigna el `OrderNumber`, guarda la orden en `Pending` con sus líneas, un movimiento de salida por línea
   y la entrada inicial del historial, todo en una transacción, y responde 201 con `Location`.

**Flujos alternativos:**
- 2a. Cliente inactivo o de otro tenant → 422 (RN-01) o 404 si no existe en el tenant.
- 2b. Producto inactivo → 422 (RN-02).
- 2c. Existencia insuficiente en **cualquier** línea → 422 con la regla y el producto; no se crea nada (RN-03).
- 2d. El mismo producto en dos líneas → 422 (RN-08).
- 2e. Sin líneas o cantidad ≤ 0 → 400 (RN-09, S5).
- 1a. Misma `Idempotency-Key` con el mismo cuerpo → la misma respuesta, sin duplicar; con otro cuerpo → 422
  `idempotency-conflict`.

**Reglas:** RN-01 a RN-09, RN-11, RN-16.
**Criterios de aceptación:**
- Dado un producto con 5 unidades, cuando creo una orden por 6, entonces recibo 422 con RN-03 y no se crea nada.
- Dadas dos líneas de 1 unidad a $10.03 con tasa 16%, cuando creo la orden, entonces el subtotal es 20.06, el impuesto
  3.20 (1.60 + 1.60) y el total 23.26 (RN-07).
- Dado un producto con 10 unidades, cuando creo una orden por 4, entonces la existencia queda en 6 y hay un movimiento
  de salida por 4 en el ledger (RN-04).
- Dado el tenant con su última orden `ORD-20261114-000041`, cuando creo otra ese día, entonces recibe
  `ORD-20261114-000042` (RN-16).

### CU-08 · Modificar orden
**Actor:** Vendedor o TenantAdmin
**Precondiciones:** orden en `Pending`; `If-Match` con el `ETag` actual.
**Flujo principal:**
1. El usuario agrega o quita líneas o cambia cantidades.
2. El dominio aparta o devuelve existencia con movimientos en el ledger y recalcula impuesto por línea y totales.
3. El sistema guarda y responde 200 con la orden y su nuevo `ETag`.

**Flujos alternativos:**
- 1a. Orden en otro estado → 409 con `currentState` (RN-12).
- 1b. `If-Match` distinto del actual → 412; sin `If-Match` → 428 o 412 según F6.
- 2a. Aumentar una cantidad sin existencia suficiente → 422 (RN-03); nada cambia.
- 2b. Quitar la última línea → 422 (RN-09, S2): se cancela la orden en su lugar.

**Reglas:** RN-03, RN-07, RN-08, RN-09, RN-12, RN-13.
**Criterios de aceptación:**
- Dada una orden `Paid`, cuando intento agregar una línea, entonces recibo 409 (RN-12).
- Dados dos usuarios con el mismo `ETag`, cuando ambos modifican la orden, entonces el segundo recibe 412.
- Dada una línea de 4 unidades, cuando la bajo a 1, entonces la existencia sube 3 con un movimiento y los totales se
  recalculan (RN-13).

### CU-09 · Cambiar el estado de una orden (pagar, enviar, entregar, cancelar)
**Actor:** según la transición (RN-14)
**Flujo principal:**
1. El usuario solicita la transición.
2. El dominio valida la transición (RN-10) y el permiso (RN-14), y registra el historial (RN-11).
3. Al cancelar, el dominio crea los movimientos de reversa de todas las líneas (RN-15).

**Flujos alternativos:**
- 2a. Transición no permitida (p. ej. `Pending → Shipped`, o cualquiera desde `Delivered`/`Cancelled`) → 409 con
  `currentState`.
- 2b. Usuario sin permiso para esa transición en ese estado → 403.

**Reglas:** RN-10, RN-11, RN-14, RN-15.
**Criterios de aceptación:**
- Dada una orden `Pending`, cuando la envío sin pagar, entonces recibo 409 (RN-10).
- Dada una orden `Paid`, cuando un Vendedor la cancela, entonces recibe 403; cuando un TenantAdmin la cancela, entonces
  queda `Cancelled` y la existencia de sus líneas regresa (RN-14, RN-15).
- Dada cualquier transición válida, entonces el historial registra estado anterior, nuevo, usuario y fecha (RN-11).

### CU-10 · Consultar órdenes, inventario y clientes
**Actor:** cualquier usuario del tenant
**Flujo principal:** listas paginadas con filtros, orden y búsqueda (`docs/guia-api.md`), y detalle por id.
**Flujos alternativos:**
- Recurso de otro tenant o inexistente → 404, nunca 403.
- Inactivos: no aparecen por defecto; se incluyen con un filtro explícito.

**Reglas:** RN-26, RN-27.
**Criterios de aceptación:**
- Dado un usuario del tenant A, cuando pide una orden del tenant B por id, entonces recibe 404.
- Dado un PlatformAdmin, cuando pide órdenes de cualquier tenant, entonces recibe 403 (RN-27).

### CU-11 · Reportes del tenant
**Actor:** TenantAdmin
**Flujo principal:**
1. **Ventas por periodo:** número de órdenes y total por día o por mes en un rango de fechas.
2. **Productos más vendidos:** los N primeros por cantidad en un rango.
3. **Inventario bajo:** productos activos con existencia por debajo de su punto de reorden.

**Flujos alternativos:** rango inválido o demasiado grande, N fuera de límite → 400 (límites técnicos en F5).
**Reglas:** RN-25, RN-29.
**Criterios de aceptación:**
- Dadas órdenes `Pending`, `Paid`, `Delivered` y `Cancelled` en el rango, cuando pido ventas, entonces solo cuentan
  `Paid` y `Delivered` (RN-25).
- Dado un producto con existencia 3 y punto de reorden 5, entonces aparece en inventario bajo; con existencia 5, no (RN-29).

### Casos de uso futuros (fuera del Bloque 1)
- **CU-F1 · Crédito a clientes (B2B):** límite de crédito por cliente; enviar antes de pagar. Cambia P3.
- **CU-F2 · Listas de precios por cliente (B2B).** Cambia RN-05 (de qué precio se congela).
- **CU-F3 · Cancelación automática de órdenes `Pending` sin pagar** tras N horas (P4), con `TimeProvider` y un
  `BackgroundService`.

## 3. Reglas de negocio

La columna "Prueba(s)" se llena en F2 (dominio) y F6 (integración): cada prueba cita su regla.

| Regla | Descripción | Origen | Caso de uso | Prueba(s) |
| --- | --- | --- | --- | --- |
| RN-01 | Una orden solo se crea para un cliente **activo** del tenant. | P10 | CU-07 | |
| RN-02 | Solo productos **activos** en órdenes nuevas o líneas agregadas. | P10 | CU-07, CU-08 | |
| RN-03 | **No se vende sin existencia:** crear o ampliar una orden exige existencia suficiente en cada línea; si falta en una, se rechaza todo (422) y nada cambia. | P4 | CU-07, CU-08 | |
| RN-04 | Crear la orden descuenta la existencia con un movimiento de salida por línea en el ledger. | P4 | CU-07 | |
| RN-05 | El precio se congela en la línea (`UnitPrice`) al agregarla; cambios posteriores del producto no la afectan. | P6 | CU-04, CU-07 | |
| RN-06 | La tasa de impuesto del tenant se congela en la orden al crearla. | P7 | CU-07 | |
| RN-07 | Importe de línea = cantidad × precio; impuesto de línea = redondeo comercial a 2 decimales del importe × tasa; subtotal = Σ importes; impuesto = Σ impuestos de línea; total = subtotal + impuesto. | P7, P8 | CU-07, CU-08 | |
| RN-08 | Un producto aparece **una sola vez** por orden; para cambiar cantidad se edita su línea. | ADR 12, S4 | CU-07, CU-08 | |
| RN-09 | Una orden tiene **al menos una línea** con cantidad entera > 0. | S2, S5 | CU-07, CU-08 | |
| RN-10 | Transiciones válidas: `Pending→Paid`, `Paid→Shipped`, `Shipped→Delivered`, `Pending→Cancelled`, `Paid→Cancelled`. Cualquier otra → 409. | P3 | CU-09 | |
| RN-11 | Cada transición (y la creación) se registra en el historial con estado anterior, nuevo, usuario y fecha. | P2, ADR 12 | CU-07, CU-09 | |
| RN-12 | Una orden solo se modifica en `Pending`; en otro estado → 409. | P9 | CU-08 | |
| RN-13 | Modificar una orden aparta o devuelve existencia con movimientos en el ledger y recalcula sus totales (RN-07). | P9 | CU-08 | |
| RN-14 | Permisos por transición: pagar → Vendedor o TenantAdmin; enviar y entregar → Almacén o TenantAdmin; cancelar `Pending` → Vendedor o TenantAdmin; cancelar `Paid` → solo TenantAdmin. Sin permiso → 403. | P5, P15 | CU-09 | |
| RN-15 | Cancelar devuelve la existencia de todas las líneas con movimientos de reversa. | P4 | CU-09 | |
| RN-16 | `OrderNumber` consecutivo y único por tenant, formato `ORD-AAAAMMDD-NNNNNN`. | ARQUITECTURA §9, guía | CU-07 | |
| RN-17 | El ledger es **inmutable**: un movimiento nunca se modifica ni se borra; toda corrección es un movimiento nuevo. | ADR 2, P4 | CU-05 | |
| RN-18 | La existencia nunca es negativa. | S6 | CU-05, CU-07, CU-08 | |
| RN-19 | Devolución con orden de origen: la orden está `Delivered` y la cantidad ≤ vendida − ya devuelta de ese producto en esa orden; si no, 422. Sin orden de origen, sin tope. | P11 | CU-05 | |
| RN-20 | Entradas, devoluciones y ajustes de inventario: solo Almacén o TenantAdmin. | P15 | CU-05 | |
| RN-21 | El SKU es único por tenant → 409. | errores.md | CU-04 | |
| RN-22 | El email de un cliente activo es único por tenant → 409. | P13 | CU-06 | |
| RN-23 | Clientes, productos y categorías no se borran: se desactivan y reactivan; sus órdenes pasadas no cambian. | P10, S1 | CU-03, CU-04, CU-06 | |
| RN-24 | La moneda del tenant no cambia después de su primera orden. | P12 | CU-01 | |
| RN-25 | Cuenta como venta una orden `Paid`, `Shipped` o `Delivered`. | P14 | CU-11 | |
| RN-26 | El costo del producto solo se muestra con el permiso "ver costos" (TenantAdmin). | P15 | CU-04, CU-10 | |
| RN-27 | PlatformAdmin no accede a datos de negocio de ningún tenant. | P15 | CU-10 | |
| RN-28 | Los usuarios de un tenant inactivo reciben 403 en todo. | guía, F3 | CU-02 | |
| RN-29 | Inventario bajo = producto activo con existencia < punto de reorden. | P14 | CU-11 | |

## 4. Fuera de alcance del Bloque 1
Cobro real y reembolsos (P2), pagos parciales (P2), envíos parciales (P11), descuentos (P7), varias monedas por
tenant (P12), crédito y listas de precios (CU-F1, CU-F2), cancelación automática (CU-F3).

## 5. Supuestos confirmados
Derivados de las decisiones para que los casos de uso queden completos; confirmados por el dueño del producto al aprobar F0.5.

| # | Supuesto | De dónde sale |
| --- | --- | --- |
| S1 | Las categorías también solo se desactivan (como P10); un producto pertenece a una sola categoría. | P10 |
| S2 | No se puede quitar la última línea de una orden: para eso se cancela. | P9 |
| S3 | Precio de venta > 0; costo y punto de reorden ≥ 0. | P6, P14, P15 |
| S4 | Un producto aparece una sola vez por orden (ya lo dice el ADR 12: `AddItem` valida duplicados). | ADR 12 |
| S5 | Las cantidades son unidades enteras mayores que 0. | — |
| S6 | La existencia nunca es negativa, tampoco por un ajuste. | P4 |
