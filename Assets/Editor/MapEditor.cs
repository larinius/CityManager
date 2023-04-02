using System;
using UnityEditor;
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


        }

        [DrawGizmo(GizmoType.NonSelected)]
        private void OnSceneGUI()
        {

            // Draw a gizmo at the gizmoPosition
            Handles.color = Color.green;
            Handles.DrawWireCube(gizmoPosition, Vector3.one * gridSize);

            // Draw a gizmo at the mouse hit position
            Handles.color = Color.green;
            Handles.DrawWireCube(gizmoPosition, Vector3.one * gridSize);

            Vector3 nearestGridPoint = GetNearestPointOnGrid(gizmoPosition);

            Debug.Log("Cube at " + nearestGridPoint);
            Handles.color = Color.green;
            Handles.DrawWireCube(nearestGridPoint, Vector3.one * gridSize);



            // Check if the left mouse button is pressed down
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                // Create a new instance of the selected prefab at the nearest grid point
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                instance.transform.position = nearestGridPoint;
                Debug.Log("Spawned " + instance.name + " at " + nearestGridPoint);
            }
        }

        private void UpdateGizmoPosition()
        {
            // Update the gizmo position based on the mouse position
            Event e = Event.current;
            if (e.type == EventType.MouseMove)
            {
                gizmoPosition = GetMouseHitPosition(e);
                SceneView.RepaintAll();
                Debug.Log("Mouse at " + gizmoPosition);
            }
        }

        private Vector3 GetMouseHitPosition(Event e)
        {
            Vector3 position = Vector3.zero;

            // Calculate the direction from the camera to the mouse cursor
            Camera currentCamera = SceneView.lastActiveSceneView.camera;
            Vector3 mousePosition = e.mousePosition;
            mousePosition.z = currentCamera.nearClipPlane;
            Vector3 worldPosition = currentCamera.ScreenToWorldPoint(mousePosition);
            Vector3 direction = (worldPosition - currentCamera.transform.position);

            Debug.DrawLine(currentCamera.transform.position, currentCamera.transform.position + direction * 1000f, Color.yellow);

            // Perform a raycast and check if it hit anything
            RaycastHit hit;
            if (Physics.Raycast(currentCamera.transform.position, direction, out hit, 1000f))
            {
                position = hit.point;
                Debug.Log("Hit " + position);
            }

            return position;
        }



        public Vector3 GetNearestPointOnGrid(Vector3 position)
        {
            // Calculate the nearest point on the grid based on the current grid size
            position -= SceneView.lastActiveSceneView.camera.transform.position;

            position = new Vector3(
                Handles.SnapValue(position.x, gridSize),
                Handles.SnapValue(position.y, gridSize),
                Handles.SnapValue(position.z, gridSize)
            );

            position += SceneView.lastActiveSceneView.camera.transform.position;
            return position;
        }

    }
}