using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FirefighterPool : MonoBehaviour
{
    [Header("Settings")]
    public GameObject firefighterPrefab;
    public float stepInterval = 2f;
    public float cellSize = 8f; // Tamaño de celda de tu grid 3D

    private List<FirefighterMovement> firefighterPool = new();
    private State[] firstGame;
    private int currentStepIndex = 0;

    private IEnumerator Start()
    {
        Debug.Log("📡 Cargando jugada desde API...");
        firstGame = APIHelper.GetFirstGame();

        if (firstGame == null || firstGame.Length == 0)
        {
            Debug.LogError(" No se pudo cargar la jugada.");
            yield break;
        }

        Debug.Log($" Jugada con {firstGame.Length} pasos cargada.");

        // Crear pool según la cantidad de bomberos del primer step
        int firefightersCount = firstGame[0].firefighters.Length;
        AddFirefightersToPool(firefightersCount);

        // Inicializar posiciones
        InitializeFirefighters(firstGame[0]);

        // Loop de simulación
        while (currentStepIndex < firstGame.Length)
        {
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

            //  Calcular posición 3D según grid
            Vector3 worldPos = GetWorldPosition(fData);

            ff.transform.position = worldPos;
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
            Debug.Log($"⚙️ Número de bomberos cambió ({step.firefighters.Length}), ajustando pool...");
            AddFirefightersToPool(step.firefighters.Length);
            InitializeFirefighters(step);
        }

        for (int i = 0; i < step.firefighters.Length; i++)
        {
            var data = step.firefighters[i];
            var ff = firefighterPool[i];

            ff.firefighterId = data.id;

            Vector3 newPos = GetWorldPosition(data);
            ff.MoveTo(newPos);
        }

        Debug.Log($" Step {step.step} ejecutado.");
    }

    //  Convierte coordenadas del JSON (x,y) a coordenadas del mundo (X,Y,Z)
    private Vector3 GetWorldPosition(Firefighter data)
    {
        // Base en unidades del grid
        float baseX = data.position.x * cellSize;
        float baseZ = data.position.y * cellSize;

        // Centrado dentro de la celda
        baseX += cellSize / 2f;
        baseZ += cellSize / 2f;

        // Pequeño offset para diferenciar bomberos en la misma celda
        float offsetX = (data.id % 2) * 0.25f;
        float offsetZ = ((data.id / 2) % 2) * 0.25f;

        // Altura sobre el suelo
        float y = 0.5f;

        return new Vector3(baseX + offsetX, y, baseZ + offsetZ);
    }
}
