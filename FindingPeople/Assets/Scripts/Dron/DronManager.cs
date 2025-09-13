using UnityEngine;
using System.Collections.Generic;


// Agent in acharge of controlling all the subagents and deciding which one goes to each point
public class DronManager : MonoBehaviour
{
    public Vector3 targetPos = Vector3.zero; 
    private Vector3 lastTargetPos; 

    public DronHeight dronElevator; 
    public DronMovement dronMovement;

    // References to other components, objects and subagents
    [SerializeField] DronesManager DronesManager;
    [SerializeField] DroneLandingModule DronLanding;
    [SerializeField] DronLanding dronLandingStoper;
    [SerializeField] CharacterController characterController;
    [SerializeField] Rigidbody rb;
    [SerializeField] BoxCollider box;
    [SerializeField] SphereCollider sphere;
    [SerializeField] Transform landingpos;
    [SerializeField] GameObject Camera;

    [SerializeField] Animator wingsmov;

    public int droneid;

    private bool movementStarted = false;

    private bool isObjective = false;

    private bool isFinish = false;

    // Function which informs that the person of interest has been found
    public void setTaregt() 
    {
        isObjective = true;
    }

    // Function called at the start of the simulation
    public void HelpStart()
    {
        lastTargetPos = targetPos; 
        dronMovement.enabled = false;   
    }

    // Function which decides when to start the horizontal movement
    void Update()
    {

        // Starts elevation agent
        if (targetPos != lastTargetPos && !isFinish)
        {
            dronElevator.StartElevation();
            dronMovement.enabled = false;
            movementStarted = false;
            lastTargetPos = targetPos;
        }

        // Starts movement agent
        if (dronElevator.reachedHeight && !movementStarted && !isFinish)
        {
            dronMovement.enabled = true; // Activa el movimiento horizontal
            movementStarted = true;
        }
    }

    // Function to inform that the drone has reached its target position or the person of interest
    public void OnReachedTarget()
    {
        setTargetPos(DronesManager.getnewPositioninGrid(droneid, transform.position));
    }

    // Function for updating the target position
    public void setTargetPos(Vector3 pos) 
    {
        lastTargetPos = targetPos; 
        targetPos = pos;
    }

    // Function which acts like the detection agent which is a trigger collider of the field of view of the drone
    // This functions also checks if the person found is the person of interest or not
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
            Debug.Log("Person of interest found:  " + collision.gameObject.name);
        }
    }


    // Starts the landing agent
    public void startLanding() 
    {
        DronLanding.enabled = true;
        dronMovement.enabled = false;
        characterController.enabled = true;
        DronLanding.BeginLanding(DronesManager.objective);
        dronLandingStoper.enabled = true;

        // Moves the camara to give a betetr prespective of the scene
        Camera.transform.position = landingpos.position;
        Camera.GetComponent<Camera>().fieldOfView = 60f;
    }

    // Function which stops all the agents and components of the drone when it has landed
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
        rb.isKinematic = false;
        rb.angularDamping = 30f;
        wingsmov.SetBool("Active", false);
        wingsmov.enabled = false;
    }
}
