using UnityEngine;

[RequireComponent(typeof(DronePatrolRadius))]
[RequireComponent(typeof(DroneLandingModule))]
[RequireComponent(typeof(TargetDetector))]
public class DroneController : MonoBehaviour
{
    [Header("Landing")]
    [Tooltip("How far from the target the drone should land (meters).")]
    public float landingRadius = 1.0f;

    [Tooltip("If landing is aborted, return to patrol automatically.")]
    public bool autoResumePatrolIfAborted = true;

    private DronePatrolRadius patrol;
    private DroneLandingModule landing;
    private TargetDetector detector;

    private enum DroneState { Patrolling, Landing, Landed }
    private DroneState state = DroneState.Patrolling;

    // Keep references to handlers to unsubscribe safely (avoid inline lambda bug).
    private System.Action onLandingStartedHandler;
    private System.Action onLandedHandler;
    private System.Action<string> onAbortedHandler;

    private void Awake()
    {
        patrol = GetComponent<DronePatrolRadius>();
        landing = GetComponent<DroneLandingModule>();
        detector = GetComponent<TargetDetector>();

        onLandingStartedHandler = OnLandingStarted;
        onLandedHandler = OnLanded;
        onAbortedHandler = OnLandingAborted;
    }

    private void OnEnable()
    {
        detector.OnTargetDetected += HandleTargetDetected;
        landing.OnLandingStarted += onLandingStartedHandler;
        landing.OnLanded += onLandedHandler;
        landing.OnAborted += onAbortedHandler;
    }

    private void OnDisable()
    {
        detector.OnTargetDetected -= HandleTargetDetected;
        landing.OnLandingStarted -= onLandingStartedHandler;
        landing.OnLanded -= onLandedHandler;
        landing.OnAborted -= onAbortedHandler;
    }

    private void Start()
    {
        SetPatrol(true);
        SetDetection(true);
        state = DroneState.Patrolling;
    }

    // ---- Public helpers ------------------------------------------------------

    /// <summary>Resume patrolling (e.g., after a landed pause).</summary>
    public void ResumePatrol()
    {
        if (state == DroneState.Landing) return; // ignore during landing
        state = DroneState.Patrolling;
        SetPatrol(true);
        SetDetection(true);
    }

    /// <summary>Abort landing flow and (optionally) return to patrol.</summary>
    public void ForceAbort(string reason = "Aborted by external request.")
    {
        landing.CancelLanding(reason);
        if (autoResumePatrolIfAborted)
        {
            ResumePatrol();
        }
        else
        {
            // Stay idle; keep detector on so we can catch a new target.
            state = DroneState.Patrolling;
            SetPatrol(false);
            SetDetection(true);
        }
    }

    // ---- Event handlers ------------------------------------------------------

    private void HandleTargetDetected(Transform target)
    {
        // Only react when patrolling.
        if (state != DroneState.Patrolling || target == null) return;

        // Pause patrol and detection to avoid re-entry, then start landing.
        SetPatrol(false);
        SetDetection(false);

        landing.StartLanding(target, landingRadius);
        // State will flip to Landing in OnLandingStarted (once landing has a valid spot).
    }

    private void OnLandingStarted()
    {
        state = DroneState.Landing;
    }

    private void OnLanded()
    {
        state = DroneState.Landed;
        SetPatrol(false);
        // Keep detection OFF while landed so we don't immediately try to land again.
        SetDetection(false);
    }

    private void OnLandingAborted(string reason)
    {
        if (autoResumePatrolIfAborted)
        {
            state = DroneState.Patrolling;
            SetPatrol(true);
            SetDetection(true);
        }
        else
        {
            // Remain idle; allow detection so a new target can be picked up.
            state = DroneState.Patrolling;
            SetPatrol(false);
            SetDetection(true);
        }
    }

    // ---- Internal toggles ----------------------------------------------------

    private void SetPatrol(bool on)
    {
        if (patrol != null) patrol.Enabled = on;
    }

    private void SetDetection(bool on)
    {
        if (detector != null) detector.enabled = on; // Toggle component to stop scans cleanly
    }
}
