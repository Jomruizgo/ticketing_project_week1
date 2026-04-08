# TEST_CASES.md — Ticketing Project
## Feature: Autenticación de Usuarios

**Contexto:** Ver [BUSINESS_CONTEXT.md](./BUSINESS_CONTEXT.md) para el contexto completo del negocio.
**Historias de Usuario fuente:** Ver [documentoDiseno.md](./documentoDiseno.md).

---

## HU1: Registro de nuevo usuario comprador

> **Descripción:** Como un nuevo visitante de la plataforma, quiero poder crear una cuenta de usuario, para tener una identidad única en el sistema y asegurar el acceso a mis futuros tickets.

**Técnicas ISTQB aplicadas:**
- Partición de Equivalencia — formatos de correo y contraseñas válidos/inválidos
- Análisis de Valores Límite — longitud mínima de contraseña (7 vs. 8 caracteres)
- Transición de Estado / Flujo de Trabajo — redirección post-registro
- Adivinanza de Errores (Error Guessing) — campos vacíos y correos duplicados

---

### CP-HU1-01: Registro exitoso de un nuevo usuario comprador
**Tipo:** Flujo Básico

```gherkin
Dado que un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando ingresa "Carlos" en el campo Nombre
  Y ingresa "Gómez" en el campo Apellido
  Y ingresa "carlos.gomez@dominio.com" en el campo Correo Electrónico
  Y ingresa "SofkaTech2026!" en el campo Contraseña
  Y ingresa "SofkaTech2026!" en el campo Confirmar Contraseña
  Y envía el formulario de registro
Entonces el sistema crea la cuenta de usuario correctamente
  Y muestra un mensaje de confirmación de registro exitoso en pantalla
  Y redirige automáticamente al usuario a la página de inicio de sesión
```

---

### CP-HU1-02: Intento de registro con campos obligatorios vacíos
**Tipo:** Flujo Alterno

```gherkin
Dado que un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando deja vacíos los campos de Nombre, Apellido, Correo Electrónico, Contraseña y Confirmar Contraseña
  Y envía el formulario de registro
Entonces el sistema impide el envío del formulario
  Y muestra un mensaje de error específico junto a cada campo indicando que es obligatorio
```

---

### CP-HU1-03: Validación de formato de correo electrónico inválido
**Tipo:** Flujo Alterno | **Técnica:** Partición de Equivalencia

```gherkin
Dado que un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando ingresa datos válidos en los campos de Nombre, Apellido y Contraseñas
  Pero ingresa el valor "carlos.gomez.dominio.com" en el campo Correo Electrónico
  Y envía el formulario de registro
Entonces el sistema impide el registro
  Y muestra un mensaje de error en el campo de correo indicando que el formato no es válido
```

---

### CP-HU1-04: Validación de contraseñas que no coinciden
**Tipo:** Flujo Alterno

```gherkin
Dado que un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando ingresa datos válidos en los campos de Nombre, Apellido y Correo Electrónico
  Y ingresa "SofkaTech2026!" en el campo Contraseña
  Pero ingresa "Diferente2026*" en el campo Confirmar Contraseña
  Y envía el formulario de registro
Entonces el sistema impide el registro
  Y muestra un mensaje de error indicando que las contraseñas no coinciden
```

---

### CP-HU1-05: Política de contraseña — longitud inferior al límite mínimo
**Tipo:** Flujo Alterno | **Técnica:** Análisis de Valores Límite (RN2 — mín. 8 caracteres)

```gherkin
Dado que un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando ingresa datos válidos en los campos de Nombre, Apellido y Correo Electrónico
  Y ingresa una contraseña de 7 caracteres "Sofka1!" en el campo Contraseña y Confirmar Contraseña
  Y envía el formulario de registro
Entonces el sistema impide el registro
  Y muestra un mensaje de error indicando que la contraseña debe tener una longitud mínima de 8 caracteres
```

---

### CP-HU1-06: Política de contraseña — sin letra mayúscula
**Tipo:** Flujo Alterno | **Técnica:** Partición de Equivalencia (RN2)

