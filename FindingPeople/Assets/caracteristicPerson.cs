using UnityEngine;
using System.Collections.Generic;


// Class to store the caracteristics of a specific caracter asset
public class caracteristicPerson : MonoBehaviour
{
    [SerializeField] public List<string> Caracteristics = new List<string>();
    public bool isObj = false;
    public string  desc = "";
}
