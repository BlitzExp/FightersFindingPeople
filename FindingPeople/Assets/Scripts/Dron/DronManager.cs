using UnityEngine;
using System.Collections.Generic;

public class DronManager : MonoBehaviour
{
    public Vector3 targetPos = Vector3.zero; 
    private Vector3 lastTargetPos; 

    public DronHeight dronElevator; 
    public DronMovement dronMovement;

    [SerializeField] DronesManager DronesManager;
    [SerializeField] DroneLandingModule DronLanding;
    [SerializeField] DronLanding dronLandingStoper;
    [SerializeField] CharacterController characterController;
    [SerializeField] Rigidbody rb;
    [SerializeField] BoxCollider box;
    [SerializeField] SphereCollider sphere;

    public int droneid;

    private bool movementStarted = false;

    private bool isObjective = false;

    private bool isFinish = false;
    public void setTaregt() 
    {
        isObjective = true;
    }

    public void HelpStart()
    {
        lastTargetPos = targetPos; 
        dronMovement.enabled = false;   
    }

    void Update()
    {
        // Si targetPos cambió, inicia la elevación


        if (targetPos != lastTargetPos && !isFinish)
        {
            dronElevator.StartElevation();
            dronMovement.enabled = false;
            movementStarted = false;
            lastTargetPos = targetPos; // Actualizamos la referencia
        }

        // Cuando alcanza los 120m y aún no ha empezado a moverse
        if (dronElevator.reachedHeight && !movementStarted && !isFinish)
        {
            dronMovement.enabled = true; // Activa el movimiento horizontal
            movementStarted = true;
        }
    }

    // Llamado por DronMovement cuando llega al destino
    public void OnReachedTarget()
    {
        setTargetPos(DronesManager.getnewPositioninGrid(droneid, transform.position));
    }

    public void setTargetPos(Vector3 pos) 
    {
        lastTargetPos = targetPos; // Actualiza la referencia
        targetPos = pos;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Dron"))
        {
            Debug.Log("otro dron");
        }
        else if (collision.gameObject.CompareTag("Person"))
        {
            List<string> personCharacteristics = collision.gameObject
                .GetComponent<caracteristicPerson>().Caracteristics;

            bool isTarget = true;
            string description = DronesManager.targetdescription.ToLower();

            foreach (string word in personCharacteristics)
            {
                if (!description.Contains(word.ToLower()))
                {
                    isTarget = false;
                    break;
                }
            }

            if (!isTarget)
            {
                return;
            }

            targetPos = collision.transform.position;
            DronesManager.targetpos = targetPos;
            setTaregt();
            DronesManager.isTaregt = true;
            DronesManager.objective = collision.gameObject.transform;
            Debug.Log("Encontrada persona objetivo: " + collision.gameObject.name);
        }
    }

    public void startLanding() 
    {
        DronLanding.enabled = true;
        dronMovement.enabled = false;
        characterController.enabled = true;
        DronLanding.BeginLanding(DronesManager.objective);
        dronLandingStoper.enabled = true;
    }

    public void stopLanding()
    {
        DronLanding.enabled = false;
        characterController.enabled = false;
        dronElevator.enabled = false;
        sphere.enabled = false;
        box.enabled = false;
        isFinish = true;
        rb.useGravity = true;
        dronLandingStoper.enabled = false; 
        //Pon la grabedad a 9.81
        rb.isKinematic = false;
        rb.angularDamping = 30f;

    }
}
