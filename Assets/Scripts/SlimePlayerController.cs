using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FeelinSlimy
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class SlimePlayerController : MonoBehaviour
    {
        [Header("1. Movement & Jumping")]
        [Tooltip("Base horizontal movement speed of the Slime (A/D or Left/Right).")]
        public float moveSpeed = 7.4f;
        [Tooltip("Upward velocity applied when jumping (Space / W / Up Arrow).")]
        public float jumpForce = 13.5f;
        [Tooltip("Forgiveness time (in seconds) allowing a jump shortly after walking off a ledge.")]
        public float coyoteTime = 0.14f;
        [Tooltip("Forgiveness time (in seconds) buffering a jump input pressed slightly before landing.")]
        public float jumpBufferTime = 0.14f;
        [Tooltip("Ground acceleration rate for snappy, responsive starts.")]
        public float groundAcceleration = 68f;
        [Tooltip("Ground deceleration rate when releasing input.")]
        public float groundDeceleration = 62f;
        [Tooltip("Mid-air horizontal acceleration rate.")]
        public float airAcceleration = 46f;
        [Tooltip("Extra multiplier when turning around to make direction changes feel extra snappy.")]
        public float turnSpeedMultiplier = 1.85f;
        [Tooltip("Offset below the Slime's center used to check for solid ground.")]
        public Vector2 groundCheckOffset = new Vector2(0f, -0.52f);
        [Tooltip("Size of the box used to detect solid ground below the Slime.")]
        public Vector2 groundCheckSize = new Vector2(0.75f, 0.18f);

        [Header("1b. Slime Residue, Speed Boost & Wall Climb")]
        [Tooltip("Whether the Slime leaves a trail of slimy residue on floors, platforms, and walls.")]
        public bool leaveSlimeResidue = true;
        [Tooltip("Speed multiplier when moving over slimy residue on floors/platforms.")]
        public float residueSpeedMultiplier = 1.48f;
        [Tooltip("Duration (in seconds) that the slimy speed & climb buff lingers after touching residue.")]
        public float residueBuffPersistence = 0.45f;
        [Tooltip("How long (in seconds) each slime residue patch stays on the ground/wall.")]
        public float residueLifetime = 18f;
        [Tooltip("Minimum distance moved before dropping a new slime trail patch.")]
        public float residueSpawnSpacing = 0.38f;
        [Tooltip("Upward climbing speed when pressing into a wall or holding Up on a slimy wall.")]
        public float wallClimbSpeed = 6.4f;
        [Tooltip("Speed multiplier when climbing on a wall coated with slime residue.")]
        public float residueWallClimbMultiplier = 1.38f;
        [Tooltip("Controlled downward slide speed when clinging to a wall without climbing up.")]
        public float wallSlideSpeed = 1.8f;
        [Tooltip("Horizontal and vertical launch velocity when jumping off a wall.")]
        public Vector2 wallJumpForce = new Vector2(10.2f, 13.4f);
        [Tooltip("Horizontal offset from the Slime center for left/right wall detection.")]
        public float wallCheckDistance = 0.52f;
        [Tooltip("Size of the box used to detect walls on the left and right.")]
        public Vector2 wallCheckSize = new Vector2(0.18f, 0.62f);

        [Header("1c. Juicy Physics & Spring Squash/Stretch")]
        [Tooltip("Base gravity scale during upward rise.")]
        public float baseGravityScale = 3.1f;
        [Tooltip("Gravity multiplier when falling downward for punchy platformer feel.")]
        public float fallGravityMultiplier = 1.55f;
        [Tooltip("Gravity multiplier when jump button is released early (variable jump height).")]
        public float lowJumpGravityMultiplier = 2.15f;
        [Tooltip("Reduced gravity multiplier near the peak of a jump for extra hang-time control.")]
        public float apexGravityMultiplier = 0.58f;
        [Tooltip("Vertical speed threshold around jump peak where apex hang-time applies.")]
        public float apexVelocityThreshold = 2.2f;
        [Tooltip("Spring stiffness for jelly squash-and-stretch deformation.")]
        public float squashSpringStiffness = 240f;
        [Tooltip("Spring damping for jelly squash-and-stretch oscillation.")]
        public float squashSpringDamping = 14f;
        [Tooltip("Maximum visual tilt angle (degrees) when accelerating or moving fast.")]
        public float maxMovementTiltAngle = 11f;

        [Header("2. Emotion Absorption (Hold E)")]
        [Tooltip("Maximum distance from which the Slime can pull and absorb the Emotion Creature.")]
        public float absorbRange = 3.2f;
        [Tooltip("How long (in seconds) the player must hold E to finish pulling and absorbing.")]
        public float absorbHoldDuration = 0.65f;
        [Tooltip("Speed at which the Emotion Creature is pulled toward the Slime while holding E.")]
        public float absorbPullSpeed = 4.8f;
        [Tooltip("Optional explicit reference to the room's Emotion Creature (auto-detected if left empty).")]
        public EmotionCreature targetCreature;

        [Header("3. Anger Stomp Ability (Press S / Down / Shift)")]
        [Tooltip("Whether the Slime has absorbed the Anger Creature and unlocked Anger Stomp.")]
        public bool hasAngerStomp = false;
        [Tooltip("Downward slam velocity during the Anger Stomp.")]
        public float stompDownwardSpeed = 22f;
        [Tooltip("Radius of the shockwave on Stomp impact that crushes nearby Large Enemies.")]
        public float stompShockwaveRadius = 2.6f;
        [Tooltip("Cooldown time (in seconds) between Anger Stomps.")]
        public float stompCooldown = 0.45f;
        [Tooltip("Upward bounce velocity when the Slime crushes an enemy directly.")]
        public float stompKillBounceForce = 10.5f;

        [Header("4. Health & Damage Rules")]
        [Tooltip("Maximum health points (hearts) for the Slime.")]
        public int maxHealth = 3;
        [Tooltip("Brief invulnerability duration (in seconds) after taking damage.")]
        public float invincibilityDuration = 1.1f;
        [Tooltip("Y position below which the Slime is considered to have fallen out of the room.")]
        public float fallLimitY = -8.5f;

        [Header("5. Visual Appearance")]
        [Tooltip("Default Slime color before absorbing an emotion.")]
        public Color calmSlimeColor = new Color(0.28f, 0.88f, 0.95f, 1f);
        [Tooltip("Slime color after absorbing the Anger emotion.")]
        public Color angerSlimeColor = new Color(1f, 0.28f, 0.18f, 1f);
        [Tooltip("Optional custom sprite for the calm Slime.")]
        public Sprite customCalmSprite;
        [Tooltip("Optional custom sprite for the angry powered-up Slime.")]
        public Sprite customAngrySprite;

        public int CurrentHealth { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsTouchingWall { get; private set; }
        public bool IsWallClimbing { get; private set; }
        public int WallDirection { get; private set; }
        public bool IsOnResidueBoosted { get; private set; }
        public bool IsOnSlimyWall { get; private set; }
        public bool IsStomping { get; private set; }
        public float CurrentAbsorbProgress01 { get; private set; }
        public bool CanReachCreature { get; private set; }
        public float StompCooldownRemaining => Mathf.Max(0f, _nextStompAllowedTime - Time.time);

        private Rigidbody2D _rb;
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _collider;
        private LineRenderer _tetherLine;

        private Vector3 _initialPosition;
        private Vector3 _initialScale;
        private float _horizontalInput;
        private float _verticalInput;
        private bool _jumpHeld;
        private bool _stompRequested;
        private bool _absorbHeld;

        private float _lastGroundedTime = -10f;
        private float _lastJumpPressedTime = -10f;
        private float _lastWallTouchTime = -10f;
        private int _lastWallDirection;
        private float _wallJumpLockTimer;
        private float _knockbackTimer;
        private float _invincibilityTimer;
        private float _nextStompAllowedTime;
        private Coroutine _stompRoutine;

        private float _residueFloorBuffTimer;
        private float _residueWallBuffTimer;
        private Vector3 _lastGroundResiduePos;
        private Vector3 _lastWallResiduePos;
        private float _runDustTimer;
        private float _preLandVelocityY;

        private Vector2 _squashDeform = Vector2.one;
        private Vector2 _squashVelocity = Vector2.zero;
        private float _visualTiltAngle;

        private void Awake()
        {
            CacheComponents();
            if (Application.isPlaying)
            {
                _initialPosition = transform.position;
                _initialScale = transform.localScale;
                _lastGroundResiduePos = _initialPosition;
                _lastWallResiduePos = _initialPosition;
                CurrentHealth = maxHealth;
                SetupTetherLine();
            }
        }

        private void OnEnable()
        {
            CacheComponents();
            ApplyVisualState();
        }

        private void OnValidate()
        {
            CacheComponents();
            ApplyVisualState();
        }

        private void CacheComponents()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();

            if (_rb != null)
            {
                _rb.gravityScale = 3.2f;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void SetupTetherLine()
        {
            if (_tetherLine != null) return;
            GameObject lineObj = new GameObject("AbsorptionTetherLine");
            lineObj.transform.SetParent(transform, false);
            _tetherLine = lineObj.AddComponent<LineRenderer>();
            _tetherLine.positionCount = 2;
            _tetherLine.startWidth = 0.08f;
            _tetherLine.endWidth = 0.16f;
            _tetherLine.material = new Material(Shader.Find("Sprites/Default"));
            _tetherLine.startColor = new Color(1f, 0.9f, 0.3f, 0.9f);
            _tetherLine.endColor = new Color(1f, 0.3f, 0.15f, 0.95f);
            _tetherLine.sortingOrder = 12;
            _tetherLine.enabled = false;
        }

        public void ApplyVisualState()
        {
            if (_spriteRenderer == null) return;

            if (hasAngerStomp)
            {
                _spriteRenderer.sprite = customAngrySprite != null
                    ? customAngrySprite
                    : ProceduralVisuals.GetSlimeSprite(true);
                _spriteRenderer.color = angerSlimeColor;
            }
            else
            {
                _spriteRenderer.sprite = customCalmSprite != null
                    ? customCalmSprite
                    : ProceduralVisuals.GetSlimeSprite(false);
                _spriteRenderer.color = calmSlimeColor;
            }
            _spriteRenderer.sortingOrder = 10;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                if (_tetherLine != null) _tetherLine.enabled = false;
                if (RetroSoundEffects.Instance != null) RetroSoundEffects.Instance.UpdateAbsorbPullSound(false, 0f);
                return;
            }

            ReadPlayerInput();
            UpdateGroundedAndWallState();
            UpdateResidueTrailAndBuffs();
            HandleAbsorptionInteraction();
            UpdateVisualFeedback();

            if (transform.position.y < fallLimitY)
            {
                CurrentHealth = 0;
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerFailure("The Slime fell out of the test room!");
                }
            }
        }

        private void ReadPlayerInput()
        {
            _horizontalInput = 0f;
            _verticalInput = 0f;
            _absorbHeld = false;
            _jumpHeld = false;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) _horizontalInput -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) _horizontalInput += 1f;

                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) _verticalInput += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) _verticalInput -= 1f;

                if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                {
                    _lastJumpPressedTime = Time.time;
                }

                if (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed)
                {
                    _jumpHeld = true;
                }

                _absorbHeld = kb.eKey.isPressed;

                if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame)
                {
                    _stompRequested = true;
                }
            }

            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                if (Mathf.Abs(stick.x) > 0.2f) _horizontalInput = Mathf.Sign(stick.x);
                if (Mathf.Abs(stick.y) > 0.25f) _verticalInput = Mathf.Sign(stick.y);
                if (gp.buttonSouth.wasPressedThisFrame) _lastJumpPressedTime = Time.time;
                if (gp.buttonSouth.isPressed) _jumpHeld = true;
                if (gp.buttonWest.isPressed) _absorbHeld = true;
                if (gp.buttonEast.wasPressedThisFrame || gp.rightTrigger.wasPressedThisFrame) _stompRequested = true;
            }
        }

        private void UpdateGroundedAndWallState()
        {
            Vector2 checkCenter = (Vector2)transform.position + groundCheckOffset;
            Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, groundCheckSize, 0f);
            bool foundGround = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col == null || col == _collider || col.isTrigger) continue;
                if (col.GetComponent<LargeEnemy>() != null) continue;
                foundGround = true;
                break;
            }

            bool wasGrounded = IsGrounded;
            IsGrounded = foundGround;

            int detectedWallDir = 0;
            if (CheckSolidWallAtOffset(new Vector2(-wallCheckDistance, 0f)))
                detectedWallDir = -1;
            else if (CheckSolidWallAtOffset(new Vector2(wallCheckDistance, 0f)))
                detectedWallDir = 1;

            IsTouchingWall = detectedWallDir != 0;
            WallDirection = detectedWallDir;
            if (IsTouchingWall)
            {
                _lastWallTouchTime = Time.time;
                _lastWallDirection = detectedWallDir;
            }

            if (IsGrounded)
            {
                _lastGroundedTime = Time.time;
                if (!wasGrounded)
                {
                    if (IsStomping)
                    {
                        CompleteStompImpact();
                    }
                    else
                    {
                        OnJuicyLand(Mathf.Abs(_preLandVelocityY));
                    }
                }
            }
            else if (_rb != null)
            {
                _preLandVelocityY = _rb.linearVelocity.y;
            }
        }

        private bool CheckSolidWallAtOffset(Vector2 localOffset)
        {
            Vector2 center = (Vector2)transform.position + localOffset;
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, wallCheckSize, 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col == null || col == _collider || col.isTrigger) continue;
                if (col.GetComponent<LargeEnemy>() != null) continue;
                return true;
            }
            return false;
        }

        private Color CurrentSlimeColor => hasAngerStomp ? angerSlimeColor : calmSlimeColor;

        private void UpdateResidueTrailAndBuffs()
        {
            Vector2 pos = transform.position;
            Vector2 feetPos = pos + groundCheckOffset;

            SlimeResiduePatch.CheckResidueNear(feetPos, 0.36f, out bool nearFloorResidue, out _);
            bool nearWallResidue = false;
            if (IsTouchingWall)
            {
                Vector2 wallPos = pos + new Vector2(WallDirection * wallCheckDistance, 0f);
                SlimeResiduePatch.CheckResidueNear(wallPos, 0.42f, out _, out nearWallResidue);
            }

            if (IsGrounded && nearFloorResidue)
            {
                _residueFloorBuffTimer = residueBuffPersistence;
            }
            else if (_residueFloorBuffTimer > 0f)
            {
                _residueFloorBuffTimer = Mathf.Max(0f, _residueFloorBuffTimer - Time.deltaTime);
            }

            if (IsTouchingWall && nearWallResidue)
            {
                _residueWallBuffTimer = residueBuffPersistence;
            }
            else if (_residueWallBuffTimer > 0f)
            {
                _residueWallBuffTimer = Mathf.Max(0f, _residueWallBuffTimer - Time.deltaTime);
            }

            IsOnResidueBoosted = _residueFloorBuffTimer > 0f;
            IsOnSlimyWall = _residueWallBuffTimer > 0f;

            if (!leaveSlimeResidue) return;

            if (IsGrounded)
            {
                float distFromLast = Vector2.Distance(feetPos, _lastGroundResiduePos);
                bool movingOnGround = _rb != null && Mathf.Abs(_rb.linearVelocity.x) > 0.35f;
                if (distFromLast >= residueSpawnSpacing || (movingOnGround && distFromLast >= residueSpawnSpacing * 0.75f))
                {
                    Vector3 spawnPos = new Vector3(feetPos.x, feetPos.y + 0.05f, 0f);
                    SlimeResiduePatch.Spawn(spawnPos, Vector2.up, CurrentSlimeColor, 1f, residueLifetime);
                    _lastGroundResiduePos = feetPos;
                    _residueFloorBuffTimer = residueBuffPersistence;
                }
            }

            if (IsTouchingWall && (IsWallClimbing || (_rb != null && Mathf.Abs(_rb.linearVelocity.y) > 0.3f)))
            {
                Vector2 wallSurfacePos = pos + new Vector2(WallDirection * (wallCheckDistance - 0.04f), 0f);
                if (Vector2.Distance(wallSurfacePos, _lastWallResiduePos) >= residueSpawnSpacing * 0.85f)
                {
                    Vector2 wallNormal = new Vector2(-WallDirection, 0f);
                    SlimeResiduePatch.Spawn(wallSurfacePos, wallNormal, CurrentSlimeColor, 0.9f, residueLifetime);
                    _lastWallResiduePos = wallSurfacePos;
                    _residueWallBuffTimer = residueBuffPersistence;
                }
            }

            if (_rb != null && IsGrounded && Mathf.Abs(_rb.linearVelocity.x) > 3.5f)
            {
                _runDustTimer -= Time.deltaTime;
                if (_runDustTimer <= 0f)
                {
                    _runDustTimer = IsOnResidueBoosted ? 0.09f : 0.16f;
                    Vector2 kickDir = new Vector2(-Mathf.Sign(_rb.linearVelocity.x) * 0.65f, 0.75f);
                    SlimeDropletEffect.SpawnSplash(feetPos, CurrentSlimeColor, 1, 1.8f, 3.4f, kickDir, 0.72f);
                }
            }
        }

        private void OnJuicyLand(float impactSpeedY)
        {
            float intensity = Mathf.Clamp01(impactSpeedY / 18f);
            if (intensity < 0.08f) return;

            ApplySquashImpulse(new Vector2(1f + intensity * 0.42f, 1f - intensity * 0.34f));

            Vector2 feetPos = (Vector2)transform.position + groundCheckOffset;
            if (leaveSlimeResidue)
            {
                SlimeResiduePatch.Spawn(
                    new Vector3(feetPos.x, feetPos.y + 0.05f, 0f),
                    Vector2.up,
                    CurrentSlimeColor,
                    Mathf.Lerp(1.05f, 1.55f, intensity),
                    residueLifetime
                );
                _lastGroundResiduePos = feetPos;
            }

            int dropCount = Mathf.RoundToInt(Mathf.Lerp(2f, 7f, intensity));
            SlimeDropletEffect.SpawnSplash(feetPos, CurrentSlimeColor, dropCount, 2.2f, 5.8f * (0.6f + intensity * 0.6f), Vector2.up);

            if (RetroSoundEffects.Instance != null && intensity > 0.22f)
            {
                RetroSoundEffects.Instance.PlayLandSplat(intensity);
            }
        }

        public void ApplySquashImpulse(Vector2 targetSquashScale)
        {
            _squashDeform = targetSquashScale;
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= Time.fixedDeltaTime;
                _lastJumpPressedTime = -10f;
                _stompRequested = false;
                return;
            }

            if (_wallJumpLockTimer > 0f)
            {
                _wallJumpLockTimer -= Time.fixedDeltaTime;
            }

            if (_stompRequested)
            {
                _stompRequested = false;
                if (hasAngerStomp && !IsStomping && Time.time >= _nextStompAllowedTime)
                {
                    if (_stompRoutine != null) StopCoroutine(_stompRoutine);
                    _stompRoutine = StartCoroutine(ExecuteAngerStompRoutine());
                    return;
                }
            }

            if (IsStomping)
            {
                _lastJumpPressedTime = -10f;
                return;
            }

            Vector2 vel = _rb.linearVelocity;

            float speedMultiplier = IsOnResidueBoosted ? residueSpeedMultiplier : 1f;
            float targetSpeedX = _horizontalInput * moveSpeed * speedMultiplier;
            float accelRate;
            if (IsGrounded)
            {
                accelRate = Mathf.Abs(targetSpeedX) > 0.01f ? groundAcceleration : groundDeceleration;
            }
            else
            {
                accelRate = airAcceleration;
            }

            if (Mathf.Abs(_horizontalInput) > 0.01f && Mathf.Abs(vel.x) > 0.5f && Mathf.Sign(_horizontalInput) != Mathf.Sign(vel.x))
            {
                accelRate *= turnSpeedMultiplier;
            }

            if (_wallJumpLockTimer > 0f)
            {
                accelRate *= 0.35f;
            }

            vel.x = Mathf.MoveTowards(vel.x, targetSpeedX, accelRate * Time.fixedDeltaTime);

            bool pressingTowardWall = IsTouchingWall && Mathf.Abs(_horizontalInput) > 0.1f && Mathf.Sign(_horizontalInput) == WallDirection;
            bool holdingClimbUpOnSlimyWall = IsTouchingWall && IsOnSlimyWall && _verticalInput > 0.1f;
            bool canEngageWallClimb = IsTouchingWall && _wallJumpLockTimer <= 0f && (pressingTowardWall || holdingClimbUpOnSlimyWall || (IsOnSlimyWall && !IsGrounded));

            IsWallClimbing = false;
            if (canEngageWallClimb)
            {
                IsWallClimbing = true;
                float climbBoost = IsOnSlimyWall ? residueWallClimbMultiplier : 1f;
                bool wantsClimbUp = pressingTowardWall || _verticalInput > 0.1f;

                if (_verticalInput < -0.1f)
                {
                    vel.y = -wallClimbSpeed * 1.15f;
                }
                else if (wantsClimbUp)
                {
                    vel.y = Mathf.MoveTowards(vel.y, wallClimbSpeed * climbBoost, 65f * Time.fixedDeltaTime);
                }
                else
                {
                    float slideCap = IsOnSlimyWall ? wallSlideSpeed * 0.35f : wallSlideSpeed;
                    vel.y = Mathf.Max(vel.y, -slideCap);
                }
            }

            bool jumpBuffered = (Time.time - _lastJumpPressedTime) <= jumpBufferTime;
            bool canGroundJump = IsGrounded || (Time.time - _lastGroundedTime <= coyoteTime);
            bool canWallJump = !canGroundJump && (IsTouchingWall || (Time.time - _lastWallTouchTime <= coyoteTime));

            if (jumpBuffered && (canGroundJump || canWallJump))
            {
                _lastJumpPressedTime = -10f;
                Vector2 feetPos = (Vector2)transform.position + groundCheckOffset;

                if (canGroundJump)
                {
                    float jumpBoost = IsOnResidueBoosted ? 1.07f : 1f;
                    vel.y = jumpForce * jumpBoost;
                    _lastGroundedTime = -10f;
                    IsGrounded = false;
                    ApplySquashImpulse(new Vector2(0.72f, 1.36f));

                    if (leaveSlimeResidue)
                    {
                        SlimeResiduePatch.Spawn(new Vector3(feetPos.x, feetPos.y + 0.05f, 0f), Vector2.up, CurrentSlimeColor, 1.15f, residueLifetime);
                    }
                    SlimeDropletEffect.SpawnSplash(feetPos, CurrentSlimeColor, 5, 2.4f, 5.2f, Vector2.up);

                    if (RetroSoundEffects.Instance != null)
                    {
                        RetroSoundEffects.Instance.PlayJump();
                    }
                }
                else if (canWallJump)
                {
                    int wallDir = IsTouchingWall ? WallDirection : _lastWallDirection;
                    if (wallDir == 0) wallDir = _spriteRenderer != null && _spriteRenderer.flipX ? -1 : 1;

                    float awayDir = -wallDir;
                    float wallBoost = IsOnSlimyWall ? 1.12f : 1f;
                    vel.x = awayDir * wallJumpForce.x * wallBoost;
                    vel.y = wallJumpForce.y * wallBoost;

                    _lastWallTouchTime = -10f;
                    _wallJumpLockTimer = 0.15f;
                    IsWallClimbing = false;
                    ApplySquashImpulse(new Vector2(0.7f, 1.38f));

                    Vector2 wallContact = (Vector2)transform.position + new Vector2(wallDir * wallCheckDistance, 0f);
                    if (leaveSlimeResidue)
                    {
                        SlimeResiduePatch.Spawn(wallContact, new Vector2(awayDir, 0f), CurrentSlimeColor, 1.15f, residueLifetime);
                    }
                    SlimeDropletEffect.SpawnSplash(wallContact, CurrentSlimeColor, 6, 2.8f, 6.0f, new Vector2(awayDir, 0.65f).normalized);

                    if (RetroSoundEffects.Instance != null)
                    {
                        RetroSoundEffects.Instance.PlayWallJump();
                    }
                }
            }

            if (IsWallClimbing)
            {
                _rb.gravityScale = 0.25f;
            }
            else if (!IsGrounded)
            {
                if (Mathf.Abs(vel.y) <= apexVelocityThreshold)
                {
                    _rb.gravityScale = baseGravityScale * apexGravityMultiplier;
                }
                else if (vel.y < 0f)
                {
                    _rb.gravityScale = baseGravityScale * fallGravityMultiplier;
                }
                else if (!_jumpHeld)
                {
                    _rb.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
                }
                else
                {
                    _rb.gravityScale = baseGravityScale;
                }
            }
            else
            {
                _rb.gravityScale = baseGravityScale;
            }

            _rb.linearVelocity = vel;
        }

        private void HandleAbsorptionInteraction()
        {
            if (targetCreature == null)
            {
                targetCreature = FindFirstObjectByType<EmotionCreature>();
            }

            if (targetCreature == null || targetCreature.IsAbsorbed || hasAngerStomp)
            {
                CanReachCreature = false;
                CurrentAbsorbProgress01 = 0f;
                if (_tetherLine != null) _tetherLine.enabled = false;
                if (RetroSoundEffects.Instance != null) RetroSoundEffects.Instance.UpdateAbsorbPullSound(false, 0f);
                return;
            }

            float dist = Vector2.Distance(transform.position, targetCreature.transform.position);
            CanReachCreature = dist <= absorbRange;

            if (CanReachCreature && _absorbHeld)
            {
                CurrentAbsorbProgress01 += Time.deltaTime / Mathf.Max(0.1f, absorbHoldDuration);
                CurrentAbsorbProgress01 = Mathf.Clamp01(CurrentAbsorbProgress01);

                targetCreature.BeginOrUpdatePull(transform.position, CurrentAbsorbProgress01, absorbPullSpeed);

                if (_tetherLine != null)
                {
                    _tetherLine.enabled = true;
                    _tetherLine.SetPosition(0, transform.position);
                    _tetherLine.SetPosition(1, targetCreature.transform.position);
                }

                if (RetroSoundEffects.Instance != null)
                {
                    RetroSoundEffects.Instance.UpdateAbsorbPullSound(true, CurrentAbsorbProgress01);
                }

                if (CurrentAbsorbProgress01 >= 1f || Vector2.Distance(transform.position, targetCreature.transform.position) <= 0.55f)
                {
                    AcquireEmotionPower(targetCreature);
                }
            }
            else
            {
                if (CurrentAbsorbProgress01 > 0f)
                {
                    CurrentAbsorbProgress01 = Mathf.Max(0f, CurrentAbsorbProgress01 - Time.deltaTime * 2f);
                    targetCreature.ReleasePull();
                }
                if (_tetherLine != null) _tetherLine.enabled = false;
                if (RetroSoundEffects.Instance != null) RetroSoundEffects.Instance.UpdateAbsorbPullSound(false, 0f);
            }
        }

        private void AcquireEmotionPower(EmotionCreature creature)
        {
            creature.CompleteAbsorption();
            hasAngerStomp = true;
            CurrentAbsorbProgress01 = 0f;
            CanReachCreature = false;

            if (_tetherLine != null) _tetherLine.enabled = false;
            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.UpdateAbsorbPullSound(false, 0f);
                RetroSoundEffects.Instance.PlayPowerAcquire();
            }

            ApplyVisualState();

            GameObject ringObj = new GameObject("PowerAcquireRing");
            ringObj.transform.position = transform.position;
            TransientRingEffect ring = ringObj.AddComponent<TransientRingEffect>();
            ring.Initialize(angerSlimeColor, 0.5f, 3.8f, 0.42f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowTemporaryHint("ANGER STOMP UNLOCKED! Jump above the Gloom Brute and press S / Down / Shift!");
            }
        }

        private IEnumerator ExecuteAngerStompRoutine()
        {
            IsStomping = true;
            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.PlayStompLaunch();
            }

            if (IsGrounded)
            {
                _rb.linearVelocity = new Vector2(0f, jumpForce * 0.72f);
                IsGrounded = false;
                yield return new WaitForSeconds(0.16f);
            }
            else
            {
                float savedGravity = _rb.gravityScale;
                _rb.gravityScale = 0f;
                _rb.linearVelocity = Vector2.zero;
                transform.localScale = new Vector3(_initialScale.x * 1.25f, _initialScale.y * 0.75f, _initialScale.z);
                yield return new WaitForSeconds(0.08f);
                _rb.gravityScale = savedGravity;
            }

            float maxStompTime = 1.2f;
            float elapsed = 0f;
            while (IsStomping && elapsed < maxStompTime)
            {
                elapsed += Time.fixedDeltaTime;
                _rb.linearVelocity = new Vector2(0f, -stompDownwardSpeed);
                transform.localScale = new Vector3(_initialScale.x * 0.78f, _initialScale.y * 1.32f, _initialScale.z);

                if (IsGrounded && elapsed > 0.04f)
                {
                    CompleteStompImpact();
                    yield break;
                }
                yield return new WaitForFixedUpdate();
            }

            if (IsStomping)
            {
                CompleteStompImpact();
            }
        }

        private void CompleteStompImpact()
        {
            if (!IsStomping) return;
            IsStomping = false;
            _nextStompAllowedTime = Time.time + stompCooldown;
            ApplySquashImpulse(new Vector2(1.48f, 0.56f));

            Vector2 feetPos = (Vector2)transform.position + groundCheckOffset;
            if (leaveSlimeResidue)
            {
                SlimeResiduePatch.Spawn(new Vector3(feetPos.x, feetPos.y + 0.05f, 0f), Vector2.up, CurrentSlimeColor, 1.85f, residueLifetime);
                SlimeResiduePatch.Spawn(new Vector3(feetPos.x - 0.65f, feetPos.y + 0.05f, 0f), Vector2.up, CurrentSlimeColor, 1.2f, residueLifetime);
                SlimeResiduePatch.Spawn(new Vector3(feetPos.x + 0.65f, feetPos.y + 0.05f, 0f), Vector2.up, CurrentSlimeColor, 1.2f, residueLifetime);
                _lastGroundResiduePos = feetPos;
            }
            SlimeDropletEffect.SpawnSplash(feetPos, CurrentSlimeColor, 10, 3.5f, 8.2f, Vector2.up, 1.2f);

            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.PlayStompImpact();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerCameraShake(0.24f, 0.26f);
            }

            GameObject shockwaveObj = new GameObject("StompShockwaveRing");
            shockwaveObj.transform.position = transform.position + new Vector3(0f, -0.3f, 0f);
            TransientRingEffect ring = shockwaveObj.AddComponent<TransientRingEffect>();
            ring.Initialize(new Color(1f, 0.32f, 0.15f, 0.95f), 0.5f, stompShockwaveRadius * 2f, 0.32f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, stompShockwaveRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                LargeEnemy enemy = hits[i].GetComponent<LargeEnemy>();
                if (enemy != null && !enemy.IsDefeated)
                {
                    enemy.DefeatByStomp(transform.position);
                }
            }
        }

        public void BounceAfterStompKill()
        {
            IsStomping = false;
            _nextStompAllowedTime = Time.time + stompCooldown * 0.5f;
            if (_stompRoutine != null)
            {
                StopCoroutine(_stompRoutine);
                _stompRoutine = null;
            }
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, stompKillBounceForce);
            ApplySquashImpulse(new Vector2(0.68f, 1.42f));
        }

        public void TakeDamage(int damageAmount, Vector2 knockbackVelocity)
        {
            if (_invincibilityTimer > 0f || IsStomping) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
            _invincibilityTimer = invincibilityDuration;
            _knockbackTimer = 0.22f;
            _rb.linearVelocity = knockbackVelocity;
            ApplySquashImpulse(new Vector2(1.38f, 0.65f));
            SlimeDropletEffect.SpawnSplash(transform.position, CurrentSlimeColor, 7, 3.0f, 6.5f, knockbackVelocity.normalized);

            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.PlayPlayerHurt();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerCameraShake(0.18f, 0.2f);
            }

            if (CurrentHealth <= 0 && GameManager.Instance != null)
            {
                GameManager.Instance.TriggerFailure("The Slime ran out of health! Absorb the Anger Creature and Stomp the Brute from above.");
            }
        }

        private void UpdateVisualFeedback()
        {
            if (_spriteRenderer == null) return;

            if (Mathf.Abs(_horizontalInput) > 0.05f)
            {
                _spriteRenderer.flipX = _horizontalInput < 0f;
            }

            Color targetBaseColor = hasAngerStomp ? angerSlimeColor : calmSlimeColor;
            if (IsOnResidueBoosted || IsOnSlimyWall)
            {
                float glow = (Mathf.Sin(Time.time * 14f) * 0.5f + 0.5f) * 0.16f;
                targetBaseColor = Color.Lerp(targetBaseColor, Color.white, glow);
            }

            if (_invincibilityTimer > 0f)
            {
                _invincibilityTimer -= Time.deltaTime;
                bool flashWhite = Mathf.FloorToInt(_invincibilityTimer * 16f) % 2 == 0;
                _spriteRenderer.color = flashWhite ? new Color(1f, 1f, 1f, 0.45f) : targetBaseColor;
            }
            else
            {
                _spriteRenderer.color = targetBaseColor;
            }

            if (!IsStomping)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                Vector2 targetDeform = Vector2.one;

                if (_rb != null)
                {
                    Vector2 vel = _rb.linearVelocity;
                    if (IsWallClimbing)
                    {
                        float climbStretch = Mathf.Clamp(Mathf.Abs(vel.y) * 0.025f, 0f, 0.22f);
                        targetDeform = new Vector2(0.84f - climbStretch * 0.4f, 1.18f + climbStretch);
                    }
                    else if (!IsGrounded)
                    {
                        float airStretch = Mathf.Clamp(Mathf.Abs(vel.y) * 0.022f, 0f, 0.28f);
                        targetDeform = new Vector2(1f - airStretch * 0.55f, 1f + airStretch);
                    }
                    else
                    {
                        float speedRatio = Mathf.Clamp01(Mathf.Abs(vel.x) / (moveSpeed * residueSpeedMultiplier));
                        float wobble = Mathf.Sin(Time.time * (14f + speedRatio * 10f)) * 0.06f * speedRatio;
                        float breath = Mathf.Sin(Time.time * 3.8f) * 0.025f * (1f - speedRatio);
                        targetDeform = new Vector2(
                            1f + speedRatio * 0.1f + wobble + breath,
                            1f - speedRatio * 0.07f - wobble - breath
                        );
                    }
                }

                Vector2 springForce = (targetDeform - _squashDeform) * squashSpringStiffness - _squashVelocity * squashSpringDamping;
                _squashVelocity += springForce * dt;
                _squashDeform += _squashVelocity * dt;
                _squashDeform.x = Mathf.Clamp(_squashDeform.x, 0.5f, 1.65f);
                _squashDeform.y = Mathf.Clamp(_squashDeform.y, 0.5f, 1.65f);

                transform.localScale = new Vector3(
                    _initialScale.x * _squashDeform.x,
                    _initialScale.y * _squashDeform.y,
                    _initialScale.z
                );

                float targetTilt = 0f;
                if (_rb != null)
                {
                    if (IsWallClimbing)
                    {
                        targetTilt = WallDirection * 8f;
                    }
                    else
                    {
                        float normVx = Mathf.Clamp(_rb.linearVelocity.x / (moveSpeed * residueSpeedMultiplier), -1f, 1f);
                        targetTilt = -normVx * maxMovementTiltAngle;
                    }
                }
                _visualTiltAngle = Mathf.Lerp(_visualTiltAngle, targetTilt, dt * 14f);
                transform.rotation = Quaternion.Euler(0f, 0f, _visualTiltAngle);
            }
            else
            {
                transform.rotation = Quaternion.identity;
            }
        }

        public void ResetPlayer()
        {
            if (_stompRoutine != null)
            {
                StopCoroutine(_stompRoutine);
                _stompRoutine = null;
            }

            hasAngerStomp = false;
            IsStomping = false;
            IsTouchingWall = false;
            IsWallClimbing = false;
            IsOnResidueBoosted = false;
            IsOnSlimyWall = false;
            WallDirection = 0;
            CurrentHealth = maxHealth;
            CurrentAbsorbProgress01 = 0f;
            CanReachCreature = false;
            _invincibilityTimer = 0f;
            _knockbackTimer = 0f;
            _wallJumpLockTimer = 0f;
            _lastJumpPressedTime = -10f;
            _lastGroundedTime = -10f;
            _lastWallTouchTime = -10f;
            _residueFloorBuffTimer = 0f;
            _residueWallBuffTimer = 0f;
            _nextStompAllowedTime = 0f;
            _squashDeform = Vector2.one;
            _squashVelocity = Vector2.zero;
            _visualTiltAngle = 0f;

            transform.position = _initialPosition;
            transform.localScale = _initialScale;
            transform.rotation = Quaternion.identity;
            _lastGroundResiduePos = _initialPosition;
            _lastWallResiduePos = _initialPosition;

            if (_rb != null)
            {
                _rb.position = _initialPosition;
                _rb.linearVelocity = Vector2.zero;
                _rb.gravityScale = baseGravityScale;
            }

            if (_tetherLine != null)
            {
                _tetherLine.enabled = false;
            }

            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.UpdateAbsorbPullSound(false, 0f);
            }

            ApplyVisualState();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 groundCenter = transform.position + (Vector3)groundCheckOffset;
            Gizmos.DrawWireCube(groundCenter, groundCheckSize);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position + new Vector3(-wallCheckDistance, 0f, 0f), wallCheckSize);
            Gizmos.DrawWireCube(transform.position + new Vector3(wallCheckDistance, 0f, 0f), wallCheckSize);

            Gizmos.color = new Color(1f, 0.65f, 0.15f, 0.55f);
            Gizmos.DrawWireSphere(transform.position, absorbRange);

            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.55f);
            Gizmos.DrawWireSphere(transform.position, stompShockwaveRadius);
        }
    }
}
