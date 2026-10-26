using UnityEngine;

namespace FeelinSlimy
{
    [RequireComponent(typeof(AudioSource))]
    public class RetroSoundEffects : MonoBehaviour
    {
        public static RetroSoundEffects Instance { get; private set; }

        [Header("Volume Settings")]
        [Tooltip("Master volume for all game sound effects.")]
        [Range(0f, 1f)]
        public float masterVolume = 0.6f;

        [Header("Optional Custom Audio Clips (Drag & Drop to Override)")]
        [Tooltip("Optional custom sound for Slime jumping.")]
        public AudioClip customJumpClip;
        [Tooltip("Optional custom sound while pulling the emotion creature.")]
        public AudioClip customAbsorbPullClip;
        [Tooltip("Optional custom sound when absorbing the emotion creature and gaining power.")]
        public AudioClip customPowerAcquireClip;
        [Tooltip("Optional custom sound when initiating the Anger Stomp in mid-air.")]
        public AudioClip customStompLaunchClip;
        [Tooltip("Optional custom sound when the Anger Stomp slams into the ground.")]
        public AudioClip customStompImpactClip;
        [Tooltip("Optional custom sound when the Large Enemy is crushed by the Stomp.")]
        public AudioClip customEnemyDefeatClip;
        [Tooltip("Optional custom sound when the Slime takes damage.")]
        public AudioClip customPlayerHurtClip;
        [Tooltip("Optional custom sound when reaching the Exit Portal.")]
        public AudioClip customVictoryClip;
        [Tooltip("Optional custom sound when losing all health or falling.")]
        public AudioClip customFailureClip;

        private AudioSource _sfxSource;
        private AudioSource _loopSource;

        private AudioClip _genJump;
        private AudioClip _genPull;
        private AudioClip _genAcquire;
        private AudioClip _genStompLaunch;
        private AudioClip _genStompImpact;
        private AudioClip _genEnemyDefeat;
        private AudioClip _genHurt;
        private AudioClip _genVictory;
        private AudioClip _genFailure;
        private AudioClip _genWallJump;
        private AudioClip _genLandSplat;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _sfxSource = GetComponent<AudioSource>();
            _sfxSource.playOnAwake = false;

            GameObject loopObj = new GameObject("PullLoopAudio");
            loopObj.transform.SetParent(transform, false);
            _loopSource = loopObj.AddComponent<AudioSource>();
            _loopSource.playOnAwake = false;
            _loopSource.loop = true;

            GenerateFallbackClips();
        }

        private void GenerateFallbackClips()
        {
            _genJump = CreateSweepClip("SynthJump", 0.16f, 240f, 560f, false);
            _genPull = CreateSweepClip("SynthPull", 0.25f, 300f, 460f, false);
            _genAcquire = CreateArpeggioClip("SynthAcquire", new float[] { 330f, 440f, 554.37f, 659.25f, 880f }, 0.08f);
            _genStompLaunch = CreateSweepClip("SynthStompLaunch", 0.18f, 520f, 160f, false);
            _genStompImpact = CreateSweepClip("SynthStompImpact", 0.32f, 150f, 42f, true);
            _genEnemyDefeat = CreateArpeggioClip("SynthEnemyDefeat", new float[] { 180f, 260f, 380f, 520f }, 0.07f);
            _genHurt = CreateSweepClip("SynthHurt", 0.24f, 280f, 95f, true);
            _genVictory = CreateArpeggioClip("SynthVictory", new float[] { 261.63f, 329.63f, 392.00f, 523.25f, 659.25f }, 0.1f);
            _genFailure = CreateArpeggioClip("SynthFailure", new float[] { 300f, 260f, 210f, 145f }, 0.12f);
            _genWallJump = CreateSweepClip("SynthWallJump", 0.14f, 310f, 680f, false);
            _genLandSplat = CreateSweepClip("SynthLandSplat", 0.11f, 190f, 85f, true);
        }

