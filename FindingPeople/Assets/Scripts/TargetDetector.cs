using UnityEngine;
using System;

public class TargetDetector : MonoBehaviour
{
    [Tooltip("Search radius for objects tagged 'Target'.")]
    public float detectionRadius = 8f;

    [Tooltip("How often (seconds) to scan for targets.")]
    public float scanInterval = 0.25f;

    [Tooltip("Layers to consider when searching (leave as default to include everything).")]
    public LayerMask layerMask = ~0;

    public event Action<Transform> OnTargetDetected;

    private float timer;

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = scanInterval;
            Scan();
        }
    }

    private void Scan()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, layerMask, QueryTriggerInteraction.Ignore);
        Transform closest = null;
        float closestSqr = float.MaxValue;

        foreach (var c in hits)
        {
            if (!c || !c.CompareTag("Target")) continue;
            float sqr = (c.transform.position - transform.position).sqrMagnitude;
            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = c.transform;
            }
        }

        if (closest != null)
        {
            OnTargetDetected?.Invoke(closest);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRadius);
        Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}
