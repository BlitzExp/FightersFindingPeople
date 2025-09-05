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
    [SerializeField] private DronManager _dronManager;
    public void SetXPos() // A�adido par�metro
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
        return new Vector3(xpos, ypos - 25.14f, zpos);
    }
    public void StartGame()
    {
        startscreen.SetActive(false);
        Debug.Log("Position: " + GetPosition());
        genrateTerrain.terrainPosition = GetPosition();
        _dronManager.targetPos = GetPosition();
        genrateTerrain.StartGeneration();
        Time.timeScale = 1f; // Asegura que el tiempo est� en marcha
    }
}
