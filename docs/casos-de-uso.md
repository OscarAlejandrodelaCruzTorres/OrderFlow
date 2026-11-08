# Casos de uso de OrderFlow

**Estado: por llenar en F0.5.** El arquitecto conduce la fase haciendo las preguntas de la sección 3 una por una; con
las respuestas se escriben los actores, los casos de uso y las reglas RN-xx. Toda regla de negocio del código sale de
aquí, y cada una tiene al menos una prueba que la cita.

## 1. Actores y permisos

| Actor | Rol en el token | Qué puede hacer | Permisos finos |
| --- | --- | --- | --- |
| PlatformAdmin | `PlatformAdmin` | administrar tenants (alta, migración, baja) | — |
| TenantAdmin | `TenantAdmin` | _(por definir)_ | _(por definir)_ |
| Vendedor | `TenantUser` | _(por definir)_ | _(por definir)_ |
| Almacén | `TenantUser` | _(por definir)_ | _(por definir)_ |
| Consulta | `TenantUser` | _(por definir)_ | _(por definir)_ |

## 2. Casos de uso

Plantilla:
```
CU-NN · Nombre
Actor:
Precondiciones:
Flujo principal:
  1.
  2.
Flujos alternativos:
  2a.
Reglas: RN-xx, RN-yy
Criterios de aceptación:
  Dado ..., cuando ..., entonces ...
```

Lista inicial a definir:
- CU-01 Dar de alta un tenant (compartido o dedicado)
- CU-02 Dar de baja un tenant y exportar sus datos
- CU-03 Administrar categorías
- CU-04 Administrar productos y precios
- CU-05 Registrar entrada, ajuste o devolución de inventario
- CU-06 Administrar clientes
- CU-07 Crear orden
- CU-08 Modificar orden
- CU-09 Cambiar el estado de una orden (pagar, enviar, entregar, cancelar)
- CU-10 Consultar órdenes, inventario y clientes
- CU-11 Reportes del tenant

## 3. Preguntas de producto (resolver antes de F2)
1. ¿Hay pago dentro del sistema o solo se registra que la orden se pagó?
2. ¿Se permiten envíos parciales? ¿Devoluciones? ¿Afectan inventario?
3. ¿Se puede cancelar una orden pagada o enviada? ¿Quién puede?
4. ¿Qué estados existen y qué transiciones son válidas?
5. ¿Se congela el precio al crear la orden? ¿Qué pasa si cambia el precio del producto?
6. ¿Hay descuentos o impuestos por línea u orden? ¿Una sola moneda por tenant?
7. ¿Se puede modificar una orden después de creada? ¿Hasta qué estado?
8. ¿Qué reportes necesita un TenantAdmin?
9. ¿Un cliente puede pertenecer a varios tenants? (esperado: no; es otro cliente en cada tenant)
10. ¿Se borran clientes y productos, o solo se desactivan?

## 4. Reglas de negocio

| Regla | Descripción | Caso de uso | Prueba(s) |
| --- | --- | --- | --- |
| RN-01 | _(por definir)_ | | |
