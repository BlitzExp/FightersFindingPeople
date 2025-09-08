using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    private float xpos = 0f;
    private float ypos = 0f;
    private float zpos = 0f;
    public int numDrones = 3;

    public int personsCount = 0;
    public int objectivesCount = 0;

    [Header("Terrain cordinates")]
    [SerializeField] private TMP_InputField xInput; 
    [SerializeField] private TMP_InputField yInput;
    [SerializeField] private TMP_InputField zInput;

    [SerializeField] private GameObject startscreen;
    [SerializeField] private GameObject descriptionscreen;

    [SerializeField] private GameObject refPoint;

    [SerializeField] private TerrainGenerator genrateTerrain;

    [Header("Number of Drones")]
    [SerializeField] TMP_InputField numberDrones;


    [Header("Gizmo Settings")]
    [SerializeField] private float radius = 25f;
    [SerializeField] private float height = 10f;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private int segments = 64;
    [SerializeField] private bool drawWireSphere = false;

    public personclass[] personsToSpawn;
    [SerializeField] private DronManager _dronManager;

    //Obtains the value of the input for the X axis
    public void SetXPos() 
    {
        if (float.TryParse(xInput.text, out float result))
        {
            xpos = result;
        }
        else
        {
            xInput.text = xpos.ToString();
        }
    }

    //Obtains the value of the input for the Y axis (This is not being used)
    public void SetYPos()
    {
        if (float.TryParse(yInput.text, out float result))
        {
            ypos = result;
        }
        else
        {
            yInput.text = ypos.ToString();
        }
    }

    //Obtains the value of the input for the Z axis (This is being used insted of the Y one)
    public void SetZPos() 
    {
        if (float.TryParse(zInput.text, out float result))
        {
            zpos = result;
        }
        else
        {
            zInput.text = zpos.ToString();
        }

    }

    //Returns a Vector3 with the position values for the center of the search radious
    public Vector3 GetPosition()
    {
        return new Vector3(xpos, ypos, zpos);
    }

    //Obtains the number of drones to spawn
    public void SetNumDrones()
    {
        if (int.TryParse(numberDrones.text, out int result))
        {
            numDrones = result;
            if (result < 3)
            {
                numDrones = 3;
                numberDrones.text = numDrones.ToString();
            }
        }
        else
        {
            numberDrones.text = numDrones.ToString();
        }
    }


    //Once the "Start Game" button is pressed, this function will hide the start screen and start the terrain generation
    public void passDescriptions() 
    { 
        startscreen.SetActive(false);
        descriptionscreen.SetActive(true);
    }

    public void StartGame()
    {
        descriptionscreen.SetActive(false);
        startscreen.SetActive(false);
        Debug.Log("Position: " + GetPosition());
        Debug.Log("Number of Drones: " + numDrones);
        genrateTerrain.SetTerrainPosition(GetPosition());
        refPoint.transform.position = GetPosition();
        genrateTerrain.SetPersonsToSpawn(personsToSpawn);
        genrateTerrain.personsCount = personsCount;
        genrateTerrain.objectivesCount = objectivesCount;
        _dronManager.targetPos = GetPosition();
        genrateTerrain.StartGeneration();
        Time.timeScale = 1f; 
    }

    // Mark the search area in the scene view
    private void OnDrawGizmos()
    {
        if (refPoint == null) return;

        Gizmos.color = gizmoColor;
        Vector3 center = refPoint.transform.position;

        DrawCircle(center, radius, segments);

        Vector3 topCenter = center + Vector3.up * height;
        DrawCircle(topCenter, radius, segments);

        int verticalLines = Mathf.Clamp(8, 3, 32);
        for (int i = 0; i < verticalLines; i++)
        {
            float angle = i * (2f * Mathf.PI / verticalLines);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 a = center + dir * radius;
            Vector3 b = topCenter + dir * radius;
            Gizmos.DrawLine(a, b);
        }

        if (drawWireSphere)
        {
            Vector3 sphereCenter = center + Vector3.up * (height * 0.5f);
            Gizmos.DrawWireSphere(sphereCenter, Mathf.Max(radius, height * 0.5f));
        }
    }

    // Function to draw a circle in the XZ plane
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
