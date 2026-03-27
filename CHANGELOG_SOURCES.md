# Bitacora de Nueva Feature

---

## 1. Identificacion del problema

Analicé el proyecto para identificar que posibles Features se pueden implementar que sean de alta complejidad; Las funcionalidades (CRUD) de eventos ya estan existentes pero deberia restringir a que solamente los usuarios con un rol especifico pudiera hacerlo, pues no tiene logica que el sistema permita crear, editar, eliminar eventos a la misma persona que los va a comprar; Para esto yo he identificado que no existe la funcionalidad inicialmente para el registro del usuario y que implemente el Login y poder mostrar una UI dependiendo del rol implica 2 Features minimamente: Registro de usuarios, Inicio de sesión.

---

## 2. Consulta a la IA (sin sesgo)

Le pedi a la IA sin sesgarla con mi posible feature, que analizara el proyecto y que segun el mismo, identificara las posibles features con las que podriamos continuar con el proyecto y propuso las siguientes:

| # | Feature propuesta |
|---|-------------------|
| 1 | Sistema de Autenticación y Usuarios (Auth Service) |
| 2 | Sistema de Notificaciones en Tiempo Real (Notification Service) |
| 3 | Sistema de Reservas con Timeout y Concurrencia (Reservation Engine) |

> Pero entre las tres opciones, recomendo realizar primero el **sistema de Autenticación**.

**Fuente:** Proyecto

---

## 3. ¿Una feature o dos?

Le mencione a la IA la funcionalidad que deseo implementar pero le mencione que tenia dudas sobre si era una sola feature o eran dos distintas, a lo que me respondio:

### SCRUM
En marcos como Scrum y enfoques de producto:

- Una feature representa una unidad de valor para el usuario o negocio
- Debe poder priorizarse, desarrollarse y probarse de forma independiente

Registro y login cumplen esto:
- Un usuario puede necesitar registrarse sin necesariamente iniciar sesión en ese momento
- Puedes priorizar “registro” antes que “login” o viceversa (aunque en la práctica suelen ir juntos)

---

### Historias de Usuario
Del concepto popularizado por Mike Cohn:

> “Como [usuario], quiero [acción], para [valor]”

Aplicándolo:

- “Como usuario quiero registrarme…”
- “Como usuario quiero iniciar sesión…”

Son acciones distintas con valores distintos, por eso se separan.

---

### BDD/Gherkin
En herramientas como Cucumber usando Gherkin:

- Una Feature agrupa escenarios relacionados a una capacidad del sistema
- No se recomienda mezclar capacidades diferentes en una misma feature

“Registro” y “Login” son capacidades diferentes:

| Capacidad | Acción |
|-----------|--------|
| Registro  | Crear cuenta |
| Login     | Autenticarse |

---

### Principio INVEST
También de Bill Wake — **I: Independent (Independiente)**

Si puedes desarrollar o probar algo sin depender completamente de otro, debería ser separado.

| Feature  | Responsabilidad |
|----------|-----------------|
| Registro | Crear usuario |
| Login    | Validar credenciales |

Aunque se relacionan, no son la misma responsabilidad.

**Fuentes:**
- User Stories Applied – Mike Cohn
- INVEST in Good Stories – Bill Wake
- https://scrumguides.org/ (SCRUM)
- https://cucumber.io/docs/gherkin/reference/ (BDD y Gherkin)
- Domain-Driven Design: Tackling Complexity in the Heart of Software – Eric Evans (DDD)
- Clean Architecture – Robert C. Martin

---

## 4. Decision sobre el nombre de la feature

Decido cambiar la feature de 'Inicio de sesión por parte de los usuarios' a **'Autenticación de usuarios'** ya que de esta manera se incluirian ambas funcionalidades y harian parte del mismo dominio, siguiendo buenas practicas.

---

## 5. Definicion de la feature e historias de usuario

Defino concretamente la feature en la que se va a trabajar, adiciono las dos historias de usuarios y asi mismo los criterios de aceptación de una manera muy generica.

---

## 6. Profundizacion de criterios de aceptacion

Le pido a la IA que segun esa feature y HUs, profundice en los criterios de aceptación, luego reviso y realizo ajustes segun el alcance que desee implementar despues en el desarrollo.

---

## 7. Reglas de negocio

