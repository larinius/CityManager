using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CityManagerEditor {

    public enum EditorState {
        Build,
        Paint,
        Fill,
        Delete
    }



    public partial class MapEditor : EditorWindow {
        private Vector2 scrollPosition;

        // The list of asset IDs
        public List<string> assetIDs;

        // The file name for the JSON file
        public string fileName = "AssetList.json";

        private EditorState currentState = EditorState.Build;

        private GameObject prefab = null;

        public float gridSize = 1.0f;
        private Vector3 gizmoPosition = Vector3.zero;
        private Vector3 gizmoSize = Vector3.one;

        private bool buildMode = false;
        private bool brushMode = false;
        private bool fillMode = false;

        private bool snapRotation90 = false;

        private bool lockX = false;
        private bool lockY = false;
        private bool lockZ = false;

        private bool rotateX = false;
        private bool rotateY = false;
        private bool rotateZ = false;

        private bool scatterMode = false;
        private float scatterRadius = 3.0f;

        [SerializeField]
        private List<GameObject> palette = new List<GameObject>();



        private int paletteTileWidth = 100;  // number of buttons per row

        [SerializeField]
        public VisualTreeAsset m_VisualTreeAsset = default;

        [MenuItem("Window/Level Editor")]

        [InitializeOnLoadMethod]
        public static void Initialize() {
            SceneView.duringSceneGui += OnSceneGUIHandler;
        }

        private void OnEnable() {
            SceneView.duringSceneGui += UpdateGizmoPositionHandler;
            // Register a callback for the hierarchyChanged event

            if (paletteDictionary.Count == 0) {
                CreateNewPalette("Default");
                paletteKey = "Default";
                UpdatePaletteSelector();
            }
        }

        private void OnDisable() {
            // Unregister the callback for the hierarchyChanged event
            // Load the asset IDs from the JSON file
            SaveListToJson(palette, "Palette");
        }

        private void Awake() {
            // Get the serialized list from EditorPrefs
        }

        // Save the palette list to EditorPrefs on OnDestroy
        private void OnDestroy() {
            // Serialize the list to a string
        }

        private static void UpdateGizmoPositionHandler(SceneView sceneView) {
            // Find all instances of MapEditor in the scene
            MapEditor[] mapEditors = Resources.FindObjectsOfTypeAll<MapEditor>();

            // Call the OnSceneGUI method for each instance
            foreach (MapEditor mapEditor in mapEditors) {
                mapEditor.UpdateGizmoPosition();
            }
        }

        private static void OnSceneGUIHandler(SceneView sceneView) {
            // Find all instances of MapEditor in the scene
            MapEditor[] mapEditors = Resources.FindObjectsOfTypeAll<MapEditor>();

            // Call the OnSceneGUI method for each instance
            foreach (MapEditor mapEditor in mapEditors) {
                mapEditor.OnSceneGUI();
            }
        }

        private void AddPrefab(GameObject prefab) {
            // Add the prefab to the palette list
            GameObject newSelection = Selection.activeObject as GameObject;
            if (prefab != null) {
                // Add the selected prefab to the palette if it's not already there
                paletteDictionary[paletteKey].Prefabs.Add(prefab);
                //var assetID = GetPrefabAssetID(prefab);
                //assetIDs.Add(assetID);
            }
        }

        [DrawGizmo(GizmoType.NonSelected)]
        private void OnSceneGUI() {
            switch (currentState) {
                case EditorState.Build:
                    BuildMode();
                    break;

                case EditorState.Paint:
                    PaintMode();
                    break;

                case EditorState.Fill:
                    FillMode();
                    break;
            }
        }

        private void BuildMode() {
            gizmoSize = new Vector3(gridSize, gridSize, gridSize);

            try {
                Renderer prefabRenderer = prefab.GetComponent<MeshRenderer>();
                gizmoSize = prefabRenderer.bounds.size;
            }
            catch { }

            // Draw a gizmo at the mouse hit position
            Handles.color = Color.green;
            Handles.DrawWireCube(gizmoPosition, gizmoSize);

            // Check if the left mouse button is pressed down
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0) {
                SpawnObject(prefab, gizmoPosition);
            }
        }

        private void PaintMode() {
        }

        private void FillMode() {
        }

        private void UpdateGizmoPosition() {
            // Update the gizmo position based on the mouse position

            Event e = Event.current;
            if (e.type == EventType.MouseMove) {
                var newPos = GetMouseHitPosition(e);
                gizmoPosition = GetNearestPointOnGrid(newPos);
                SceneView.RepaintAll();
            }
        }

        private void SpawnObject(GameObject prefab, Vector3 position) {
            // Check if an object exists at the same position and delete it
            var oldObject = GetObjectByTagAndPosition(position, "Tile");
            if (oldObject) {
                UnityEngine.Object.DestroyImmediate(oldObject);
            }

            int layerMask = LayerMask.GetMask("Default");

            if (CanSpawn(prefab, position, layerMask) == false) {
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

            if (holder != null) {
                newObject.transform.parent = holder.transform;
            }
            else {
                GameObject newHolder = new GameObject($"Holder-{newObject.name}");
                newHolder.tag = "Holder";
                newHolder.transform.position = newObject.transform.position;
                newObject.transform.parent = newHolder.transform;
            }
        }

        private void MergeMeshes() {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null || !selectedObject.name.Contains("Holder")) {
                return;
            }

            var children = GetChildrenOfGameObject(selectedObject);
            Debug.Log(selectedObject.name + " " + children.Count);

            // Get the mesh filters of all the gameObjects
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            foreach (GameObject item in children) {
                MeshFilter meshFilter = item.GetComponent<MeshFilter>();
                if (meshFilter != null) {
                    meshFilters.Add(meshFilter);
                }
            }

            // Combine the meshes
            CombineInstance[] combineInstances = new CombineInstance[meshFilters.Count];
            for (int i = 0; i < meshFilters.Count; i++) {
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
            for (int i = 0; i < children.Count; i++) {
                if (children[i]) {
                    DestroyImmediate(children[i]);
                }
            }
        }

        private void ClearObjects() {
        }
    }
}