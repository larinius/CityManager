using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityManagerEditor
{
    public class MapEditor : EditorWindow
    {
        public GameObject[] prefabs;
        private GameObject prefab = null;

        public float gridSize = 1.0f;
        private Vector3 gizmoPosition = Vector3.zero;
        private Vector3 gizmoSize = Vector3.one;

        [SerializeField]
        public VisualTreeAsset m_VisualTreeAsset = default;

        [MenuItem("Window/Level Editor")]
        public static void ShowExample()
        {
            MapEditor wnd = GetWindow<MapEditor>();
            wnd.titleContent = new GUIContent("GridBrush");
        }

        public void CreateGUI() { }

        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            SceneView.duringSceneGui += OnSceneGUIHandler;
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += UpdateGizmoPositionHandler;
        }

        private static void UpdateGizmoPositionHandler(SceneView sceneView)
        {
            // Find all instances of MapEditor in the scene
            MapEditor[] mapEditors = Resources.FindObjectsOfTypeAll<MapEditor>();

            // Call the OnSceneGUI method for each instance
            foreach (MapEditor mapEditor in mapEditors)
            {
                mapEditor.UpdateGizmoPosition();
            }
        }

        private static void OnSceneGUIHandler(SceneView sceneView)
        {
            // Find all instances of MapEditor in the scene
            MapEditor[] mapEditors = Resources.FindObjectsOfTypeAll<MapEditor>();

            // Call the OnSceneGUI method for each instance
            foreach (MapEditor mapEditor in mapEditors)
            {
                mapEditor.OnSceneGUI();
            }
        }

        private void OnGUI()
        {
            // Create GUI layout for prefab palette
            GUILayout.BeginVertical();
            foreach (GameObject prefab in prefabs)
            {
                if (GUILayout.Button(prefab.name))
                {
                    // Select the prefab when the button is clicked
                    Selection.activeObject = prefab;
                }
            }
            GUILayout.EndVertical();

            // Draw a line to separate the palette from the scene view
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

            // Create GUI layout for brush settings
            GUILayout.BeginVertical();
            GUILayout.Label("Brush Settings:");
            // Check if prefab is null, and if so, assign a default value
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("DefaultPrefab");
            }

            // Show the prefab field in the GUI
            prefab =
                EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false)
                as GameObject;

            gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            GUILayout.Space(20);

            if (GUILayout.Button("Merge Meshes"))
            {
                MergeMeshes();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Clear"))
            {
                ClearObjects();
            }

            GUILayout.EndVertical();
        }

        [DrawGizmo(GizmoType.NonSelected)]
        private void OnSceneGUI()
        {
            gizmoSize = new Vector3(gridSize, gridSize, gridSize);

            try
            {
                Renderer prefabRenderer = prefab.GetComponent<MeshRenderer>();
                gizmoSize = prefabRenderer.bounds.size;
            }
            catch { }

            // Draw a gizmo at the mouse hit position
            Handles.color = Color.green;
            Handles.DrawWireCube(gizmoPosition, gizmoSize);

            // Check if the left mouse button is pressed down
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                SpawnObject(prefab, gizmoPosition);
            }
        }

        private void UpdateGizmoPosition()
        {
            // Update the gizmo position based on the mouse position

            Event e = Event.current;
            if (e.type == EventType.MouseMove)
            {
                var newPos = GetMouseHitPosition(e);
                gizmoPosition = GetNearestPointOnGrid(newPos);
                SceneView.RepaintAll();
            }
        }

        private Vector3 GetMouseHitPosition(Event e)
        {
            Vector3 position = Vector3.zero;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity))
            {
                position = hit.point;
            }

            return position;
        }

        public Vector3 GetNearestPointOnGrid(Vector3 position)
        {
            // Round the position values to the nearest multiple of gridSize
            float x = Mathf.Round(position.x / gridSize) * gridSize;
            float z = Mathf.Round(position.z / gridSize) * gridSize;

            // Calculate the Y position
            float y = Mathf.Round((position.y + gizmoSize.y / 2) / 0.1f) * 0.1f;

            // Create a new vector with the rounded values
            Vector3 newPosition = new Vector3(x, y, z);

            return newPosition;
        }

        public GameObject GetObjectByTagAndPosition(Vector3 position, string tag)
        {
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objectsWithTag)
            {
                if (obj.transform.position == position)
                {
                    return obj;
                }
            }
            return null;
        }

        public GameObject GetObjectByTagAndName(string tag, string nameSubstring)
        {
            try
            {
                GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject obj in objectsWithTag)
                {
                    if (obj.name.ToLower().Contains(nameSubstring.ToLower()))
                    {
                        return obj;
                    }
                }
            }
            catch
            {
                return null;
            }
            return null;
        }

        public bool CanSpawn(GameObject prefab, Vector3 position, LayerMask layerMask)
        {
            Vector3 spawnPos = position;
            Vector3 size = prefab.GetComponent<Renderer>().bounds.size * 0.9f;
            Collider[] overlaps = Physics.OverlapBox(
                spawnPos,
                size / 2f,
                Quaternion.identity,
                layerMask
            );

            Debug.Log($"{overlaps.Length}, {size}, {spawnPos}");

            return overlaps.Length == 0;
        }

        private void SpawnObject(GameObject prefab, Vector3 position)
        {
            // Check if an object exists at the same position and delete it
            var oldObject = GetObjectByTagAndPosition(position, "Tile");
            if (oldObject)
            {
                UnityEngine.Object.DestroyImmediate(oldObject);
            }

            int layerMask = LayerMask.GetMask("Default");

            if (CanSpawn(prefab, position, layerMask) == false)
            {
                return;
            }

            // Create a new instance of the selected prefab at the nearest grid point
            GameObject newObject = UnityEngine.Object.Instantiate(
                prefab,
                position,
                Quaternion.identity
            );

            newObject.tag = "Tile";
            BoxCollider boxCollider = newObject.AddComponent<BoxCollider>();

            // Set the object's position
            newObject.transform.position = position;

            // Make the new object a child of the Holder object
            var holder = GetObjectByTagAndName("Holder", newObject.name);

            if (holder != null)
            {
                newObject.transform.parent = holder.transform;
            }
            else
            {
                GameObject newHolder = new GameObject($"Holder-{newObject.name}");
                newHolder.tag = "Holder";
                newHolder.transform.position = newObject.transform.position;
                newObject.transform.parent = newHolder.transform;
            }
        }

        public List<GameObject> GetChildrenOfGameObject(GameObject parentObject)
        {
            List<GameObject> children = new List<GameObject>();
            Transform parentTransform = parentObject.transform;
            int childCount = parentTransform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform childTransform = parentTransform.GetChild(i);
                GameObject child = childTransform.gameObject;
                children.Add(child);
            }
            return children;
        }

        private void MergeMeshes()
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null || !selectedObject.name.Contains("Holder"))
            {
                return;
            }

            var children = GetChildrenOfGameObject(selectedObject);
            Debug.Log(selectedObject.name + " " + children.Count);

            // Get the mesh filters of all the gameObjects
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            foreach (GameObject item in children)
            {
                MeshFilter meshFilter = item.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    meshFilters.Add(meshFilter);
                }
            }

            // Combine the meshes
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Count];
            for (int i = 0; i < meshFilters.Count; i++)
            {
                combineInstances[i].mesh = meshFilters[i].sharedMesh;
                combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combineInstances, true);

            // Create a new GameObject with the combined mesh
            GameObject mergedObject = new GameObject($"{selectedObject.name}-merged");
            mergedObject.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
            mergedObject.AddComponent<MeshRenderer>().sharedMaterial = children[0]
                .GetComponent<MeshRenderer>()
                .sharedMaterial;

            // Set the position and rotation of the new GameObject to match the first GameObject
            mergedObject.transform.parent = selectedObject.transform;
            mergedObject.transform.rotation = children[0].transform.rotation;

            // Add the mesh collider to the new gameobject
            MeshCollider meshCollider = mergedObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = combinedMesh;

            // Delete the original gameObjects
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i])
                {
                    DestroyImmediate(children[i]);
                }
            }
        }

        private void ClearObjects() { }
    }
}
