using System;
using UnityEngine;

namespace Tutorial_5
{
    public enum EffectType { None, ILD, ITD }
    
    [RequireComponent(typeof(AudioSource))]
    public class AudioController : MonoBehaviour
    {
        [SerializeField] private EffectType currentEffect = EffectType.None;
        [Tooltip("Use position of player and audio source in the scene instead of the stereo pan slider")]
        [SerializeField] private bool useScene = true;
        
        [Range(0, 1)]
        [SerializeField] private float volume = 1f;
        [Tooltip("Scale factor for distance attenuation (higher = faster falloff, lower = sound travels further)")]
        [SerializeField] private float distanceScale = 1f;
        [Range(-1f, 1f)] 
        [Tooltip("-1 = left, 0 = center, 1 = right")]
        [SerializeField] private float stereoPosition = 0f;
        [Tooltip("Maximum ITD delay in milliseconds")]
        [SerializeField] private float maxHaasDelay = 20f;
        [SerializeField] private int sampleRate = 44100;

        [Header("References")]
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private Transform player;
        
        private AudioSource audioSource;
        
        private float[] leftDelayBuffer;
        private float[] rightDelayBuffer;
        private int bufferSize;
        private int leftWriteIndex = 0;
        private int rightWriteIndex = 0;
        private int leftReadIndex = 0;
        private int rightReadIndex = 0;
        private int leftDelaySamples;
        private int rightDelaySamples;

        void Awake()
        {
            // Ensure AudioSource is assigned
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            // Ensure Collider is a trigger
            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // Initialize delay buffers
            bufferSize = Mathf.CeilToInt((maxHaasDelay / 1000f) * sampleRate);
            leftDelayBuffer = new float[bufferSize];
            rightDelayBuffer = new float[bufferSize];
            
            // main camera is the player
            if (Camera.main != null) player = Camera.main.transform;

            // Initialize audio source (Moved to AFTER buffer initialization to prevent race condition)
            if (audioClip != null)
            {
                audioSource.clip = audioClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        void Start()
        {
            // Start logic moved to Awake to ensure buffers are ready for OnAudioFilterRead
        }

        private void Update()
        {
            if (isCollected) return;

            // 强制调试：每一帧都检查状态
            if (!useScene) {
                if (Time.frameCount % 100 == 0) Debug.LogWarning("[AudioController] useScene is FALSE! Please check Inspector.");
                // 强制开启，防止用户忘记勾选
                useScene = true; 
            }

            if (useScene)
            {
                UpdateParamsFromScene();
            }
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (channels < 2 || leftDelayBuffer == null || rightDelayBuffer == null)
            {
                Debug.LogError("This script requires a stereo audio source with initialized delay buffers.");
                return;
            }
            
            for (var i = 0; i < data.Length; i++)
            {
                // apply volume
                data[i] *= volume;
            }

            // Apply the selected effect
            switch (currentEffect)
            {
                case EffectType.ILD:
                    ApplyILD(data, channels);
                    break;
                case EffectType.ITD:
                    ApplyITD(data, channels);
                    break;
            }
        }
    
        void ApplyILD(float[] data, int channels)
        {
            float leftGain  = Mathf.Clamp01(1f - stereoPosition);
            float rightGain = Mathf.Clamp01(1f + stereoPosition);

            // normalize to [0,1]
            leftGain  *= 0.5f;
            rightGain *= 0.5f;

            for (int i = 0; i < data.Length; i += channels)
            {
                data[i]     *= leftGain;
                data[i + 1] *= rightGain;
            }
        }

        void ApplyITD(float[] data, int channels)
        {
            // Calculate delay in samples
            float delayMs = Mathf.Abs(stereoPosition) * maxHaasDelay * 3f;
            int delaySamples = Mathf.FloorToInt((delayMs / 1000f) * sampleRate);

            for (int i = 0; i < data.Length; i += channels)
            {
                float left = data[i];
                float right = data[i + 1];

                if (stereoPosition > 0f)
                {
                    // sound from right → delay LEFT
                    leftDelayBuffer[leftWriteIndex] = left;
                    left = leftDelayBuffer[leftReadIndex];
                }
                else
                {
                    // sound from left → delay RIGHT
                    rightDelayBuffer[rightWriteIndex] = right;
                    right = rightDelayBuffer[rightReadIndex];
                }

                data[i]     = left;
                data[i + 1] = right;

                leftWriteIndex  = (leftWriteIndex + 1) % bufferSize;
                rightWriteIndex = (rightWriteIndex + 1) % bufferSize;

                leftReadIndex  = (leftWriteIndex  - delaySamples + bufferSize) % bufferSize;
                rightReadIndex = (rightWriteIndex - delaySamples + bufferSize) % bufferSize;
            }
        }

        private void UpdateParamsFromScene()
        {
            if (player == null) {
                // 尝试再次获取 player
                if (Camera.main != null) {
                     player = Camera.main.transform;
                     Debug.Log("[AudioController] Found Player (Camera.main)!");
                }
                else {
                    if (Time.frameCount % 100 == 0) Debug.LogError("[AudioController] Player is NULL and Camera.main is NULL! Cannot calculate audio.");
                    return;
                }
            }

            Vector3 toSource = transform.position - player.position;
            float distance = toSource.magnitude;

            // stereo pan (left / right)
            Vector3 right = player.right;
            stereoPosition = Mathf.Clamp(
                Vector3.Dot(toSource.normalized, right),
                -1f, 1f
            );

            // volume attenuation
            // distanceScale 用于适配微缩场景
            volume = 1f / (1f + distance * distanceScale);
            
        }

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            audioSource.hideFlags = HideFlags.HideInInspector;
        }
        // --- Collision Logic ---
    private bool isCollected = false;
    // Callback event
    public Action<AudioController> OnCollected;
    // Flag for final target
    public bool isFinalTarget = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        // Debug.Log($"[AudioController] Triggered by: {other.gameObject.name} | Tag: {other.tag}");

        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            Debug.Log("[AudioController] Player Detected! Starting collection sequence...");
            // Notify Spawner (or anyone listening)
            OnCollected?.Invoke(this);
            StartCoroutine(CollectSequence(isFinalTarget));
        }
    }

    private System.Collections.IEnumerator CollectSequence(bool isFinal)
    {
        isCollected = true;
        
        // Visual Effect: Spin and Shrink
        // Audio Effect: Distinct sound for final target

        float duration = isFinal ? 2.0f : 0.5f; // Slightly longer for final
        float timer = 0f;
        Vector3 initialScale = transform.localScale;

        // Change audio to "collection" sound
        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f; // Make it 2D so user hears it clearly
            // Final: Lower pitch (0.5) to signify "Power Down" or "Completion"
            // Normal: High pitch (2.0) for "Ding!"
            audioSource.pitch = isFinal ? 0.5f : 2.0f; 
            audioSource.volume = 1.0f;     // Full volume
        }
        
        // Ensure OnAudioFilterRead passes full volume
        this.volume = 1.0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;

            // Shrink
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
            
            // Spin
            transform.Rotate(Vector3.up, 360f * Time.deltaTime * (isFinal ? 1f : 5f)); 

            yield return null;
        }

        if (isFinal) Debug.Log($"[AudioController] FINAL Target collected! Game Over.");
        else Debug.Log($"[AudioController] Player collected {gameObject.name}!");
        
        Destroy(gameObject);
    }
    }
}
