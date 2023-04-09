using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CityManagerEditor {

    public partial class MapEditor : EditorWindow {

        // Get the asset ID of the active selected prefab
        public static string GetPrefabAssetID(GameObject prefab) {
            if (prefab != null && PrefabUtility.IsPartOfAnyPrefab(prefab)) {
                //string prefabPath = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(prefab));
                //string assetID = AssetDatabase.AssetPathToGUID(prefabPath);
                //return assetID;

                string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefab);
                string assetID = AssetDatabase.AssetPathToGUID(prefabPath);
                return assetID;
            }

            return null;
        }

        public Vector3 GetNearestPointOnGrid(Vector3 position) {
            // Round the position values to the nearest multiple of gridSize
            float x = Mathf.Round(position.x / gridSize) * gridSize;
            float z = Mathf.Round(position.z / gridSize) * gridSize;

            // Calculate the Y position
            float y = Mathf.Round((position.y + gizmoSize.y / 2) / 0.1f) * 0.1f;

            // Create a new vector with the rounded values
            Vector3 newPosition = new Vector3(x, y, z);

            return newPosition;
        }

        public GameObject GetObjectByTagAndPosition(Vector3 position, string tag) {
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objectsWithTag) {
                if (obj.transform.position == position) {
                    return obj;
                }
            }
            return null;
        }

        public GameObject GetObjectByTagAndName(string tag, string nameSubstring) {
            try {
                GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject obj in objectsWithTag) {
                    if (obj.name.ToLower().Contains(nameSubstring.ToLower())) {
                        return obj;
                    }
                }
            }
            catch {
                return null;
            }
            return null;
        }

        public List<GameObject> GetChildrenOfGameObject(GameObject parentObject) {
            List<GameObject> children = new List<GameObject>();
            Transform parentTransform = parentObject.transform;
            int childCount = parentTransform.childCount;
            for (int i = 0; i < childCount; i++) {
                Transform childTransform = parentTransform.GetChild(i);
                GameObject child = childTransform.gameObject;
                children.Add(child);
            }
            return children;
        }

        public bool CanSpawn(GameObject prefab, Vector3 position, LayerMask layerMask) {
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

        public void SaveListToJson(List<GameObject> objects, string key) {
            // Serialize the list of game objects to JSON format
            Debug.Log($"Saving {objects.Count}");
            List<string> items = new List<string>();

            foreach (var obj in objects) {
                var id = GetPrefabAssetID(obj);
                items.Add(id);
                Debug.Log(id);
            }

            var dict = new Dictionary<string, List<string>>();

            dict["Palette"] = items;

            string json = JsonConvert.SerializeObject(dict);

            // Save the JSON data to a file using the specified key as the filename
            string filePath = Path.Combine(Application.dataPath, key + ".json");
            File.WriteAllText(filePath, json);
        }

        public void SavePaletteToJson(Dictionary<string, Palette> palettes, string key) {
            // Serialize the list of game objects to JSON format

            var dict = new Dictionary<string, List<string>>();

            foreach (KeyValuePair<string, Palette> kvp in palettes) {
                string k = kvp.Key;

                List<GameObject> objects = kvp.Value.Prefabs;
                List<string> items = new List<string>();

                foreach (GameObject obj in objects) {
                    var id = GetPrefabAssetID(obj);
                    items.Add(id);
                }

                dict[k] = items;
            }

            string json = JsonConvert.SerializeObject(dict);

            // Save the JSON data to a file using the specified key as the filename
            string filePath = Path.Combine(Application.dataPath, key + ".json");
            File.WriteAllText(filePath, json);
        }

        public List<GameObject> LoadListFromJson(string key) {
            List<GameObject> gameObjects = new List<GameObject>();

            string filePath = Path.Combine(Application.dataPath, key + ".json");
            // Read the JSON data from the file
            string json = File.ReadAllText(filePath);

            // Deserialize the JSON data into a PaletteJson object
            PaletteJson paletteJson = JsonConvert.DeserializeObject<PaletteJson>(json);

            // Loop through the asset IDs in the palette and load the corresponding GameObjects
            foreach (string assetID in paletteJson.Palette) {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(assetID));
                if (obj != null) {
                    gameObjects.Add(obj);
                }
            }

            return gameObjects;
        }

        public Dictionary<string, Palette> LoadPaletteFromJson(string key) {


            string filePath = Path.Combine(Application.dataPath, key + ".json");

            string json = File.ReadAllText(filePath);

            Dictionary<string, List<string>> paletteJson = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);            

            Dictionary<string, Palette> newPalette = new Dictionary<string, Palette>();

            foreach (KeyValuePair<string, List<string>> pair in paletteJson) {
                string paletteName = pair.Key;
                Palette palette = new Palette();
                palette.Prefabs = new List<GameObject>();
                palette.Name = paletteName;

                List<string> assetIDs = pair.Value;

                foreach (string id in assetIDs) {
                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id));
                    if (obj != null) {
                        palette.Prefabs.Add(obj);                        
                    }

                }

                newPalette[paletteName] = palette;
            }



            return newPalette;
        }

        // Save the asset IDs to a JSON file
        public void SaveToJson(UnityEngine.Object obj, string key) {
            // Read the JSON data from the file
            string filePath = Path.Combine(Application.dataPath, fileName);
            string json = File.ReadAllText(filePath);

            // Deserialize the JSON data back into a list of dictionaries
            List<Dictionary<string, object>> list = JsonUtility.FromJson<List<Dictionary<string, object>>>(json);

            // Find the dictionary with the specified key, or create a new one if it doesn't exist
            Dictionary<string, object> dict = list.Find(d => d.ContainsKey("key") && d["key"].Equals(key));
            if (dict == null) {
                dict = new Dictionary<string, object>();
                list.Add(dict);
            }

            // Update the dictionary with the object and the key
            dict["key"] = key;
            dict["data"] = obj;

            // Serialize the list back into JSON format
            json = JsonUtility.ToJson(list, true);

            // Write the JSON data back to the file
            File.WriteAllText(filePath, json);
        }

        // Load the asset IDs from the JSON file
        public T LoadFromJson<T>(string key) where T : UnityEngine.Object {
            // Read the JSON data from the file
            string filePath = Path.Combine(Application.dataPath, fileName);
            string json = File.ReadAllText(filePath);

            // Deserialize the JSON data back into a list of dictionaries
            List<Dictionary<string, object>> list = JsonUtility.FromJson<List<Dictionary<string, object>>>(json);

            // Find the dictionary with the specified key, or return null if it doesn't exist
            Dictionary<string, object> dict = list.Find(d => d.ContainsKey("key") && d["key"].Equals(key));
            if (dict == null) {
                return null;
            }

            // Extract the object from the dictionary
            T obj = dict["data"] as T;

            return obj;
        }

        private Vector3 GetMouseHitPosition(Event e) {
            Vector3 position = Vector3.zero;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity)) {
                position = hit.point;
            }

            return position;
        }
    }
}