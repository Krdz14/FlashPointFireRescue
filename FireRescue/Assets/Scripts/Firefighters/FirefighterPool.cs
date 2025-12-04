using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FirefighterPool : MonoBehaviour
{

    [Header("Scene References")]
    public Transform tableroGenerado;
    public DoorManager doorManager;

    [Header("Settings")]
    public GameObject firefighterPrefab;
    public GameObject victimPrefab;
    public float stepInterval = 2f;
    public float cellSize = 4f; // Tamaño de celda de tu grid 3D
    //public Vector3 boardCenter = new Vector3(-12f, 0f, 0f);


    private List<FirefighterMovement> firefighterPool = new();
    private State[] firstGame;
    private int currentStepIndex = 0;

    [Header("Grid Origin")]
    public Vector3 gridOrigin = new Vector3(-13.13f, 0f, -17.34f);

    public ModelPool modelPool;
    

    private IEnumerator Start()
    {
        Debug.Log(" Cargando jugada desde API...");
        firstGame = APIHelper.GetFirstGame();

        if (firstGame == null || firstGame.Length == 0)
        {
            Debug.LogError(" No se pudo cargar la jugada.");
            yield break;
        }

        Debug.Log($"FirefighterPool Start(): verificando referencias...");
        Debug.Log($"modelPool: {(modelPool == null ? "NULL" : "OK")}");
        Debug.Log($"firstGame: {(firstGame == null ? "NULL" : "OK")}");

        if (firstGame != null)
            Debug.Log($"firstGame length: {firstGame.Length}");

        if (firstGame != null && firstGame.Length > 0)
            Debug.Log($"firstGame[0].grid: {(firstGame[0].grid == null ? "NULL" : "OK")}");

        // Inicializar entorno
        int[,] grid = modelPool.ConvertTo2DArray(firstGame[0].grid);
        modelPool.InitializeGrid(grid);

        // Crear pool según la cantidad de bomberos del primer step
        int firefightersCount = firstGame[0].firefighters.Length;
        AddFirefightersToPool(firefightersCount);

        // Inicializar posiciones
        InitializeFirefighters(firstGame[0]);

        // Loop de simulación
        while (currentStepIndex < firstGame.Length)
        {
            modelPool.UpdateGrid(modelPool.ConvertTo2DArray(firstGame[currentStepIndex].grid));
            UpdateFirefighterPositions(firstGame[currentStepIndex]);
            currentStepIndex++;
            yield return new WaitForSeconds(stepInterval);
        }

        Debug.Log(" Jugada completada.");
    }

    private void AddFirefightersToPool(int amount)
    {
        firefighterPool.Clear();

        for (int i = 0; i < amount; i++)
        {
            GameObject go = Instantiate(firefighterPrefab, transform);
            go.SetActive(false);
            FirefighterMovement f = go.GetComponent<FirefighterMovement>();
            firefighterPool.Add(f);

            if (victimPrefab != null)
            {
                GameObject victim = Instantiate(victimPrefab, transform);
                victim.SetActive(false);
                f.carriedVictim = victim; 
            }
        }

        

        Debug.Log($" Pool dinámico creado con {amount} bomberos.");
    }

    //  Inicializa los bomberos en el primer step
    private void InitializeFirefighters(State firstState)
    {
        if (firstState.firefighters.Length != firefighterPool.Count)
        {
            AddFirefightersToPool(firstState.firefighters.Length);
        }

        for (int i = 0; i < firstState.firefighters.Length; i++)
        {
            var fData = firstState.firefighters[i];
            var ff = firefighterPool[i];

            ff.firefighterId = fData.id;

            Vector3 spawnPos;

            if (fData.initialPosition != null)
            {
                spawnPos = GetWorldPositionI(fData.initialPosition, fData.id);
            }
            else
            {
                spawnPos = GetWorldPosition(fData.position, fData.id);
            }

            ff.transform.position = spawnPos;
            ff.gameObject.SetActive(true);
            ff.name = $"Firefighter_{fData.id}";

        }

        Debug.Log($" {firstState.firefighters.Length} bomberos inicializados.");
    }

    //  Actualiza las posiciones en cada step
    private void UpdateFirefighterPositions(State step)
    {
        if (step.firefighters.Length != firefighterPool.Count)
        {
            Debug.Log($" Número de bomberos cambió ({step.firefighters.Length}), ajustando pool...");
            AddFirefightersToPool(step.firefighters.Length);
            InitializeFirefighters(step);
        }

         for (int i = 0; i < step.firefighters.Length; i++)
        {
            var data = step.firefighters[i];
            var ff = firefighterPool[i];

            if (ff == null)
            {
                Debug.LogWarning($" Bombero {i} en el pool es null, se omitirá este paso.");
                continue;
            }

            // Calcular la posición de destino con tu método 3D
            Vector3 targetPos = GetWorldPosition(data.position, data.id);

            // Mover al bombero a esa posición
            ff.MoveTo(targetPos);

            ff.SetCarrying(data.carrying);

        }

            if (doorManager != null)
        {
            doorManager.UpdateDoorsState(step);
        }
        
        Debug.Log($"Step {step.step} ejecutado ({step.firefighters.Length} bomberos movidos).");

    }

    //  Convierte coordenadas del JSON (x,y) a coordenadas del mundo (X,Y,Z)
    private Vector3 GetWorldPosition(Position pos, int firefighterId)
    {
        float baseX = pos.x * cellSize;
        float baseZ = pos.y * cellSize;

        // Centrado dentro de la celda
        baseX += cellSize / 2f;
        baseZ += cellSize / 2f;

        // Pequeño offset si varios bomberos comparten la celda
        float offsetX = (firefighterId % 2) * 0.25f;
        float offsetZ = ((firefighterId / 2) % 2) * 0.25f;

        // Altura sobre el suelo
        float y = 0f;

        // Aplica el offset global del tablero
        Vector3 worldPos = new Vector3(baseX + offsetX, y, baseZ + offsetZ);
        worldPos += gridOrigin;

        return worldPos;
    }

    private Vector3 GetWorldPositionI(InitialPosition pos, int firefighterId)
    {
        float baseX = pos.x * cellSize;
        float baseZ = pos.y * cellSize;

        // Centrado dentro de la celda
        baseX += cellSize / 2f;
        baseZ += cellSize / 2f;

        // Pequeño offset si varios bomberos comparten la celda
        float offsetX = (firefighterId % 2) * 0.25f;
        float offsetZ = ((firefighterId / 2) % 2) * 0.25f;

        // Altura sobre el suelo
        float y = 0f;

        // Aplica el offset global del tablero
        Vector3 worldPos = new Vector3(baseX + offsetX, y, baseZ + offsetZ);
        worldPos += gridOrigin;

        return worldPos;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        // 🔹 Dibuja una cuadrícula base del tablero
        int rows = 10;   // Ajusta al número real de filas de tu grid
        int cols = 8;   // Ajusta al número real de columnas
        float yOffset = 0.05f; // Altura mínima para que no se superponga con el suelo

        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                // Calcula el centro de cada celda según cellSize
                Vector3 center = transform.position + new Vector3(
                    x * cellSize + cellSize / 2f,
                    yOffset,
                    z * cellSize + cellSize / 2f
                );

                // Dibuja el contorno de la celda
                Gizmos.color = Color.gray;
                Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.05f, cellSize));

                if (x ==0 && z ==0)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.05f, cellSize));
                }
            }
        }

        

        // 🔹 Dibuja las celdas de los bomberos (si existen)
        if (firefighterPool != null)
        {
            Gizmos.color = Color.green;
            foreach (var ff in firefighterPool)
            {
                if (ff == null || !ff.gameObject.activeSelf) continue;

                Vector3 center = ff.transform.position;
                Gizmos.DrawWireCube(center, new Vector3(cellSize, 0.1f, cellSize));
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(center + Vector3.up * 0.05f, 0.1f);
                Gizmos.color = Color.green;
            }
        }
    }
    #endif


}

