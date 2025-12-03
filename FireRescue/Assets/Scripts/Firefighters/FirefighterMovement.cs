using UnityEngine;
using System.Collections;

public class FirefighterMovement : MonoBehaviour
{
    public int firefighterId;
    public float moveSpeed = 3f;

    public void MoveTo(Vector3 targetPos)
    {
        StopAllCoroutines();
        StartCoroutine(MoveSmooth(targetPos));
    }

    private IEnumerator MoveSmooth(Vector3 targetPos)
    {
        Vector3 start = transform.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * moveSpeed;
            transform.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
    }
}
