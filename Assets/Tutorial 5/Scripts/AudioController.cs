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
        [SerializeField] private bool useScene;
        
        [Range(0, 1)]
        [SerializeField] private float volume = 1f;
        [Range(0, 1)] [Tooltip("0 is left, 1 is right")]
        [SerializeField] private float stereoPosition = 0.5f;
        [Tooltip("Maximum ITD delay in milliseconds")]
        [SerializeField] private float maxHaasDelay = 20f;
        [SerializeField] private int sampleRate = 44100;

        [Header("References")]
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private Transform player;
        [SerializeField] private Transform leftEar;
        [SerializeField] private Transform rightEar;
        
        [Header("Volume Settings")]
        [SerializeField] private float maxDistance = 100f;
        [SerializeField] private float minDistance = 1f;
        
        [Header("Stereo Settings")]
        [Range(1f, 5f)]
        [Tooltip("Higher values make the stereo effect more pronounced")]
        [SerializeField] private float stereoSensitivity = 2f;
        
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

        void Start()
        {
            // Initialize audio source
            audioSource.clip = audioClip;
            audioSource.loop = true;
            audioSource.Play();

            // Initialize delay buffers
            bufferSize = Mathf.CeilToInt((maxHaasDelay / 1000f) * sampleRate);
            leftDelayBuffer = new float[bufferSize];
            rightDelayBuffer = new float[bufferSize];
            
            // main camera is the player
            player = Camera.main.transform;
            
            // Find left and right ear under the player/camera
            if (leftEar == null)
                leftEar = player.Find("LeftEar");
            if (rightEar == null)
                rightEar = player.Find("RightEar");
        }

        private void Update()
        {
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
    
        private void ApplyILD(float[] data, int channels)
        {
            // Calculate left and right gain based on stereo position
            // stereoPosition: 0 = full left, 0.5 = center, 1 = full right
            
            // Linear panning law: 
            // When stereoPosition = 0 (left): leftGain = 1, rightGain = 0
            // When stereoPosition = 0.5 (center): leftGain = 0.5, rightGain = 0.5
            // When stereoPosition = 1 (right): leftGain = 0, rightGain = 1
            float rightGain = stereoPosition;
            float leftGain = 1f - stereoPosition;
            
            // Apply gain to each channel in the interleaved buffer
            // Buffer format: [Left1, Right1, Left2, Right2, Left3, Right3, ...]
            for (int i = 0; i < data.Length; i += channels)
            {
                data[i] *= leftGain;         // Left channel (even indices: 0, 2, 4, ...)
                data[i + 1] *= rightGain;    // Right channel (odd indices: 1, 3, 5, ...)
            }
        }

     
        private void ApplyITD(float[] data, int channels)
        {
            // 1. Calculate delay in milliseconds based on stereo position
            // stereoPosition: 0 = delay right channel, 0.5 = no delay, 1 = delay left channel
            float delayMs;
            bool delayLeft;
            
            if (stereoPosition < 0.5f)
            {
                // Sound is on the left, delay the right channel
                delayMs = (0.5f - stereoPosition) * 2f * maxHaasDelay; // Map [0, 0.5] to [maxDelay, 0]
                delayLeft = false;
            }
            else if (stereoPosition > 0.5f)
            {
                // Sound is on the right, delay the left channel
                delayMs = (stereoPosition - 0.5f) * 2f * maxHaasDelay; // Map [0.5, 1] to [0, maxDelay]
                delayLeft = true;
            }
            else
            {
                // Center position, no delay
                return;
            }
            
            // 2. Calculate delay in samples: d_n = d_t × f
            // Convert milliseconds to seconds, then multiply by sample rate
            int delaySamples = Mathf.RoundToInt((delayMs / 1000f) * sampleRate);
            
            // Clamp delay to buffer size
            delaySamples = Mathf.Min(delaySamples, bufferSize - 1);
            
            // 3. Apply delay using circular buffer
            for (int i = 0; i < data.Length; i += channels)
            {
                if (delayLeft)
                {
                    // Delay left channel
                    // Write current sample to delay buffer
                    leftDelayBuffer[leftWriteIndex] = data[i];
                    
                    // Calculate read index (write index - delay)
                    leftReadIndex = (leftWriteIndex - delaySamples + bufferSize) % bufferSize;
                    
                    // Read delayed sample
                    data[i] = leftDelayBuffer[leftReadIndex];
                    
                    // Advance write index
                    leftWriteIndex = (leftWriteIndex + 1) % bufferSize;
                }
                else
                {
                    // Delay right channel
                    // Write current sample to delay buffer
                    rightDelayBuffer[rightWriteIndex] = data[i + 1];
                    
                    // Calculate read index (write index - delay)
                    rightReadIndex = (rightWriteIndex - delaySamples + bufferSize) % bufferSize;
                    
                    // Read delayed sample
                    data[i + 1] = rightDelayBuffer[rightReadIndex];
                    
                    // Advance write index
                    rightWriteIndex = (rightWriteIndex + 1) % bufferSize;
                }
            }
        }

        private void UpdateParamsFromScene()
        {
            if (player == null || leftEar == null || rightEar == null)
                return;
            
            // Audio source position is this transform's position
            Vector3 audioSourcePos = transform.position;
            
            // 1. Calculate stereo position based on distance from each ear
            float distanceToLeftEar = Vector3.Distance(leftEar.position, audioSourcePos);
            float distanceToRightEar = Vector3.Distance(rightEar.position, audioSourcePos);
            
            // Calculate stereo position: closer ear gets higher value
            // If left ear is closer, stereoPosition approaches 0 (left)
            // If right ear is closer, stereoPosition approaches 1 (right)
            float totalDistance = distanceToLeftEar + distanceToRightEar;
            if (totalDistance > 0.00001f)
            {
                // Normalize: rightEar distance as ratio determines left bias
                float normalizedPosition = distanceToLeftEar / totalDistance;
                
                // Apply non-linear mapping to enhance stereo effect
                // Calculate offset from center (0.5)
                float offset = normalizedPosition - 0.5f;
                
                // Amplify the offset based on sensitivity
                offset *= stereoSensitivity;
                
                // Clamp and recombine
                stereoPosition = Mathf.Clamp01(0.5f + offset);
            }
            else
            {
                // If both ears are at the same position, center the sound
                stereoPosition = 0.5f;
            }
            
            // 2. Calculate volume based on distance from player to audio source
            float distanceToPlayer = Vector3.Distance(player.position, audioSourcePos);
            
            // Apply inverse distance attenuation
            // volume = 1 at minDistance, decreases to 0 at maxDistance
            if (distanceToPlayer <= minDistance)
            {
                volume = 1f;
            }
            else if (distanceToPlayer >= maxDistance)
            {
                volume = 0f;
            }
            else
            {
                // Linear falloff between minDistance and maxDistance
                volume = 1f - ((distanceToPlayer - minDistance) / (maxDistance - minDistance));
            }
        }

        private void OnValidate()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
            audioSource.hideFlags = HideFlags.HideInInspector;
        }
    }
}
