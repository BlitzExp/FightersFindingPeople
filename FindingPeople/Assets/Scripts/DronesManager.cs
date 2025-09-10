using System.Collections.Generic;
using UnityEngine;

public class DronesManager : MonoBehaviour
{
    public static Vector3 centerpoint = Vector3.zero;
    public static int numDrones = 3;
    public static string targetdescription = "No target selected";

    public static Vector3 targetpos = Vector3.zero;

    private List<Vector3> Grid;
    private List<List<Vector3>> DroneGrids; // Subconjunto de posiciones para cada dron
    private Dictionary<int, int> droneIndices; // Índice actual por dron

    // Configuración de la visión del dron
    private float radius = 20f;
    private float viewSizeX = 16f;
    private float viewSizeZ = 9.5f;

    public static bool isTaregt = false;

    public static Transform objective;

    public void setTargetdescription(string desc)
    {
        targetdescription = desc;
    }

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

    public void createGrid()
    {
        Grid = new List<Vector3>();
        DroneGrids = new List<List<Vector3>>();
        droneIndices = new Dictionary<int, int>();

        // Número de pasos en cada eje
        int stepsX = Mathf.CeilToInt((radius * 2f) / viewSizeX);
        int stepsZ = Mathf.CeilToInt((radius * 2f) / viewSizeZ);

        // Offset para centrar el grid
        float startX = centerpoint.x - (stepsX - 1) * viewSizeX / 2f;
        float startZ = centerpoint.z - (stepsZ - 1) * viewSizeZ / 2f;

        // Inicializar sublistas
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


        Debug.Log($"Grid centrada creada con {Grid.Count} posiciones repartidas en {numDrones} drones (Round-robin).");

        // Recorre todo DroneGrids e imprime lo de cada dron
        for (int i = 0; i < DroneGrids.Count; i++)
        {
            List<Vector3> assignedGrid = DroneGrids[i];
            Debug.Log($"Dron {i} tiene {assignedGrid.Count} posiciones asignadas.");
            foreach (var pos in assignedGrid)
            {
                Debug.Log($"Posición del dron {i}: {pos}");
            }
        }

    }


    public Vector3 getnewPositioninGrid(int droneId, Vector3 currentDronePos)
    {
        if (targetpos != Vector3.zero) 
        {
            return targetpos;
        }

        if (droneId < 0 || droneId >= numDrones)
        {
            Debug.LogError($"DroneId {droneId} inválido.");
            return centerpoint;
        }

        List<Vector3> assignedGrid = DroneGrids[droneId];

        if (assignedGrid == null || assignedGrid.Count == 0)
        {
            Debug.Log($"El dron {droneId} ya visitó todas sus posiciones.");
            return currentDronePos; // Se queda donde está
        }

        // Buscar la posición más cercana
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

        // Remover la posición para no repetirla
        assignedGrid.Remove(closest);

        return closest;
    }

    // Gizmos para visualizar la grid
    private void OnDrawGizmos()
    {
        if (DroneGrids == null) return;

        // Colores distintos para cada dron
        Color[] droneColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };

        for (int i = 0; i < DroneGrids.Count; i++)
        {
            List<Vector3> assignedGrid = DroneGrids[i];
            if (assignedGrid == null || assignedGrid.Count == 0) continue;

            // Seleccionar color (si hay más drones que colores, se repiten)
            Color c = droneColors[i % droneColors.Length];
            Gizmos.color = new Color(c.r, c.g, c.b, 0.3f); // semi-transparente para no tapar todo

            // Dibujar cada celda sólida con altura de 30m
            foreach (var pos in assignedGrid)
            {
                Vector3 cubeSize = new Vector3(viewSizeX, 30f, viewSizeZ);
                Gizmos.DrawCube(pos, cubeSize);
            }

            // Dibujar el área total asignada al dron con un wirecube
            Vector3 min = assignedGrid[0];
            Vector3 max = assignedGrid[0];

            foreach (var pos in assignedGrid)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            Vector3 center = (min + max) / 2f;
            Vector3 size = new Vector3(max.x - min.x + viewSizeX, 60f, max.z - min.z + viewSizeZ);

            Gizmos.color = c; // color fuerte para el borde
            Gizmos.DrawWireCube(center, size);
        }
    }

}
