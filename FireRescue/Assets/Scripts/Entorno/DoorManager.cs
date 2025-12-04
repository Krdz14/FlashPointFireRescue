// Assets/Scripts/Entorno/DoorManager.cs
using UnityEngine;
using System.Collections.Generic;

public class DoorManager : MonoBehaviour
{
    [Header("Door Prefab")]
    public GameObject doorPrefab;

    [Header("Grid Settings - Grid pequeño 6x8")]
    public float cellSize = 4f;

    // ORIGEN CORREGIDO PARA QUE EL GRID 6×8 QUEDE CENTRADO EN EL GRID 10×8
    public Vector3 gridOrigin = new Vector3(-5.13f, 0f, -17.34f);

    // Rotación real del grid pequeño
    private Quaternion gridRotation = Quaternion.Euler(0, 180f, 0);

    // Medidas del grid pequeño
    private int rows = 6;  // eje X local
    private int cols = 8;  // eje Z local

    private Dictionary<string, GameObject> doorObjects = new Dictionary<string, GameObject>();

    void Start()
    {
        InitializeAllDoors();
    }

    // ============================================================
    // CREACIÓN DE PUERTAS
    // ============================================================

    private void InitializeAllDoors()
    {
        if (doorPrefab == null)
        {
            Debug.LogError("❌ doorPrefab no está asignado");
            return;
        }

        int[,] interiorDoors = new int[,]
        {
            {1, 3, 1, 4},
            {1, 6, 1, 7},
            {2, 5, 2, 6},
            {2, 8, 3, 8},
            {3, 2, 3, 3},
            {4, 4, 5, 4},
            {4, 6, 4, 7},
            {6, 5, 6, 6}
        };

        for (int i = 0; i < interiorDoors.GetLength(0); i++)
        {
            int r1 = interiorDoors[i, 0] - 1;
            int c1 = interiorDoors[i, 1] - 1;
            int r2 = interiorDoors[i, 2] - 1;
            int c2 = interiorDoors[i, 3] - 1;

            CreateDoorBetween(r1, c1, r2, c2);
        }

        Debug.Log($"✅ {doorObjects.Count} puertas creadas");
    }

    private void CreateDoorBetween(int row1, int col1, int row2, int col2)
    {
        string key = GetDoorKey(row1, col1, row2, col2);

        if (doorObjects.ContainsKey(key))
            return;

        Vector3 p1 = GetWorldPosition(row1, col1);
        Vector3 p2 = GetWorldPosition(row2, col2);

        Vector3 mid = (p1 + p2) * 0.5f;

        GameObject door = Instantiate(doorPrefab, mid, Quaternion.identity);
        door.name = $"Door_{key}";

        if (row1 != row2)
            door.transform.rotation = Quaternion.Euler(0, 90, 0);
        else
            door.transform.rotation = Quaternion.Euler(0, 0, 0);

        doorObjects[key] = door;
    }

    // ============================================================
    // ACTUALIZACIÓN SEGÚN STATE
    // ============================================================

    public void UpdateDoorsState(State currentState)
    {
        foreach (var d in doorObjects.Values)
            d.SetActive(true);

        if (currentState == null || currentState.firefighters == null)
            return;

        foreach (var f in currentState.firefighters)
        {
            if (f.Opendoor == null) continue;

            foreach (var coords in f.Opendoor)
            {
                if (coords.Length < 2) continue;
                OpenDoorAtPosition(coords[0], coords[1]);
            }
        }
    }

    private void OpenDoorAtPosition(int row, int col)
    {
        int[] dr = { -1, 1, 0, 0 };
        int[] dc = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int nr = row + dr[i];
            int nc = col + dc[i];

            if (nr < 0 || nr >= rows || nc < 0 || nc >= cols)
                continue;

            string key = GetDoorKey(row, col, nr, nc);
            if (doorObjects.ContainsKey(key))
                doorObjects[key].SetActive(false);
        }
    }

    // ============================================================
    // POSICIONAMIENTO CON ROTACIÓN SOBRE CENTRO
    // ============================================================

    private Vector3 GetWorldPosition(int row, int col)
    {
        float w = rows * cellSize;
        float h = cols * cellSize;

        // centro del grid pequeño
        Vector3 center = new Vector3(w / 2f, 0, h / 2f);

        // posición local sin rotación
        Vector3 local = new Vector3(
            row * cellSize + cellSize * 0.5f,
            0,
            col * cellSize + cellSize * 0.5f
        );

        // posición world antes de rotar
        Vector3 worldPos = gridOrigin + local;

        // pivot real en world space
        Vector3 pivot = gridOrigin + center;

        // rotación CORRECTA alrededor del pivot
        Vector3 rotated = RotatePointAroundPivot(worldPos, gridOrigin + center, gridRotation);
        return rotated;
    }

    private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }

    private string GetDoorKey(int r1, int c1, int r2, int c2)
    {
        if (r1 > r2 || (r1 == r2 && c1 > c2))
        {
            (r1, r2) = (r2, r1);
            (c1, c2) = (c2, c1);
        }
        return $"{r1}_{c1}_{r2}_{c2}";
    }

    // ============================================================
    // LIMPIEZA
    // ============================================================

    public void ClearDoors()
    {
        foreach (var d in doorObjects.Values)
            Destroy(d);

        doorObjects.Clear();
    }

    // ============================================================
    // GIZMOS
    // ============================================================

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = GetWorldPosition(r, c);
                Gizmos.DrawWireCube(pos, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
#endif
}
