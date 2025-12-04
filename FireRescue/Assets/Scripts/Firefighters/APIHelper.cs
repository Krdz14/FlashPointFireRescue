using UnityEngine;
using System.Net;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public static class APIHelper
{
    private const string url = "https://these-screw-ringtone-asthma.trycloudflare.com/unity/get_all_states";

    public static SimulationData GetSimulationData()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        request.Method = "GET";
        request.Timeout = 10000; // 10 segundos

        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
        {
            string json = reader.ReadToEnd();

            Debug.Log($"JSON recibido (primeros 300 chars): {json.Substring(0, Mathf.Min(json.Length, 300))}");

            SimulationData data = JsonConvert.DeserializeObject<SimulationData>(json);

            if (data == null || data.states == null || data.states.Length == 0)
            {
                Debug.LogError(" No se pudieron deserializar los datos del JSON.");
                return null;
            }

            Debug.Log($"Se cargaron {data.states.Length} pasos desde la API.");
            return data;
        }
    }

    public static State[] GetFirstGame()
    {
        SimulationData data = GetSimulationData();
        if (data == null) return null;

        var states = data.states;
        if (states.Length == 0)
        {
            Debug.LogWarning("No hay estados en el JSON.");
            return null;
        }

        if (states[0].grid == null)
        {
            Debug.LogError(" El campo 'grid' en el primer estado es null.");
            return null;
        }

        int firstGameStart = -1;
    int nextGameStart = states.Length;

    for (int i = 0; i < states.Length; i++)
    {
        if (states[i].step == 1)
        {
            // Si no hemos encontrado el inicio aún
            if (firstGameStart == -1)
            {
                firstGameStart = i;
            }
            // Si ya habíamos encontrado uno antes, este es el inicio de la siguiente jugada
            else
            {
                nextGameStart = i;
                break;
            }
        }
    }

    // Si no se encontró ningún step == 1
    if (firstGameStart == -1)
    {
        Debug.LogWarning("No se encontró ningún step == 1, devolviendo todos los estados.");
        return states;
    }

    // Cortamos la primera jugada detectada
    var firstGame = states
        .Skip(firstGameStart)
        .Take(nextGameStart - firstGameStart)
        .ToArray();

        Debug.Log($" Primera jugada detectada: {firstGame.Length} pasos.");
        Debug.Log($" Step inicial: {firstGame[0].step}, Step final: {firstGame[^1].step}");
        Debug.Log($" Grid size: {firstGame[0].grid.Length}x{firstGame[0].grid[0].Length}");
        Debug.Log($" Índices: {firstGameStart} → {nextGameStart - 1}");

        return firstGame;
    }
}
