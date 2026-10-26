using System.Collections;
using UnityEngine;

namespace FeelinSlimy
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class LargeEnemy : MonoBehaviour
    {
        [Header("Enemy Identity & Visuals")]
        [Tooltip("Display name of this dangerous large enemy.")]
        public string enemyName = "Gloom Brute";
        [Tooltip("Primary color tint of the large enemy.")]
        public Color enemyColor = new Color(0.52f, 0.22f, 0.68f, 1f);
        [Tooltip("Optional custom sprite for the large enemy.")]
        public Sprite customSprite;

        [Header("Patrol Behaviour")]
        [Tooltip("How far left and right from its start position the enemy patrols.")]
        public float patrolDistance = 3.5f;
        [Tooltip("Horizontal movement speed while patrolling.")]
        public float patrolSpeed = 2.4f;

        [Header("Combat & Damage Settings")]
        [Tooltip("Damage dealt to the Slime on normal contact.")]
        public int contactDamage = 1;
        [Tooltip("Horizontal knockback force applied to the Slime on normal contact.")]
        public float knockbackForceX = 7.5f;
        [Tooltip("Upward knockback force applied to the Slime on normal contact.")]
        public float knockbackForceY = 5.5f;

        public bool IsDefeated { get; private set; }

        private Vector3 _initialPosition;
        private Vector3 _initialScale;
        private int _patrolDirection = 1;
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _collider;
        private Rigidbody2D _rb;
        private Coroutine _defeatRoutine;

        private void Awake()
        {
            CacheComponents();
            if (Application.isPlaying)
            {
                _initialPosition = transform.position;
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
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();

            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rb.useFullKinematicContacts = true;
            }
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
                _spriteRenderer.sprite = ProceduralVisuals.GetLargeEnemySprite();
            }
            _spriteRenderer.color = enemyColor;
            _spriteRenderer.sortingOrder = 6;
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || IsDefeated) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            float minX = _initialPosition.x - patrolDistance;
            float maxX = _initialPosition.x + patrolDistance;

            Vector2 pos = _rb.position;
            pos.x += _patrolDirection * patrolSpeed * Time.fixedDeltaTime;

            if (pos.x >= maxX)
            {
                pos.x = maxX;
                _patrolDirection = -1;
            }
            else if (pos.x <= minX)
            {
                pos.x = minX;
                _patrolDirection = 1;
            }

            _rb.MovePosition(pos);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.flipX = _patrolDirection < 0;
            }

            float stompStep = Mathf.Abs(Mathf.Sin(Time.time * patrolSpeed * 2.6f)) * 0.06f;
            transform.localScale = new Vector3(
                _initialScale.x * (1f + stompStep),
                _initialScale.y * (1f - stompStep),
                _initialScale.z
            );
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleSlimeInteraction(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleSlimeInteraction(collision.gameObject);
        }

        private void HandleSlimeInteraction(GameObject other)
        {
            if (!Application.isPlaying || IsDefeated) return;

            SlimePlayerController slime = other.GetComponent<SlimePlayerController>();
            if (slime == null) return;

            if (slime.IsStomping)
            {
                DefeatByStomp(slime.transform.position);
                slime.BounceAfterStompKill();
            }
            else
            {
                float dir = Mathf.Sign(slime.transform.position.x - transform.position.x);
                if (Mathf.Approximately(dir, 0f)) dir = -_patrolDirection;
                Vector2 knockback = new Vector2(dir * knockbackForceX, knockbackForceY);
                slime.TakeDamage(contactDamage, knockback);
            }
        }

        public void DefeatByStomp(Vector3 impactOrigin)
        {
            if (IsDefeated) return;
            IsDefeated = true;

            if (_collider != null) _collider.enabled = false;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;

            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.PlayEnemyDefeat();
            }

            GameObject ringObj = new GameObject("EnemyDefeatBurst");
            ringObj.transform.position = transform.position;
            TransientRingEffect ring = ringObj.AddComponent<TransientRingEffect>();
            ring.Initialize(new Color(1f, 0.35f, 0.2f, 0.95f), 0.6f, 4.5f, 0.45f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerCameraShake(0.28f, 0.28f);
                GameManager.Instance.NotifyEnemyDefeated(this);
            }

            if (_defeatRoutine != null) StopCoroutine(_defeatRoutine);
            _defeatRoutine = StartCoroutine(PlaySquashDefeatAnimation());
        }

        private IEnumerator PlaySquashDefeatAnimation()
        {
            float duration = 0.38f;
            float elapsed = 0f;
            Vector3 startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scaleX = Mathf.Lerp(_initialScale.x, _initialScale.x * 1.85f, t);
                float scaleY = Mathf.Lerp(_initialScale.y, _initialScale.y * 0.12f, t);
                transform.localScale = new Vector3(scaleX, scaleY, _initialScale.z);
                transform.position = startPos + new Vector3(0f, -(_initialScale.y - scaleY) * 0.4f, 0f);

                if (_spriteRenderer != null)
                {
                    Color flash = Color.Lerp(Color.white, new Color(1f, 0.25f, 0.2f, 0f), t);
                    _spriteRenderer.color = flash;
                }

                yield return null;
            }

            gameObject.SetActive(false);
        }

        public void ResetEnemy()
        {
            if (_defeatRoutine != null)
            {
                StopCoroutine(_defeatRoutine);
                _defeatRoutine = null;
            }

            IsDefeated = false;
            _patrolDirection = 1;
            transform.position = _initialPosition;
            transform.localScale = _initialScale;

            if (_rb != null)
            {
                _rb.position = _initialPosition;
                _rb.linearVelocity = Vector2.zero;
            }

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            gameObject.SetActive(true);
            ApplyVisuals();
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? _initialPosition : transform.position;
            Gizmos.color = new Color(0.85f, 0.2f, 0.9f, 0.85f);
            Vector3 left = center + Vector3.left * patrolDistance;
            Vector3 right = center + Vector3.right * patrolDistance;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawWireCube(left, new Vector3(0.3f, 0.8f, 0.1f));
            Gizmos.DrawWireCube(right, new Vector3(0.3f, 0.8f, 0.1f));
        }
    }
}