        public void PlayJump() => PlayClip(customJumpClip != null ? customJumpClip : _genJump, 0.75f);
        public void PlayWallJump() => PlayClip(customJumpClip != null ? customJumpClip : _genWallJump, 0.8f);
        public void PlayLandSplat(float intensity = 0.6f) => PlayClip(_genLandSplat, Mathf.Clamp(intensity * 0.65f, 0.2f, 0.75f));
        public void PlayPowerAcquire() => PlayClip(customPowerAcquireClip != null ? customPowerAcquireClip : _genAcquire, 1f);
        public void PlayStompLaunch() => PlayClip(customStompLaunchClip != null ? customStompLaunchClip : _genStompLaunch, 0.85f);
        public void PlayStompImpact() => PlayClip(customStompImpactClip != null ? customStompImpactClip : _genStompImpact, 1f);
        public void PlayEnemyDefeat() => PlayClip(customEnemyDefeatClip != null ? customEnemyDefeatClip : _genEnemyDefeat, 1f);
        public void PlayPlayerHurt() => PlayClip(customPlayerHurtClip != null ? customPlayerHurtClip : _genHurt, 0.95f);
        public void PlayVictory() => PlayClip(customVictoryClip != null ? customVictoryClip : _genVictory, 1f);
        public void PlayFailure() => PlayClip(customFailureClip != null ? customFailureClip : _genFailure, 0.95f);

        public void UpdateAbsorbPullSound(bool active, float progress01)
        {
            if (_loopSource == null) return;
            if (!active)
            {
                if (_loopSource.isPlaying) _loopSource.Stop();
                return;
            }

            AudioClip clipToUse = customAbsorbPullClip != null ? customAbsorbPullClip : _genPull;
            if (_loopSource.clip != clipToUse)
            {
                _loopSource.clip = clipToUse;
            }
            _loopSource.volume = masterVolume * 0.55f;
            _loopSource.pitch = Mathf.Lerp(0.85f, 1.65f, Mathf.Clamp01(progress01));
            if (!_loopSource.isPlaying)
            {
                _loopSource.Play();
            }
        }

        private void PlayClip(AudioClip clip, float volumeScale)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip, masterVolume * volumeScale);
        }

        private static AudioClip CreateSweepClip(string clipName, float duration, float startFreq, float endFreq, bool addNoise)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] samples = new float[sampleCount];
            float phase = 0f;
            System.Random rng = new System.Random(clipName.GetHashCode());

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += (2f * Mathf.PI * freq) / sampleRate;
                float tone = Mathf.Sin(phase) * 0.7f + Mathf.Sign(Mathf.Sin(phase)) * 0.15f;
                if (addNoise)
                {
                    float noise = ((float)rng.NextDouble() * 2f - 1f) * 0.45f;
                    tone = Mathf.Lerp(tone, noise, 0.45f);
                }
                float envelope = Mathf.Sin(Mathf.Clamp01(1f - t) * Mathf.PI * 0.5f);
                samples[i] = Mathf.Clamp(tone * envelope * 0.5f, -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static AudioClip CreateArpeggioClip(string clipName, float[] notes, float noteDuration)
        {
            int sampleRate = 44100;
            int samplesPerNote = Mathf.Max(1, Mathf.RoundToInt(sampleRate * noteDuration));
            int totalSamples = samplesPerNote * notes.Length;
            float[] samples = new float[totalSamples];
            float phase = 0f;

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                for (int i = 0; i < samplesPerNote; i++)
                {
                    float localT = i / (float)samplesPerNote;
                    phase += (2f * Mathf.PI * freq) / sampleRate;
                    float wave = Mathf.Sin(phase) * 0.75f + Mathf.Sin(phase * 2f) * 0.25f;
                    float env = 1f - localT * 0.65f;
                    samples[n * samplesPerNote + i] = Mathf.Clamp(wave * env * 0.45f, -1f, 1f);
                }
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
