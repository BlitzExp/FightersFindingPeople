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

    //Animation and Rigidbody
    private Animator botAnim;
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool isMoving = false;
    private float currentSpeed;

    // Initialize components and constraints
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        botAnim = GetComponent<Animator>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }


    //After initializing the character, start the movement routine
    private void Start()
    {
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


    // Makes the movement and rotation of the caracter posible in the terrain environment throught the use of Rigidbody. Also makes sure the character is on the desired radious
    private void FixedUpdate()
    {
        if (terrain == null) return; // no terrain -> don't move vertically

        if (isMoving)
        {
            Vector3 newPos = rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;

            if (centerPoint != null && Vector3.Distance(centerPoint.position, newPos) > movementRadius)
            {
                moveDirection = (centerPoint.position - rb.position).normalized;
                newPos = rb.position + moveDirection * currentSpeed * Time.fixedDeltaTime;
            }

            float terrainHeight = terrain.SampleHeight(newPos);
            Vector3 terrainPos = new Vector3(newPos.x, terrainHeight, newPos.z);

            rb.MovePosition(terrainPos);

            if (moveDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                rb.rotation = Quaternion.Lerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    // The caracter routine, it moves and stops in random intervals and directions
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

    // Generates a random direction on the XZ plane
    private Vector3 GetRandomDirection()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        return new Vector3(randomDir.x, 0, randomDir.y);
    }

    public void stopMovement()
    {
        isMoving = false;
        botAnim.SetBool("IsMoving", false);
        botAnim.SetFloat("Speed", currentSpeed);
        StopAllCoroutines();
        this.enabled = false;
    }
}
