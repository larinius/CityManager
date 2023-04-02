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
        void OnSceneGUI()
        {
            // Check if the left mouse button is pressed down
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                // Get the current mouse position in world space
                Vector3 mousePosition = Event.current.mousePosition;
                mousePosition.y = Camera.current.pixelHeight - mousePosition.y;
                mousePosition = Camera.current.ScreenToWorldPoint(mousePosition);

                // Snap the mouse position to the nearest grid point
                Vector3 gridPosition = GetNearestPointOnGrid(mousePosition);

                // Create a new instance of the selected prefab at the grid position
                if (prefab != null)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    instance.transform.position = gridPosition;

                    Debug.Log("Prefab instantiated at " + gridPosition);

                }
            }
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