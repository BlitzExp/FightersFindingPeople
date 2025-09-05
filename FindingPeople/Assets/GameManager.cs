using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    private float xpos = 0f;
    private float ypos = 0f;
    private float zpos = 0f;

    public int personsCount = 0;
    public int objectivesCount = 0;

    [SerializeField] private TMP_InputField xInput; // Cambiado a TMP_InputField
    [SerializeField] private TMP_InputField yInput;
    [SerializeField] private TMP_InputField zInput;

    [SerializeField] private GameObject startscreen;

    [SerializeField] private GameObject refPoint;

    [SerializeField] private TerrainGenerator genrateTerrain;

    [Header("Gizmo Settings")]
    [SerializeField] private float radius = 25f;
    [SerializeField] private float height = 10f;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private int segments = 64;
    [SerializeField] private bool drawWireSphere = false;

    public personclass[] personsToSpawn;
    [SerializeField] private DronManager _dronManager;
    public void SetXPos() 
    {
        if (float.TryParse(xInput.text, out float result))
        {
            xpos = result; // Aseg�rate de que 'result' sea un float
        }
        else
        {
            Debug.LogError("El valor de X no es un n�mero v�lido.");
            xInput.text = xpos.ToString();
        }
    }
    public void SetYPos() // A�adido par�metro
    {
        if (float.TryParse(yInput.text, out float result))
        {
            ypos = result;
        }
        else
        {
            Debug.LogError("El valor de Y no es un n�mero v�lido.");
            yInput.text = ypos.ToString();
        }
    }
    public void SetZPos() // A�adido par�metro
    {
        if (float.TryParse(zInput.text, out float result))
        {
            zpos = result;
        }
        else
        {
            Debug.LogError("El valor de Z no es un n�mero v�lido.");
            zInput.text = zpos.ToString();
        }
    }
    public Vector3 GetPosition()
    {
        return new Vector3(xpos, ypos, zpos);
    }
    public void StartGame()
    {
        startscreen.SetActive(false);
        Debug.Log("Position: " + GetPosition());
        genrateTerrain.SetTerrainPosition(GetPosition());
        refPoint.transform.position = GetPosition();
        genrateTerrain.SetPersonsToSpawn(personsToSpawn);
        genrateTerrain.personsCount = personsCount;
        genrateTerrain.objectivesCount = objectivesCount;
        _dronManager.targetPos = GetPosition();
        genrateTerrain.StartGeneration();
        Time.timeScale = 1f; // Asegura que el tiempo est� en marcha
    }


    private void OnDrawGizmos()
    {
        if (refPoint == null) return;

        Gizmos.color = gizmoColor;
        Vector3 center = refPoint.transform.position;

        // Draw bottom circle
        DrawCircle(center, radius, segments);

        // Draw top circle
        Vector3 topCenter = center + Vector3.up * height;
        DrawCircle(topCenter, radius, segments);

        // Draw vertical lines
        int verticalLines = Mathf.Clamp(8, 3, 32);
        for (int i = 0; i < verticalLines; i++)
        {
            float angle = i * (2f * Mathf.PI / verticalLines);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 a = center + dir * radius;
            Vector3 b = topCenter + dir * radius;
            Gizmos.DrawLine(a, b);
        }

        // Optional sphere
        if (drawWireSphere)
        {
            Vector3 sphereCenter = center + Vector3.up * (height * 0.5f);
            Gizmos.DrawWireSphere(sphereCenter, Mathf.Max(radius, height * 0.5f));
        }
    }

    private void DrawCircle(Vector3 center, float r, int seg)
    {
        if (seg < 3) seg = 3;
        float step = 2f * Mathf.PI / seg;
        Vector3 prev = center + new Vector3(Mathf.Cos(0f) * r, 0f, Mathf.Sin(0f) * r);
        for (int i = 1; i <= seg; i++)
        {
            float a = i * step;
            Vector3 next = center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
