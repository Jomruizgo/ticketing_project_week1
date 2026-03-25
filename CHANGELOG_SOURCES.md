BITACORA DE NUEVA FEATURE
--------------------------

1. Analicé el proyecto para identificar que posibles Features se pueden implementar que sean de alta complejidad; Las funcionalidades (CRUD) de eventos ya estan existentes pero deberia restringir a que solamente los usuarios con un rol especifico pudiera hacerlo, pues no tiene logica que el sistema permita crear, editar, eliminar eventos a la misma persona que los va a comprar; Para esto yo he identificado que no existe la funcionalidad inicialmente para el registro del usuario y que implementar el Login y poder mostrar una UI dependiendo del rol implica 2 Features minimamente: Registro de usuarios, Inicio de sesión.


2. Le pedi a la IA sin sesgarla con mi posible feature, que analizara el proyecto y que segun el mismo, identificara las posibles features con las que podriamos continuar con el proyecto y propuso las siguientes: 

* Sistema de Autenticación y Usuarios (Auth Service)
* Sistema de Notificaciones en Tiempo Real (Notification Service)
* Sistema de Reservas con Timeout y Concurrencia (Reservation Engine)

Pero entre las tres opciones, recomendo realizar primero el sistema de Autenticación.

FUENTE: Proyecto


3. Le mencione a la IA la funcionalidad que deseo implementar pero le mencione que tenia dudas sobre si era una sola feature o eran dos distintas, a lo que me respondio:

*SCRUM*
En marcos como Scrum y enfoques de producto:

Una feature representa una unidad de valor para el usuario o negocio
Debe poder priorizarse, desarrollarse y probarse de forma independiente

Registro y login cumplen esto:
* Un usuario puede necesitar registrarse sin necesariamente iniciar sesión en ese momento
* Puedes priorizar “registro” antes que “login” o viceversa (aunque en la práctica suelen ir juntos)

---------------------------------------------------------------------

*HISTORIAS DE USUARIO*
Del concepto popularizado por Mike Cohn:

“Como [usuario], quiero [acción], para [valor]”

Aplicándolo:

* “Como usuario quiero registrarme…”
* “Como usuario quiero iniciar sesión…”

Son acciones distintas con valores distintos, por eso se separan.

-----------------------------------------------------------------------

*BDD/Gherkin*
En herramientas como Cucumber usando Gherkin:

Una Feature agrupa escenarios relacionados a una capacidad del sistema
No se recomienda mezclar capacidades diferentes en una misma feature

“Registro” y “Login” son capacidades diferentes:

* Crear cuenta 
* Autenticarse

------------------------------------------------------------------------

*Principio INVEST*
También de Bill Wake:

I – Independent (Independiente)

Si puedes desarrollar o probar algo sin depender completamente de otro, debería ser separado.

Registro:
* Crear usuario 

Login:
* Validar credenciales 

Aunque se relacionan, no son la misma responsabilidad


FUENTES: 
- User Stories Applied – Mike Cohn
- INVEST in Good Stories - Bill Wake
- https://scrumguides.org/ (SCRUM)
- https://cucumber.io/docs/gherkin/reference/ (BDD y Gherkin)
- Domain-Driven Design: Tackling Complexity in the Heart of Software – Eric Evans (DDD)
- Clean Architecture – Robert C. Martin


4. Decido cambiar la feature de 'Inicio de sesión por parte de los usuarios' a 'Autenticación de usuarios' ya que de esta manera se incluirian ambas funcionalidades y harian parte del mismo dominio, siguiendo buenas practicas.


5. Defino concretamente la feature en la que se va a trabajar, adiciono las dos historias de usuarios y asi mismo los criterios de aceptación de una manera muy generica.

6. Le pido a la IA que segun esa feature y HUs, profundice en los criterios de aceptación, luego reviso y realizo ajustes segun alcance que desee implementar despues en el desarrollo.

