using UnityEngine;
using UnityEngine.InputSystem; // IMPORTANTE

public class CameraSwitcher : MonoBehaviour
{
    public Camera[] cameras;
    private int currentIndex = 0;

    void Start()
    {
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].gameObject.SetActive(i == currentIndex);
    }

    void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SwitchCamera();
        }
    }

    void SwitchCamera()
    {
        cameras[currentIndex].gameObject.SetActive(false);

        currentIndex = (currentIndex + 1) % cameras.Length;

        cameras[currentIndex].gameObject.SetActive(true);
    }
}