Según la rubrica debemos desarrollar tambien las reglas de negocio, hablando con nuestros instructores se menciona que las reglas de negocio ya se encuentran implicitas en los criterios de aceptación, pero me queda la duda, por lo que decido consultar y encuentre con que las reglas de negocio gobiernan todo y por lo tanto son normas o politicas que el sistema debe cumplir si o si independientemente de como se implemente; Es por esta razón que tomo la decisión de separar los criterios de aceptación de las reglas de negocio aunque tengan cierta relación y como desconozco Reglas de negocio comunes, solicito a la IA segun los criterios de aceptacion que me recomiende algunas.

**Fuentes:**
- Business Rules Applied – Barbara von Halle
- Building Business Solutions: Business Analysis with Business Rules – Ronald G. Ross


8. Empiezo a investigar sobre UML y C4 para definir el metodo de modelamiento y asi comenzar con el diagrama de la arquitectura, luego de ver varias herramientas decidi realizarlo en Draw.io por familiaridad.

FUENTES: 
* https://c4model.com/
* https://www.omg.org/spec/UML/


9. Luego de realizar el nivel 2 de contenedores del diagrama C4 y pensar como iba a ser la comunicación del nuevo servicio con los demás, me detengo un momento a pensar que arquitectura puede ser la ideal para el nuevo servicio y decido usar la arquitectura de capas por que mi feature requiere un sistema pequeño sin alta complejidad, sin mucho crecimiento esperado.

FUENTES: 
* Clean Architecture - Robert C. Martin


10. Comence el diseño del nivel 3 del diagrama C4 y decidi seguir el patron de diseño de service layer, separando la logica de de negocio de los controladores y repositorios, tambien, para tener mayor control sobre cada hu y modularizar un poco mas, decidi crear dos controladores y en los servicios incluir las acciones de cada uno teniendo en cuenta los criterios de aceptacion, luego pense en los repositorios a crear y obviamente esta el de usuario que nos permite toda la gestion del mismo y por otro lado se encuentra un repositorio que gestiona los intentos de login y nos permite cumplir con la regla de negocio RN5 Limite de intentos fallidos: El sistema debe bloquear por un tiempo el acceso después de un 3 intentos fallidos de inicio de sesión de la HU2, luego con el diagrama casi listo le solicite a la IA su opinion teniendo en cuenta que era el nivel 3 de un c4, me recomendo crear un servicio unicamente para la gestion de contraseñas argumentando que: 'EL sistema debe bloquear por un tiempo el acceso después de un 3 intentos fallidos de inicio de sesión' y la acepte tambien siguiendo el principio de responsabilidad unica;


11. Ajuste estetico sobre los puntos adicionados con ayuda de la IA sin alterar mis palabras ni lo expresado en este archivo y luego se comienza con un analisis de posibles otros patrones de diseño que puedan ser implementados en el nuevo servicio;

* En un inicio, por conocer mas en profundidad el patron strategy, pense elegirlo para el servicio PasswordService (encargado de hashear y verificar) donde se pudiera cambiar el metodo o algoritmo de hasheo sin afectar el comportamiento, comence a hacerlo sin profundizar mucho en analisis pero luego cai en cuenta de que no se podria aplicar en este servicio por que al cambiar el algoritmo se perderia la correlación y persistencia con los datos almacenados en la base de datos, lo que no permitiria que el usuario pudiera ingresar sesión.

* Tambien se visualizo si depronto podria haber Patron Observer pero al no haber eventos que notificar a multiples suscriptores se descarto.

* Si se lograron identificar adicional al Service Layer (pensado desde el inicio), el Repository tambien esta implicito por ejemplo en la abstracción realizada a PostgreSQL, tambien con ayuda de la IA se obtuvo la identificación de un patron de diseño desconocido para mi llamado Guard Clause que consiste en realizar validaciones tempranas en los servicios que corta el flujo antes de ejecutar alguna logica costosa.

FUENTES: 
* Patrones de Diseño - Erich Gamma

12. Me di cuenta que en los primeros pasos, cuando elegi la feature a realizar, no estime el alcance sobre lo que incluye y lo que no, tampoco realice una descripción del impacto en la aplicación o el por que se va a realizar, es por esto que hice modificaciones en el documento word ' Documento de Diseño' en esta primera sección.