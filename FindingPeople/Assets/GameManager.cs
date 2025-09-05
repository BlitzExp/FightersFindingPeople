using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    private float xpos = 0f;
    private float ypos = 0f;
    private float zpos = 0f;

    [SerializeField] private TMP_InputField xInput; // Cambiado a TMP_InputField
    [SerializeField] private TMP_InputField yInput;
    [SerializeField] private TMP_InputField zInput;

    [SerializeField] private GameObject startscreen;

    [SerializeField] private TerrainGenerator genrateTerrain;

    public void SetXPos() // Añadido parámetro
    {
        if (float.TryParse(xInput.text, out float result))
        {
            xpos = result; // Asegúrate de que 'result' sea un float
        }
        else
        {
            Debug.LogError("El valor de X no es un número válido.");
            xInput.text = xpos.ToString();
        }
    }
    public void SetYPos() // Añadido parámetro
    {
        if (float.TryParse(yInput.text, out float result))
        {
            ypos = result;
        }
        else
        {
            Debug.LogError("El valor de Y no es un número válido.");
            yInput.text = ypos.ToString();
        }
    }
    public void SetZPos() // Añadido parámetro
    {
        if (float.TryParse(zInput.text, out float result))
        {
            zpos = result;
        }
        else
        {
            Debug.LogError("El valor de Z no es un número válido.");
            zInput.text = zpos.ToString();
        }
    }
    public Vector3 GetPosition()
    {
        return new Vector3(xpos, ypos - 25.14f, zpos);
    }
    public void StartGame()
    {
        startscreen.SetActive(false);
        Debug.Log("Position: " + GetPosition());
        genrateTerrain.terrainPosition = GetPosition();
        genrateTerrain.StartGeneration();
        Time.timeScale = 1f; // Asegura que el tiempo esté en marcha
    }
}
