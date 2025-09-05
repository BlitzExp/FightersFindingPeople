using UnityEngine;

/// <summary>
/// Patrols randomly within a radius around a center point, keeping a constant
/// hover altitude above the terrain. Altitude only changes when the sampled
/// ground height changes (no random vertical wobble).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DronePatrolRadius : MonoBehaviour
{
    [Header("Patrol Area")]
    [Tooltip("If set, patrols around this transform; otherwise uses the starting position as center.")]
    public Transform centerTransform;
    [Tooltip("Meters from center to roam.")]
    public float patrolRadius = 10f;
    [Tooltip("Minimum fraction of radius to pick waypoints (0..1). 0 = anywhere within radius, 1 = on the ring.")]
    [Range(0f, 1f)] public float innerRadiusFraction = 0.35f;

    [Header("Motion")]
    public float moveSpeed = 3f;
    public float turnSpeedDegPerSec = 180f;
    [Tooltip("Meters to consider the waypoint reached.")]
    public float arriveThreshold = 0.25f;
    [Tooltip("Seconds before giving up and picking a new waypoint (in case of obstacles).")]
    public float waypointTimeout = 6f;

    [Header("Altitude Control")]
    [Tooltip("Desired altitude above ground.")]
    public float hoverAltitude = 2f;
    [Tooltip("Layers considered ground for raycasts.")]
    public LayerMask groundMask = ~0;
    [Tooltip("Vertical speed cap used while adjusting to terrain changes.")]
    public float altitudeAdjustSpeed = 6f;
    [Tooltip("Ignore tiny terrain height noise below this (meters).")]
    public float altitudeChangeThreshold = 0.01f;

    [Header("Debug")]
    public bool drawGizmos = true;

    public bool Enabled { get; set; } = true;

    CharacterController controller;
    Vector3 centerWorld;
    Vector3 currentWaypoint;
    float waypointTimer;
    bool hasWaypoint;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        centerWorld = centerTransform ? centerTransform.position : transform.position;
        PickNewWaypoint();
    }

    void Update()
    {
        if (!Enabled) return;

        // keep center in sync if using a live transform
        if (centerTransform) centerWorld = centerTransform.position;

        // timed re-path (safety)
        waypointTimer += Time.deltaTime;
        if (!hasWaypoint || waypointTimer >= waypointTimeout)
        {
            PickNewWaypoint();
        }

        // horizontal step toward waypoint (flat)
        Vector3 pos = transform.position;
        Vector3 targetXZ = new Vector3(currentWaypoint.x, pos.y, currentWaypoint.z);
        Vector3 to = targetXZ - pos;
        Vector3 horiz = new Vector3(to.x, 0f, to.z);
        float dist = horiz.magnitude;

        Vector3 horizStep = Vector3.zero;
        if (dist > arriveThreshold)
        {
            horizStep = (horiz / dist) * moveSpeed;
        }
        else
        {
            PickNewWaypoint();
        }

        // rotate toward motion
        if (horizStep.sqrMagnitude > 1e-5f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(horizStep.x, 0f, horizStep.z), Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeedDegPerSec * Time.deltaTime);
        }

        // altitude: follow ground, only if terrain Y changed
        float groundY = SampleGroundY(new Vector3(pos.x, pos.y, pos.z));
        float desiredY = groundY + hoverAltitude;
        float dy = desiredY - pos.y;
        float vy = 0f;

        if (Mathf.Abs(dy) > altitudeChangeThreshold)
        {
            // move toward the desired hover height at a capped vertical speed
            float maxStep = altitudeAdjustSpeed * Time.deltaTime;
            vy = Mathf.Clamp(dy, -maxStep, maxStep);
        }
        // else: keep current y (no change)

        // apply movement
        controller.Move((horizStep + new Vector3(0f, vy, 0f)) * Time.deltaTime);
    }

    void PickNewWaypoint()
    {
        // choose a random point inside an annulus [innerRadius, patrolRadius]
        float inner = Mathf.Clamp01(innerRadiusFraction) * patrolRadius;
        float r = Mathf.Lerp(inner, patrolRadius, Mathf.Sqrt(Random.value)); // sqrt bias for uniform area
        float a = Random.value * Mathf.PI * 2f;

        Vector3 flat = new Vector3(
            centerWorld.x + Mathf.Cos(a) * r,
            centerWorld.y + 1000f,  // cast from above
            centerWorld.z + Mathf.Sin(a) * r
        );

        // snap waypoint to ground XZ and set Y so we’ll hover over it
        if (Physics.Raycast(flat, Vector3.down, out RaycastHit hit, 5000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            currentWaypoint = new Vector3(hit.point.x, hit.point.y + hoverAltitude, hit.point.z);
        }
        else
        {
            // if no ground found, fallback to flat plane at y=0
            currentWaypoint = new Vector3(flat.x, hoverAltitude, flat.z);
        }

        hasWaypoint = true;
        waypointTimer = 0f;
    }

    float SampleGroundY(Vector3 worldXZ)
    {
        Vector3 from = new Vector3(worldXZ.x, worldXZ.y + 1000f, worldXZ.z);
        if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 5000f, groundMask, QueryTriggerInteraction.Ignore))
            return hit.point.y;
        return 0f; // default if nothing hit
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Vector3 c = centerTransform ? centerTransform.position : (Application.isPlaying ? centerWorld : transform.position);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.DrawSphere(c, patrolRadius);
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 1f);
        Gizmos.DrawWireSphere(c, patrolRadius);

        if (hasWaypoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentWaypoint, 0.15f);
        }
    }
#endif
}
