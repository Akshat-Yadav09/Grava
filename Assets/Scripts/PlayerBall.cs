using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the player ball's physics, speed limits, and win state.
/// Attach this to PlayerObj.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class PlayerBall : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Maximum movement speed to prevent tunneling through walls.")]
    [SerializeField] [Range(5f, 50f)] private float maxSpeed = 18f;

    [Tooltip("Extra linear drag applied to the ball for smooth handling.")]
    [SerializeField] [Range(0f, 5f)] private float linearDrag = 0.2f;

    [Header("Visual & Feedback")]
    [Tooltip("Optional TrailRenderer to enable motion trails.")]
    [SerializeField] private TrailRenderer trailRenderer;

    [Header("Events")]
    public UnityEvent onBallWon;

    private Rigidbody2D rb;
    private bool isWon = false;

    public Rigidbody2D Rigidbody => rb;
    public bool IsWon => isWon;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetupRigidbody();
    }

    private void Reset()
    {
        SetupRigidbody();
    }

    private void SetupRigidbody()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f; // Zero out ambient gravity so only black hole affects it
            rb.linearDamping = linearDrag;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Prevents clipping through walls
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Butter smooth rendering
        }
    }

    private void FixedUpdate()
    {
        if (isWon) return;

        // Apply drag from inspector dynamically
        rb.linearDamping = linearDrag;

        // Clamp speed to terminal velocity so ball never flies out of control
        Vector2 vel = GetVelocity();
        float currentSpeed = vel.magnitude;
        if (currentSpeed > maxSpeed)
        {
            SetVelocity(vel.normalized * maxSpeed);
        }
    }

    /// <summary>
    /// Smoothly transitions the ball into the win condition.
    /// </summary>
    public void ReachGoal(Vector2 goalPosition)
    {
        if (isWon) return;
        isWon = true;

        SetVelocity(Vector2.zero);
        rb.isKinematic = true;
        onBallWon?.Invoke();

        StartCoroutine(AnimateWinAbsorption(goalPosition));
    }

    private System.Collections.IEnumerator AnimateWinAbsorption(Vector2 goalPosition)
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float duration = 0.6f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, goalPosition, t);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        transform.position = goalPosition;
        transform.localScale = Vector3.zero;
    }

    public Vector2 GetVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public void SetVelocity(Vector2 newVelocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = newVelocity;
#else
        rb.velocity = newVelocity;
#endif
    }
}
