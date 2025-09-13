using UnityEngine;
using System.Collections.Generic;
using TMPro;

//Class for initializing the simulation and recovering the user inputs
public class GameManager : MonoBehaviour
{
    private float xpos = 0f;
    private float ypos = 0f;
    private float zpos = 0f;
    public int numDrones = 3;
    public string description = "";

    public int personsCount = 0;
    public int objectivesCount = 0;
    private int clper = -1;

    [Header("Terrain cordinates")]
    [SerializeField] private TMP_InputField xInput; 
    [SerializeField] private TMP_InputField yInput;
    [SerializeField] private TMP_InputField zInput;

    [SerializeField] private GameObject startscreen;
    [SerializeField] private GameObject descriptionscreen;
    [SerializeField] private GameObject popupdesc;
    [SerializeField] private TMP_Text closestperson;

    [SerializeField] private GameObject refPoint;

    [SerializeField] private TerrainGenerator genrateTerrain;

    [Header("Number of Drones")]
    [SerializeField] TMP_InputField numberDrones;

    [Header("description")]
    [SerializeField] TMP_InputField _Description;


    [Header("Gizmo Settings")]
    [SerializeField] private float radius = 25f;
    [SerializeField] private float height = 10f;
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.8f);
    [SerializeField] private int segments = 64;
    [SerializeField] private bool drawWireSphere = false;

    public personclass[] personsToSpawn;
    [SerializeField] private DronManager _dronManager;
    [SerializeField] private DronesManager _dronesManager;

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

    //Obtains the description for the search
    public void SetDesc()
    {
        description = _Description.text.ToLower();
        
    }

    //Function for starting the game after the star button is pressed
    public void StartGame()
    {
        // Deativate the start screen UI
        startscreen.SetActive(false);

        // Mesajes for showing the recollected information
        Debug.Log("Position: " + GetPosition());
        Debug.Log("Number of Drones: " + numDrones);

       
        genrateTerrain.SetTerrainPosition(GetPosition());

        // Pass information to the DronesManager which will handle the all the drones
        DronesManager.centerpoint = GetPosition();
        DronesManager.numDrones = numDrones;
        _dronesManager.setTargetdescription(description);

        refPoint.transform.position = GetPosition();

        // Pass information for the generation of the persons in the scene
        genrateTerrain.SetPersonsToSpawn(personsToSpawn);
        genrateTerrain.personsCount = personsCount;
        genrateTerrain.objectivesCount = objectivesCount;

        // Acrivates the procedural genration of the terrain
        genrateTerrain.StartGeneration();

        // Creates the grid for all the drones
        _dronesManager.createGrid();

        //Hay que llamar a esto para cada dron
        _dronesManager.spawnDrones();

        Time.timeScale = 1f; 
    }


    //Function for intrepreting the user description and checking with the existing persons description for the closest match
    public void searchForPersonDescription() 
    {
        int similar = 0;
        int closestobj = -1;
        bool containsallWord = false;

        for (int i = 0; i < personsToSpawn.Length; i++)
        {
            int numberofcorrect = 0;
            containsallWord = true;
            List<string> personcaracteristicslist = personsToSpawn[i].prefab.GetComponent<caracteristicPerson>().Caracteristics;
            bool containsWord = false;
            foreach (string word in personcaracteristicslist)
            {
                if (description.Contains(word.ToLower()))
                {
                    numberofcorrect++;
                    containsWord = true;
                }
                else
                {
                    containsWord = false;
                    containsallWord = false;
                }
            }

            if (containsallWord) 
            {
                personsToSpawn[i].isObjective = true;
                personsToSpawn[i].prefab.GetComponent<caracteristicPerson>().isObj = true;
                StartGame();
                break;
            }

            if (numberofcorrect > similar)
            {
                similar = numberofcorrect;
                closestobj = i;
            }
        }

        popupdesc.SetActive(true);
        if (closestobj != -1)
        {
            closestperson.text = "Closest match: " + personsToSpawn[closestobj].prefab.GetComponent<caracteristicPerson>().desc;
            clper = closestobj;
        }
        else
            Debug.Log("No person exist with that description");
    }


    // Function in case the user does not want the closest match 
    public void closepopup()
    {
        popupdesc.SetActive(false);
    }

    // Function for spawning the closest match
    public void spawnclosest()
    {
        if (clper < 0 || clper >= personsToSpawn.Length) return;
        personsToSpawn[clper].isObjective = true;
        description = personsToSpawn[clper].prefab.GetComponent<caracteristicPerson>().desc.ToLower();
        personsToSpawn[clper].prefab.GetComponent<caracteristicPerson>().isObj = true;
        StartGame();
    }

    // Makes the gizmos for the search area visible in the scene
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
