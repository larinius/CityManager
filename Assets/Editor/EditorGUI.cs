using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CityManagerEditor {

    [Serializable]
    public class PaletteJson {
        public string Name;
        public List<string> Palette;
    }

    public class Palette {
        public string Name;
        public List<GameObject> Prefabs;
    }

    public partial class MapEditor : EditorWindow {
        private string[] options = new string[] { "Option 1", "Option 2", "Option 3" };

        [SerializeField]
        private Dictionary<string, Palette> paletteDictionary = new Dictionary<string, Palette>();

        private List<string> paletteNames = new List<string>();

        private int selectedPaletteIndex = 0;

        private string newPaletteName = "";

        private SerializedProperty paletteProperty;

        private string paletteKey = "";

        private void UpdatePaletteSelector() {
            paletteNames.Clear();
            // Populate the list of palette names and set the selected index to the first item
            foreach (string name in paletteDictionary.Keys) {
                paletteNames.Add(name);
            }
            selectedPaletteIndex = 0;
        }

        private void CreateNewPalette(string name) {
            Palette palette = new Palette();
            palette.Name = name;
            palette.Prefabs = new List<GameObject>();
            paletteDictionary.Add(palette.Name, palette);
            paletteNames.Add(palette.Name);
            selectedPaletteIndex = paletteNames.Count - 1;
            newPaletteName = "";

            UpdatePaletteSelector();
        }

        private void OnGUI() {
            EditorGUI.BeginChangeCheck();

            if (paletteDictionary.Count != 0) {
                selectedPaletteIndex = EditorGUILayout.Popup("Palette", selectedPaletteIndex, paletteNames.ToArray());
                var paletteKey = paletteNames[selectedPaletteIndex];
            }
            if (EditorGUI.EndChangeCheck()) {
                paletteKey = paletteNames[selectedPaletteIndex];
            }

            // Add a button for creating a new palette
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            newPaletteName = EditorGUILayout.TextField("New Palette Name", newPaletteName);

            if (GUILayout.Button("Create New Palette")) {
                if (!string.IsNullOrEmpty(newPaletteName) && !paletteDictionary.ContainsKey(newPaletteName)) {
                    CreateNewPalette(newPaletteName);
                }
                else {
                    Debug.Log("Pallette name error");
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (paletteKey != "") {
                if (GUILayout.Button("Delete palette")) {
                    paletteDictionary.Remove(paletteKey);
                    UpdatePaletteSelector();
                }
                if (GUILayout.Button("Delete All")) {
                    paletteDictionary.Clear();
                    UpdatePaletteSelector();
                }
            }

            if (GUILayout.Button("Save")) {
                SavePaletteToJson(paletteDictionary, "Palettes");
            }

            EditorGUI.BeginChangeCheck();
            if (GUILayout.Button("Load")) {
                if (EditorGUI.EndChangeCheck()) {
                    paletteDictionary = LoadPaletteFromJson("Palettes");
                    UpdatePaletteSelector();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Label("Palette:");

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(10));

            GUILayout.BeginVertical();
            GUILayout.Label("Prefab Palette");

            if (paletteKey != "") {
                for (int row = 0; row < 3; row++) {
                    GUILayout.BeginHorizontal();
                    for (int col = 0; col < 3; col++) {
                        int index = row * 3 + col;
                        if (index >= paletteDictionary[paletteKey].Prefabs.Count) {
                            // If we reach the end of the button list, draw an empty space
                            GUILayout.Box("", GUILayout.Width(paletteTileWidth), GUILayout.Height(paletteTileWidth));
                        }
                        else {
                            GameObject obj = paletteDictionary[paletteKey].Prefabs[index];
                            Texture2D preview = AssetPreview.GetAssetPreview(obj);
                            if (GUILayout.Button(new GUIContent(preview), GUILayout.Width(paletteTileWidth), GUILayout.Height(paletteTileWidth))) {
                                Debug.Log($"Button at ({index}) was clicked");
                                prefab = obj;
                            }
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else {
                Debug.Log("Key empty");
            }

            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(5));
            gridSize = EditorGUILayout.FloatField("Grid Size", gridSize, GUILayout.Width(200));

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Add prefab")) {
                AddPrefab(Selection.activeObject as GameObject);
            }

            if (GUILayout.Button("Clear palette")) {
                palette.Clear();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Space(20);
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            currentState = (EditorState)
                GUILayout.Toolbar(
                    (int)currentState,
                    new string[] { "Build", "Paint", "Fill" },
                    EditorStyles.toolbarButton
                );

            GUILayout.EndHorizontal();

            GUILayout.Space(20);

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Random rotation", GUILayout.Width(150));
            rotateX = EditorGUILayout.ToggleLeft("X", rotateX, GUILayout.Width(50));
            rotateY = EditorGUILayout.ToggleLeft("Y", rotateY, GUILayout.Width(50));
            rotateZ = EditorGUILayout.ToggleLeft("Z", rotateZ, GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();
            snapRotation90 = EditorGUILayout.Toggle("Snap rotation to 90", snapRotation90, GUILayout.Width(150));

            GUILayout.Space(20);
            EditorGUILayout.BeginVertical();
            scatterMode = EditorGUILayout.Toggle("Scatter objects", scatterMode, GUILayout.Width(200));
            scatterRadius = EditorGUILayout.FloatField("Radius", scatterRadius, GUILayout.Width(200));
            EditorGUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.EndVertical();

            GUILayout.BeginVertical();

            GUILayout.Space(20);

            if (GUILayout.Button("Merge Meshes")) {
                MergeMeshes();
            }

            GUILayout.Space(20);

            if (GUILayout.Button("Clear")) {
                ClearObjects();
            }



            GUILayout.EndVertical();
        }
    }
}