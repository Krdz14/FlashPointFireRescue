# Flash Point: FireRescue - Simulación
Repositorio grupal para la creación de la solución del reto AD25 del juego de Flash Point Fire Rescue

### Autores
* Elizabeth Orduña
* Kate Rodriguez
* Alejandro Cruz

### Descripción
Implementación de una simulación multiagente del juego de mesa cooperativo Flash Point: Fire Rescue utilizando el framework Mesa de Python. El sistema modela un escenario de emergencia donde bomberos autónomos trabajan coordinadamente para rescatar víctimas de un edificio en llamas, utilizando algoritmos de pathfinding, sistemas de subastas y toma de decisiones basada en prioridades.

## Estrategia
### Prioridades
1. Prevención de Explosiones
2. Evacuación de Víctimas
3. Extinción Cercana
4. Entrada a la Casa
5. Resolución de POI en Posición
6. Objetivo Asignado
7. Cierre Estratégico de Puertas

### Algoritmo de ofertas (bidding)
bid = distancia_factor × riesgo_factor × tipo_factor × AP_factor

Donde:
- distancia_factor = 100 / (costo_ruta + 1)
- riesgo_factor = 50 / (riesgo_posición + 1)
- tipo_factor = {víctima: 10, falsa_alarma: 6, fuego: 5}
- AP_factor = (AP_actual / AP_máximo) × 1.2

**Gana el bombero con la oferta más alta para cada objetivo**

### Pathfinding con Dijkstra
Costo base = 1

Ajustes de riesgo:
+ Fuego en destino: +1
+ Víctima en brazos + fuego: ∞ (imposible)
+ Riesgo > 50: +5
+ Riesgo > 30: +3
+ Riesgo > 15: +1

Cálculo de riesgo:
+ Fuego en casilla: 30
+ Humo en casilla: 10
+ Fuegos vecinos: 10 cada uno
+ Humo + fuego vecinos: +15 extra

### Proceso de decisión
Cada bombero, cuando llega su turno, recibe 4 puntos de acción (AP) y evalúa la situación siguiendo un sistema de prioridades estricto. El agente revisa cada prioridad en orden y ejecuta la primera que aplique, consumiendo AP hasta agotarlos o hasta que no pueda realizar más acciones.

1. ¿Hay peligro inminente de explosión?
2. ¿Estoy llevando una víctima?
3. ¿Hay fuego o humo justo al lado mío?
4. ¿Estoy dentro de la casa?
5. ¿Hay algo importante en mi posición actual?
6. ¿Tengo un objetivo asignado por el sistema de coordinación?
    * Calcula la ruta más eficiente usando Dijkstra
    * Se mueve paso a paso hacia ese objetivo
    * Si encuentra puertas cerradas, las abre
    * Si encuentra paredes bloqueando, las rompe (si el daño estructural lo permite)
    * Si el objetivo ya no existe o no puede llegar, abandona el plan y espera nuevas instrucciones
7. ¿Puedo cerrar alguna puerta estratégicamente?

