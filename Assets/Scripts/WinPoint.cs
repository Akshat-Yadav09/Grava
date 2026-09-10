using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger zone for the Win Point.
/// Attach this to WinPoint.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class WinPoint : MonoBehaviour
{
    [Header("Win Settings")]
    [Tooltip("Optional tag filter to verify it is the player ball.")]
    [SerializeField] private string targetTag = "Untagged"; // Supports untagged or "Player"

    [Tooltip("Play a pulse animation when the player reaches the win point.")]
    [SerializeField] private bool pulseOnWin = true;

    [Tooltip("Color to transition to when won.")]
    [SerializeField] private Color winColor = new Color(0.2f, 1f, 0.4f, 1f);

    [Header("Events")]
    public UnityEvent onWinTriggered;

    private bool hasWon = false;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasWon) return;

        // Check if the entering object is the PlayerBall
        if (other.TryGetComponent<PlayerBall>(out var player))
        {
            TriggerWin(player);
        }
        else if (other.CompareTag(targetTag) && other.attachedRigidbody != null)
        {
            if (other.attachedRigidbody.TryGetComponent<PlayerBall>(out var pb))
            {
                TriggerWin(pb);
            }
        }
    }

    private void TriggerWin(PlayerBall player)
    {
        hasWon = true;
        Debug.Log("<color=#00FF88><b>[Grava] Level Completed!</b> Player reached WinPoint!</color>");

        player.ReachGoal(transform.position);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = winColor;
        }

        onWinTriggered?.Invoke();

        if (pulseOnWin)
        {
            StartCoroutine(PulseAnimation());
        }
    }

    private System.Collections.IEnumerator PulseAnimation()
    {
        Vector3 baseScale = transform.localScale;
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime * 3f;
            float scaleMultiplier = 1f + 0.3f * Mathf.Sin(timer * Mathf.PI);
            transform.localScale = baseScale * scaleMultiplier;
            yield return null;
        }
        transform.localScale = baseScale;
    }
}
