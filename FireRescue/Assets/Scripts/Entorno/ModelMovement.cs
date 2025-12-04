using UnityEngine;

public class ModelMovement : MonoBehaviour
{
    private int currentState = 0;

    public void SetState(int newState)
    {
        if (newState == currentState) return; // no hay cambio
        currentState = newState;

        switch (newState)
        {
            case 0:
                gameObject.SetActive(false); // vacío
                break;

            case 2:
                // humo
                gameObject.SetActive(true);
                break;

            case 3:
                // fuego
                gameObject.SetActive(true);
                break;

            case 4:
                // víctima
                gameObject.SetActive(true);
                break;

            case 6:
                // POI
                gameObject.SetActive(true);
                break;

            case 7:
                // pared / exterior
                gameObject.SetActive(true);
                break;

            default:
                gameObject.SetActive(true);
                break;
        }
    }
}