```gherkin
Dado que un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando ingresa datos válidos en los campos de Nombre, Apellido y Correo Electrónico
  Y ingresa "sofkatech2026!" en el campo Contraseña y Confirmar Contraseña
  Y envía el formulario de registro
Entonces el sistema impide el registro
  Y muestra un mensaje de error indicando que la contraseña debe incluir al menos una letra mayúscula
```

---

### CP-HU1-07: Política de contraseña — sin carácter especial
**Tipo:** Flujo Alterno | **Técnica:** Partición de Equivalencia (RN2)

```gherkin
Dado que un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando ingresa datos válidos en los campos de Nombre, Apellido y Correo Electrónico
  Y ingresa "SofkaTech2026" en el campo Contraseña y Confirmar Contraseña
  Y envía el formulario de registro
Entonces el sistema impide el registro
  Y muestra un mensaje de error indicando que la contraseña debe incluir al menos un carácter especial
```

---

### CP-HU1-08: Intento de registro con correo electrónico ya existente
**Tipo:** Excepción | **Regla de Negocio:** RN1 — Correo único

```gherkin
Dado que existe un usuario previamente registrado con el correo "usuario.existente@dominio.com"
  Y un nuevo visitante se encuentra en el formulario de registro de la plataforma
Cuando ingresa datos válidos en los campos de Nombre, Apellido y Contraseñas
  Pero ingresa "usuario.existente@dominio.com" en el campo Correo Electrónico
  Y envía el formulario de registro
Entonces el sistema responde con un error de conflicto HTTP 409
  Y muestra el mensaje informativo "Este correo electrónico ya está en uso. ¿Deseas iniciar sesión?"
```

---

### CP-HU1-09: Validación del almacenamiento seguro de la contraseña
**Tipo:** Regla de Negocio | **Regla de Negocio:** RN3 — Almacenamiento con bcrypt

```gherkin
Dado que el servicio backend recibe una solicitud válida de registro de usuario con la contraseña "SofkaTech2026!"
Cuando el sistema procesa la solicitud para almacenar el usuario en la base de datos PostgreSQL
Entonces la contraseña es encriptada utilizando el algoritmo de hash bcrypt antes de su persistencia
  Y se verifica que no se almacena en texto plano en la base de datos
```

---

## HU2: Inicio y cierre de sesión de usuarios

> **Descripción:** Como usuario registrado en el aplicativo, quiero iniciar sesión y poder cerrarla cuando lo desee, para tener una identidad única en el sistema, asegurar el acceso al historial de mis tickets y proteger mi cuenta al finalizar.

**Técnicas ISTQB aplicadas:**
- Partición de Equivalencia — correos válidos/inválidos y credenciales correctas/incorrectas
- Análisis de Valores Frontera — bloqueo de cuenta (2.° intento: sin bloqueo; 3.° intento: bloqueo)
- Transición de Estados — ciclo de vida de sesión (No autenticado → Autenticado → Expirado/Cerrado) y estado de cuenta (Activa → Bloqueada)
- Predicción de Errores (Error Guessing) — inyección SQL, espacios en blanco, tokens JWT alterados o expirados

---

### CP-HU2-01: Inicio de sesión exitoso con credenciales válidas (Rol Comprador)
**Tipo:** Flujo Básico

```gherkin
Dado que un usuario con rol de comprador está registrado en el sistema
  Y se encuentra en la página de inicio de sesión
Cuando ingresa su correo electrónico válido y su contraseña correcta
  Y hace clic en el botón de iniciar sesión
Entonces el sistema autentica al usuario exitosamente
  Y el sistema genera y almacena un token JWT válido en el cliente
  Y el usuario es redirigido a la vista principal de su cuenta
  Y el usuario puede visualizar su historial de tickets
```

---

### CP-HU2-02: Inicio de sesión exitoso con credenciales válidas (Rol Admin)
**Tipo:** Flujo Básico

```gherkin
Dado que un usuario con rol de administrador está registrado en el sistema
  Y se encuentra en la página de inicio de sesión
Cuando ingresa su correo electrónico válido y su contraseña correcta
  Y hace clic en el botón de iniciar sesión
Entonces el sistema autentica al administrador exitosamente
  Y el usuario es redirigido al panel de administración del catálogo de eventos
```

---

### CP-HU2-03: Intento de inicio de sesión con campos vacíos
**Tipo:** Flujo Alterno

