Objetivo: El objetivo de este archivo es documentar fuentes que ha sido utilies para realizar la docuemntación de un plan para la implementación de una feature/épica.

1. https://www.atlassian.com/agile/project-management/epics
En esta fuente podemos encontrar información de la definión de lo que es una epica y ejemplos.

2. https://www.parallelhq.com/blog/how-to-write-epic-in-agile
- En esta fuente esta mas actualizada ya que habla especificamente del 2026 y tambien nos habla de cómo descomponer una épica en HUs.
- Aqui tambien encontramos que algunos equipos suelen estructurar las feature/Epic basandose en el mismo patron de una HU: "As a [user], I want [goal], so that [value].” (Aunque luego de la reunión con Santiago y Camilo nos queda claro que esta no es la unica estructura con la que se puede definir una HU, es solo una convensión común)
- En esta fuente tambien encontramos información sobre Planificion y ejecución de la épica, sin embargo, esto parece estar mas enfocado al seguimiento que a la estructuración de una plan documenado.

3. https://medium.com/@habibullah.diu/understanding-brd-prd-sdd-tsd-the-blueprint-of-modern-software-engineering-689aac9acc0b
Esta fuente habla de cuatro documentos clave para todo proyecto de sofware moderno, los cuales son BRD, PRD, SDD y TSD, mencionandose la estructura basica de cada uno de estos, para un diseño de software con intención y claridad.

4. https://www.scrum.org/forum/scrum-forum/41535/epic-acceptance-criteria
Esta fuente se tra de un foro donde se habla sobre si una feature/Epic debe tener quiterios de aceptación o no, de aqui queda claro que no hay un concenso sobre ellos y que todo depende de lo que significa feature/epic para tu equipo y si tiene una ventaja o no que esta venga con croterios de aceptación, lo que si parece ser un concenso es la feature/epic definitivamente es algo que de debe refinar y transformas en historias de usuarios, por lo menos en formación que tuvimos con Santiago la definición que el maneja de feature/Epic, esta no tiene criterios de aceptación.

5. https://rodrigo-lara.medium.com/arquitectura-de-un-sistema-de-venta-de-boletos-para-eventos-de-gran-escala-c20b9170b2d4
En esta fuente se encuentra un punto de vista de la evolución arquitectonica que deben o suelen tener proyectos de software similares al presente proyecto. Esto nos para darnos cuenta que tan desviada, o coherente puede estar nuestra arquitectura actual respecto al estado del arte.

6. https://scrum.menzinsky.com/2018/01/que-son-y-como-funcionan-las-historias.html
En esta fuente se habla de la Historias técnicas, sin embargo, se llaga la conclusión de que para el desarrollo de la Feature correspondiente al presente entregable no es necesario tener historias técnicas, tal vez tendrpia sentido una historia técnica como habilitador en en el caso de la notificación por correo, pero esto se puede manejar directamente con una HU.

7. https://medium.com/@hector-reyesaleman/visual-representations-of-software-systems-diagrams-as-communication-tools-d3fa106ba3a6
En esta fuente se habla sobre los diagramas arquitectónicos, a partir de dicha referecia se toma la deción se usar un par de tipode  graficos en el presente trabajo, los cuales son: Diagrama de secuencia y diagrama de contenedores en C4. Tambien se usa el siguiente video de youtube como guia pra realizar los diagramas con la herramienta draw.io https://www.youtube.com/watch?v=QEXGbpsUXaI 


8. https://javiergarzas.com/2012/05/descomponer-historias-de-usuario-en-tareas-1.html
Esta Fuente habla de recomendaciones para dividir una HU en tasks. Las cuales son consideradas teniendo en cuenta que para nuestro caso seguimos TDD.

9. https://www.taurusgalaxy.com/post/incrementando-calidad-con-dod-y-dor
Referencia para crear DoR y DoD



Dadas la refecncias anteriores ya tenemos herramientas para crear un marco que nos permita documentar un plan de implementación de una feature/Epica, el marco al que hemos concluido debe contener lo siguiente:

- Feature
 * Definición
 * Probelma que resuelve
 * Supuestos
 * Alcance

- Palabras clave o vocabulario de negocio

- Refinamiento en hacia Hus
 * Definición
 * Verificación INVEST
 * Criterios de Aceptación
 * DoR
 * DoD
 * Estimación de Efuerzo (opcional)

- Análisis de Impacto arquitectónico

- Tareas (Opcional)


https://medium.com/@daniel_giraldo/fluent-validation-d931c0382348
Heramienta FluentValidation aunque se podiran usar similares esta puede ser de ayuda como herrmienta de validación

https://github.com/open-telemetry
https://www.dynatrace.com/news/blog/what-is-opentelemetry/
Herramienta Open Telemetry puede ser de ayuda para temas de trazabilidad
