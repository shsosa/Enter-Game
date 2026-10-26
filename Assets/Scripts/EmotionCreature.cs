using UnityEngine;

namespace FeelinSlimy
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class EmotionCreature : MonoBehaviour
    {
        [Header("Emotion Identity")]
        [Tooltip("Name of the emotion this small creature represents.")]
        public string emotionName = "Anger";
        [Tooltip("Name of the ability granted to the Slime when absorbed.")]
        public string grantedAbilityName = "Anger Stomp";
        [Tooltip("Color tint of the small emotion creature.")]
        public Color creatureColor = new Color(1f, 0.35f, 0.18f, 1f);
        [Tooltip("Optional custom sprite. Leave empty to use the default Anger Spark sprite.")]
        public Sprite customSprite;

        [Header("Safe Movement Settings")]
        [Tooltip("How far left and right the creature gently wanders from its starting spot.")]
        public float wanderDistance = 1.2f;
        [Tooltip("How fast the creature moves horizontally while wandering.")]
        public float wanderSpeed = 1.4f;
        [Tooltip("Height of the gentle hopping/bobbing motion.")]
        public float hopHeight = 0.28f;
        [Tooltip("Speed of the hopping/bobbing motion.")]
        public float hopFrequency = 4.5f;

        public bool IsAbsorbed { get; private set; }
        public bool IsBeingPulled { get; private set; }
        public float PullProgress01 { get; private set; }

        private Vector3 _initialPosition;
        private Vector3 _initialScale;
        private SpriteRenderer _spriteRenderer;
        private CircleCollider2D _collider;
        private float _wanderTimer;

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
            if (_collider == null) _collider = GetComponent<CircleCollider2D>();
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
                _spriteRenderer.sprite = ProceduralVisuals.GetAngerCreatureSprite();
            }
            _spriteRenderer.color = creatureColor;
            _spriteRenderer.sortingOrder = 5;
        }

        private void Update()
        {
            if (!Application.isPlaying || IsAbsorbed) return;

            if (!IsBeingPulled)
            {
                _wanderTimer += Time.deltaTime;
                float offsetX = Mathf.Sin(_wanderTimer * wanderSpeed) * wanderDistance;
                float offsetY = Mathf.Abs(Mathf.Sin(_wanderTimer * hopFrequency)) * hopHeight;
                transform.position = _initialPosition + new Vector3(offsetX, offsetY, 0f);

                float pulse = 1f + Mathf.Sin(_wanderTimer * 6f) * 0.06f;
                transform.localScale = new Vector3(_initialScale.x * pulse, _initialScale.y * (2f - pulse), _initialScale.z);
                _spriteRenderer.flipX = Mathf.Cos(_wanderTimer * wanderSpeed) < 0f;
            }
        }

        public void BeginOrUpdatePull(Vector3 targetPosition, float pullProgress01, float pullSpeed)
        {
            if (IsAbsorbed) return;
            IsBeingPulled = true;
            PullProgress01 = Mathf.Clamp01(pullProgress01);

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, pullSpeed * Time.deltaTime);
            float jitter = Mathf.Sin(Time.time * 45f) * 0.06f;
            float shrink = Mathf.Lerp(1f, 0.55f, PullProgress01);
            transform.localScale = _initialScale * shrink + new Vector3(jitter, -jitter, 0f);
        }

        public void ReleasePull()
        {
            if (IsAbsorbed) return;
            IsBeingPulled = false;
            PullProgress01 = 0f;
            transform.localScale = _initialScale;
        }

        public void CompleteAbsorption()
        {
            if (IsAbsorbed) return;
            IsAbsorbed = true;
            IsBeingPulled = false;
            PullProgress01 = 1f;
            SpawnBurstEffect(transform.position, creatureColor);
            gameObject.SetActive(false);
        }

        public void ResetCreature()
        {
            IsAbsorbed = false;
            IsBeingPulled = false;
            PullProgress01 = 0f;
            _wanderTimer = 0f;
            transform.position = _initialPosition;
            transform.localScale = _initialScale;
            gameObject.SetActive(true);
            ApplyVisuals();
        }

        private static void SpawnBurstEffect(Vector3 position, Color color)
        {
            GameObject ringObj = new GameObject("AbsorbBurstRing");
            ringObj.transform.position = position;
            TransientRingEffect effect = ringObj.AddComponent<TransientRingEffect>();
            effect.Initialize(color, 0.4f, 3.2f, 0.35f);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? _initialPosition : transform.position;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.7f);
            Gizmos.DrawLine(center + Vector3.left * wanderDistance, center + Vector3.right * wanderDistance);
            Gizmos.DrawWireSphere(center + Vector3.left * wanderDistance, 0.12f);
            Gizmos.DrawWireSphere(center + Vector3.right * wanderDistance, 0.12f);
        }
    }

    public class TransientRingEffect : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Color _baseColor;
        private float _startScale;
        private float _endScale;
        private float _duration;
        private float _elapsed;

        public void Initialize(Color color, float startScale, float endScale, float duration)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralVisuals.GetRingSprite();
            _sr.color = color;
            _sr.sortingOrder = 15;
            _baseColor = color;
            _startScale = startScale;
            _endScale = endScale;
            _duration = Mathf.Max(0.05f, duration);
            transform.localScale = Vector3.one * _startScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            float scale = Mathf.Lerp(_startScale, _endScale, 1f - Mathf.Pow(1f - t, 2f));
            transform.localScale = new Vector3(scale, scale, 1f);
            if (_sr != null)
            {
                _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, (1f - t) * _baseColor.a);
            }
            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    public class SlimeDropletEffect : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Vector2 _velocity;
        private float _gravity;
        private Color _baseColor;
        private Vector3 _baseScale;
        private float _duration;
        private float _elapsed;

        public static void SpawnSplash(Vector3 origin, Color color, int count, float minSpeed, float maxSpeed, Vector2 biasDir, float scaleMultiplier = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject dropObj = new GameObject("SlimeDroplet");
                dropObj.transform.position = origin + (Vector3)(Random.insideUnitCircle * 0.14f);
                SlimeDropletEffect drop = dropObj.AddComponent<SlimeDropletEffect>();

                Vector2 dir = (Random.insideUnitCircle.normalized * 0.7f + biasDir * 0.85f).normalized;
                if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
                float speed = Random.Range(minSpeed, maxSpeed);
                float size = Random.Range(0.24f, 0.52f) * scaleMultiplier;
                float life = Random.Range(0.22f, 0.45f);

                drop.Initialize(color, dir * speed, 15f, size, life);
            }
        }

        public void Initialize(Color color, Vector2 initialVelocity, float gravity, float scale, float duration)
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralVisuals.GetSlimeDropletSprite();
            _sr.color = color;
            _sr.sortingOrder = 14;
            _baseColor = color;
            _velocity = initialVelocity;
            _gravity = gravity;
            _baseScale = Vector3.one * scale;
            _duration = Mathf.Max(0.05f, duration);
            transform.localScale = _baseScale;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            _velocity.y -= _gravity * Time.deltaTime;
            transform.position += (Vector3)(_velocity * Time.deltaTime);

            float speed = _velocity.magnitude;
            if (speed > 0.1f)
            {
                float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }

            float shrink = 1f - t * 0.85f;
            float stretch = 1f + Mathf.Clamp(speed * 0.06f, 0f, 0.65f);
            transform.localScale = new Vector3(_baseScale.x * stretch * shrink, _baseScale.y * (1f / Mathf.Sqrt(stretch)) * shrink, 1f);

            if (_sr != null)
            {
                _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, (1f - t * t) * _baseColor.a);
            }

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    public class SlimeResiduePatch : MonoBehaviour
    {
        private static readonly System.Collections.Generic.List<SlimeResiduePatch> ActivePatches = new System.Collections.Generic.List<SlimeResiduePatch>();
        private const int MaxActivePatches = 140;

        public Vector2 SurfaceNormal { get; private set; }
        public bool IsWallSurface => Mathf.Abs(SurfaceNormal.x) > 0.55f;
        public float EffectiveRadius { get; private set; }

        private SpriteRenderer _sr;
        private Color _baseColor;
        private Vector3 _targetScale;
        private float _lifetime;
        private float _elapsed;
        private float _pulseSeed;

        public static SlimeResiduePatch Spawn(Vector3 position, Vector2 surfaceNormal, Color slimeColor, float widthScale = 1f, float lifetime = 16f)
        {
            for (int i = ActivePatches.Count - 1; i >= 0; i--)
            {
                SlimeResiduePatch existing = ActivePatches[i];
                if (existing == null)
                {
                    ActivePatches.RemoveAt(i);
                    continue;
                }

                if (Vector2.Dot(existing.SurfaceNormal, surfaceNormal) > 0.8f &&
                    Vector2.Distance(existing.transform.position, position) < 0.28f * widthScale)
                {
                    existing.Refresh(slimeColor, widthScale, lifetime);
                    return existing;
                }
            }

            while (ActivePatches.Count >= MaxActivePatches)
            {
                SlimeResiduePatch oldest = ActivePatches[0];
                ActivePatches.RemoveAt(0);
                if (oldest != null)
                {
                    Destroy(oldest.gameObject);
                }
            }

            GameObject obj = new GameObject("SlimeResiduePatch");
            obj.transform.position = position;
            SlimeResiduePatch patch = obj.AddComponent<SlimeResiduePatch>();
            patch.Setup(surfaceNormal, slimeColor, widthScale, lifetime);
            ActivePatches.Add(patch);
            return patch;
        }

        public static bool CheckResidueNear(Vector2 worldPos, float queryRadius, out bool onFloorResidue, out bool onWallResidue)
        {
            onFloorResidue = false;
            onWallResidue = false;

            for (int i = ActivePatches.Count - 1; i >= 0; i--)
            {
                SlimeResiduePatch patch = ActivePatches[i];
                if (patch == null)
                {
                    ActivePatches.RemoveAt(i);
                    continue;
                }

                float combinedRadius = queryRadius + patch.EffectiveRadius;
                if (((Vector2)patch.transform.position - worldPos).sqrMagnitude <= combinedRadius * combinedRadius)
                {
                    if (patch.IsWallSurface)
                        onWallResidue = true;
                    else
                        onFloorResidue = true;

                    if (onFloorResidue && onWallResidue)
                        return true;
                }
            }

            return onFloorResidue || onWallResidue;
        }

        public static void ClearAllResidue()
        {
            for (int i = ActivePatches.Count - 1; i >= 0; i--)
            {
                if (ActivePatches[i] != null)
                {
                    Destroy(ActivePatches[i].gameObject);
                }
            }
            ActivePatches.Clear();
        }

        private void Setup(Vector2 surfaceNormal, Color slimeColor, float widthScale, float lifetime)
        {
            SurfaceNormal = surfaceNormal.sqrMagnitude > 0.01f ? surfaceNormal.normalized : Vector2.up;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProceduralVisuals.GetResiduePatchSprite();
            _sr.sortingOrder = 4;

            float angle = Mathf.Atan2(SurfaceNormal.y, SurfaceNormal.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _pulseSeed = Random.Range(0f, 10f);
            Refresh(slimeColor, widthScale, lifetime);
            transform.localScale = new Vector3(_targetScale.x * 0.25f, _targetScale.y * 1.5f, 1f);
        }

        public void Refresh(Color slimeColor, float widthScale, float lifetime)
        {
            _baseColor = new Color(
                Mathf.Clamp01(slimeColor.r * 1.05f),
                Mathf.Clamp01(slimeColor.g * 1.05f),
                Mathf.Clamp01(slimeColor.b * 1.05f),
                0.86f
            );
            if (_sr != null) _sr.color = _baseColor;

            float clampedWidth = Mathf.Clamp(widthScale, 0.65f, 2.2f);
            _targetScale = new Vector3(0.82f * clampedWidth, 0.56f * Mathf.Lerp(1f, 1.25f, (clampedWidth - 0.65f) * 0.5f), 1f);
            EffectiveRadius = 0.48f * clampedWidth;
            _lifetime = Mathf.Max(2f, lifetime);
            _elapsed = 0f;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _lifetime);

            float popT = Mathf.Clamp01(_elapsed / 0.14f);
            float popCurve = 1f + Mathf.Sin(popT * Mathf.PI) * 0.24f;
            float wobble = 1f + Mathf.Sin(Time.time * 4.5f + _pulseSeed) * 0.05f;
            float fadeShrink = t > 0.78f ? Mathf.Lerp(1f, 0.25f, (t - 0.78f) / 0.22f) : 1f;

            transform.localScale = new Vector3(
                _targetScale.x * popCurve * wobble * fadeShrink,
                _targetScale.y * (2f - popCurve * 0.92f) * fadeShrink,
                1f
            );

            if (_sr != null)
            {
                float alpha = t > 0.75f ? Mathf.Lerp(_baseColor.a, 0f, (t - 0.75f) / 0.25f) : _baseColor.a;
                _sr.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, alpha);
            }

            if (t >= 1f)
            {
                ActivePatches.Remove(this);
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            ActivePatches.Remove(this);
        }
    }
}
