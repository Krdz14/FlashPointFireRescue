from fastapi import FastAPI
from pydantic import BaseModel
from fastapi.middleware.cors import CORSMiddleware
from Project.FlashPointFireRescue.Project import FirefightingModel, get_grid_state, FirefighterAgentRand  # tu script principal
import numpy as np

# --- Inicializar FastAPI ---
app = FastAPI()

# Acceso a Unity
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # o pon la IP de tu Unity client
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- Inicializar el modelo ---
model = FirefightingModel(width=8, height=10, num_firefighters=6)

# --- Estructuras de datos para el frontend (Unity) ---
class GridResponse(BaseModel):
    grid: list
    step: int
    stats: dict

step_count = 0

# --- Endpoint: obtener estado actual ---
@app.get("/state", response_model=GridResponse)
def get_state():
    global step_count
    grid_array = get_grid_state(model)
    grid_list = grid_array.tolist()

    stats = {
        "victims_rescued": model.victims_rescued,
        "victims_lost": model.victims_lost,
        "fires_extinguished": model.fires_extinguished,
        "smoke_extinguished": model.smoke_extinguished,
        "pois_revealed": model.pois_revealed,
        "damage_points": model.damage_points,
        "game_over": model.game_over,
        "victory": model.victory
    }

    return GridResponse(
        grid=grid_list,
        step=step_count,
        stats=stats
    )

# --- Endpoint: avanzar un paso ---
@app.post("/step", response_model=GridResponse)
def step_model():
    global step_count
    model.step()
    step_count += 1
    grid_array = get_grid_state(model)
    grid_list = grid_array.tolist()

    stats = {
        "victims_rescued": model.victims_rescued,
        "victims_lost": model.victims_lost,
        "fires_extinguished": model.fires_extinguished,
        "smoke_extinguished": model.smoke_extinguished,
        "pois_revealed": model.pois_revealed,
        "damage_points": model.damage_points,
        "game_over": model.game_over,
        "victory": model.victory
    }

    return GridResponse(
        grid=grid_list,
        step=step_count,
        stats=stats
    )

# --- Endpoint: reiniciar el modelo ---
@app.post("/reset")
def reset_model():
    global model, step_count
    model = FirefightingModel(width=8, height=10, num_firefighters=6)
    step_count = 0
    return {"message": "Modelo reiniciado."}
