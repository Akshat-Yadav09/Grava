using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum ZoneBehavior
{
    [Tooltip("Black hole smoothly collapses and cuts gravity when the cursor is outside allowed zones.")]
    DeactivateOutside = 0,

    [Tooltip("Black hole clamps to the perimeter of the nearest allowed zone.")]
    ClampToZone = 1
}

/// <summary>
/// Controls the Black Hole:
/// 1. Follows the mouse position with smooth inertia (no jerky snapping).
/// 2. Respects Black Hole Zones (colliders) if any exist in the scene.
/// 3. Applies a refined, "feel-good" gravitational force to the Player Ball.
/// 4. Animates concentric visual rings for celestial aesthetics.
/// </summary>
public class BlackHoleController : MonoBehaviour
{
    [Header("Target Reference")]
    [Tooltip("The ball to attract. If left empty, will automatically find PlayerBall in scene.")]
    [SerializeField] private PlayerBall targetBall;

    [Header("Mouse Following & Inertia")]
    [Tooltip("Camera used to project mouse position to world space. Defaults to Camera.main.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Smoothing duration for mouse follow. Lower = snappier, Higher = heavier inertia.")]
    [SerializeField] [Range(0.01f, 0.4f)] private float followSmoothTime = 0.08f;

    [Tooltip("Maximum speed the black hole can move towards the cursor.")]
    [SerializeField] private float maxFollowSpeed = 60f;

    [Header("Zone Restriction")]
    [Tooltip("When enabled, black hole operation is restricted to BlackHoleZones in the scene. If no zones exist, it works everywhere.")]
    [SerializeField] private bool useZoneRestriction = true;

    [Tooltip("How the black hole behaves relative to zone boundaries.")]
    [SerializeField] private ZoneBehavior zoneBehavior = ZoneBehavior.DeactivateOutside;

    [Tooltip("If true, gravity will only pull the ball if the ball itself is also inside an active Black Hole Zone.")]
    [SerializeField] private bool requireBallInZone = false;

    [Tooltip("How fast the black hole smoothly scales/fades in and out when crossing zone boundaries.")]
    [SerializeField] [Range(2f, 30f)] private float zoneTransitionSpeed = 14f;

    [Header("Feel-Good Gravity Settings")]
    [Tooltip("Base gravitational pull power (G).")]
    [SerializeField] [Range(10f, 300f)] private float gravityStrength = 90f;

    [Tooltip("Maximum distance at which gravity begins to pull the ball.")]
    [SerializeField] [Range(2f, 30f)] private float influenceRadius = 10f;

    [Tooltip("Plummer softening factor (epsilon). Prevents infinite force explosion when ball is close.")]
    [SerializeField] [Range(0.2f, 4f)] private float softeningDistance = 1.2f;

    [Tooltip("Distance falloff power. 2.0 = Inverse-Square (Newtonian). 1.5 = Gentler, more forgiving.")]
    [SerializeField] [Range(1f, 2.5f)] private float falloffExponent = 1.6f;

    [Header("Core & Slingshot Tuning")]
    [Tooltip("Radius of the inner core/event horizon.")]
    [SerializeField] [Range(0.2f, 3f)] private float coreRadius = 1.2f;

    [Tooltip("Damping applied to relative velocity inside core. Prevents violent bouncing and lets you 'steer' the ball.")]
    [SerializeField] [Range(0f, 15f)] private float coreDamping = 3.5f;

    [Tooltip("Subtle tangential swirl force that helps the ball orbit rather than crash head-on.")]
    [SerializeField] [Range(-15f, 15f)] private float tangentialSwirl = 4f;

    [Tooltip("How much black hole movement momentum transfers to the ball (slingshot boost).")]
    [SerializeField] [Range(0f, 1.5f)] private float slingshotTransfer = 0.4f;

    [Header("Visual Ring Animation")]
    [Tooltip("Whether to rotate concentric child rings (accretion disk effect).")]
    [SerializeField] private bool rotateRings = true;
    [SerializeField] private float ringRotationSpeed = 40f;

    // Runtime state
    private Vector3 currentVelocity;
    private Vector3 lastPosition;
    private Vector3 blackHoleVelocity;
    private Vector3 initialLocalScale;
    private float currentActivationFactor = 1f;
    private bool isInsideZone = true;

    public bool IsInZone => isInsideZone;
    public float ActivationFactor => currentActivationFactor;

    private void Awake()
    {
        initialLocalScale = transform.localScale;
        if (initialLocalScale == Vector3.zero)
        {
            initialLocalScale = Vector3.one;
        }
    }

    private void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (targetBall == null)
        {
            targetBall = FindFirstObjectByType<PlayerBall>();
        }

        lastPosition = transform.position;

