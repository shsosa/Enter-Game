using System;
using UnityEngine;

namespace FeelinSlimy
{
    public enum SlimeAnim
    {
        Idle,
        Move,
        Jump,
        Rise,
        Fall,
        Land,
        StompWindup,
        StompFall,
        StompImpact,
        Hurt,
        Absorb,
        Powerup,
        Death
    }

    [Serializable]
    public class SlimeClip
    {
        public Sprite[] frames = new Sprite[0];
        public float fps = 12f;
        public bool loop = true;

        public bool IsEmpty => frames == null || frames.Length == 0;
        public float Duration => IsEmpty ? 0f : frames.Length / Mathf.Max(1f, fps);
    }

    [Serializable]
    public class SlimeAnimationSet
    {
        public SlimeClip idle = new SlimeClip();
        public SlimeClip move = new SlimeClip();
        public SlimeClip jump = new SlimeClip();
        public SlimeClip rise = new SlimeClip();
        public SlimeClip fall = new SlimeClip();
        public SlimeClip land = new SlimeClip();
        public SlimeClip stompWindup = new SlimeClip();
        public SlimeClip stompFall = new SlimeClip();
        public SlimeClip stompImpact = new SlimeClip();
        public SlimeClip hurt = new SlimeClip();
        public SlimeClip absorb = new SlimeClip();
        public SlimeClip powerup = new SlimeClip();
        public SlimeClip death = new SlimeClip();

