from mesa import Agent, Model
from mesa.space import MultiGrid
from mesa.datacollection import DataCollector
import random

# Imports numéricos
import numpy as np
import pandas as pd
import heapq
from collections import deque
from enum import Enum

# Importamos los siguientes paquetes para el mejor manejo de valores numéricos.
import seaborn as sns
sns.set()

# matplotlib lo usaremos crear una animación de cada uno de los pasos del modelo.
import matplotlib
import matplotlib.pyplot as plt
import matplotlib.animation as animation
plt.rcParams["animation.html"] = "jshtml"
matplotlib.rcParams['animation.embed_limit'] = 2**128
from matplotlib.colors import ListedColormap
from matplotlib.patches import Patch

class CellState(Enum):
    """Estados posibles de una celda"""
    EMPTY = 0
    FIRE = 1
    SMOKE = 2
    VICTIM = 3
    FALSE_ALARM = 4
    POINT_OF_INTEREST = 6

class FirefighterState(Enum):
    """Estados posibles de un bombero"""
    IDLE = "idle"
    RESOLVING_POI = "resolving_poi"
    DRAGGING_VICTIM = "dragging_victim"
    EXTINGUISHING = "extinguishing"
    MOVING = "moving"

# --- Agente Bombero ---
class FirefighterAgentRand(Agent):
    def __init__(self, model):
        super().__init__(model)
        self.AP = 4
        self.countAP = 0
        self.carrying = False
        self.state = FirefighterState.IDLE
        self.alive = True
        self.entry_target = None  # Posición de entrada asignada
        self.savedVictims = 0
        self.firesExtinguished = 0
        self.smokeExtinguished = 0

    # Moverse hacia una posición objetivo 
    def move_towards(self, target_pos):
        x, y = self.pos
        tx, ty = target_pos
        new_x, new_y = x, y

        if x < tx:
            new_x += 1
        elif x > tx:
            new_x -= 1
        elif y < ty:
            new_y += 1
        elif y > ty:
            new_y -= 1
        
        old_pos = self.pos
        new_pos = (new_x, new_y)
        key = tuple(sorted([old_pos, new_pos]))

        # Verificar si hay puerta entre ambas celdas
        if self.model.are_connected_by_door(old_pos, new_pos):
            door = self.model.doors[key]
            if not door["open"]:
                # La puerta está cerrada: abrirla cuesta 1 AP y no se mueve aún
                self.countAP += 1
                door["open"] = True
                self.state = FirefighterState.MOVING
                return  # No se mueve todavía

            else:
                # Si la puerta ya está abierta, cruza y se cierra después
                self.model.grid.move_agent(self, new_pos)
                door["open"] = False
                return

        # Si no hay puerta, moverse normalmente
        if 0 <= new_x < self.model.width and 0 <= new_y < self.model.height:
            self.model.grid.move_agent(self, new_pos)

    def step(self):
        if not self.alive:
            return
        
        self.countAP = 0
        while self.countAP < self.AP:
            action_taken = False
            x, y = self.pos
            cell = self.model.cell_states[(x, y)]

            # Si está fuera de la casa y no está cargando una víctima
            if not self.model.is_inside(self.pos) and not self.carrying:
                # Moverse hacia la posición de entrada asignada
                if self.entry_target:
                    self.move_towards(self.entry_target)
                    action_taken = True
                continue

            # Si está cargando una víctima
            if self.carrying:
                # Encontrar la salida más cercana
                safe_pos = self.model.get_nearest_exit(self.pos)

                # Si ya está en la zona segura
                if (x, y) == safe_pos:
                    self.model.assignEntryTargets(self, self.pos)
                    self.carrying = False
                    self.model.victims_rescued += 1
                    self.savedVictims += 1
                    self.model.clean_cell(self.pos)
                    action_taken = True
                # Si no está en la zona segura, moverse hacia ella
                else:
                    self.move_towards(safe_pos)
                    move_cost = 2 if CellState.FIRE in cell else 1
                    self.countAP += move_cost
                    action_taken = True
                continue

            # Verificar contenido de la celda actual y actuar según prioridades
            if CellState.POINT_OF_INTEREST in cell:
                self.state = FirefighterState.RESOLVING_POI
                self.model.pois_revealed += 1
                poi_state = self.model.reveal_poi(self.pos)
                if poi_state == CellState.VICTIM:
                    self.carrying = True
                    self.model.clean_cell(self.pos)
                    self.state = FirefighterState.DRAGGING_VICTIM
                    self.countAP += 2
                    action_taken = True
                    continue
                elif poi_state == CellState.FALSE_ALARM:
                    self.model.clean_cell(self.pos)
                    self.countAP += 1
                    action_taken = True
                    continue
            
            # Apagar fuego
            if CellState.FIRE in cell:
                self.state = FirefighterState.EXTINGUISHING
                self.model.cell_states[(x, y)].remove(CellState.FIRE)
                self.model.cell_states[(x, y)].append(CellState.SMOKE)
                self.model.fires_extinguished += 1
                self.firesExtinguished += 1
                self.countAP += 2
                action_taken = True
                continue
            
            # Eliminar humo
            if CellState.SMOKE in cell:
                self.state = FirefighterState.EXTINGUISHING
                self.model.cell_states[(x, y)].remove(CellState.SMOKE)
                self.model.smoke_extinguished += 1
                self.smokeExtinguished += 1
                self.countAP += 1
                action_taken = True
                continue

            # No hay acciones urgentes, moverse aleatoriamente
            neighbors = self.model.grid.get_neighborhood(self.pos, moore=False, include_center=False)
            neighbors = [n for n in neighbors if self.model.is_inside(n)]
            new_pos = random.choice(neighbors)
            new_cell = self.model.cell_states[(new_pos[0], new_pos[1])]
            move_cost = 2 if CellState.FIRE in new_cell else 1
            if move_cost <= self.AP:
                self.model.grid.move_agent(self, new_pos)
                self.countAP += move_cost
                action_taken = True

            # --- No terminar en fuego ---
            if CellState.FIRE in self.model.cell_states[(self.pos[0], self.pos[1])]:
                self.model.grid.move_agent(self, (x, y))
                self.countAP = 0

            if not action_taken:
                break


