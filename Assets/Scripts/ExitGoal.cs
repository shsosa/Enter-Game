using UnityEngine;

namespace FeelinSlimy
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ExitGoal : MonoBehaviour
    {
        [Header("Exit Portal Settings")]
        [Tooltip("Color of the glowing exit portal.")]
        public Color portalColor = new Color(0.25f, 0.95f, 0.65f, 1f);
        [Tooltip("If enabled, the Large Enemy must be defeated before the exit activates.")]
        public bool requireEnemyDefeated = false;
        [Tooltip("Optional custom sprite for the exit goal.")]
        public Sprite customSprite;

        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _collider;
        private Vector3 _initialScale;

        private void Awake()
        {
            CacheComponents();
            if (Application.isPlaying)
            {
                _initialScale = transform.localScale;
            }
        }

        private void OnEnable()
        {
            CacheComponents();
            ApplyVisuals();
        }

        private void OnValidate()
        {
            CacheComponents();
            ApplyVisuals();
        }

        private void CacheComponents()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
            if (_collider != null) _collider.isTrigger = true;
        }

        private void ApplyVisuals()
        {
            if (_spriteRenderer == null) return;
            if (customSprite != null)
            {
                _spriteRenderer.sprite = customSprite;
            }
            else if (_spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = ProceduralVisuals.GetExitPortalSprite();
            }
            _spriteRenderer.color = portalColor;
            _spriteRenderer.sortingOrder = 3;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            float pulse = 1f + Mathf.Sin(Time.time * 3.5f) * 0.05f;
            transform.localScale = new Vector3(_initialScale.x * pulse, _initialScale.y * pulse, _initialScale.z);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!Application.isPlaying) return;
            SlimePlayerController slime = other.GetComponent<SlimePlayerController>();
            if (slime == null) return;

            if (GameManager.Instance != null)
            {
                if (requireEnemyDefeated && !GameManager.Instance.IsEnemyDefeated)
                {
                    GameManager.Instance.ShowTemporaryHint("Defeat the Gloom Brute with your Anger Stomp first!");
                    return;
                }
                GameManager.Instance.TriggerVictory();
            }
        }
    }
}
