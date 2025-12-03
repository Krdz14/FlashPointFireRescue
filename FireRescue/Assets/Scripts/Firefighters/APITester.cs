using UnityEngine;

public class APITest : MonoBehaviour
{
    void Start()
    {
        var firstGame = APIHelper.GetFirstGame();

        if (firstGame != null)
        {
            Debug.Log($" Primera jugada contiene {firstGame.Length} pasos.");
            Debug.Log($"Primer paso: {firstGame[0].step}");
            Debug.Log($"Último paso: {firstGame[^1].step}");
            Debug.Log($"Cantidad de bomberos en el primer paso: {firstGame[0].firefighters.Length}");
            Debug.Log($"Primer bombero ID: {firstGame[0].firefighters[0].id}");
        }
        else
        {
            Debug.LogError(" No se pudo obtener la jugada.");
        }
    }
}
