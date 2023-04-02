using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace CityManagerEditor
{

    struct MapObject
    {
        public GameObject instance;
        public Vector3 position;
        public MapObject(GameObject gameObject, Vector3 position)
        {
            this.instance = gameObject;
            this.position = position;
        }
    }


    public class MapEditor : EditorWindow
    {

        public GameObject[] prefabs;
        private GameObject prefab = null;
        public float gridSize = 1.0f;
        private Vector3 gizmoPosition = Vector3.zero;
        private List<MapObject> mapObjects = new List<MapObject>();

        [SerializeField]
        public VisualTreeAsset m_VisualTreeAsset = default;

        [MenuItem("Window/UI Toolkit/GridBrush")]
        public static void ShowExample()
        {
            MapEditor wnd = GetWindow<MapEditor>();
            wnd.titleContent = new GUIContent("GridBrush");
        }

        public void CreateGUI()
        {

        }

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
            prefab = EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false) as GameObject;

            gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            GUILayout.Space(20);

            if (GUILayout.Button("Merge Meshes"))
            {
                //myScript.MergeMeshes();
                MergeMeshes();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Clear"))
            {
                //myScript.MergeMeshes();
                ClearObjects();
            }

            GUILayout.EndVertical();

        }

        [DrawGizmo(GizmoType.NonSelected)]
        private void OnSceneGUI()
        {
            Renderer prefabRenderer = prefab.GetComponent<MeshRenderer>();
            Bounds bounds = prefabRenderer.bounds;

            // Draw a gizmo at the mouse hit position
            Handles.color = Color.green;
            Handles.DrawWireCube(gizmoPosition, bounds.size);                       

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

            Renderer prefabRenderer = prefab.GetComponent<MeshRenderer>();
            Bounds bounds = prefabRenderer.bounds;

            //float y = Mathf.Round(position.y / gridSize) * gridSize;
            float y = position.y + bounds.size.y / 2;
            float z = Mathf.Round(position.z / gridSize) * gridSize;

            // Create a new vector with the rounded values
            Vector3 newPosition = new Vector3(x, y, z);

            return newPosition;
        }


        private int GetObjectIndexAtPosition(Vector3 position)
        {
            for (int i = 0; i < mapObjects.Count; i++)
            {
                if (mapObjects[i].position == position)
                {
                    return i;
                }
            }

            return -1;
        }


        private void SpawnObject(GameObject prefab, Vector3 position)
        {
            // Check if an object exists at the same position and replace it
            int existingIndex = GetObjectIndexAtPosition(position);
            if (existingIndex != -1)
            {
                MapObject removeObject = mapObjects[existingIndex];
                mapObjects.RemoveAt(existingIndex);

                // Destroy the object from the scene
                if (removeObject.instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(removeObject.instance);
                }
            }

            // Create a new object and add it to the list
            //GameObject newObject = new GameObject("New Object");
            

            // Create a new instance of the selected prefab at the nearest grid point
            GameObject instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
            MapObject newObject = new MapObject(instance, position);
            // Set the object's position
            newObject.instance.transform.position = position;
            mapObjects.Add(newObject);

            // Make the new object a child of the Holder object
            if (GameObject.Find("Holder") == null)
            {
                GameObject holder = new GameObject("Holder");
                newObject.instance.transform.parent = holder.transform;
            }
            else
            {
                newObject.instance.transform.parent = GameObject.Find("Holder").transform;
            }
        }

        private void MergeMeshes()
        {
            // Get the mesh renderers and filters of all the gameObjects
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            foreach (MapObject item in mapObjects)
            {
                MeshFilter meshFilter = item.instance.GetComponent<MeshFilter>();
                MeshRenderer meshRenderer = item.instance.GetComponent<MeshRenderer>();
                if (meshFilter != null && meshRenderer != null)
                {
                    meshFilters.Add(meshFilter);
                    meshRenderers.Add(meshRenderer);
                }
            }

            // Combine the meshes
            Mesh combinedMesh = new Mesh();
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Count];
            for (int i = 0; i < meshFilters.Count; i++)
            {
                combineInstances[i].mesh = meshFilters[i].sharedMesh;
                combineInstances[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }
            combinedMesh.CombineMeshes(combineInstances, true);

            // Create a new GameObject with the combined mesh
            GameObject mergedObject = new GameObject("Merged Object");
            mergedObject.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
            mergedObject.AddComponent<MeshRenderer>().sharedMaterial = meshRenderers[0].sharedMaterial;


            if (GameObject.Find("Holder") == null)
            {
                GameObject holder = new GameObject("Holder");
                mergedObject.transform.parent = holder.transform;
            }


            // Set the position and rotation of the new GameObject to match the first GameObject
            //mergedObject.transform.position = mapObjects[0].position;
            mergedObject.transform.rotation = mapObjects[0].instance.transform.rotation;



            // Delete the original gameObjects
            for(int i=0; i<mapObjects.Count; i++) 
            {
                if (mapObjects[i].instance)
                {
                    DestroyImmediate(mapObjects[i].instance);
                }
            }

            ClearObjects();
        }

        private void ClearObjects()
        {
            for (int i = 0; i < mapObjects.Count; i++)
            {
                //if (mapObjects[i].instance)
                //{
                //    DestroyImmediate(mapObjects[i].instance);
                //}
                mapObjects.RemoveAt(i);
            }
        }

    }
}