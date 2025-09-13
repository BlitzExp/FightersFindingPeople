using System.Collections.Generic;
using UnityEngine;

//Class for managing thew different drones and their positions

public class DronesManager : MonoBehaviour
{
    public static Vector3 centerpoint = Vector3.zero;
    public static int numDrones = 3;
    public static string targetdescription = "No target selected";

    public static Vector3 targetpos = Vector3.zero;


    // Grid and drone position management
    private List<Vector3> Grid;
    private List<List<Vector3>> DroneGrids; 
    private Dictionary<int, int> droneIndices;

    // Configuration for the drone vision area and grid
    private float radius = 20f;
    private float viewSizeX = 16f;
    private float viewSizeZ = 9.5f;

    public static bool isTaregt = false;

    public static Transform objective;

    [SerializeField] private GameObject Dron;
    [SerializeField] private CamaraManager _CamaraManager;

    [SerializeField] private GameObject Camara;


    // Function for setting the target description
    public void setTargetdescription(string desc)
    {
        targetdescription = desc;
    }

    // Function for getting a new position to targer for a drone  
    public Vector3 setTargetPos(Vector3 currentDronePos, int droneId)
    {
        if (targetpos == Vector3.zero)
        {
            return getnewPositioninGrid(droneId, currentDronePos);
        }
        else
        {
            return targetpos;
        }
    }

    // Function for dividing the search area into diferent zones for each drone
    public void createGrid()
    {
        Grid = new List<Vector3>();
        DroneGrids = new List<List<Vector3>>();
        droneIndices = new Dictionary<int, int>();

        int stepsX = Mathf.CeilToInt((radius * 2f) / viewSizeX);
        int stepsZ = Mathf.CeilToInt((radius * 2f) / viewSizeZ);

        float startX = centerpoint.x - (stepsX - 1) * viewSizeX / 2f;
        float startZ = centerpoint.z - (stepsZ - 1) * viewSizeZ / 2f;

        for (int i = 0; i < numDrones; i++)
        {
            DroneGrids.Add(new List<Vector3>());
            droneIndices[i] = 0;
        }

        int cellIndex = 0;
        for (int i = 0; i < stepsX; i++)
        {
            for (int j = 0; j < stepsZ; j++)
            {
                float x = startX + i * viewSizeX;
                float z = startZ + j * viewSizeZ;
                Vector3 pos = new Vector3(x, centerpoint.y, z);
                Grid.Add(pos);

                int droneId = cellIndex % numDrones;
                DroneGrids[droneId].Add(pos);

                cellIndex++;
            }
        }


        Debug.Log($"Grid created with  {Grid.Count} positions");

    }

    // Function used to get a unvisited position in the grid for a specific drone
    public Vector3 getnewPositioninGrid(int droneId, Vector3 currentDronePos)
    {
        if (targetpos != Vector3.zero) 
        {
            return targetpos;
        }

        List<Vector3> assignedGrid = DroneGrids[droneId];


        // In case the drone has already visited all its positions
        if (assignedGrid == null || assignedGrid.Count == 0)
        {
            Debug.Log($"The drone {droneId} has visited all its positions.");
            return currentDronePos; 
        }

        // Search for the closest match
        Vector3 closest = assignedGrid[0];
        float minDist = Vector3.Distance(currentDronePos, closest);

        foreach (var pos in assignedGrid)
        {
            float dist = Vector3.Distance(currentDronePos, pos);
            if (dist < minDist)
            {
                closest = pos;
                minDist = dist;
            }
        }

        assignedGrid.Remove(closest);

        return closest;
    }

    // Function for spawning the drones in the scene and starting the agents
    public void spawnDrones()
    {
        for (int i = 0; i < numDrones; i++)
        {
            Vector3 spawnPos = new Vector3(i * 2f, 17.346f, 0f); // Separados 5m en X
            GameObject dronInstance = Instantiate(Dron, spawnPos, Quaternion.identity);
            dronInstance.name = $"Dron_{i}";
            dronInstance.GetComponent<DronManager>().droneid = i;
            dronInstance.GetComponent<DronManager>().HelpStart();
            dronInstance.GetComponent<DronManager>().OnReachedTarget();
            Camara.SetActive(false);
            Canvas.ForceUpdateCanvases();
            _CamaraManager.SetUpCameras();
        }
    }

    // Gizmos to visualize the search area of each drone, with different colors
    private void OnDrawGizmos()
    {
        if (DroneGrids == null) return;

        // Colors for each drone
        Color[] droneColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

        for (int i = 0; i < DroneGrids.Count; i++)
        {
            List<Vector3> assignedGrid = DroneGrids[i];
            if (assignedGrid == null || assignedGrid.Count == 0) continue;

            Color c = droneColors[i % droneColors.Length];
            Gizmos.color = new Color(c.r, c.g, c.b, 0.3f);

            foreach (var pos in assignedGrid)
            {
                Vector3 cubeSize = new Vector3(viewSizeX, 30f, viewSizeZ);
                Gizmos.DrawCube(pos, cubeSize);
            }

            Vector3 min = assignedGrid[0];
            Vector3 max = assignedGrid[0];

            foreach (var pos in assignedGrid)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            Vector3 center = (min + max) / 2f;
            Vector3 size = new Vector3(max.x - min.x + viewSizeX, 60f, max.z - min.z + viewSizeZ);

            Gizmos.color = c;
            Gizmos.DrawWireCube(center, size);
        }
    }


 

}
