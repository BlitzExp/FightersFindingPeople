using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BotMovement : MonoBehaviour
{
    [Header("Configuración Movimiento")]
    public Vector2 speedRange = new Vector2(2f, 6f); // Rango de velocidad aleatoria
    public float rotationSpeed = 5f;                 // Velocidad de giro
    public float movementRadius = 20f;               // Radio máximo permitido
    public Transform centerPoint;                    // Punto central definido por el usuario

    [Header("Tiempos Random")]
    public Vector2 moveDurationRange = new Vector2(2f, 5f);   // Tiempo moviéndose
    public Vector2 stopDurationRange = new Vector2(1f, 3f);   // Tiempo quieto

    private Animator botAnim;
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isMoving = false;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        botAnim = GetComponent<Animator>();
    }

    private void Start()
    {
        StartCoroutine(MovementRoutine());
    }


    public void setCenterPoint(Transform point)
    {
        centerPoint = point;
    }

    private void FixedUpdate()
    {
        if (isMoving)
        {
            Vector3 newPos = rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;
            if (Vector3.Distance(centerPoint.position, newPos) <= movementRadius)
            {
                rb.MovePosition(newPos);
            }
            else
            {
                moveDirection = (centerPoint.position - rb.position).normalized;
            }
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                rb.rotation = Quaternion.Lerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            // --- Fase de movimiento ---
            isMoving = true;
            currentSpeed = Random.Range(speedRange.x, speedRange.y);
            moveDirection = GetRandomDirection();

            botAnim.SetBool("IsMoving", true);
            botAnim.SetFloat("Speed", currentSpeed);

            float moveTime = Random.Range(moveDurationRange.x, moveDurationRange.y);
            yield return new WaitForSeconds(moveTime);

            // --- Fase de stop ---
            isMoving = false;
            currentSpeed = 0f;

            botAnim.SetBool("IsMoving", false);
            botAnim.SetFloat("Speed", currentSpeed);

            float stopTime = Random.Range(stopDurationRange.x, stopDurationRange.y);
            yield return new WaitForSeconds(stopTime);
        }
    }

    private Vector3 GetRandomDirection()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        return new Vector3(randomDir.x, 0, randomDir.y);
    }
}
