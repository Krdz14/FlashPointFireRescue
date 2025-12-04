using UnityEngine;
using System.Net;
using System.IO;
using System.Linq;

public static class APIHelper
{
    private const string url = "https://endif-among-human-exclusive.trycloudflare.com/unity/get_all_states";

    public static SimulationData GetSimulationData()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        StreamReader reader = new StreamReader(response.GetResponseStream());
        string json = reader.ReadToEnd();

        SimulationData data = JsonUtility.FromJson<SimulationData>(json);

        if (data == null || data.states == null || data.states.Length == 0)
        {
            Debug.LogError(" No se pudieron deserializar los datos del JSON.");
            return null;
        }

        Debug.Log($" Se cargaron {data.states.Length} pasos desde la API.");
        return data;
    }

    public static State[] GetFirstGame()
    {
        SimulationData data = GetSimulationData();
        if (data == null) return null;

        var states = data.states;
        if (states.Length == 0)
        {
            Debug.LogWarning(" No hay estados en el JSON.");
            return null;
        }

        // Buscamos el índice donde el contador de steps se reinicia
        int firstGameStart = 0;
        int nextGameStart = states.Length; // Por defecto, termina al final

        for (int i = 1; i < states.Length; i++)
        {
            if (states[i].step <= states[i - 1].step)
            {
                nextGameStart = i; // Aquí empezó una nueva jugada
                break;
            }
        }

        // Cortamos desde el inicio hasta el reinicio detectado
        var firstGame = states
            .Skip(firstGameStart)
            .Take(nextGameStart - firstGameStart)
            .ToArray();

        Debug.Log($" Primera jugada detectada: {firstGame.Length} pasos.");
        Debug.Log($" Step inicial: {firstGame[0].step}, Step final: {firstGame[^1].step}");
        Debug.Log($" Índices: {firstGameStart} → {nextGameStart - 1}");

        return firstGame;
    }

}