# --- Modelo principal ---
class FirefightingModel(Model):
    def __init__(self, width, height, num_firefighters):
        super().__init__()
        self.grid = MultiGrid(width, height, torus=False)
        self.width = width
        self.height = height
        self.is_inside = None  # Será definido más adelante
        self.assignEntryTargets = None  # Será definido más adelante

        # Estado del juego
        self.game_over = False
        self.victory = False

        # Contadores
        self.victims_rescued = 0
        self.victims_lost = 0
        self.firefighters_dead = 0
        self.fires_extinguished = 0
        self.smoke_extinguished = 0
        self.pois_revealed = 0
        self.damage_points = 0
        self.active_pois = 0

        # Delimitación del área de la casa
        self.house_x_min = 1
        self.house_x_max = max(1, self.width - 2)
        self.house_y_min = 1
        self.house_y_max = max(1, self.height - 2)

        # Metodo para verificar si una posición está dentro de la casa
        def is_inside(p):
            x, y = p
            return (self.house_x_min <= x <= self.house_x_max) and (self.house_y_min <= y <= self.house_y_max)
        
        
        self.is_inside = is_inside

        # Estados de celdas (matriz para el área de la casa 6x8)
        # El grid es 10x8, la casa está en posiciones (1-6, 0-7)
        self.cell_states = {}  # {(x,y): [CellState, ...]}
        for x in range(width):
            for y in range(height):
                self.cell_states[(x, y)] = []

        # POIs pool
        # POIs disponibles
        self.victims_pool = 10
        self.false_alarms_pool = 5

        # Paredes (simplificado: almacenar segmentos con daño)
        self.walls = {}  # {((x1,y1), (x2,y2)): damage_points}

        # Puertas
        self.doors = {}  # {((x1,y1), (x2,y2)): {"open": False}}

        
        # Posiciones de salida (esquinas del grid)
        self.exit_positions = [ (0, 5), (2, 0), (3, 7), (5, 2) 
        ] # Posiciones iniciales

        # Inicializar grid
        self.initialize_grid()

        # Inicializar puertas
        self.initialize_doors()

        # DataCollector
        self.datacollector = DataCollector(
            model_reporters={
                "Victims_Rescued": lambda m: m.victims_rescued,
                "Victims_Lost": lambda m: m.victims_lost,
                "Firefighters_Dead": lambda m: m.firefighters_dead,
                "Fires_Extinguished": lambda m: m.fires_extinguished,
                "Smoke_Extinguished": lambda m: m.smoke_extinguished,
                "POIs_Revealed": lambda m: m.pois_revealed,
                "Damage_Points": lambda m: m.damage_points,
                "Game_Over": lambda m: m.game_over,
                "Victory": lambda m: m.victory
            }
        )


        # Definir entry targets para cuando salgan
        def assignEntryTargets(agent, pos):
            x, y = pos
            if x == 0:
                entry = (1, y)
            elif x == self.width - 1:
                entry = (self.width - 2, y)
            elif y == 0:
                entry = (x, 1)
            elif y == self.height - 1:
                entry = (x, self.height - 2)
            else:
                entry = None
            agent.entry_target = entry
        self.assignEntryTargets = assignEntryTargets

        self.firefighters = []
        # Crear agentes bomberos
        for i in range(num_firefighters):
            firefighter = FirefighterAgentRand(self)
            firefighter.alive = True
            self.grid.place_agent(firefighter, self.exit_positions[i % len(self.exit_positions)])
            start_pos = self.exit_positions[i % len(self.exit_positions)]
            assignEntryTargets(firefighter, start_pos)
            self.firefighters.append(firefighter)

    def initialize_doors(self):
      """Inicializa las puertas del tablero"""
      # Formato: (r1, c1, r2, c2) donde r=fila, c=columna
      door_coords = [
          (1, 3, 1, 4),
          (2, 5, 2, 6),
          (2, 8, 3, 8),
          (3, 2, 3, 3),
          (4, 4, 5, 4),
          (4, 6, 4, 7),
          (6, 5, 6, 6),
          (6, 7, 6, 8)
      ]
      
      for r1, c1, r2, c2 in door_coords:
          # Convertir a coordenadas de Mesa (x, y)
          # r (fila) va de 1-6 → x va de house_x_min a house_x_max (1-6)
          # c (columna) va de 1-8 → y va de house_y_min a house_y_max (1-8)
          
          x1 = self.house_x_min + (r1 - 1)  # r1 en [1,6] → x1 en [1,6]
          y1 = self.house_y_min + (c1 - 1)  # c1 en [1,8] → y1 en [1,8]
          x2 = self.house_x_min + (r2 - 1)  # r2 en [1,6] → x2 en [1,6]
          y2 = self.house_y_min + (c2 - 1)  # c2 en [1,8] → y2 en [1,8]
          
          # Verificar que las puertas estén dentro del área de la casa
          if (self.house_x_min <= x1 <= self.house_x_max and 
              self.house_y_min <= y1 <= self.house_y_max and
              self.house_x_min <= x2 <= self.house_x_max and 
              self.house_y_min <= y2 <= self.house_y_max):

              
                key = tuple(sorted([(x1, y1), (x2, y2)]))
                self.doors[key] = {"open": False}
                print(f"Puerta agregada: {key}")
          else:
              print(f"Puerta fuera de límites: ({x1},{y1}) ↔ ({x2},{y2})")

    def initialize_grid(self):
        """Inicializa el mapa con fuegos y POIs"""
        # Crear 10 fuegos aleatorios en la casa (área 1-6, 0-7)

        attempts = 0
        fire_count = 0
        max_attempts = 100
        while fire_count < 10 and attempts < max_attempts:
            x = self.random.randint(self.house_x_min, self.house_x_max)
            y = self.random.randint(self.house_y_min, self.house_y_max)
            if CellState.FIRE not in self.cell_states[(x, y)]:
                self.cell_states[(x, y)].append(CellState.FIRE)
                fire_count += 1
            attempts += 1

        # Crear 3 POIs iniciales (no sobre fuegos)
        poi_count = 0
        while poi_count < 3 and attempts < max_attempts:
            # Solo generar humo dentro de la casa
            x = self.random.randint(self.house_x_min, self.house_x_max)
            y = self.random.randint(self.house_y_min, self.house_y_max)
            if (CellState.FIRE not in self.cell_states[(x, y)] and
                CellState.POINT_OF_INTEREST not in self.cell_states[(x, y)]):
                self.create_new_poi((x, y))
                poi_count += 1
            attempts += 1
    
    def are_connected_by_door(self, pos1, pos2):
        key = tuple(sorted([pos1, pos2]))
        return key in self.doors

    def step(self):
        if self.game_over:
            return
        # Agentes actúan
        for firefighter in self.firefighters:
            firefighter.step()
        
        # Avanza el fuego (crear 1 humo aleatorio)
        self.advance_fire()

        # Spawnear nuevo POI si hay menos de 3 activos
        if self.active_pois < 3:
            self.spawn_poi()

        # Recolectar datos
        self.datacollector.collect(self)

        # Verificar condiciones de fin de juego
        self.check_game_over()

    def advance_fire(self):
        """Avanza el fuego creando humo en una celda adyacente a un fuego activo"""
        # Intentos para encontrar una celda sin agentes
        attempts = 0
        max_attempts = 20
        placed = False

        while attempts < max_attempts and not placed:
            # Solo generar humo dentro de la casa
            x = self.random.randint(self.house_x_min, self.house_x_max)
            y = self.random.randint(self.house_y_min, self.house_y_max)

            # comprobar si hay agentes en la celda
            occupants = self.grid.get_cell_list_contents((x, y))
            if any(True for o in occupants):  # hay al menos un agente -> evitar poner humo aquí
                attempts += 1
                continue

            # Colocar humo en la celda libre de agentes
            self.add_smoke((x, y))
            placed = True

        # Si no se colocó humo (se agotaron intentos), simplemente no colocar esta vez

        # Verificar si el humo está en vecindad de fuego
        neighbors = self.get_von_neumann_neighbors((x, y))
        for neighbor in neighbors:
            if self.has_fire(neighbor):
                # Convertir humo en fuego
                if CellState.SMOKE in self.cell_states[(x, y)]:
                    self.cell_states[(x, y)].remove(CellState.SMOKE)
                self.cell_states[(x, y)].append(CellState.FIRE)
                break

        # Flashover: Humo adyacente a fuego se convierte en fuego
        self.apply_flashover()

        # Verificar bomberos en fuego (Knocked Down)
        self.check_knocked_down()

        # Verificar víctimas en fuego (Lost)
        self.check_victims_lost()
    
    def add_smoke(self, pos):
        """Agrega humo a una posición"""
        if CellState.SMOKE not in self.cell_states[pos]:
            # Si ya hay fuego, no agregar humo (podría causar explosión)
            if CellState.FIRE in self.cell_states[pos]:
                # TODO: Implementar explosiones si se requiere
                pass
            else:
                self.cell_states[pos].append(CellState.SMOKE)
    
    def apply_flashover(self):
        """Convierte humo adyacente a fuego en fuego"""
        changed = True
        while changed:
            changed = False
            for pos, states in list(self.cell_states.items()):
                if CellState.SMOKE in states:
                    neighbors = self.get_von_neumann_neighbors(pos)
                    for neighbor in neighbors:
                        if self.has_fire(neighbor):
                            self.cell_states[pos].remove(CellState.SMOKE)
                            self.cell_states[pos].append(CellState.FIRE)
                            changed = True
                            break

    def check_knocked_down(self):
        """Verifica si algún bombero está en fuego"""
        for f in self.firefighters:
            if not f.alive:
                continue
            if self.has_fire(f.pos):
                # Bombero derribado - ir a la salida más cercana
                safe_pos = self.get_nearest_exit(f.pos)
                f.move_towards(safe_pos)
                f.countAP += 1  # Costo de movimiento
                if f.carrying:
                    self.victims_lost += 1
                    f.carrying = False

    def check_victims_lost(self):
        """Verifica si alguna víctima está en fuego"""
        for pos, states in list(self.cell_states.items()):
            if CellState.POINT_OF_INTEREST in states and CellState.FIRE in states:
                # Víctima perdida
                self.clean_cell(pos)
                self.victims_lost += 1

    def spawn_poi(self):
        """Genera un nuevo POI en una celda vacía dentro de la casa"""
        current_POIs = self.active_pois
        while current_POIs < 3:
            # Solo generar humo dentro de la casa
            x = self.random.randint(self.house_x_min, self.house_x_max)
            y = self.random.randint(self.house_y_min, self.house_y_max)
            if (CellState.VICTIM not in self.cell_states[(x, y)] and
                CellState.FALSE_ALARM not in self.cell_states[(x, y)]):

                if CellState.FIRE in self.cell_states[(x, y)]:
                    self.cell_states[(x, y)].remove(CellState.FIRE)
                if CellState.SMOKE in self.cell_states[(x, y)]:
                    self.cell_states[(x, y)].remove(CellState.SMOKE)

                self.create_new_poi((x, y))
                current_POIs += 1
                self.active_pois += 1
    
    def create_new_poi(self, pos):
        """Crea un nuevo POI en la posición especificada"""
        if CellState.POINT_OF_INTEREST not in self.cell_states[pos]:
            self.cell_states[pos].append(CellState.POINT_OF_INTEREST)
            self.active_pois += 1

    def check_game_over(self):
        """Verifica si el juego ha terminado"""
        # Victoria: 7 víctimas rescatadas
        if self.victims_rescued >= 7:
            self.game_over = True
            self.victory = True

        # Derrota: 4 víctimas perdidas
        if self.victims_lost >= 4:
            self.game_over = True
            self.victory = False

        # Derrota: 24 puntos de daño
        if self.damage_points >= 24:
            self.game_over = True
            self.victory = False

    def clean_cell(self, pos):
        """Limpia una celda (remueve víctima/falsa alarma)"""
        if CellState.VICTIM in self.cell_states[pos]:
            self.cell_states[pos].remove(CellState.VICTIM)
            self.active_pois = max(0, self.active_pois - 1)
        
        if CellState.FALSE_ALARM in self.cell_states[pos]:
            self.cell_states[pos].remove(CellState.FALSE_ALARM)
            self.active_pois = max(0, self.active_pois - 1)

        if CellState.POINT_OF_INTEREST in self.cell_states[pos]:
            self.cell_states[pos].remove(CellState.POINT_OF_INTEREST)
            self.active_pois = max(0, self.active_pois - 1)
            
    def reveal_poi(self, pos):
        """Revela el POI en la posición dada y retorna su estado"""
        # Remueve el marcador de interés
        if CellState.POINT_OF_INTEREST in self.cell_states[pos]:
            self.cell_states[pos].remove(CellState.POINT_OF_INTEREST)
            self.active_pois -= 1

        # Decide qué había realmente
        if self.victims_pool > 0 and self.false_alarms_pool > 0:
            if self.random.random() < 0.67:
                self.cell_states[pos].append(CellState.VICTIM)
                self.victims_pool -= 1
                return CellState.VICTIM
            else:
                self.cell_states[pos].append(CellState.FALSE_ALARM)
                self.false_alarms_pool -= 1
                return CellState.FALSE_ALARM
        elif self.victims_pool > 0:
            self.cell_states[pos].append(CellState.VICTIM)
            self.victims_pool -= 1
            return CellState.VICTIM
        elif self.false_alarms_pool > 0:
            self.cell_states[pos].append(CellState.FALSE_ALARM)
            self.false_alarms_pool -= 1
            return CellState.FALSE_ALARM
        else:
            return None
    
    def has_fire(self, pos):
        """Verifica si hay fuego en la posición dada"""
        return CellState.FIRE in self.cell_states[pos]
    
    def get_nearest_exit(self, pos):
        """Encuentra la salida más cercana"""
        min_dist = float('inf')
        closest = self.exit_positions[0]
        for exit_pos in self.exit_positions:
            dist = abs(pos[0] - exit_pos[0]) + abs(pos[1] - exit_pos[1])
            if dist < min_dist:
                min_dist = dist
                closest = exit_pos
        return closest
    
    def get_von_neumann_neighbors(self, pos):
        """Retorna los vecinos de von Neumann de una posición"""
        x, y = pos
        neighbors = []
        for dx, dy in [(1, 0), (-1, 0), (0, 1), (0, -1)]:
            nx, ny = x + dx, y + dy
            if 0 <= nx < self.width and 0 <= ny < self.height and 0 < nx < self.width-1 and 0 < ny < self.height-1:
                neighbors.append((nx, ny))
        return neighbors           

