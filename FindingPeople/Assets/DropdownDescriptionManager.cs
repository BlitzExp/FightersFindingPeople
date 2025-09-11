using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DropdownDescriptionManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown dropdownPrefab;
    [SerializeField] private TMP_InputField inputFieldPrefab;
    [SerializeField] private Transform container;

    [Header("Descripciones")]
    [TextArea]
    [SerializeField] private List<string> descripciones = new List<string>();

    [SerializeField] private GameManager _gameManager;

    [SerializeField] private List<string> SearchWords = new List<string>();

    private int lastNumDrones = -1;
    private List<(TMP_Dropdown dropdown, TMP_InputField inputField)> uiPairs
        = new List<(TMP_Dropdown, TMP_InputField)>();

    private void Update()
    {
        if (_gameManager != null && _gameManager.numDrones != lastNumDrones)
        {
            SyncUIWithDrones();
        }
    }

    private void SyncUIWithDrones()
    {
        int numDrones = _gameManager.numDrones;
        lastNumDrones = numDrones;

        // Ajustar lista de descripciones al número de drones
        while (descripciones.Count < numDrones)
            descripciones.Add("");

        if (descripciones.Count > numDrones)
            descripciones.RemoveRange(numDrones, descripciones.Count - numDrones);

        // Borrar UI previa
        foreach (var pair in uiPairs)
        {
            Destroy(pair.dropdown.gameObject);
            Destroy(pair.inputField.gameObject);
        }
        uiPairs.Clear();

        // Crear dropdown + inputfield para cada dron
        for (int i = 0; i < numDrones; i++)
        {
            int index = i;

            // Crear dropdown
            TMP_Dropdown newDropdown = Instantiate(dropdownPrefab, container);

            // Limpiar y agregar opciones "Description n"
            newDropdown.ClearOptions();
            List<string> optionNames = new List<string>();
            for (int j = 0; j < numDrones; j++)
                optionNames.Add($"Description {j + 1}");
            newDropdown.AddOptions(optionNames);

            // Crear inputfield
            TMP_InputField newField = Instantiate(inputFieldPrefab, container);

            // Forzar que arranque siempre en "Description 1"
            newDropdown.value = 0;
            newField.text = descripciones[0];

            // Guardar siempre la última opción seleccionada
            int lastSelected = 0;

            // Evento: cuando cambie dropdown → guardar texto previo y actualizar inputfield
            newDropdown.onValueChanged.AddListener((optionIndex) =>
            {
                // Guardar texto previo
                descripciones[lastSelected] = newField.text;

                // Actualizar campo con el valor guardado de la nueva opción
                newField.text = descripciones[optionIndex];

                // Actualizar índice
                lastSelected = optionIndex;
            });

            // Evento: cuando cambie inputfield → actualizar lista
            newField.onValueChanged.AddListener((val) =>
            {
                descripciones[newDropdown.value] = val;
            });

            uiPairs.Add((newDropdown, newField));
        }
    }

    public void ValidateDescriptions()
    {
        bool containsWord = false;
        bool containsAllWords = false;
        for (int i = 0; i < descripciones.Count; i++)
        {
            containsWord = false;
            string desc = descripciones[i];
            foreach (string word in SearchWords)
            {
                if (desc.Contains(word))
                {
                    containsWord = true;
                }
            }

            if (!containsWord) 
            {
                Debug.Log($"No matching person in description {i + 1}");
                containsAllWords = true;

            }
        }

        if (containsAllWords == true)
        {
            Debug.Log("Create a matching description for each person");
            return;
        }
        else 
        {
            _gameManager.StartGame();
        }
    }
}
