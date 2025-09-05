using UnityEngine;

public class DronManager : MonoBehaviour
{
    public Vector3 targetPos = Vector3.zero;  // Posición objetivo inicial
    private Vector3 lastTargetPos;            // Para detectar cambios en targetPos

    public DronHeight dronElevator;       // Asigna en el Inspector
    public DronMovement dronMovement;         // Asigna en el Inspector

    private bool movementStarted = false;

    void Start()
    {
        lastTargetPos = targetPos;            // Inicializamos con la posición actual
        dronMovement.enabled = false;         // El movimiento comienza desactivado
    }

    void Update()
    {
        // Si targetPos cambió, inicia la elevación
        if (targetPos != lastTargetPos)
        {
            dronElevator.StartElevation();
            dronMovement.enabled = false;
            movementStarted = false;
            lastTargetPos = targetPos; // Actualizamos la referencia
        }

        // Cuando alcanza los 120m y aún no ha empezado a moverse
        if (dronElevator.reachedHeight && !movementStarted)
        {
            dronMovement.enabled = true; // Activa el movimiento horizontal
            movementStarted = true;
        }
    }

    // Llamado por DronMovement cuando llega al destino
    public void OnReachedTarget()
    {
        dronMovement.enabled = false; // Desactiva el movimiento
    }
}
