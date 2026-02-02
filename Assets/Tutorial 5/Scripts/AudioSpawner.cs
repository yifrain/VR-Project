using UnityEngine;

namespace Tutorial_5
{
    public class AudioSpawner : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The audio source object to spawn (drag AudioController here)")]
        public GameObject audioTemplate;
        
        [Tooltip("The Terrain object to spawn on (for bounds calculation)")]
        public Transform terrainObject;

        [Tooltip("Height to spawn at (relative to ground)")]
        public float spawnHeight = 0.5f;

        [Header("Game Logic")]
        public int maxCollectCount = 3;
        private int currentCollectCount = 0;

        void Start()
        {
            if (audioTemplate == null)
            {
                Debug.LogError("[AudioSpawner] Please assign the Audio Template!");
                return;
            }

            // Try to find terrain if not assigned
            if (terrainObject == null)
            {
                GameObject t = GameObject.Find("Terrain");
                if (t != null) terrainObject = t.transform;
                else Debug.LogWarning("[AudioSpawner] Terrain not assigned and not found named 'Terrain'. Using default range.");
            }

            // Start the game loop by spawning the first one
            SpawnOne();
        }

        void SpawnOne()
        {
            if (currentCollectCount >= maxCollectCount)
            {
                Debug.Log("GAME OVER - YOU WIN!");
                return;
            }

            Vector3 spawnPos = GetRandomPositionOnTerrain();

            // Instantiate (Clone) the template
            GameObject clone = Instantiate(audioTemplate, spawnPos, Quaternion.identity);
            clone.name = $"AudioTarget_{currentCollectCount + 1}";
            clone.SetActive(true);

            // Ensure it has a Trigger Collider so Player can pick it up
            Collider col = clone.GetComponent<Collider>();
            if (col == null)
            {
                // If no collider, add a SphereCollider
                SphereCollider sphere = clone.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 1.0f; // Adjust size as needed
            }
            else
            {
                // If existing collider, make sure it's a trigger
                col.isTrigger = true;
            }

            // Setup Callback
            AudioController controller = clone.GetComponent<AudioController>();
            if (controller != null)
            {
                controller.OnCollected += HandleCollection;
                // If this is the last one (currentCollectCount is 0-indexed count of collected items)
                // When spawning the 3rd item (index 2), currentCollectCount is 2. 
                // Wait, logic check:
                // Start -> current=0 -> SpawnOne -> Spawns Target_1
                // Collect 1 -> current=1 -> SpawnOne -> Spawns Target_2
                // Collect 2 -> current=2 -> SpawnOne -> Spawns Target_3. (2 == 3-1). Yes.
                if (currentCollectCount == maxCollectCount - 1)
                {
                    controller.isFinalTarget = true;
                }
            }
        }

        private void HandleCollection(AudioController collectedObj)
        {
            // Unsubscribe to prevent memory leaks
            collectedObj.OnCollected -= HandleCollection;
            
            currentCollectCount++;
            Debug.Log($"[AudioSpawner] Collected {currentCollectCount}/{maxCollectCount}");

            // Spawn next one immediately
            if (currentCollectCount < maxCollectCount)
            {
                SpawnOne();
            }
            else
            {
                Debug.Log("Congratulations! All targets collected.");
            }
        }

        private Vector3 GetRandomPositionOnTerrain()
        {
            float randX = 0f;
            float randZ = 0f;

            if (terrainObject != null)
            {
                // Use Renderer bounds if available
                Renderer rend = terrainObject.GetComponent<Renderer>();
                if (rend != null)
                {
                    Bounds bounds = rend.bounds;
                    // Shrink bounds slightly to avoid spawning on the very edge
                    float margin = bounds.size.x * 0.1f; 
                    randX = Random.Range(bounds.min.x + margin, bounds.max.x - margin);
                    randZ = Random.Range(bounds.min.z + margin, bounds.max.z - margin);
                }
                else
                {
                    // Fallback to Transform scale (less accurate for complex meshes but okay for cubes/planes)
                    // Assuming standard plane 10x10 scaled
                    float sizeX = terrainObject.localScale.x * 10f; // Plane default size is 10
                    float sizeZ = terrainObject.localScale.z * 10f;
                    randX = Random.Range(-sizeX/2f, sizeX/2f) + terrainObject.position.x;
                    randZ = Random.Range(-sizeZ/2f, sizeZ/2f) + terrainObject.position.z;
                }
            }
            else
            {
                // Fallback hardcoded values
                 randX = Random.Range(-10f, 10f);
                 randZ = Random.Range(-10f, 10f);
            }

            Vector3 finalPos = new Vector3(randX, spawnHeight, randZ);

            // Raycast to find exact ground height
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(randX, 500f, randZ), Vector3.down, out hit, 1000f))
            {
                finalPos = hit.point + Vector3.up * spawnHeight;
            }

            return finalPos;
        }
    }
}
