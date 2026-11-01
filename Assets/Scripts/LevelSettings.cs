using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FeelinSlimy
{
    [CreateAssetMenu(fileName = "LevelSettings", menuName = "Feelin' Slimy/Level Settings", order = 0)]
    public class LevelSettings : ScriptableObject
    {
        public static event Action<LevelSettings> Changed;

        [Header("Level Info")]
        [Tooltip("Display name of this level configuration.")]
        public string levelName = "Test Room";
        [Tooltip("Designer notes describing what this level variation is testing.")]
        [TextArea(2, 5)]
        public string description = "";

        public PlayerMovementSettings playerMovement = new PlayerMovementSettings();
        public SlimeTrailSettings slimeTrail = new SlimeTrailSettings();
        public JumpPhysicsSettings jumpPhysics = new JumpPhysicsSettings();
        public AbsorptionSettings absorption = new AbsorptionSettings();
        public StompAbilitySettings angerStomp = new StompAbilitySettings();
        public PlayerHealthSettings playerHealth = new PlayerHealthSettings();
        public PlayerVisualSettings playerVisuals = new PlayerVisualSettings();
        public EnemySettings enemy = new EnemySettings();
        public CreatureSettings creature = new CreatureSettings();
        public ExitSettings exit = new ExitSettings();
        public CameraSettings cameraSettings = new CameraSettings();
        public RoomLayoutSettings layout = new RoomLayoutSettings();

        private void OnValidate()
        {
            NotifyChanged();
        }

        public void NotifyChanged()
        {
            Changed?.Invoke(this);
        }

        public static T[] FindAll<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public void ApplyToScene(bool includeLayout, Action<GameObject> destroy = null, Action<GameObject> registerCreated = null)
        {
            foreach (SlimePlayerController player in FindAll<SlimePlayerController>()) ApplyToPlayer(player);
            foreach (LargeEnemy e in FindAll<LargeEnemy>()) enemy.ApplyTo(e);
            foreach (EmotionCreature c in FindAll<EmotionCreature>()) creature.ApplyTo(c);
            foreach (ExitGoal x in FindAll<ExitGoal>()) exit.ApplyTo(x);

            GameManager gm = GameManager.Instance != null ? GameManager.Instance : Object.FindFirstObjectByType<GameManager>();
            if (gm != null) cameraSettings.ApplyTo(gm);

            if (includeLayout && layout.applyLayout) layout.ApplyToScene(destroy, registerCreated);
        }

        public void ApplyToPlayer(SlimePlayerController player)
        {
            if (player == null) return;
            playerMovement.ApplyTo(player);
            slimeTrail.ApplyTo(player);
            jumpPhysics.ApplyTo(player);
            absorption.ApplyTo(player);
            angerStomp.ApplyTo(player);
            playerHealth.ApplyTo(player);
            playerVisuals.ApplyTo(player);
        }

        public void CaptureFromScene()
        {
            SlimePlayerController player = Object.FindFirstObjectByType<SlimePlayerController>(FindObjectsInactive.Include);
            if (player != null)
            {
                playerMovement.CaptureFrom(player);
                slimeTrail.CaptureFrom(player);
                jumpPhysics.CaptureFrom(player);
                absorption.CaptureFrom(player);
                angerStomp.CaptureFrom(player);
                playerHealth.CaptureFrom(player);
                playerVisuals.CaptureFrom(player);
            }

            LargeEnemy e = Object.FindFirstObjectByType<LargeEnemy>(FindObjectsInactive.Include);
            if (e != null) enemy.CaptureFrom(e);
            EmotionCreature c = Object.FindFirstObjectByType<EmotionCreature>(FindObjectsInactive.Include);
            if (c != null) creature.CaptureFrom(c);
            ExitGoal x = Object.FindFirstObjectByType<ExitGoal>(FindObjectsInactive.Include);
            if (x != null) exit.CaptureFrom(x);
            GameManager gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gm != null) cameraSettings.CaptureFrom(gm);

            layout.CaptureFromScene(player, e, c, x);
        }
    }

    [Serializable]
    public class PlayerMovementSettings
    {
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

        public void ApplyTo(SlimePlayerController p)
        {
            p.moveSpeed = moveSpeed;
            p.jumpForce = jumpForce;
            p.coyoteTime = coyoteTime;
            p.jumpBufferTime = jumpBufferTime;
            p.groundAcceleration = groundAcceleration;
            p.groundDeceleration = groundDeceleration;
            p.airAcceleration = airAcceleration;
            p.turnSpeedMultiplier = turnSpeedMultiplier;
            p.groundCheckOffset = groundCheckOffset;
            p.groundCheckSize = groundCheckSize;
        }

        public void CaptureFrom(SlimePlayerController p)
        {
            moveSpeed = p.moveSpeed;
            jumpForce = p.jumpForce;
            coyoteTime = p.coyoteTime;
            jumpBufferTime = p.jumpBufferTime;
            groundAcceleration = p.groundAcceleration;
            groundDeceleration = p.groundDeceleration;
            airAcceleration = p.airAcceleration;
            turnSpeedMultiplier = p.turnSpeedMultiplier;
            groundCheckOffset = p.groundCheckOffset;
            groundCheckSize = p.groundCheckSize;
        }
    }

    [Serializable]
    public class SlimeTrailSettings
    {
        [Tooltip("Whether the Slime leaves a trail of slimy residue on floors, platforms, and walls.")]
        public bool leaveSlimeResidue = true;
        [Tooltip("Speed multiplier when moving over slimy residue on floors/platforms.")]
        public float residueSpeedMultiplier = 1.48f;
        [Tooltip("Speed multiplier when moving on regular (non-slimy) surfaces. Lower = slower.")]
        public float normalSurfaceSpeedMultiplier = 0.65f;
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
        [Tooltip("Slide speed multiplier on walls coated with slime residue. Lower = slower slide.")]
        public float residueWallSlideMultiplier = 0.15f;
        [Tooltip("Horizontal and vertical launch velocity when jumping off a wall.")]
        public Vector2 wallJumpForce = new Vector2(10.2f, 13.4f);
        [Tooltip("Horizontal offset from the Slime center for left/right wall detection.")]
        public float wallCheckDistance = 0.52f;
        [Tooltip("Size of the box used to detect walls on the left and right.")]
        public Vector2 wallCheckSize = new Vector2(0.18f, 0.62f);

        public void ApplyTo(SlimePlayerController p)
        {
            p.leaveSlimeResidue = leaveSlimeResidue;
            p.residueSpeedMultiplier = residueSpeedMultiplier;
            p.normalSurfaceSpeedMultiplier = normalSurfaceSpeedMultiplier;
            p.residueBuffPersistence = residueBuffPersistence;
            p.residueLifetime = residueLifetime;
            p.residueSpawnSpacing = residueSpawnSpacing;
            p.wallClimbSpeed = wallClimbSpeed;
            p.residueWallClimbMultiplier = residueWallClimbMultiplier;
            p.wallSlideSpeed = wallSlideSpeed;
            p.residueWallSlideMultiplier = residueWallSlideMultiplier;
            p.wallJumpForce = wallJumpForce;
            p.wallCheckDistance = wallCheckDistance;
            p.wallCheckSize = wallCheckSize;
        }

        public void CaptureFrom(SlimePlayerController p)
        {
            leaveSlimeResidue = p.leaveSlimeResidue;
            residueSpeedMultiplier = p.residueSpeedMultiplier;
            normalSurfaceSpeedMultiplier = p.normalSurfaceSpeedMultiplier;
            residueBuffPersistence = p.residueBuffPersistence;
            residueLifetime = p.residueLifetime;
            residueSpawnSpacing = p.residueSpawnSpacing;
            wallClimbSpeed = p.wallClimbSpeed;
            residueWallClimbMultiplier = p.residueWallClimbMultiplier;
            wallSlideSpeed = p.wallSlideSpeed;
            residueWallSlideMultiplier = p.residueWallSlideMultiplier;
            wallJumpForce = p.wallJumpForce;
            wallCheckDistance = p.wallCheckDistance;
            wallCheckSize = p.wallCheckSize;
        }
    }

    [Serializable]
    public class JumpPhysicsSettings
    {
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

        public void ApplyTo(SlimePlayerController p)
        {
            p.baseGravityScale = baseGravityScale;
            p.fallGravityMultiplier = fallGravityMultiplier;
            p.lowJumpGravityMultiplier = lowJumpGravityMultiplier;
            p.apexGravityMultiplier = apexGravityMultiplier;
            p.apexVelocityThreshold = apexVelocityThreshold;
            p.squashSpringStiffness = squashSpringStiffness;
            p.squashSpringDamping = squashSpringDamping;
            p.maxMovementTiltAngle = maxMovementTiltAngle;
        }

        public void CaptureFrom(SlimePlayerController p)
        {
            baseGravityScale = p.baseGravityScale;
            fallGravityMultiplier = p.fallGravityMultiplier;
            lowJumpGravityMultiplier = p.lowJumpGravityMultiplier;
            apexGravityMultiplier = p.apexGravityMultiplier;
            apexVelocityThreshold = p.apexVelocityThreshold;
            squashSpringStiffness = p.squashSpringStiffness;
            squashSpringDamping = p.squashSpringDamping;
            maxMovementTiltAngle = p.maxMovementTiltAngle;
        }
    }

    [Serializable]
    public class AbsorptionSettings
    {
        [Tooltip("Maximum distance from which the Slime can pull and absorb the Emotion Creature.")]
        public float absorbRange = 3.2f;
        [Tooltip("How long (in seconds) the player must hold E to finish pulling and absorbing.")]
        public float absorbHoldDuration = 0.65f;
        [Tooltip("Speed at which the Emotion Creature is pulled toward the Slime while holding E.")]
        public float absorbPullSpeed = 4.8f;

        public void ApplyTo(SlimePlayerController p)
        {
            p.absorbRange = absorbRange;
            p.absorbHoldDuration = absorbHoldDuration;
            p.absorbPullSpeed = absorbPullSpeed;
        }

        public void CaptureFrom(SlimePlayerController p)
        {
            absorbRange = p.absorbRange;
            absorbHoldDuration = p.absorbHoldDuration;
            absorbPullSpeed = p.absorbPullSpeed;
        }
    }

    [Serializable]
    public class StompAbilitySettings
    {
        [Tooltip("If enabled, the Slime starts the level (and every restart) with Anger Stomp already unlocked.")]
        public bool startWithAngerStomp = false;
        [Tooltip("Downward slam velocity during the Anger Stomp.")]
        public float stompDownwardSpeed = 22f;
        [Tooltip("Radius of the shockwave on Stomp impact that crushes nearby Large Enemies.")]
        public float stompShockwaveRadius = 2.6f;
        [Tooltip("Cooldown time (in seconds) between Anger Stomps.")]
        public float stompCooldown = 0.45f;
        [Tooltip("Upward bounce velocity when the Slime crushes an enemy directly.")]
        public float stompKillBounceForce = 10.5f;

        public void ApplyTo(SlimePlayerController p)
        {
            p.startWithAngerStomp = startWithAngerStomp;
            if (startWithAngerStomp || !Application.isPlaying) p.hasAngerStomp = startWithAngerStomp;
            p.stompDownwardSpeed = stompDownwardSpeed;
            p.stompShockwaveRadius = stompShockwaveRadius;
            p.stompCooldown = stompCooldown;
            p.stompKillBounceForce = stompKillBounceForce;
        }

        public void CaptureFrom(SlimePlayerController p)
        {
            startWithAngerStomp = p.startWithAngerStomp;
            stompDownwardSpeed = p.stompDownwardSpeed;
            stompShockwaveRadius = p.stompShockwaveRadius;
            stompCooldown = p.stompCooldown;
            stompKillBounceForce = p.stompKillBounceForce;
        }
    }

    [Serializable]
    public class PlayerHealthSettings
    {
        [Tooltip("Maximum health points (hearts) for the Slime.")]
        [Min(1)]
        public int maxHealth = 3;
        [Tooltip("Brief invulnerability duration (in seconds) after taking damage.")]
        public float invincibilityDuration = 1.1f;
        [Tooltip("Y position below which the Slime is considered to have fallen out of the room.")]
        public float fallLimitY = -8.5f;

        public void ApplyTo(SlimePlayerController p)
        {
            p.maxHealth = maxHealth;
            p.invincibilityDuration = invincibilityDuration;
            p.fallLimitY = fallLimitY;
        }

        public void CaptureFrom(SlimePlayerController p)
        {
            maxHealth = p.maxHealth;
            invincibilityDuration = p.invincibilityDuration;
            fallLimitY = p.fallLimitY;
        }
    }

    [Serializable]
    public class PlayerVisualSettings
    {
        [Tooltip("Default Slime color before absorbing an emotion.")]
        public Color calmSlimeColor = new Color(0.28f, 0.88f, 0.95f, 1f);
        [Tooltip("Slime color after absorbing the Anger emotion.")]
        public Color angerSlimeColor = new Color(1f, 0.28f, 0.18f, 1f);
        [Tooltip("Optional custom sprite for the calm Slime.")]
        public Sprite customCalmSprite;
        [Tooltip("Optional custom sprite for the angry powered-up Slime.")]
        public Sprite customAngrySprite;

        public void ApplyTo(SlimePlayerController p)
        {
            p.calmSlimeColor = calmSlimeColor;
            p.angerSlimeColor = angerSlimeColor;
            p.customCalmSprite = customCalmSprite;
            p.customAngrySprite = customAngrySprite;
            p.ApplyVisualState();
        }

        public void CaptureFrom(SlimePlayerController p)
        {
            calmSlimeColor = p.calmSlimeColor;
            angerSlimeColor = p.angerSlimeColor;
            customCalmSprite = p.customCalmSprite;
            customAngrySprite = p.customAngrySprite;
        }
    }

    [Serializable]
    public class EnemySettings
    {
        [Header("Identity & Visuals")]
        [Tooltip("Display name of the dangerous large enemy.")]
        public string enemyName = "Gloom Brute";
        [Tooltip("Primary color tint of the large enemy.")]
        public Color enemyColor = new Color(0.52f, 0.22f, 0.68f, 1f);
        [Tooltip("Optional custom sprite for the large enemy.")]
        public Sprite customSprite;

        [Header("Patrol")]
        [Tooltip("How far left and right from its start position the enemy patrols.")]
        public float patrolDistance = 3.5f;
        [Tooltip("Horizontal movement speed while patrolling.")]
        public float patrolSpeed = 2.4f;

        [Header("Chase Ability")]
        [Tooltip("If enabled, the enemy charges toward the Slime when it comes close (stays inside its patrol zone).")]
        public bool chasePlayer = false;
        [Tooltip("Horizontal distance at which the enemy notices and chases the Slime.")]
        public float chaseRange = 5f;
        [Tooltip("Maximum vertical distance at which the enemy still notices the Slime.")]
        public float chaseVerticalTolerance = 2.5f;
        [Tooltip("Horizontal movement speed while chasing.")]
        public float chaseSpeed = 4f;

        [Header("Contact Attack")]
        [Tooltip("Damage dealt to the Slime on normal contact.")]
        public int contactDamage = 1;
        [Tooltip("Horizontal knockback force applied to the Slime on normal contact.")]
        public float knockbackForceX = 7.5f;
        [Tooltip("Upward knockback force applied to the Slime on normal contact.")]
        public float knockbackForceY = 5.5f;

        [Header("Weaknesses")]
        [Tooltip("If enabled, landing directly on the enemy with the Anger Stomp defeats it. Otherwise the Slime bounces off harmlessly.")]
        public bool defeatedByDirectStomp = true;
        [Tooltip("If enabled, the Anger Stomp landing shockwave defeats the enemy when it is inside the radius.")]
        public bool defeatedByShockwave = true;

        public void ApplyTo(LargeEnemy e)
        {
            e.enemyName = enemyName;
            e.enemyColor = enemyColor;
            e.customSprite = customSprite;
            e.patrolDistance = patrolDistance;
            e.patrolSpeed = patrolSpeed;
            e.chasePlayer = chasePlayer;
            e.chaseRange = chaseRange;
            e.chaseVerticalTolerance = chaseVerticalTolerance;
            e.chaseSpeed = chaseSpeed;
            e.contactDamage = contactDamage;
            e.knockbackForceX = knockbackForceX;
            e.knockbackForceY = knockbackForceY;
            e.defeatedByDirectStomp = defeatedByDirectStomp;
            e.defeatedByShockwave = defeatedByShockwave;
            if (!e.IsDefeated) e.ApplyVisuals();
        }

        public void CaptureFrom(LargeEnemy e)
        {
            enemyName = e.enemyName;
            enemyColor = e.enemyColor;
            customSprite = e.customSprite;
            patrolDistance = e.patrolDistance;
            patrolSpeed = e.patrolSpeed;
            chasePlayer = e.chasePlayer;
            chaseRange = e.chaseRange;
            chaseVerticalTolerance = e.chaseVerticalTolerance;
            chaseSpeed = e.chaseSpeed;
            contactDamage = e.contactDamage;
            knockbackForceX = e.knockbackForceX;
            knockbackForceY = e.knockbackForceY;
            defeatedByDirectStomp = e.defeatedByDirectStomp;
            defeatedByShockwave = e.defeatedByShockwave;
        }
    }

    [Serializable]
    public class CreatureSettings
    {
        [Tooltip("Name of the emotion this small creature represents.")]
        public string emotionName = "Anger";
        [Tooltip("Name of the ability granted to the Slime when absorbed.")]
        public string grantedAbilityName = "Anger Stomp";
        [Tooltip("Color tint of the small emotion creature.")]
        public Color creatureColor = new Color(1f, 0.35f, 0.18f, 1f);
        [Tooltip("Optional custom sprite. Leave empty to use the default Anger Spark sprite.")]
        public Sprite customSprite;
        [Tooltip("How far left and right the creature gently wanders from its starting spot.")]
        public float wanderDistance = 1.2f;
        [Tooltip("How fast the creature moves horizontally while wandering.")]
        public float wanderSpeed = 1.4f;
        [Tooltip("Height of the gentle hopping/bobbing motion.")]
        public float hopHeight = 0.28f;
        [Tooltip("Speed of the hopping/bobbing motion.")]
        public float hopFrequency = 4.5f;

        public void ApplyTo(EmotionCreature c)
        {
            c.emotionName = emotionName;
            c.grantedAbilityName = grantedAbilityName;
            c.creatureColor = creatureColor;
            c.customSprite = customSprite;
            c.wanderDistance = wanderDistance;
            c.wanderSpeed = wanderSpeed;
            c.hopHeight = hopHeight;
            c.hopFrequency = hopFrequency;
            c.ApplyVisuals();
        }

        public void CaptureFrom(EmotionCreature c)
        {
            emotionName = c.emotionName;
            grantedAbilityName = c.grantedAbilityName;
            creatureColor = c.creatureColor;
            customSprite = c.customSprite;
            wanderDistance = c.wanderDistance;
            wanderSpeed = c.wanderSpeed;
            hopHeight = c.hopHeight;
            hopFrequency = c.hopFrequency;
        }
    }

    [Serializable]
    public class ExitSettings
    {
        [Tooltip("Color of the glowing exit portal.")]
        public Color portalColor = new Color(0.25f, 0.95f, 0.65f, 1f);
        [Tooltip("If enabled, the Large Enemy must be defeated before the exit activates.")]
        public bool requireEnemyDefeated = false;
        [Tooltip("Optional custom sprite for the exit goal.")]
        public Sprite customSprite;

        public void ApplyTo(ExitGoal x)
        {
            x.portalColor = portalColor;
            x.requireEnemyDefeated = requireEnemyDefeated;
            x.customSprite = customSprite;
            x.ApplyVisuals();
        }

        public void CaptureFrom(ExitGoal x)
        {
            portalColor = x.portalColor;
            requireEnemyDefeated = x.requireEnemyDefeated;
            customSprite = x.customSprite;
        }
    }

    [Serializable]
    public class CameraSettings
    {
        [Tooltip("Whether the camera smoothly tracks the Slime horizontally.")]
        public bool smoothFollowPlayer = true;
        [Tooltip("Minimum and maximum X coordinates for the camera in the room.")]
        public Vector2 cameraBoundsX = new Vector2(-2.5f, 4.5f);
        [Tooltip("Orthographic size (zoom) of the camera. Bigger = see more of the room.")]
        public float cameraSize = 6.2f;
        [Tooltip("Vertical position of the camera.")]
        public float cameraHeight = 0.8f;
        [Tooltip("Background color behind the room.")]
        public Color backgroundColor = new Color(0.11f, 0.14f, 0.21f, 1f);

        public void ApplyTo(GameManager gm)
        {
            gm.smoothFollowPlayer = smoothFollowPlayer;
            gm.cameraBoundsX = cameraBoundsX;
            gm.cameraSize = cameraSize;
            gm.cameraHeight = cameraHeight;
            gm.backgroundColor = backgroundColor;
            gm.ApplyCameraLook();
        }

        public void CaptureFrom(GameManager gm)
        {
            smoothFollowPlayer = gm.smoothFollowPlayer;
            cameraBoundsX = gm.cameraBoundsX;
            cameraSize = gm.cameraSize;
            cameraHeight = gm.cameraHeight;
            backgroundColor = gm.backgroundColor;
        }
    }

    [Serializable]
    public class PlatformLayout
    {
        public string name = "Platform";
        public Vector2 position;
        public Vector2 size = new Vector2(3f, 0.65f);
        public Color color = new Color(0.32f, 0.44f, 0.56f, 1f);

        public PlatformLayout() { }

        public PlatformLayout(string name, Vector2 position, Vector2 size, Color color)
        {
            this.name = name;
            this.position = position;
            this.size = size;
            this.color = color;
        }
    }

    [Serializable]
    public class RoomLayoutSettings
    {
        public const string RoomRootName = "TestRoom_Environment";

        [Tooltip("When enabled, applying moves the player, creature, enemy and exit and rebuilds all room platforms from the list below.")]
        public bool applyLayout = true;

        [Header("Spawn Points")]
        public Vector2 playerSpawn = new Vector2(-8.2f, -2.8f);
        public Vector2 creaturePosition = new Vector2(-8.2f, 1.95f);
        public Vector2 enemyPosition = new Vector2(2.4f, -2.75f);
        public Vector2 exitPosition = new Vector2(9.2f, -2.65f);

        [Header("Platforms")]
        [Tooltip("Optional sprite used for every platform. Leave empty for the default block sprite.")]
        public Sprite platformSprite;
        public List<PlatformLayout> platforms = CreateDefaultPlatforms();

        public static List<PlatformLayout> CreateDefaultPlatforms()
        {
            Color groundCol = new Color(0.24f, 0.31f, 0.42f, 1f);
            Color platformCol = new Color(0.32f, 0.44f, 0.56f, 1f);
            Color obstacleCol = new Color(0.42f, 0.34f, 0.46f, 1f);
            return new List<PlatformLayout>
            {
                new PlatformLayout("MainGround_Floor", new Vector2(0f, -4.5f), new Vector2(24f, 1.4f), groundCol),
                new PlatformLayout("LeftBoundary_Wall", new Vector2(-11.5f, 1.0f), new Vector2(1.2f, 12.5f), groundCol),
                new PlatformLayout("RightBoundary_Wall", new Vector2(11.5f, 1.0f), new Vector2(1.2f, 12.5f), groundCol),
                new PlatformLayout("CenterDivider_Obstacle", new Vector2(-2.8f, -2.9f), new Vector2(1.4f, 1.8f), obstacleCol),
                new PlatformLayout("SteppingPlatform_Low", new Vector2(-5.2f, -1.5f), new Vector2(3.2f, 0.65f), platformCol),
                new PlatformLayout("EmotionPlatform_UpperLeft", new Vector2(-8.2f, 0.9f), new Vector2(4.2f, 0.65f), platformCol),
                new PlatformLayout("VantagePlatform_StompDrop", new Vector2(1.6f, 0.6f), new Vector2(3.8f, 0.65f), platformCol),
            };
        }

        public void ApplyToScene(Action<GameObject> destroy = null, Action<GameObject> registerCreated = null)
        {
            if (destroy == null)
            {
                destroy = go =>
                {
                    if (Application.isPlaying) Object.Destroy(go);
                    else Object.DestroyImmediate(go);
                };
            }

            GameManager gm = GameManager.Instance != null ? GameManager.Instance : Object.FindFirstObjectByType<GameManager>();
            SlimePlayerController player = gm != null && gm.player != null ? gm.player : Object.FindFirstObjectByType<SlimePlayerController>(FindObjectsInactive.Include);
            EmotionCreature creature = gm != null && gm.emotionCreature != null ? gm.emotionCreature : Object.FindFirstObjectByType<EmotionCreature>(FindObjectsInactive.Include);
            LargeEnemy enemy = gm != null && gm.largeEnemy != null ? gm.largeEnemy : Object.FindFirstObjectByType<LargeEnemy>(FindObjectsInactive.Include);
            ExitGoal exit = gm != null && gm.exitGoal != null ? gm.exitGoal : Object.FindFirstObjectByType<ExitGoal>(FindObjectsInactive.Include);

            if (player != null) player.SetSpawnPosition(playerSpawn);
            if (creature != null) creature.SetSpawnPosition(creaturePosition);
            if (enemy != null) enemy.SetSpawnPosition(enemyPosition);
            if (exit != null) exit.transform.position = new Vector3(exitPosition.x, exitPosition.y, exit.transform.position.z);

            GameObject root = GameObject.Find(RoomRootName);
            if (root == null)
            {
                root = new GameObject(RoomRootName);
                registerCreated?.Invoke(root);
            }

            foreach (RoomPlatform old in LevelSettings.FindAll<RoomPlatform>())
            {
                destroy(old.gameObject);
            }

            foreach (PlatformLayout p in platforms)
            {
                RoomPlatform rp = FeelinSlimyRoomBuilder.CreatePlatform(root.transform, p.name, p.position, new Vector3(p.size.x, p.size.y, 1f), p.color, platformSprite);
                registerCreated?.Invoke(rp.gameObject);
            }
        }

        public void CaptureFromScene(SlimePlayerController player, LargeEnemy enemy, EmotionCreature creature, ExitGoal exit)
        {
            if (player != null) playerSpawn = player.transform.position;
            if (enemy != null) enemyPosition = enemy.transform.position;
            if (creature != null) creaturePosition = creature.transform.position;
            if (exit != null) exitPosition = exit.transform.position;

            RoomPlatform[] scenePlatforms = Object.FindObjectsByType<RoomPlatform>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            if (scenePlatforms.Length == 0) return;

            platforms = new List<PlatformLayout>();
            platformSprite = null;
            foreach (RoomPlatform rp in scenePlatforms)
            {
                Vector3 scale = rp.transform.lossyScale;
                platforms.Add(new PlatformLayout(rp.name, rp.transform.position, new Vector2(scale.x, scale.y), rp.platformColor));
                if (platformSprite == null) platformSprite = rp.customSprite;
            }
        }
    }
}
