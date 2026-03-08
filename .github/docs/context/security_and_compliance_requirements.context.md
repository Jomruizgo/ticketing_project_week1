# Requisitos de Seguridad y Cumplimiento de TicketRush

## Alcance

Este MVP no implementa un dominio completo de autenticación/autorización empresarial, pero sí tiene obligaciones mínimas de seguridad operativa y manejo responsable de datos.

## Reglas obligatorias

1. No hardcodear secretos, contraseñas ni connection strings sensibles en código fuente.
2. Toda configuración sensible debe venir de variables de entorno o secretos del entorno .NET.
3. Los logs no deben exponer PII innecesaria ni credenciales.
4. Los errores HTTP no deben revelar detalles internos de infraestructura.
5. Los cambios no deben endurecer ni relajar CORS del MVP salvo requerimiento explícito.

## Datos sensibles o semi-sensibles del dominio

- `reserved_by`
- `payment_by`
- referencias de transacción o `provider_ref`

Estos datos requieren manejo prudente en logs, errores y documentación de pruebas.

## Consideraciones por componente

### Frontend

- No incluir secretos ni credenciales en código cliente.
- Tratar como no confiable cualquier dato recibido de APIs antes de renderizarlo.

### Producer y CRUD Service

- Validar inputs.
- Responder errores controlados.
- Mantener health endpoints sin exponer detalles sensibles.

### Workers

- No registrar payloads completos de pago si contienen referencias sensibles.
- Diferenciar claramente error técnico de fallo de negocio.

## Cumplimiento operativo mínimo

1. Mantener trazabilidad suficiente para depurar sin exponer datos críticos.
2. Probar cambios sensibles sin usar datos reales de producción.
3. Revisar impacto en logs, variables de entorno y documentación cuando se agregan nuevos campos personales o financieros.