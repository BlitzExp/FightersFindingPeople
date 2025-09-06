using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ObjectWander : MonoBehaviour
{
    [Header("Patrol Area")]
    public Transform centerTransform;
    public float patrolRadius = 10f;
    [Range(0f, 1f)] public float innerRadiusFraction = 0.35f;

    [Header("Motion")]
    public float moveSpeed = 3f;
    public float turnSpeedDegPerSec = 180f;

    [Header("Altitude Control")]
    public float hoverAltitude = 2f;
    public LayerMask groundMask = ~0;
    public float altitudeAdjustSpeed = 6f;
    public float altitudeChangeThreshold = 0.01f;

    CharacterController controller;
    Vector3 centerWorld;
    Vector3 currentWaypoint;
    bool hasWaypoint;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        centerWorld = centerTransform ? centerTransform.position : transform.position;
        PickNewWaypoint();
    }

    void Update()
    {
        if (centerTransform) centerWorld = centerTransform.position;

        Vector3 pos = transform.position;
        Vector3 targetXZ = new Vector3(currentWaypoint.x, pos.y, currentWaypoint.z);
        Vector3 to = targetXZ - pos;
        Vector3 horiz = new Vector3(to.x, 0f, to.z);
        float dist = horiz.magnitude;

        Vector3 horizStep = Vector3.zero;
        if (dist > 0.5f)
        {
            horizStep = (horiz / dist) * moveSpeed;
        }
        else
        {
            PickNewWaypoint();
        }

        if (horizStep.sqrMagnitude > 1e-5f)
        {
            Quaternion look = Quaternion.LookRotation(horizStep, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeedDegPerSec * Time.deltaTime);
        }

        float groundY = SampleGroundY(pos);
        float desiredY = groundY + hoverAltitude;
        float dy = desiredY - pos.y;
        float vy = 0f;

        if (Mathf.Abs(dy) > altitudeChangeThreshold)
        {
            float maxStep = altitudeAdjustSpeed * Time.deltaTime;
            vy = Mathf.Clamp(dy, -maxStep, maxStep);
        }

        controller.Move((horizStep + new Vector3(0f, vy, 0f)) * Time.deltaTime);
    }

    void PickNewWaypoint()
    {
        float inner = Mathf.Clamp01(innerRadiusFraction) * patrolRadius;
        float r = Mathf.Lerp(inner, patrolRadius, Mathf.Sqrt(Random.value));
        float a = Random.value * Mathf.PI * 2f;

        Vector3 flat = new Vector3(
            centerWorld.x + Mathf.Cos(a) * r,
            centerWorld.y + 1000f,
            centerWorld.z + Mathf.Sin(a) * r
        );

        if (Physics.Raycast(flat, Vector3.down, out RaycastHit hit, 5000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            currentWaypoint = new Vector3(hit.point.x, hit.point.y + hoverAltitude, hit.point.z);
        }
        else
        {
            currentWaypoint = new Vector3(flat.x, hoverAltitude, flat.z);
        }

        hasWaypoint = true;
    }

    float SampleGroundY(Vector3 worldXZ)
    {
        Vector3 from = new Vector3(worldXZ.x, worldXZ.y + 1000f, worldXZ.z);
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 5000f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return 0f;
    }
}