```gherkin
Dado que un usuario se encuentra en la página de inicio de sesión
Cuando deja los campos de correo electrónico y contraseña vacíos
  Y hace clic en el botón de iniciar sesión
Entonces el sistema no procesa la solicitud de autenticación
  Y el sistema muestra un mensaje de error indicando que los campos son obligatorios
```

---

### CP-HU2-04: Intento de inicio de sesión con formato de correo inválido
**Tipo:** Flujo Alterno | **Técnica:** Partición de Equivalencia

```gherkin
Dado que un usuario se encuentra en la página de inicio de sesión
Cuando ingresa un correo electrónico con formato inválido "usuario.sin.dominio"
  Y ingresa una contraseña válida
  Y hace clic en el botón de iniciar sesión
Entonces el sistema no procesa la solicitud a la API
  Y el sistema muestra un mensaje de error indicando que el formato del correo es inválido
```

---

### CP-HU2-05: Inicio de sesión con espacios en blanco al inicio o final del correo
**Tipo:** Flujo Alterno | **Técnica:** Predicción de Errores (Error Guessing)

```gherkin
Dado que un usuario registrado se encuentra en la página de inicio de sesión
Cuando ingresa su correo electrónico válido con espacios en blanco al inicio y al final
  Y ingresa su contraseña correcta
  Y hace clic en el botón de iniciar sesión
Entonces el sistema procesa la solicitud limpiando los espacios
  Y el sistema autentica al usuario exitosamente
```

---

### CP-HU2-06: Intento de acceso de un usuario no registrado
**Tipo:** Flujo Alterno | **Regla de Negocio:** RN4 — Acceso solo a usuarios registrados

```gherkin
Dado que una persona no registrada en el sistema se encuentra en la página de inicio de sesión
Cuando ingresa un correo electrónico "noexiste@correo.com" y una contraseña cualquiera
  Y hace clic en el botón de iniciar sesión
Entonces el sistema deniega el acceso
  Y el sistema muestra un mensaje informativo indicando que las credenciales son incorrectas o el usuario no existe
```

---

### CP-HU2-07: Intento de inicio de sesión con contraseña incorrecta
**Tipo:** Flujo Alterno

```gherkin
Dado que un usuario registrado se encuentra en la página de inicio de sesión
Cuando ingresa su correo electrónico válido
  Y ingresa una contraseña incorrecta
  Y hace clic en el botón de iniciar sesión
Entonces el sistema deniega el acceso
  Y el sistema muestra un mensaje de error claro indicando que las credenciales son inválidas
```

---

### CP-HU2-08: Límite de intentos fallidos — segundo intento (sin bloqueo)
**Tipo:** Regla de Negocio | **Técnica:** Análisis de Valores Frontera — valor interior (RN5)

```gherkin
Dado que un usuario registrado tiene un historial de 1 intento fallido de inicio de sesión
Cuando ingresa credenciales inválidas por segunda vez consecutiva
  Y hace clic en el botón de iniciar sesión
Entonces el sistema deniega el acceso
  Y la cuenta del usuario permanece activa sin ser bloqueada
```

---

### CP-HU2-09: Límite de intentos fallidos — bloqueo al tercer intento
**Tipo:** Regla de Negocio | **Técnica:** Análisis de Valores Frontera — valor exacto (RN5)

```gherkin
Dado que un usuario registrado tiene un historial de 2 intentos fallidos consecutivos de inicio de sesión
Cuando ingresa credenciales inválidas por tercera vez consecutiva
  Y hace clic en el botón de iniciar sesión
Entonces el sistema deniega el acceso
  Y el sistema bloquea temporalmente la cuenta del usuario
  Y el sistema muestra un mensaje indicando que la cuenta ha sido bloqueada temporalmente por seguridad
```

---

### CP-HU2-10: Intento de inicio de sesión en cuenta bloqueada temporalmente
**Tipo:** Excepción | **Técnica:** Transición de Estados (Activa → Bloqueada)

```gherkin
Dado que la cuenta de un usuario registrado se encuentra bloqueada temporalmente por superar el límite de intentos
Cuando el usuario ingresa su correo electrónico y su contraseña correcta
  Y hace clic en el botón de iniciar sesión
Entonces el sistema deniega el acceso
  Y el sistema muestra un mensaje indicando que la cuenta está bloqueada y debe esperar el tiempo establecido
```