        // Check initial cursor position relative to zones
        if (useZoneRestriction && BlackHoleZone.HasAnyZones)
        {
            Vector2 mouseScreen = GetMouseScreenPosition();
            if (mainCamera != null)
            {
                Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));
                bool cursorInZone = BlackHoleZone.IsPointInsideAnyZone(mouseWorld, out _);
                if (zoneBehavior == ZoneBehavior.DeactivateOutside && !cursorInZone)
                {
                    currentActivationFactor = 0f;
                    transform.localScale = Vector3.zero;
                    isInsideZone = false;
                }
            }
        }
    }

    private void Update()
    {
        FollowMouseWithInertia();
        AnimateVisualRings();
    }

    private void FixedUpdate()
    {
        // Calculate black hole's actual world velocity
        blackHoleVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
        lastPosition = transform.position;

        ApplyGravitationalForce();
    }

    /// <summary>
    /// Smoothly glides the Black Hole toward the mouse cursor with zone-boundary handling and smooth inertia.
    /// </summary>
    private void FollowMouseWithInertia()
    {
        if (mainCamera == null) return;

        Vector2 mouseScreen = GetMouseScreenPosition();
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, -mainCamera.transform.position.z));
        mouseWorld.z = 0f; // Keep on 2D plane

        Vector3 targetWorld = mouseWorld;
        bool hasZones = useZoneRestriction && BlackHoleZone.HasAnyZones;

        if (hasZones)
        {
            bool cursorInZone = BlackHoleZone.IsPointInsideAnyZone(mouseWorld, out _);

            if (zoneBehavior == ZoneBehavior.ClampToZone)
            {
                if (!cursorInZone)
                {
                    targetWorld = BlackHoleZone.GetClosestPointInAnyZone(mouseWorld, out _);
                }
                isInsideZone = true;
            }
            else // DeactivateOutside
            {
                isInsideZone = cursorInZone;
                if (!cursorInZone)
                {
                    // Gently pull towards nearest zone boundary while fading out
                    targetWorld = BlackHoleZone.GetClosestPointInAnyZone(mouseWorld, out _);
                }
            }
        }
        else
        {
            isInsideZone = true;
        }

        // Animate smooth scale / fade factor
        float targetFactor = isInsideZone ? 1f : 0f;
        currentActivationFactor = Mathf.MoveTowards(
            currentActivationFactor,
            targetFactor,
            zoneTransitionSpeed * Time.deltaTime
        );

        // Scale transform smoothly (collapses to 0 when outside)
        transform.localScale = initialLocalScale * currentActivationFactor;

        // Follow target position if active, transitioning, or clamped
        if (isInsideZone || currentActivationFactor > 0.01f || zoneBehavior == ZoneBehavior.ClampToZone)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetWorld,
                ref currentVelocity,
                followSmoothTime,
                maxFollowSpeed,
                Time.deltaTime
            );
        }
    }

    /// <summary>
    /// Calculates and applies softened, damped gravity scaled by zone activation factor.
    /// </summary>
    private void ApplyGravitationalForce()
    {
        if (targetBall == null || targetBall.IsWon) return;
        if (currentActivationFactor <= 0.01f) return;

        // If requireBallInZone is enabled, ball must also be inside a zone
        if (useZoneRestriction && BlackHoleZone.HasAnyZones && requireBallInZone)
        {
            if (!BlackHoleZone.IsPointInsideAnyZone(targetBall.transform.position, out _))
            {
                return;
            }
        }

        Rigidbody2D ballRb = targetBall.Rigidbody;
        if (ballRb == null) return;

        Vector2 blackHolePos = transform.position;
        Vector2 ballPos = ballRb.position;
        Vector2 toBlackHole = blackHolePos - ballPos;
        float distance = toBlackHole.magnitude;

        // Beyond reach: No gravitational effect
        if (distance > influenceRadius || distance < 0.001f) return;

        Vector2 pullDirection = toBlackHole / distance;

        // 1. Smooth boundary falloff: 0 at influence edge, 1 at center
        float normalizedDist = Mathf.Clamp01(distance / influenceRadius);
        float boundaryFade = 1f - normalizedDist;
        boundaryFade = boundaryFade * boundaryFade * (3f - 2f * boundaryFade);

        // 2. Softened Inverse-Power Pull (Plummer potential style)
        float effectiveDist = distance + softeningDistance;
        float radialMagnitude = (gravityStrength / Mathf.Pow(effectiveDist, falloffExponent)) * boundaryFade;
        Vector2 totalForce = pullDirection * radialMagnitude;

        // 3. Tangential Swirl (Accretion disk effect - encourages clean orbits)
        if (Mathf.Abs(tangentialSwirl) > 0.01f)
        {
            Vector2 tangentDir = new Vector2(-pullDirection.y, pullDirection.x);
            totalForce += tangentDir * (tangentialSwirl * boundaryFade);
        }

        // 4. Core Horizon Damping (prevents chaotic ping-pong near the center)
        if (distance < coreRadius && coreDamping > 0.01f)
        {
            float coreFactor = 1f - (distance / coreRadius);
            Vector2 relativeVelocity = targetBall.GetVelocity() - (Vector2)blackHoleVelocity;
            Vector2 dampingForce = -relativeVelocity * (coreDamping * coreFactor);
            totalForce += dampingForce;
        }

        // 5. Slingshot Momentum Transfer (flicking the mouse imparts velocity)
        if (slingshotTransfer > 0.01f && distance < influenceRadius)
        {
            Vector2 slingshotForce = (Vector2)blackHoleVelocity * (slingshotTransfer * boundaryFade);
            totalForce += slingshotForce;
        }

        // Smoothly scale applied force by zone activation factor (prevents harsh impulses)
        totalForce *= currentActivationFactor;

        // Apply force to the ball's Rigidbody2D
        ballRb.AddForce(totalForce, ForceMode2D.Force);
    }

    /// <summary>
    /// Rotates child rings at alternating speeds to look like a living singularity.
    /// </summary>
    private void AnimateVisualRings()
    {
        if (!rotateRings || currentActivationFactor <= 0.001f) return;

        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            float direction = (i % 2 == 0) ? 1f : -1f;
            float speedModifier = 1f + (i * 0.35f);
            child.Rotate(0f, 0f, direction * ringRotationSpeed * speedModifier * Time.deltaTime);
        }
    }

    /// <summary>
    /// Safely gets mouse position across both New Input System and Legacy Input.
    /// </summary>
    private Vector2 GetMouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.mousePosition;
#else
        return Vector2.zero;
#endif
    }

    private void OnDrawGizmosSelected()
    {
        // Visual debug circles in Scene View
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, influenceRadius);

        Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, coreRadius);

        if (targetBall != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetBall.transform.position);
        }
    }
}