        public SlimeClip Get(SlimeAnim anim) => anim switch
        {
            SlimeAnim.Move => move,
            SlimeAnim.Jump => jump,
            SlimeAnim.Rise => rise,
            SlimeAnim.Fall => fall,
            SlimeAnim.Land => land,
            SlimeAnim.StompWindup => stompWindup,
            SlimeAnim.StompFall => stompFall,
            SlimeAnim.StompImpact => stompImpact,
            SlimeAnim.Hurt => hurt,
            SlimeAnim.Absorb => absorb,
            SlimeAnim.Powerup => powerup,
            SlimeAnim.Death => death,
            _ => idle
        };
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(SlimePlayerController))]
    [DefaultExecutionOrder(100)]
    public class SlimeSpriteAnimator : MonoBehaviour
    {
        [Tooltip("Frames for the calm (blue) Slime. Filled automatically from Assets/Art/Slime/Calm.")]
        public SlimeAnimationSet calm = new SlimeAnimationSet();
        [Tooltip("Frames for the angry (red) Slime. Filled automatically from Assets/Art/Slime/Angry.")]
        public SlimeAnimationSet angry = new SlimeAnimationSet();

        [Header("Tuning")]
        [Tooltip("Global playback speed multiplier for every animation.")]
        public float playbackSpeed = 1f;
        [Tooltip("Horizontal speed above which the move animation plays instead of idle.")]
        public float moveSpeedThreshold = 0.6f;
        [Tooltip("Upward speed above which the rise animation plays instead of fall.")]
        public float riseSpeedThreshold = 0.5f;
        [Tooltip("Landings softer than this (0-1) skip the biggest splat frame.")]
        public float softLandingThreshold = 0.35f;

        public SlimeAnim Current { get; private set; } = SlimeAnim.Idle;

        private SlimePlayerController _player;
        private SpriteRenderer _renderer;
        private Rigidbody2D _rb;
        private bool _playingOneShot;
        private float _time;

        private SlimeAnimationSet ActiveSet => _player.hasAngerStomp ? angry : calm;

        private bool IsDead => GameManager.Instance != null
            && GameManager.Instance.CurrentState == GameManager.GameState.Failure
            && _player.CurrentHealth <= 0;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            _player.Jumped += OnJumped;
            _player.Landed += OnLanded;
            _player.StompStarted += OnStompStarted;
            _player.StompImpacted += OnStompImpacted;
            _player.Hurt += OnHurt;
            _player.PowerAcquired += OnPowerAcquired;
            _player.PlayerReset += OnPlayerReset;
        }

        private void OnDisable()
        {
            _player.Jumped -= OnJumped;
            _player.Landed -= OnLanded;
            _player.StompStarted -= OnStompStarted;
            _player.StompImpacted -= OnStompImpacted;
            _player.Hurt -= OnHurt;
            _player.PowerAcquired -= OnPowerAcquired;
            _player.PlayerReset -= OnPlayerReset;
        }

        private void CacheComponents()
        {
            if (_player == null) _player = GetComponent<SlimePlayerController>();
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        }

        private void OnJumped() => PlayOnce(SlimeAnim.Jump, 2);
        private void OnLanded(float intensity) => PlayOnce(SlimeAnim.Land, intensity < softLandingThreshold ? 1 : 0);
        private void OnStompStarted() => PlayOnce(SlimeAnim.StompWindup);
        private void OnStompImpacted() => PlayOnce(SlimeAnim.StompImpact);
        private void OnHurt() => PlayOnce(SlimeAnim.Hurt);
        private void OnPowerAcquired() => PlayOnce(SlimeAnim.Powerup);

        private void OnPlayerReset()
        {
            _playingOneShot = false;
            Current = SlimeAnim.Idle;
            _time = 0f;
            ApplyFrame();
        }

        public void PlayOnce(SlimeAnim anim, int startFrame = 0)
        {
            if (_playingOneShot && Priority(anim) < Priority(Current)) return;
            SlimeClip clip = ActiveSet.Get(anim);
            if (clip.IsEmpty) return;

            _playingOneShot = true;
            Current = anim;
            _time = startFrame / Mathf.Max(1f, clip.fps);
        }

        private static int Priority(SlimeAnim anim) => anim switch
        {
            SlimeAnim.Death => 5,
            SlimeAnim.Powerup => 4,
            SlimeAnim.Hurt or SlimeAnim.StompImpact => 3,
            SlimeAnim.StompWindup => 2,
            SlimeAnim.Land or SlimeAnim.Jump => 1,
            _ => 0
        };

        private SlimeAnim PickLocomotion()
        {
            if (_player.IsStomping) return SlimeAnim.StompFall;

            Vector2 vel = _rb != null ? _rb.linearVelocity : Vector2.zero;
            if (_player.CanReachCreature && _player.CurrentAbsorbProgress01 > 0f) return SlimeAnim.Absorb;
            if (!_player.IsGrounded) return vel.y > riseSpeedThreshold ? SlimeAnim.Rise : SlimeAnim.Fall;
            return Mathf.Abs(vel.x) > moveSpeedThreshold ? SlimeAnim.Move : SlimeAnim.Idle;
        }

        private void LateUpdate()
        {
            if (IsDead && !(_playingOneShot && Current == SlimeAnim.Death))
            {
                _playingOneShot = false;
                PlayOnce(SlimeAnim.Death);
            }

            float rate = playbackSpeed;
            if (Current == SlimeAnim.Move && _rb != null)
            {
                rate *= Mathf.Lerp(0.6f, 1.5f, Mathf.Clamp01(Mathf.Abs(_rb.linearVelocity.x) / Mathf.Max(0.1f, _player.moveSpeed)));
            }
            _time += Time.deltaTime * rate;

            SlimeClip clip = ActiveSet.Get(Current);
            if (_playingOneShot && Current != SlimeAnim.Death && (clip.IsEmpty || (!clip.loop && _time >= clip.Duration)))
            {
                _playingOneShot = false;
            }

            if (!_playingOneShot)
            {
                SlimeAnim next = PickLocomotion();
                if (next != Current)
                {
                    Current = next;
                    _time = 0f;
                }
            }

            if (Current == SlimeAnim.Absorb && _player.targetCreature != null)
            {
                _renderer.flipX = _player.targetCreature.transform.position.x < transform.position.x;
            }

            ApplyFrame();
        }

        private void ApplyFrame()
        {
            SlimeClip clip = ActiveSet.Get(Current);
            if (clip.IsEmpty) clip = ActiveSet.idle;
            if (clip.IsEmpty || _renderer == null) return;

            int frame = Mathf.FloorToInt(_time * clip.fps);
            frame = clip.loop ? frame % clip.frames.Length : Mathf.Min(frame, clip.frames.Length - 1);
            if (clip.frames[frame] != null) _renderer.sprite = clip.frames[frame];
        }
    }
}
