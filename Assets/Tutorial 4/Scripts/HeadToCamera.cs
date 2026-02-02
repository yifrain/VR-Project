using System.ComponentModel;
using UnityEngine;

namespace Tutorial_4
{
    enum FilterType
    {
        None, MovingAverage, SingleExponential, DoubleExponential, OneEuro
    }
    
    [RequireComponent(typeof(HeadTracker))]
    public class HeadToCamera : MonoBehaviour
    {
        [SerializeField] private FilterType filterType;
        
        [Header("Settings")]
        [SerializeField] private float positionScale = 10f;
        
        [Header("References")]
        [SerializeField] HeadTracker headTracker;
        [SerializeField] private Filter filter;

        private Transform mainCamera;
        
        private void Start()
        {
            mainCamera = Camera.main.transform;
            headTracker = GetComponent<HeadTracker>();
            filter = FindAnyObjectByType<Filter>();
            if (filter == null)
            {
                Debug.LogError("No Filter found! Please add the Filter prefab to the scene.");
            }
        }

        void Update()
        {
            if (headTracker.DetectedFace != Vector3.zero)
            {
                mainCamera.localPosition = positionScale * ApplyFilter(headTracker.DetectedFace);
            }
        }

        private Vector3 ApplyFilter(Vector3 value)
        {
            switch (filterType)
            {
                case FilterType.None:
                    return value;
                case FilterType.MovingAverage:
                    return filter.MovingAverage(value);
                case FilterType.SingleExponential:
                    return filter.SingleExponential(value);
                case FilterType.DoubleExponential:
                    return filter.DoubleExponential(value);
                case FilterType.OneEuro:
                    return filter.OneEuro(value);
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}