def get_grid_state(model):
        """Obtiene el estado del grid para visualización"""
        grid = np.zeros((model.width, model.height))

        for (x, y), states in model.cell_states.items():
            # Prioridad: Fuego > Víctima > Humo > Falsa Alarma > Vacío
            if CellState.FIRE in states:
                grid[x][y] = 3  # Rojo intenso
            elif CellState.VICTIM in states:
                grid[x][y] = 4  # Amarillo
            elif CellState.SMOKE in states:
                grid[x][y] = 2  # Gris
            elif CellState.FALSE_ALARM in states:
                grid[x][y] = 1  # Verde claro
            elif CellState.POINT_OF_INTEREST in states:
                grid[x][y] = 6  # Morado
            elif (x == 0 or x == model.width - 1 or y == 0 or y == model.height - 1):
                grid[x][y] = 7  #Negro (exterior)
            else:
                grid[x][y] = 0  # Blanco/vacío

        # Marcar posiciones de bomberos (azul)
        for f in model.firefighters:
            if f.alive:
                x1, y1 = f.pos
                grid[x1][y1] = 5  # Azul
        return grid
    
if __name__ == "__main__":
    print("Iniciando simulacion...")

    MAX_STEPS = 50
    model = FirefightingModel(width=8, height=10, num_firefighters=6)

    # --- Reconfigurar DataCollector para guardar el grid visual ---
    model.datacollector = DataCollector(
        model_reporters={
            "grid": get_grid_state,  # 🔹 Guardamos el estado visual del grid
            "Victims_Rescued": lambda m: m.victims_rescued,
            "Victims_Lost": lambda m: m.victims_lost,
            "Damage_Points": lambda m: m.damage_points,
            "Fires_Extinguished": lambda m: m.fires_extinguished,
            "Smoke_Extinguished": lambda m: m.smoke_extinguished,
            "POIs_Revealed": lambda m: m.pois_revealed,
        }
    )

    step_count = 0
    while not model.game_over and step_count < MAX_STEPS:
        step_count += 1
        model.step()
        model.datacollector.collect(model)
        print(f"Turno {step_count:02d} | Rescatadas={model.victims_rescued} | Perdidas={model.victims_lost} | Fuegos Apagados={model.fires_extinguished} | Humo Eliminado={model.smoke_extinguished}")

    print("\nSimulación finalizada.")
    if model.victory:
        print("¡VICTORIA! :)")
    else:
        print("DERROTA:(")

    print("\nGenerando GIF de la simulación...")

    # Obtener los grids guardados
    model_data = model.datacollector.get_model_vars_dataframe()
    grids = model_data["grid"].values  # Lista de arrays de estados

    # Definir colores personalizados
    colors = [
        "white",       # 0 → Vacío
        "lightgreen",  # 1 → Falsa alarma
        "gray",        # 2 → Humo
        "red",         # 3 → Fuego
        "yellow",      # 4 → Víctima
        "blue",        # 5 → Bombero
        "purple",      # 6 → Punto de interés
        "black"     # 7 Exterior
    ]
    
    labels = [
        "Vacío",
        "Falsa alarma",
        "Humo",
        "Fuego",
        "Víctima",
        "Bombero",
        "Punto de interés",
        "Exterior"
    ]

    # Crear mapa de colores
    cmap = ListedColormap(colors)

    fig, ax = plt.subplots(figsize=(5, 4))
    ax.set_xticks([])
    ax.set_yticks([])

    # Mostrar el primer frame
    first_grid = np.rot90(grids[0], k=-1)
    img = ax.imshow(first_grid, cmap=cmap, origin='upper', vmin=0, vmax=7)
    ax.set_title("Simulación de Bomberos - Flash Point")

    # Crear recuadros de color para la leyenda
    legend_elements = [
        Patch(facecolor=colors[i], edgecolor='black', label=labels[i])
        for i in range(len(colors))
    ]

    # Agregar la leyenda a la figura
    ax.legend(
        handles=legend_elements,
        loc='upper right',
        bbox_to_anchor=(1.35, 1),
        title="Leyenda",
        frameon=True
    )

    def animate(i):
        frame = np.rot90(grids[i], k=-1)
        img.set_data(frame)
        ax.set_title(f"Turno {i+1} | Rescatadas={model.victims_rescued} | Perdidas={model.victims_lost}")
        return [img]

    anim = animation.FuncAnimation(fig, animate, frames=len(grids), interval=400, blit=True)
    anim.save("fire_simulation.gif", writer="pillow", fps=3)
    plt.close(fig)

    print("GIF guardado como 'fire_simulation.gif'")