---

### CP-HU2-11: Límite de longitud máxima en el campo de correo electrónico
**Tipo:** Seguridad / Límites de entrada | **Técnica:** Análisis de Valores Frontera

```gherkin
Dado que un usuario se encuentra en la página de inicio de sesión
Cuando ingresa un correo electrónico que excede los 255 caracteres permitidos
  Y ingresa una contraseña válida
  Y hace clic en el botón de iniciar sesión
Entonces el sistema no procesa la solicitud
  Y el sistema muestra un mensaje de error por longitud máxima excedida
```

---

### CP-HU2-12: Intento de inyección SQL en los campos de inicio de sesión
**Tipo:** Seguridad | **Técnica:** Predicción de Errores (Error Guessing)

```gherkin
Dado que un usuario se encuentra en la página de inicio de sesión
Cuando ingresa en el campo de correo el valor "' OR 1=1 --"
  Y ingresa una contraseña cualquiera
  Y hace clic en el botón de iniciar sesión
Entonces el sistema maneja la entrada de forma segura
  Y el sistema deniega el acceso sin exponer detalles internos de la base de datos
```

---

### CP-HU2-13: Persistencia de sesión con token JWT válido
**Tipo:** Flujo Básico | **Técnica:** Transición de Estados (Autenticado — sesión vigente)

```gherkin
Dado que un usuario ha iniciado sesión exitosamente y posee un token JWT vigente
Cuando el usuario navega hacia la vista de su historial de tickets
Entonces el sistema valida el token sin solicitar nuevas credenciales
  Y el sistema permite el acceso a la información protegida
```

---

### CP-HU2-14: Acceso denegado por token JWT expirado
**Tipo:** Excepción | **Técnica:** Transición de Estados (Autenticado → Expirado)

```gherkin
Dado que un usuario inició sesión pero su token JWT ha expirado
Cuando el usuario intenta acceder a la vista de su historial de tickets
Entonces el sistema intercepta la petición devolviendo un código 401 Unauthorized
  Y el sistema redirige al usuario a la página de inicio de sesión
```

---

### CP-HU2-15: Cierre de sesión exitoso
**Tipo:** Flujo Básico | **Técnica:** Transición de Estados (Autenticado → No autenticado)

```gherkin
Dado que un usuario tiene una sesión activa en el sistema
  Y se encuentra en cualquier vista protegida
Cuando el usuario hace clic en la opción de cerrar sesión
Entonces el sistema invalida y elimina el token JWT del lado del cliente
  Y el sistema redirige al usuario a la vista pública principal
```

---

### CP-HU2-16: Intento de acceso a ruta protegida tras cerrar sesión
**Tipo:** Flujo Alterno | **Técnica:** Transición de Estados (No autenticado — sin token)

```gherkin
Dado que un usuario ha cerrado su sesión exitosamente
Cuando intenta acceder directamente a la URL de su historial de tickets
Entonces el sistema verifica la ausencia del token
  Y el sistema bloquea el acceso
  Y el sistema redirige al usuario a la página de inicio de sesión
```

---

## Resumen de cobertura

| HU | Total CPs | Flujo Básico | Flujo Alterno | Excepción | Regla de Negocio | Seguridad |
|----|-----------|:------------:|:-------------:|:---------:|:----------------:|:---------:|
| HU1 | 9 | 1 | 5 | 1 | 2 | — |
| HU2 | 16 | 4 | 5 | 3 | 2 | 2 |
| **Total** | **25** | **5** | **10** | **4** | **4** | **2** |

### Trazabilidad de reglas de negocio

| Regla | Descripción | Casos de prueba |
|-------|-------------|-----------------|
| RN1 | Correo único en BD | CP-HU1-08 |
| RN2 | Política de contraseña (8 chars, mayúscula, carácter especial) | CP-HU1-05, CP-HU1-06, CP-HU1-07 |
| RN3 | Almacenamiento con bcrypt | CP-HU1-09 |
| RN4 | Acceso solo a usuarios registrados | CP-HU2-06 |
| RN5 | Bloqueo temporal tras 3 intentos fallidos | CP-HU2-08, CP-HU2-09, CP-HU2-10 |
