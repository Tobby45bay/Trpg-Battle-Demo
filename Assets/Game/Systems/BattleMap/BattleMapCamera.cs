using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Systems.BattleMap
{
    [RequireComponent(typeof(Camera))]
    public class BattleMapCamera : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cursorTransform;
        [SerializeField] private Tilemap mapTilemap;

        [SerializeField] private float followSpeed = 10f;
        [SerializeField] private Vector3 cameraOffset = new(0, 0, -10);

        private Camera cam;
        private Bounds mapBounds;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
        }

        public void Initialize(Transform cursor, Tilemap tilemap)
        {
            cursorTransform = cursor;
            mapTilemap = tilemap;
            CacheMapBounds();
        }

        private void LateUpdate()
        {
            if (cursorTransform == null || mapTilemap == null)
                return;

            Vector3 target = cursorTransform.position;
            target.z = transform.position.z;

            target = ClampToMapBounds(target);

            transform.position = target;
        }

        private void CacheMapBounds()
        {
            mapTilemap.CompressBounds();
            mapBounds = mapTilemap.localBounds;
        }

        private Vector3 ClampToMapBounds(Vector3 target)
        {
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            float minX = mapBounds.min.x + camWidth;
            float maxX = mapBounds.max.x - camWidth;
            float minY = mapBounds.min.y + camHeight;
            float maxY = mapBounds.max.y - camHeight;

            target.x = Mathf.Clamp(target.x, minX, maxX);
            target.y = Mathf.Clamp(target.y, minY, maxY);

            return target;
        }

        public void RefreshBounds()
        {
            if (mapTilemap == null) return;
            CacheMapBounds();
        }
    }
}
