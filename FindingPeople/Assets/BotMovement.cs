using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BotMovement : MonoBehaviour
{
    [Header("Movement Config")]
    public Vector2 speedRange = new Vector2(2f, 6f);
    public float rotationSpeed = 5f;
    public float movementRadius = 20f;
    public Transform centerPoint;

    [Header("Randomness")]
    public Vector2 moveDurationRange = new Vector2(2f, 5f);
    public Vector2 stopDurationRange = new Vector2(1f, 3f);

    [Header("Terrain")]
    public Terrain terrain;

    // Anti-stuck
    [Header("Anti-Stuck")]
    public float stuckCheckInterval = 2f;   // cada cuánto revisar si se quedó trabado
    public float stuckDistanceThreshold = 0.5f; // si no se movió más que esto en el intervalo, se considera "stuck"

    private Animator botAnim;
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isMoving = false;
    private float currentSpeed;

    private Vector3 lastPosition;
    private float lastStuckCheckTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        botAnim = GetComponent<Animator>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Start()
    {
        lastPosition = rb.position;
        lastStuckCheckTime = Time.time;
        StartCoroutine(MovementRoutine());
    }

    public Vector3 getcurrentpos()
    {
        return rb.position;
    }

    public void setCenterPoint(Transform point)
    {
        centerPoint = point;
    }

    private void FixedUpdate()
    {
        if (terrain == null) return;

        if (isMoving)
        {
            Vector3 newPos = rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;

            // Mantener dentro del radio
            if (centerPoint != null && Vector3.Distance(centerPoint.position, newPos) > movementRadius)
            {
                moveDirection = (centerPoint.position - rb.position).normalized;
                newPos = rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;
            }

            // Ajustar altura al terreno
            float terrainHeight = terrain.SampleHeight(newPos);
            Vector3 terrainPos = new Vector3(newPos.x, terrainHeight, newPos.z);

            rb.MovePosition(terrainPos);

            // Rotación hacia la dirección de movimiento
            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                rb.rotation = Quaternion.Lerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        // === Anti-stuck check ===
        if (Time.time - lastStuckCheckTime > stuckCheckInterval)
        {
            float distanceMoved = Vector3.Distance(rb.position, lastPosition);

            if (isMoving && distanceMoved < stuckDistanceThreshold)
            {
                // Consideramos que está trabado → forzar nueva dirección
                moveDirection = GetRandomDirection();
                currentSpeed = Random.Range(speedRange.x, speedRange.y);
                if (botAnim != null)
                {
                    botAnim.SetBool("IsMoving", true);
                    botAnim.SetFloat("Speed", currentSpeed);
                }
            }

            lastPosition = rb.position;
            lastStuckCheckTime = Time.time;
        }
    }

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            isMoving = true;
            currentSpeed = Random.Range(speedRange.x, speedRange.y);
            moveDirection = GetRandomDirection();

            if (botAnim != null)
            {
                botAnim.SetBool("IsMoving", true);
                botAnim.SetFloat("Speed", currentSpeed);
            }

            float moveTime = Random.Range(moveDurationRange.x, moveDurationRange.y);
            yield return new WaitForSeconds(moveTime);

            isMoving = false;
            currentSpeed = 0f;

            if (botAnim != null)
            {
                botAnim.SetBool("IsMoving", false);
                botAnim.SetFloat("Speed", currentSpeed);
            }

            float stopTime = Random.Range(stopDurationRange.x, stopDurationRange.y);
            yield return new WaitForSeconds(stopTime);
        }
    }

    private Vector3 GetRandomDirection()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        return new Vector3(randomDir.x, 0, randomDir.y);
    }

    public void stopMovement()
    {
        isMoving = false;
        if (botAnim != null)
        {
            botAnim.SetBool("IsMoving", false);
            botAnim.SetFloat("Speed", currentSpeed);
        }
        StopAllCoroutines();
        this.enabled = false;
    }
}
