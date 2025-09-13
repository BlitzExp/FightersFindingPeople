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

            TMP_Text title = newField.GetComponentInChildren<TMP_Text>();
            TMP_InputField input = newField.GetComponentInChildren<TMP_InputField>();

            title.text = $"Description {i + 1}";

            description.Add("");

            int index = i; 

            input.onValueChanged.AddListener(value =>
            {
                description[index] = value;
                handleSearchObj();
            });
        }
    }

    void handleSearchObj()
    {
        Debug.Log("Lista description actualizada:");
        for (int i = 0; i < description.Count; i++)
        {
            Debug.Log($"Description {i + 1}: {description[i]}");
        }

    }
}
