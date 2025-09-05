using UnityEngine;

public class DronMovement : MonoBehaviour
{
    public DronManager dronManager;
    public float moveSpeed = 0f;
    public float maxSpeed = 30f;
    public float acceleration = 10f;
    public float rotationSpeed = 2f;
    public float rotationThreshold = 1f;
    void Update()
    {
        Vector3 currentPos = transform.position;
        Vector3 target = new Vector3(dronManager.targetPos.x, currentPos.y, dronManager.targetPos.z);
        Vector3 direction = (target - currentPos).normalized;

        if (direction == Vector3.zero) return; // Evita errores si ya está en el objetivo

        // Rotación hacia la dirección
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // Verifica si está alineado antes de moverse
        float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);
        if (angleDifference < rotationThreshold)
        {
            moveSpeed = Mathf.Min(maxSpeed, moveSpeed + acceleration * Time.deltaTime);

            if (Vector3.Distance(currentPos, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(currentPos, target, moveSpeed * Time.deltaTime);
            }
            else
            {
                dronManager.OnReachedTarget();
            }
        }
    }
}
