# Requerimientos — AuthService

## Feature: Autenticación de Usuarios

### Incluye
- Registro de usuarios
- Inicio de sesión con credenciales válidas
- Generación de token para persistencia de sesión
- Cierre de sesión

### No incluye
- Verificación de correo electrónico
- Recuperación de contraseña
- Autenticación con terceros (OAuth, Google, GitHub)
- Refresh token
- Eliminación de cuenta
- Gestión dinámica de roles
- Edición de perfil
- Rate limiting por IP (responsabilidad del API Gateway / reverse proxy, no del microservicio; RN5 cubre protección contra fuerza bruta a nivel de cuenta)
- Tests de rendimiento / benchmarks (métrica de monitoreo post-despliegue; BCrypt introduce ~300ms por diseño de seguridad y los resultados dependen del hardware del entorno, no de la lógica del servicio)

---

## HU1: Registro de nuevo usuario comprador

**Como** un nuevo visitante de la plataforma,
**Quiero** poder crear una cuenta de usuario,
**Para** tener una identidad única en el sistema y asegurar el acceso a mis futuros tickets.

### Criterios de Aceptación

- El formulario incluye los campos obligatorios: nombre, apellido, correo electrónico, contraseña y confirmar contraseña.
- El sistema valida que todos los campos estén completos antes de enviar. Si alguno está vacío muestra un mensaje de error específico junto al campo correspondiente.
- El sistema valida que el correo tenga formato válido (usuario@dominio.com).
- El sistema verifica que la contraseña y su confirmación coincidan.
- La contraseña debe cumplir con RN2; si no se cumplen las reglas se muestran mensajes claros.
- Si el correo ya está registrado el sistema impide el registro y muestra: "Este correo electrónico ya está en uso. ¿Deseas iniciar sesión?"
- Al registrarse exitosamente se muestra un mensaje de confirmación y se redirige automáticamente al login.

### Reglas de Negocio

- **RN1 — Correo único:** el correo electrónico debe ser único en la base de datos de usuarios.
- **RN2 — Política de contraseñas:** la contraseña debe tener mínimo 8 caracteres, al menos una letra mayúscula y al menos un carácter especial.
- **RN3 — Almacenamiento seguro:** las contraseñas se almacenan con hash bcrypt. Nunca en texto plano.

---

## HU2: Inicio y cierre de sesión

**Como** usuario registrado en el aplicativo,
**Quiero** iniciar sesión y poder cerrarla cuando lo desee,
**Para** tener una identidad única en el sistema, acceder al historial de mis tickets y proteger mi cuenta al finalizar.

### Criterios de Aceptación

- El sistema permite iniciar sesión cuando el usuario ingresa credenciales válidas (correo y contraseña), otorgándole acceso a su cuenta e información vinculada.
- Los campos de correo y contraseña son obligatorios y deben estar completos antes de procesar el login.
- El sistema valida que el correo ingresado tenga formato válido antes de intentar la autenticación.
- Las credenciales incorrectas muestran un mensaje de error genérico sin revelar si el correo existe en el sistema.
- El sistema impide el acceso si el usuario no está registrado, mostrando un mensaje informativo.
- La sesión se mantiene activa mediante un token válido durante su periodo de vigencia, evitando que el usuario deba iniciar sesión nuevamente.
- El sistema bloquea temporalmente la cuenta tras 3 intentos fallidos consecutivos de login (RN5).
- El cierre de sesión invalida y elimina el token en el cliente, restringe el acceso a vistas protegidas y redirige al usuario a la vista pública.

### Reglas de Negocio

- **RN4 — Acceso restringido:** solo los usuarios registrados pueden iniciar sesión en el sistema.
- **RN5 — Límite de intentos:** el sistema bloquea temporalmente la cuenta tras 3 intentos fallidos consecutivos de inicio de sesión.
