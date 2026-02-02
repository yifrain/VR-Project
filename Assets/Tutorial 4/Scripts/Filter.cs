using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Tutorial_4
{
    public class Filter : MonoBehaviour
    {
        [Header("Moving average")]
        [Range(1, 200)] public int samples = 30;
        
        [Header("Single Exponential")]
        [Range(0.01f, 1.0f)] public float seAlpha = 0.03f;
        
        [Header("Double Exponential")]
        [Range(0.0f, 1.0f)] public float deAlpha = 0.04f;
        [Range(0.0f, 1.0f)] public float deBeta = 0.5f;

        [Header("One Euro")] public float frequency = 60f; 

        // temp values for filters
        private readonly Queue<Vector3> _movingAverageBuffer = new();
        private Vector3 _singleExponential;
        private Vector3 _doubleExponential;
        private Vector3 _trend = Vector3.zero;
        private bool _isFirstDoubleExponential = true;
        private Vector3 _previousValue = Vector3.zero;

        private OneEuroFilter<Vector3> _oneEuro;

        private void Start()
        {
            _oneEuro = new OneEuroFilter<Vector3>(frequency);
        }

        public Vector3 MovingAverage(Vector3 value)
        {
            // Add the new measurement to the buffer
            _movingAverageBuffer.Enqueue(value);
            
            // Keep only the last k samples
            while (_movingAverageBuffer.Count > samples)
            {
                _movingAverageBuffer.Dequeue();
            }
            
            // Calculate the average of all samples in the buffer
            Vector3 sum = Vector3.zero;
            foreach (var sample in _movingAverageBuffer)
            {
                sum += sample;
            }
            
            return sum / _movingAverageBuffer.Count;
        }

        public Vector3 SingleExponential(Vector3 value)
        {
            // Initialize on first call
            if (_singleExponential == Vector3.zero)
            {
                _singleExponential = value;
                return value;
            }
            
            // Apply single exponential smoothing formula:
            // s_t = α * ω'_t + (1 - α) * s_{t-1}
            _singleExponential = seAlpha * value + (1 - seAlpha) * _singleExponential;
            
            return _singleExponential;
        }

        public Vector3 DoubleExponential(Vector3 value)
        {
            // Initialize on first call: s_0 = ω'_0
            if (_isFirstDoubleExponential)
            {
                _doubleExponential = value;
                _previousValue = value;
                _isFirstDoubleExponential = false;
                return value;
            }
            
            // Initialize trend on second call: d_0 = ω'_1 - ω'_0
            if (_trend == Vector3.zero)
            {
                _trend = value - _previousValue;
            }
            
            // Store previous smoothed value for trend calculation
            Vector3 previousSmoothed = _doubleExponential;
            
            // Apply double exponential smoothing formulas:
            // s_t = α * ω'_t + (1 - α) * (s_{t-1} + d_{t-1})
            _doubleExponential = deAlpha * value + (1 - deAlpha) * (_doubleExponential + _trend);
            
            // d_t = β * (s_t - s_{t-1}) + (1 - β) * d_{t-1}
            _trend = deBeta * (_doubleExponential - previousSmoothed) + (1 - deBeta) * _trend;
            
            _previousValue = value;
            
            return _doubleExponential;
        }

        public Vector3 OneEuro(Vector3 value)
        {
            return _oneEuro.Filter(value);
        }
    }
}