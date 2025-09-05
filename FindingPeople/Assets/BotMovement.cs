using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class BotMovement : MonoBehaviour
{
    [Header("Configuración Movimiento")]
    public Vector2 speedRange = new Vector2(2f, 6f);
    public float rotationSpeed = 5f;
    public float movementRadius = 20f;
    public Transform centerPoint;

    [Header("Tiempos Random")]
    public Vector2 moveDurationRange = new Vector2(2f, 5f);
    public Vector2 stopDurationRange = new Vector2(1f, 3f);

    [Header("Terrain (assign or auto-find)")]
    public Terrain terrain; // assign the Terrain (the one generated) in inspector; if null, will try to find one

    private Animator botAnim;
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isMoving = false;
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        botAnim = GetComponent<Animator>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (terrain == null)
        {
            // try to find the nearest Terrain to this bot
            terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                Terrain[] all = FindObjectsOfType<Terrain>();
                if (all.Length > 0) terrain = all[0];
            }
        }
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
        if (terrain == null) return; // no terrain -> don't move vertically

        if (isMoving)
        {
            // obstacle avoidance or steering omitted here (keep your previous avoidance if needed)

            Vector3 newPos = rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;

            if (centerPoint != null && Vector3.Distance(centerPoint.position, newPos) > movementRadius)
            {
                moveDirection = (centerPoint.position - rb.position).normalized;
                newPos = rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;
            }

            // sample height from the SAME terrain
            float terrainHeight = terrain.SampleHeight(newPos);
            // ensure world y takes into account terrain.transform.position.y (SampleHeight already does)
            Vector3 terrainPos = new Vector3(newPos.x, terrainHeight, newPos.z);

            rb.MovePosition(terrainPos);

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
}
