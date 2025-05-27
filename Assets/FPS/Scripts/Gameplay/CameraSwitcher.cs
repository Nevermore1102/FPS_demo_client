using UnityEngine;
public class CameraSwitcher : MonoBehaviour
{
    public Camera firstPersonCamera;   // Main Camera
    public Camera thirdPersonCamera;   // ThirdCamera
    public KeyCode switchKey = KeyCode.V;

    private bool wasThirdPersonBeforeAiming = false;

    private bool isFirstPerson = true;

    void Start()
    {
        SetCamera(true);
    }

    void Update()
    {
        // 切换视角
        if (Input.GetKeyDown(switchKey))
        {
            isFirstPerson = !isFirstPerson;
            SetCamera(isFirstPerson);
        }

        // 按下右键瞄准
        if (Input.GetMouseButtonDown(1))
        {
            if (!isFirstPerson)
            {
                wasThirdPersonBeforeAiming = true;
                isFirstPerson = true;
                SetCamera(true);
            }
            else
            {
                wasThirdPersonBeforeAiming = false;
            }
        }

        // 松开右键，恢复到瞄准前的视角
        if (Input.GetMouseButtonUp(1))
        {
            if (wasThirdPersonBeforeAiming)
            {
                isFirstPerson = false;
                SetCamera(false);
            }
        }
    }


    void SetCamera(bool firstPerson)
    {
        if (firstPersonCamera != null) firstPersonCamera.enabled = firstPerson;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = !firstPerson;
    }
}