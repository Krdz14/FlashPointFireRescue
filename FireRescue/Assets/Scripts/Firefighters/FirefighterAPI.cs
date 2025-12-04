
using System;

[Serializable]
public class Position
{
    public int x;
    public int y;
}

[Serializable]
public class InitialPosition
{
    public int x;
    public int y;
}

[Serializable]
public class FirefighterStats
{
    public int saved_victims;
    public int fires_extinguished;
    public int smoke_extinguished;
}

[Serializable]
public class Firefighter
{
    public int id;
    public Position position;
    public InitialPosition initialPosition;
    public bool carrying;
    public string state;
    public int ap_used;
    public int[][]? Opendoor; 
    public FirefighterStats stats;
}

[Serializable]
public class Stats
{
    public int victims_rescued;
    public int victims_lost;
    public int fires_extinguished;
    public int smoke_extinguished;
    public int damage_points;
    public int active_pois;
    public int pois_revealed;
}

[Serializable]
public class GameStatus
{
    public bool game_over;
    public bool victory;
}

[Serializable]
public class Pools
{
    public int victims_remaining;
    public int false_alarms_remaining;
}

[Serializable]
public class State
{
    public int step;
    public int[][] grid;
    public Firefighter[] firefighters;
    public Stats stats;
    public GameStatus game_status;
    public Pools pools;
}

[Serializable]
public class SimulationData
{
    public State[] states;
}
