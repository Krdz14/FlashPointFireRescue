using UnityEngine;
using System.Collections;

public class FirefighterMovement : MonoBehaviour
{
    [Header("Settings")]
    public int firefighterId;
    public float moveSpeed = 3f;

    [Header("Victim Handling")]
    public bool carrying = false;
    public GameObject carriedVictim; 
    public Vector3 victimOffset = new Vector3(0, 0f, -3f);

    public void MoveTo(Vector3 targetPos)
    {
        StopAllCoroutines();
        StartCoroutine(MoveSmooth(targetPos));
    }

    public void SetCarrying(bool isCarrying)
    {
        carrying = isCarrying;

        if (carriedVictim != null)
        {
            // Si empieza a cargar → activar y posicionar
            if (isCarrying)
            {
                carriedVictim.SetActive(true);
                carriedVictim.transform.position = transform.position + victimOffset;
                carriedVictim.transform.rotation = transform.rotation;
            }
            // Si deja de cargar → desactivar
            else
            {
                carriedVictim.SetActive(false);
            }
        }
    }

    private IEnumerator MoveSmooth(Vector3 targetPos)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, targetPos, t);
            if (carrying && carriedVictim != null && carriedVictim.activeSelf)
            {
                carriedVictim.transform.position = transform.position + victimOffset;
                carriedVictim.transform.rotation = transform.rotation;
            }
            yield return null;
        }

        transform.position = targetPos;

        if (carrying && carriedVictim != null && carriedVictim.activeSelf)
        {
            carriedVictim.transform.position = transform.position + victimOffset;
            carriedVictim.transform.rotation = transform.rotation;
        }
    }
}
