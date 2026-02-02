using Unity.VisualScripting;
using UnityEngine;

namespace Tutorial_4
{
    public class FilterDebug : MonoBehaviour
    {
        [Header("Visual toggles")]
        public bool showMeasurement = true;
        public bool showMovingAverage = false;
        public bool showSingleExponential = false;
        public bool showDoubleExponential = false;
        public bool showOneEuro = false;
        
        [Header("Colors")]
        public Color measurementColor = Color.green;
        public Color movingAverageColor = Color.blue;
        public Color singleExponentialColor = Color.yellow;
        public Color doubleExponentialColor = Color.cyan;
        public Color oneEuroColor = Color.red;

        [Header("Config")]
        [Range(0.0f, 1.0f)] public float noiseStrength = 0.05f;

        [Header("References")] public Filter filter;

        private Vector3 _measurement;
        private Vector3 _movingAverage;
        private Vector3 _singleExponential;
        private Vector3 _doubleExponential;
        private Vector3 _oneEuro;
        
        private GameObject _measurementSphere;
        private GameObject _movingAverageSphere;
        private GameObject _singleExponentialSphere;
        private GameObject _doubleExponentialSphere;
        private GameObject _oneEuroSphere;
        
        private void Start()
        {
            _measurementSphere = CreateSphere(measurementColor);
            _movingAverageSphere = CreateSphere(movingAverageColor);
            _singleExponentialSphere = CreateSphere(singleExponentialColor);
            _doubleExponentialSphere = CreateSphere(doubleExponentialColor);
            _oneEuroSphere = CreateSphere(oneEuroColor);
        }
        
        private Vector3 GenerateNoise()
        {
            return new Vector3(
                Random.Range(-1f, 1f) * noiseStrength,
                Random.Range(-1f, 1f) * noiseStrength,
                0
            );
        }
        
        private void Update()
        {
            _measurement = GetMousePosition() + GenerateNoise();
            
            // Update each filter and the sphere position
            if (showMeasurement)
            {
                _measurementSphere.transform.position = _measurement;
            };
            if (showMovingAverage)
            {
                _movingAverage = filter.MovingAverage(_measurement);
                _movingAverageSphere.transform.position = _movingAverage;
            }
            if (showSingleExponential)
            {
                _singleExponential = filter.SingleExponential(_measurement);
                _singleExponentialSphere.transform.position = _singleExponential;
            }
            if (showDoubleExponential)
            {
                _doubleExponential = filter.DoubleExponential(_measurement);
                _doubleExponentialSphere.transform.position = _doubleExponential;
            }

            if (showOneEuro)
            {
                _oneEuro = filter.OneEuro(_measurement);
                _oneEuroSphere.transform.position = _oneEuro;
            }
        }
        
        private GameObject CreateSphere(Color color)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = transform;
            sphere.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            sphere.GetComponent<Renderer>().material.color = color;
            return sphere;
        }
        
        private static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
        
        private static Vector3 GetMousePosition()
                {
                    // Get Mouse Position in Screen Space
                    var mouseScreenPosition = Input.mousePosition;
        
                    // Normalize Mouse Position to a Range of (-1, 1)
                    var normalizedX = (mouseScreenPosition.x / Screen.width) * 2f - 1f;
                    var normalizedY = (mouseScreenPosition.y / Screen.height) * 2f - 1f;
        
                    // Map Normalized Values to Custom Screen Space Range
                    var screenX = Remap(normalizedX, -1f, 1f, -12f, 12f); // Example range (-12 to 12)
                    var screenY = Remap(normalizedY, -1f, 1f, -6f, 6f);   // Example range (-6 to 6)
        
                    // Set Source Position Based on Screen Space Mapping
                    return new Vector3(screenX, screenY, 0f);
                }
    }
}