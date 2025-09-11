using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class DroneLandingModule : MonoBehaviour
{
    // === Landing / Motion ===
    public float approachSpeed = 5f;
    public float descendSpeed = 2f;
    public float arriveThreshold = 0.2f;
    public float stopHeightAboveGround = 0.08f;
    public float maxLandSlope = 20f;
    public float hoverAltitudeAboveGround = 2f;
    public bool maintainHoverOnApproach = true;

    // === Dynamic follow & lock ===
    public bool followMovingTarget = true;     // follow the ring while target moves
    public float retargetInterval = 0.15f;      // how often to update spot from target
    public float minSpotShiftToApply = 0.25f;   // ignore tiny spot jitters
    public float lockWhenBelowHeight = 0.35f;   // lock the final spot when this close to ground hover height

    // === Layers ===
    public LayerMask groundMask = 0;     // set to Ground layer(s)
    public LayerMask obstacleMask = ~0;  // optional: layers to treat as obstacles (NOT ground). If 0, mask ignored

    // === Trigger Sphere (avoidance bubble) ===
    public float obstacleDetectionRadius = 0.6f;     // world meters (visualized & synced)
    public bool considerTriggerCollidersAsObstacles = false; // if obstacles are triggers, enable
    public float replanCooldown = 0.25f;
    public int replanMaxTries = 16;

    // === Controller tuning ===
    public bool zeroStepOffsetOnDescend = true;
    public bool relaxSlopeLimitOnDescend = true;
    public float extraGravity = 0f;

    public enum LandingState { Idle, ComputingSpot, Approaching, Descending, Landed, Aborted }
    public LandingState State { get; private set; } = LandingState.Idle;

    public event Action OnLandingStarted;
    public event Action OnLanded;
    public event Action<string> OnAborted;

    // === Debug ===
    [Header("Debug")]
    public bool debug = true;
    public bool debugVerbose = false;
    public float debugInterval = 0.3f;
    float lastDbg;
    string P => $"[DroneLanding:{name}] ";

    CharacterController cc;
    SphereCollider sphere;
    Rigidbody rb;

    Transform target;
    Vector3 landingSpot;           // ground point
    System.Random rng = new System.Random();

    float origStepOffset, origSlopeLimit;
    float lastY, lastProgressT, lastReplanT;
    Vector3 lastScale;

    // dynamic following
    float spotAngleRad;            // target→spot bearing we try to preserve
    float lastRetargetT;
    bool spotLocked;

    readonly RaycastHit[] rayBuf = new RaycastHit[16];
    readonly Collider[] overlapBuf = new Collider[32];



    public Transform landingTarget;
    public float landingRadius = 3f;

    public void BeginLanding(Transform landing)
    {
        
        landingTarget = landing;
        if (landingTarget == null)
        {
            Debug.LogWarning($"{P} No se asignó landingTarget.");
            return;
        }
        StartLanding(landingTarget, landingRadius);
    }



    // ================= Lifecycle =================
    void Awake()
    {
        cc = GetComponent<CharacterController>();
        sphere = GetComponent<SphereCollider>();
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        origStepOffset = cc.stepOffset;
        origSlopeLimit = cc.slopeLimit;

        ConfigureTriggerSphere();
        lastScale = transform.lossyScale;
    }

    void OnEnable()
    {
        if (debug)
        {
            Debug.Log($"{P}ENABLED\n" +
                      $"  groundMask={MaskToString(groundMask)}\n" +
                      $"  obstacleMask={MaskToString(obstacleMask)}\n" +
                      $"  worldRadius={obstacleDetectionRadius:F2}m");
            if (groundMask.value == ~0) Debug.LogWarning($"{P}groundMask=Everything (fix to Ground only).");
            if (groundMask.value == 0) Debug.LogWarning($"{P}groundMask=None (set to Ground).");
        }
    }

    void Reset()
    {
        if (!GetComponent<CharacterController>()) gameObject.AddComponent<CharacterController>();
        if (!GetComponent<SphereCollider>()) gameObject.AddComponent<SphereCollider>();
        if (!GetComponent<Rigidbody>()) gameObject.AddComponent<Rigidbody>();
        Awake();
    }

    void OnValidate()
    {
        if (!sphere) sphere = GetComponent<SphereCollider>();
        ConfigureTriggerSphere();
    }

    void ConfigureTriggerSphere()
    {
        if (!sphere) return;
        sphere.isTrigger = true;
        sphere.center = cc ? cc.center : Vector3.zero;
        float maxAxis = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z));
        if (Mathf.Approximately(maxAxis, 0f)) maxAxis = 1f;
        sphere.radius = Mathf.Max(0.01f, obstacleDetectionRadius / maxAxis); // world→local
    }

    // ================= Public API =================
    public void StartLanding(Transform t, float radiusMeters)
    {
        if (!t) { Abort("No target."); return; }
        if (State == LandingState.Approaching || State == LandingState.Descending) return;

        target = t;
        landingRadius = Mathf.Max(0.1f, radiusMeters);
        spotLocked = false;
        State = LandingState.ComputingSpot;
        OnLandingStarted?.Invoke();

        if (!TryPickLandingSpot(out landingSpot))
        {
            Abort("No valid landing spot.");
            return;
        }

        // seed bearing angle
        Vector3 tpos = target.position;
        spotAngleRad = Mathf.Atan2(landingSpot.z - tpos.z, landingSpot.x - tpos.x);
        lastRetargetT = Time.time;

        lastY = transform.position.y;
        lastProgressT = Time.time;
        State = LandingState.Approaching;
        if (debug) Debug.Log($"{P}Start → spot {landingSpot:F3}  ringR={landingRadius:F2}");
    }

    public void CancelLanding(string reason = "Canceled") => Abort(reason);

    // ================= Update =================
    void Update()
    {
        if (transform.lossyScale != lastScale)
        {
            ConfigureTriggerSphere();
            lastScale = transform.lossyScale;
        }

        // Follow target until locked
        if ((State == LandingState.Approaching || State == LandingState.Descending) && followMovingTarget && !spotLocked)
            MaybeRetargetToMovingTarget();

        if (State == LandingState.Approaching) TickApproach();
        else if (State == LandingState.Descending) TickDescend();
    }

    // ================= Trigger-based avoidance =================
    void OnTriggerEnter(Collider other) => TriggerEvent(other, "Enter");
    void OnTriggerStay(Collider other) => TriggerEvent(other, "Stay");

    void TriggerEvent(Collider other, string phase)
    {
        if (State != LandingState.Approaching && State != LandingState.Descending) return;

        bool isObs = IsObstacle(other, out string why);
        if (debugVerbose) Debug.Log($"{P}Trigger{phase}: '{other.name}' [{LayerMask.LayerToName(other.gameObject.layer)}/{other.gameObject.layer}, trig={other.isTrigger}] → {(isObs ? "OBSTACLE" : "ignored")} ({why})");
        if (!isObs) return;

        if (Time.time - lastReplanT < replanCooldown) return;
        lastReplanT = Time.time;

        if (TryPickLandingSpot(out landingSpot))
        {
            // keep the same relative bearing if possible
            Vector3 tpos = target.position;
            spotAngleRad = Mathf.Atan2(landingSpot.z - tpos.z, landingSpot.x - tpos.x);
            State = LandingState.Approaching;
            Debug.Log($"{P}REPLAN ({phase}:{other.name}) → new spot {landingSpot:F3}");
        }
        else Abort($"Replan failed after {phase}:{other.name}");
    }

    bool IsObstacle(Collider c, out string why)
    {
        why = "";
        if (!c) { why = "null"; return false; }
        var tr = c.transform;

        // ignore self & target
        if (tr.root == transform.root || tr.IsChildOf(transform)) { why = "self"; return false; }
        if (target && (tr == target || tr.IsChildOf(target))) { why = "target"; return false; }

        int layer = c.gameObject.layer;

        // Ignore "ground" only if groundMask is meaningfully set
        bool groundMaskMeaningful = (groundMask.value != 0) && (groundMask.value != ~0);
        if (groundMaskMeaningful && ((groundMask.value & (1 << layer)) != 0)) { why = "ground"; return false; }

        if (!considerTriggerCollidersAsObstacles && c.isTrigger) { why = "trigger & disabled"; return false; }

        if (obstacleMask.value != 0 && (obstacleMask.value & (1 << layer)) == 0) { why = "outside obstacleMask"; return false; }

        why = "valid obstacle";
        return true;
    }

    // ================= Dynamic follow =================
    void MaybeRetargetToMovingTarget()
    {
        if (Time.time - lastRetargetT < retargetInterval) return;
        lastRetargetT = Time.time;

        Vector3 tpos = target.position;

        // keep same bearing; recompute desired XZ on ring
        Vector3 candXZ = new Vector3(
            tpos.x + Mathf.Cos(spotAngleRad) * landingRadius,
            tpos.y + 5f,
            tpos.z + Mathf.Sin(spotAngleRad) * landingRadius
        );

        if (Physics.Raycast(candXZ, Vector3.down, out RaycastHit hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(hit.normal, Vector3.up) <= maxLandSlope)
            {
                Vector3 hover = new Vector3(hit.point.x, hit.point.y + stopHeightAboveGround, hit.point.z);
                if (!SpotHasObstacle(hover))
                {
                    // apply only if movement is meaningful to avoid jitter
                    if ((new Vector2(landingSpot.x - hit.point.x, landingSpot.z - hit.point.z)).magnitude >= minSpotShiftToApply)
                    {
                        landingSpot = hit.point;
                        if (debugVerbose) Debug.Log($"{P}Retarget follow → {landingSpot:F3}");
                    }
                    return;
                }
            }
        }

        // If bearing is invalid (blocked), try a quick local search around current angle
        float bestAngle = spotAngleRad;
        bool found = false;
        const int localTries = 8;
        for (int i = 1; i <= localTries; i++)
        {
            float delta = (i / (float)localTries) * Mathf.PI * 0.5f; // up to 90°
            for (int s = -1; s <= 1; s += 2)
            {
                float a = spotAngleRad + s * delta;
                Vector3 probeFrom = new Vector3(tpos.x + Mathf.Cos(a) * landingRadius, tpos.y + 5f, tpos.z + Mathf.Sin(a) * landingRadius);
                if (Physics.Raycast(probeFrom, Vector3.down, out RaycastHit ph, 1000f, groundMask, QueryTriggerInteraction.Ignore))
                {
                    if (Vector3.Angle(ph.normal, Vector3.up) > maxLandSlope) continue;
                    Vector3 hover = new Vector3(ph.point.x, ph.point.y + stopHeightAboveGround, ph.point.z);
                    if (SpotHasObstacle(hover)) continue;

                    if ((new Vector2(landingSpot.x - ph.point.x, landingSpot.z - ph.point.z)).magnitude >= minSpotShiftToApply)
                    {
                        landingSpot = ph.point;
                        bestAngle = a;
                        found = true;
                        break;
                    }
                }
            }
            if (found) break;
        }
        if (found)
        {
            spotAngleRad = bestAngle;
            if (debugVerbose) Debug.Log($"{P}Retarget local-search → {landingSpot:F3}");
        }
    }

    // ================= Approach =================
    void TickApproach()
    {
        Vector3 pos = transform.position;
        Vector3 targetXZ = new Vector3(landingSpot.x, pos.y, landingSpot.z);

        if (maintainHoverOnApproach)
        {
            float groundY = SampleGroundY(targetXZ, out _, out _);
            float desiredY = groundY + hoverAltitudeAboveGround;
            cc.Move(new Vector3(0f, Mathf.Lerp(pos.y, desiredY, descendSpeed * Time.deltaTime) - pos.y, 0f));
            pos = transform.position;
        }

        Vector2 toXZ = new Vector2(landingSpot.x - pos.x, landingSpot.z - pos.z);

        // 🔹 NUEVA CONDICIÓN: si está a menos de 5 m de altura y dentro de 3 m en XZ, empieza a descender
        float heightAboveGround = pos.y - SampleGroundY(pos, out _, out _);
        if (heightAboveGround <= 5f && toXZ.magnitude <= 5f)
        {
            PrepControllerForDescent();
            State = LandingState.Descending;
            if (debug) Debug.Log($"{P}Within 5m height & 3m XZ → DESCEND");
            return;
        }

        if (toXZ.magnitude <= arriveThreshold)
        {
            PrepControllerForDescent();
            State = LandingState.Descending;
            if (debug) Debug.Log($"{P}Reached XZ → DESCEND");
            return;
        }

        Vector3 dir = new Vector3(toXZ.x, 0, toXZ.y).normalized;
        if (dir.sqrMagnitude > 1e-6f)
        {
            Quaternion look = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z), Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 6f * Time.deltaTime);
        }
        cc.Move(dir * (approachSpeed * Time.deltaTime));
    }

    // ================= Descent =================
    void TickDescend()
    {
        Vector3 pos = transform.position;

        float groundY = SampleGroundY(new Vector3(landingSpot.x, pos.y, landingSpot.z), out _, out _);
        float targetY = groundY + stopHeightAboveGround;

        // lock final spot near ground
        if (!spotLocked && Mathf.Abs(pos.y - targetY) <= lockWhenBelowHeight)
        {
            spotLocked = true;
            if (debug) Debug.Log($"{P}Spot LOCKED near ground @ {landingSpot:F3}");
        }

        // gentle horizontal correction
        Vector3 horiz = new Vector3(landingSpot.x - pos.x, 0f, landingSpot.z - pos.z);
        float hMag = horiz.magnitude;
        horiz = (hMag > arriveThreshold * 0.5f)
              ? horiz.normalized * Mathf.Min(approachSpeed * 0.5f, hMag / Mathf.Max(Time.deltaTime, 1e-4f))
              : Vector3.zero;

        float dy = targetY - pos.y;
        float vy = Mathf.Clamp(dy / Mathf.Max(Time.deltaTime, 1e-4f), -descendSpeed, descendSpeed);
        if (extraGravity > 0f) vy -= extraGravity * Time.deltaTime;

        cc.Move(new Vector3(horiz.x, vy, horiz.z) * Time.deltaTime);

        // perch watchdog
        float yNow = transform.position.y;
        if (Mathf.Abs(yNow - lastY) > 0.005f) { lastY = yNow; lastProgressT = Time.time; }
        else if (Time.time - lastProgressT > 0.75f)
        {
            cc.Move(new Vector3(0f, -Mathf.Max(0.05f, descendSpeed * 0.1f), 0f));
            lastProgressT = Time.time;
        }

        bool doneV = Mathf.Abs(transform.position.y - targetY) <= 0.01f;
        bool doneH = (new Vector2(landingSpot.x - transform.position.x, landingSpot.z - transform.position.z)).sqrMagnitude <= 0.0004f;
        if (doneV && doneH)
        {
            RestoreController();
            State = LandingState.Landed;
            Debug.Log($"{P}LANDED @ {transform.position:F3}");
            OnLanded?.Invoke();
        }
    }

    // ================= Replan / Pick spot =================
    bool TryPickLandingSpot(out Vector3 spot)
    {
        Vector3 tpos = target.position;

        for (int i = 0; i < replanMaxTries; i++)
        {
            float a = (float)(rng.NextDouble() * Mathf.PI * 2f);
            Vector3 castFrom = new Vector3(tpos.x + Mathf.Cos(a) * landingRadius, tpos.y + 5f, tpos.z + Mathf.Sin(a) * landingRadius);

            if (Physics.Raycast(castFrom, Vector3.down, out RaycastHit hit, 1000f, groundMask, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Angle(hit.normal, Vector3.up) > maxLandSlope) continue;

                Vector3 hover = new Vector3(hit.point.x, hit.point.y + stopHeightAboveGround, hit.point.z);
                if (SpotHasObstacle(hover)) continue;

                spot = hit.point;
                if (debugVerbose) Debug.Log($"{P}PickSpot → {spot:F3}");
                return true;
            }
        }

        // Fallback along +X
        Vector3 fb = new Vector3(tpos.x + landingRadius, tpos.y + 5f, tpos.z);
        if (Physics.Raycast(fb, Vector3.down, out RaycastHit fh, 1000f, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(fh.normal, Vector3.up) <= maxLandSlope)
            {
                Vector3 hover = new Vector3(fh.point.x, fh.point.y + stopHeightAboveGround, fh.point.z);
                if (!SpotHasObstacle(hover)) { spot = fh.point; return true; }
            }
        }

        spot = Vector3.zero;
        if (debug) Debug.LogWarning($"{P}PickSpot failed");
        return false;
    }

    bool SpotHasObstacle(Vector3 hoverPoint)
    {
        int n = Physics.OverlapSphereNonAlloc(hoverPoint, obstacleDetectionRadius, overlapBuf, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            if (IsObstacle(overlapBuf[i], out _)) return true;
        }
        return false;
    }

    void TryReplan(string why)
    {
        if (Time.time - lastReplanT < replanCooldown) return;
        lastReplanT = Time.time;

        if (TryPickLandingSpot(out landingSpot))
        {
            Vector3 tpos = target.position;
            spotAngleRad = Mathf.Atan2(landingSpot.z - tpos.z, landingSpot.x - tpos.x);
            State = LandingState.Approaching;
            Debug.Log($"{P}REPLAN ({why}) → new spot {landingSpot:F3}");
        }
        else Abort($"Replan failed: {why}");
    }

    // ================= Controller & Ground =================
    void PrepControllerForDescent()
    {
        origStepOffset = cc.stepOffset;
        origSlopeLimit = cc.slopeLimit;
        if (zeroStepOffsetOnDescend) cc.stepOffset = 0f;
        if (relaxSlopeLimitOnDescend) cc.slopeLimit = 90f;
        lastY = transform.position.y;
        lastProgressT = Time.time;
    }

    void RestoreController()
    {
        cc.stepOffset = origStepOffset;
        cc.slopeLimit = origSlopeLimit;
    }

    float SampleGroundY(Vector3 worldXZ, out string hitName, out int hitLayer)
    {
        hitName = "(none)"; hitLayer = -1;
        Vector3 from = new Vector3(worldXZ.x, worldXZ.y + 1000f, worldXZ.z);
        int count = Physics.RaycastNonAlloc(from, Vector3.down, rayBuf, 5000f, groundMask, QueryTriggerInteraction.Ignore);

        float best = float.PositiveInfinity;
        float y = transform.position.y - hoverAltitudeAboveGround;

        for (int i = 0; i < count; i++)
        {
            var h = rayBuf[i];
            if (!h.collider) continue;
            var tr = h.collider.transform;
            if (tr.root == transform.root || tr.IsChildOf(transform)) continue;
            if (target && (tr == target || tr.IsChildOf(target))) continue;
            if (h.distance < best) { best = h.distance; y = h.point.y; hitName = h.collider.name; hitLayer = h.collider.gameObject.layer; }
        }
        return y;
    }

    void Abort(string reason)
    {
        RestoreController();
        State = LandingState.Aborted;
        Debug.LogWarning($"{P}ABORT → {reason}");
        OnAborted?.Invoke(reason);
        target = null;
    }

    // ================= Debug helpers =================
    void ThrottledLog(string msg)
    {
        if (!debugVerbose) return;
        if (Time.time - lastDbg >= debugInterval) { lastDbg = Time.time; Debug.Log(msg); }
    }

    static string MaskToString(LayerMask mask)
    {
        if (mask.value == 0) return "(none/0)";
        if (mask.value == ~0) return "(Everything)";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
            if ((mask.value & (1 << i)) != 0)
                sb.Append((sb.Length > 0 ? ", " : "") + (string.IsNullOrEmpty(LayerMask.LayerToName(i)) ? $"#{i}" : LayerMask.LayerToName(i)));
        return sb.ToString();
    }

    // ================= Gizmos =================
#if UNITY_EDITOR
    public bool gizmos = true;
    void OnDrawGizmosSelected()
    {
        if (!gizmos) return;

        if (target)
        {
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(target.position, Vector3.up, landingRadius);
        }

        Handles.color = new Color(1f, 0.3f, 0.8f, 1f);
        Handles.DrawWireDisc(transform.position, Vector3.up, obstacleDetectionRadius);

        if (State != LandingState.Idle && landingSpot != Vector3.zero)
        {
            Vector3 hover = new Vector3(landingSpot.x, landingSpot.y + stopHeightAboveGround, landingSpot.z);
            Gizmos.color = Color.green; Gizmos.DrawSphere(hover, 0.06f);
            Handles.color = new Color(1f, 0.3f, 0.8f, 1f);
            Handles.DrawWireDisc(hover, Vector3.up, obstacleDetectionRadius);
        }
    }
#endif
}