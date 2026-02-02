using MediaPipe.BlazeFace;
using UnityEngine;

namespace Tutorial_4
{
    public class HeadTracker : MonoBehaviour
    {
        [Tooltip("Index of your webcam.")]
        [SerializeField] private int webcamIndex = 0;
        [Tooltip("Threshold of the face detector")]
        [Range(0f, 1f)] 
        [SerializeField] private float threshold = 0.5f;
        [SerializeField] private ResourceSet resources;
        [Tooltip("Focal length of your webcam in pixels")]
        [SerializeField] private int focalLength = 492;
        [Tooltip("Distance between your eyes in meters.")]
        [SerializeField] private float ipd = 0.064f;

        public Vector3 DetectedFace { get; private set; }

        private FaceDetector _detector;
        private WebCamTexture _webCamTexture;

        private void Start()
        {
            _detector = new FaceDetector(resources);
            
            // Source - https://stackoverflow.com/a
            // Posted by S.Richmond
            // Retrieved 2025-11-19, License - CC BY-SA 3.0

            var devices = WebCamTexture.devices;
            /*
            foreach (var device in devices)
            {
                Debug.Log(device.name);
            }
            */
            if (devices.Length == 0)
            {
                Debug.LogWarning("No webcam found");
                return;
            }
            
            var device = devices[webcamIndex];
            _webCamTexture = new WebCamTexture(device.name);
            _webCamTexture.Play();
        }

        private void OnDestroy()
        {
            _detector?.Dispose();
        }

        private void Update()
        {
            if (_webCamTexture == null)
            {
                return;
            }
            
            _detector.ProcessImage(_webCamTexture, threshold);
            if (_detector.Detections.Length == 0)
            {
                DetectedFace = Vector3.zero;
                return;
            }
            
            SetCameraPosition(_detector.Detections[0]);
        }

        private void SetCameraPosition(Detection face)
        {
            // template code:
            DetectedFace = face.leftEye;

            // Convert UV coordinates (0-1) to pixel coordinates
            Vector2 leftEyePixels = new Vector2(
                face.leftEye.x * _webCamTexture.width,
                face.leftEye.y * _webCamTexture.height
            );
            
            Vector2 rightEyePixels = new Vector2(
                face.rightEye.x * _webCamTexture.width,
                face.rightEye.y * _webCamTexture.height
            );
            
            // Calculate distance between eyes in pixels (S_img)
            float S_img = Vector2.Distance(leftEyePixels, rightEyePixels);
            
            Debug.Log($"Distance between eyes (S_img): {S_img} pixels");
            
            // Calculate center point between eyes (u, v) in pixels
            float u = (leftEyePixels.x + rightEyePixels.x) / 2f;
            float v = (leftEyePixels.y + rightEyePixels.y) / 2f;
            
            // Calculate center point of image (cx, cy) in pixels
            float cx = _webCamTexture.width / 2f;
            float cy = _webCamTexture.height / 2f;
            
            // Calculate 3D position using the formulas
            float z = -(focalLength * ipd) / S_img;
            float x = (u - cx) * z / focalLength;
            float y = -(v - cy) * z / focalLength;
            
            DetectedFace = new Vector3(x, y, 0);

            Debug.Log($"Detected Face Position: {DetectedFace}");
        }
    }
}