using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class InputSpawner : MonoBehaviour
{
    [Header("Prefab que incluye Título + InputField")]
    public GameObject inputWithTitlePrefab;

    [Header("Contenedor de los campos")]
    public Transform inputContainer;

    [Header("Cantidad de Inputs")]
    public int numberOfInputs = 5;

    // Lista sincronizada con lo que escribe el usuario
    private List<string> description = new List<string>();

    void Start()
    {
        SpawnInputs();
    }

    void SpawnInputs()
    {
        description.Clear();

        for (int i = 0; i < numberOfInputs; i++)
        {
            GameObject newField = Instantiate(inputWithTitlePrefab, inputContainer);

            // Obtener referencias internas
            TMP_Text title = newField.GetComponentInChildren<TMP_Text>();
            TMP_InputField input = newField.GetComponentInChildren<TMP_InputField>();

            // Asignar título en formato "Description n"
            title.text = $"Description {i + 1}";

            // Inicializar valor en lista
            description.Add("");

            int index = i; // evitar problemas de closure en la lambda

            // Actualizar lista cada vez que se escriba algo
            input.onValueChanged.AddListener(value =>
            {
                description[index] = value;
                handleSearchObj();
            });
        }
    }

    // Función que se ejecuta en cada cambio de texto
    void handleSearchObj()
    {
        Debug.Log("Lista description actualizada:");
        for (int i = 0; i < description.Count; i++)
        {
            Debug.Log($"Description {i + 1}: {description[i]}");
        }

        // Aquí puedes meter la lógica de búsqueda / filtrado
    }
}
