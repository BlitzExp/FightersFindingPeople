using UnityEngine;
using System.Collections.Generic;

public class CamaraManager : MonoBehaviour
{
    private int currentDisplay = 0; // Display actual
    private List<Camera> cams = new List<Camera>();
    private List<Camera> fillerCams = new List<Camera>(); // Cámaras negras extra

    void Update()
    {
        // Cambiar al siguiente display con tecla TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            currentDisplay++;
            if (currentDisplay >= Display.displays.Length) currentDisplay = 0;

            SetupDroneCameras(cams, currentDisplay);
            Debug.Log($"Cambiado al Display {currentDisplay}");
        }
    }

    public void SetUpCameras()
    {
        cams.Clear();

        foreach (var dron in GameObject.FindGameObjectsWithTag("Dron"))
        {
            Camera cam = dron.GetComponentInChildren<Camera>();
            if (cam != null) cams.Add(cam);
        }

        SetupDroneCameras(cams, currentDisplay);
    }

    public void SetupDroneCameras(List<Camera> droneCameras, int displayId)
    {
        int maxPerDisplay = 4;
        int camsOnCurrentDisplay = 0;

        // Desactivar fillerCams anteriores
        foreach (var fcam in fillerCams) Destroy(fcam.gameObject);
        fillerCams.Clear();

        for (int i = 0; i < droneCameras.Count; i++)
        {
            Camera cam = droneCameras[i];

            if (i / maxPerDisplay == displayId)
            {
                cam.targetDisplay = displayId;

                int camIndex = camsOnCurrentDisplay;
                float x = (camIndex % 2) * 0.5f;
                float y = (camIndex / 2) * 0.5f;
                cam.rect = new Rect(x, 1f - y - 0.5f, 0.5f, 0.5f);

                camsOnCurrentDisplay++;
            }
            else
            {
                // Ocultar cámaras que no tocan en este display
                cam.rect = new Rect(0, 0, 0, 0);
            }
        }

        // 🔹 Agregar cámaras negras si hay menos de 4 en este display
        while (camsOnCurrentDisplay < maxPerDisplay)
        {
            GameObject filler = new GameObject($"FillerCam_{camsOnCurrentDisplay}");
            Camera fillerCam = filler.AddComponent<Camera>();
            fillerCam.clearFlags = CameraClearFlags.SolidColor;
            fillerCam.backgroundColor = Color.black;
            fillerCam.cullingMask = 0; // No renderiza nada
            fillerCam.targetDisplay = displayId;

            int camIndex = camsOnCurrentDisplay;
            float x = (camIndex % 2) * 0.5f;
            float y = (camIndex / 2) * 0.5f;
            fillerCam.rect = new Rect(x, 1f - y - 0.5f, 0.5f, 0.5f);

            fillerCams.Add(fillerCam);
            camsOnCurrentDisplay++;
        }

        // Activar el display actual si existe
        if (Display.displays.Length > displayId)
        {
            Display.displays[displayId].Activate();
        }
    }
}
