using UnityEngine;
using System.Collections.Generic;

public class ModelPool : MonoBehaviour
{
    [Header("Prefabs por tipo de celda")]
    public GameObject zombiePrefab;
    public GameObject ghostPrefab;
    public GameObject victimPrefab;
    public GameObject poiPrefab;
    public GameObject falseAlarmPrefab;

    [Header("Grid Settings")]
    public float cellSize = 4f;
    public Vector3 gridOrigin = new Vector3(-13.13f, 0f, -17.34f);

    private int rows, cols;
    private ModelMovement[,] cellPool;

    // Inicializa el entorno visual
    public void InitializeGrid(int[,] grid)
    {
        if (grid == null)
        {
            Debug.LogError("grid es null en InitializeGrid()");
            return;
        }

        rows = grid.GetLength(0);
        cols = grid.GetLength(1);

        Debug.Log($"Inicializando grid {rows}x{cols}");

        cellPool = new ModelMovement[rows, cols];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int cellType = grid[y, x];
                GameObject prefab = GetPrefabForType(cellType);

                if (prefab == null)
                {
                    Debug.LogError($"Prefab nulo para tipo de celda {cellType} en ({x},{y})");
                    continue;
                }

                Vector3 pos = GetWorldPosition(x, y);
                GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

                if (obj == null)
                {
                    Debug.LogError($"Falló Instantiate en ({x},{y})");
                    continue;
                }

                ModelMovement cell = obj.GetComponent<ModelMovement>();

                if (cell == null)
                {
                    Debug.LogError($"Prefab '{prefab.name}' no tiene ModelMovement en ({x},{y})");
                    continue;
                }

                cell.SetState(cellType);
                cellPool[y, x] = cell;
            }
        }

        Debug.Log("ModelPool.InitializeGrid completado correctamente");
    }


    // Convierte una lista de listas del JSON a matriz 2D de ints
    public int[,] ConvertTo2DArray(int[][] gridList)
    {
        if (gridList == null)
        {
            Debug.LogError("ConvertTo2DArray recibió gridList = null");
            return null;
        }

        if (gridList.Length == 0)
        {
            Debug.LogError("gridList está vacío (sin filas)");
            return null;
        }

        int rows = gridList.Length;
        int cols = gridList[0]?.Length ?? 0;

        if (cols == 0)
        {
            Debug.LogError("La primera fila del grid es null o está vacía");
            return null;
        }

        int[,] grid = new int[rows, cols];

        for (int y = 0; y < rows; y++)
        {
            if (gridList[y] == null)
            {
                Debug.LogWarning($"gridList[{y}] es null — rellenando con ceros");
                continue;
            }

            for (int x = 0; x < cols; x++)
            {
                grid[y, x] = gridList[y].Length > x ? gridList[y][x] : 0;
            }
        }

        Debug.Log($" ConvertTo2DArray completado ({rows}x{cols})");
        return grid;
    }

    // Actualiza el entorno cada step
    public void UpdateGrid(int[,] newGrid)
    {
        if (newGrid == null)
        {
            Debug.LogError("newGrid es null en UpdateGrid()");
            return;
        }

        int rows = newGrid.GetLength(0);
        int cols = newGrid.GetLength(1);

        // Aseguramos que el pool esté inicializado
        if (cellPool == null)
        {
            Debug.LogWarning("cellPool no estaba inicializado, llamando InitializeGrid().");
            InitializeGrid(newGrid);
            return;
        }

        // Ajusta si el tamaño del grid cambió (raro, pero posible)
        if (cellPool.GetLength(0) != rows || cellPool.GetLength(1) != cols)
        {
            Debug.LogWarning(" Tamaño del grid cambió, reiniciando.");
            InitializeGrid(newGrid);
            return;
        }

        // Recorremos todas las celdas
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int newType = newGrid[y, x];
                ModelMovement cell = cellPool[y, x];

                // Si la celda debe tener un objeto
                if (ShouldHaveObject(newType))
                {
                    // Si no hay objeto → créalo
                    if (cell == null)
                    {
                        GameObject prefab = GetPrefabForType(newType);
                        if (prefab == null) continue;

                        Vector3 pos = GetWorldPosition(x, y);
                        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);
                        cell = obj.GetComponent<ModelMovement>();
                        if (cell != null) cell.SetState(newType);

                        cellPool[y, x] = cell;
                    }
                    // Si ya existe → actualiza su estado
                    else
                    {
                        cell.SetState(newType);
                    }
                }
                else
                {
                    // Si no debe existir y sí hay un objeto → elimínalo
                    if (cell != null)
                    {
                        Destroy(cell.gameObject);
                        cellPool[y, x] = null;
                    }
                }
            }
        }
    }

    private bool ShouldHaveObject(int type)
    {
        // 2 = humo, 3 = fuego, 4 = víctima, 6 = POI, 1 = falsa alarma
        return (type == 2 || type == 3 || type == 4 || type == 6 || type == 1);
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        int invertedY = (rows - 1) - y;

        float worldX = gridOrigin.x + (x * cellSize) + cellSize / 2f;
        float worldZ = gridOrigin.z + (invertedY * cellSize) + cellSize / 2f;
        float worldY = 0.5f;

        return new Vector3(worldX, worldY, worldZ);
    }

    private GameObject GetPrefabForType(int type)
    {
        switch (type)
        {
            case 1: return falseAlarmPrefab;
            case 2: return ghostPrefab;
            case 3: return zombiePrefab;
            case 4: return victimPrefab;
            case 6: return poiPrefab;
            default: return null;
        }
    }
}
