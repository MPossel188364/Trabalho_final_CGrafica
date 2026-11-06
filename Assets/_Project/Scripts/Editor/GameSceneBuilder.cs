using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace NebulaGuard.EditorTools
{
    /// <summary>
    /// Ferramenta de editor: monta a cena Game (passo 1 do mapa) com um clique, em
    /// NebulaGuard → Montar cena Game. Cria as Sorting Layers, a tag e a layer, os sprites
    /// provisórios, a ponte de luz, o portal, a nave-mãe, o asset e o prefab do Drone e uma
    /// instância de teste do Drone.
    /// Está numa pasta "Editor": só existe dentro do Unity e não entra no build do jogo.
    /// Rodar de novo recria a cena (pede confirmação); o asset Drone, se já existir, é mantido.
    /// </summary>
    public static class GameSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string TemplateScenePath = "Assets/Scenes/SampleScene.unity";
        private const string ArtFolder = "Assets/_Project/Art";
        private const string DataFolder = "Assets/_Project/Data";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string MaterialFolder = "Assets/_Project/Materials";

        // Mesmos waypoints da tabela "Mapa (cena Game)" do CLAUDE.md. Comprimento total: 59,5.
        private static readonly Vector2[] Waypoints =
        {
            new Vector2(-14f, 4.5f), new Vector2(-9f, 4.5f), new Vector2(-9f, -5f),
            new Vector2(-3f, -5f),   new Vector2(-3f, 4.5f), new Vector2(3f, 4.5f),
            new Vector2(3f, -5f),    new Vector2(9f, -5f),   new Vector2(9f, 0f),
            new Vector2(12f, 0f),
        };

        // Do fundo para a frente, como no CLAUDE.md.
        private static readonly string[] SortingLayerNames =
            { "Background", "Bridges", "Ships", "Enemies", "Projectiles", "Effects" };

        [MenuItem("NebulaGuard/Montar cena Game")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Montar cena Game",
                    "A cena Game já existe e será recriada do zero. Continuar?", "Recriar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!File.Exists(TemplateScenePath))
            {
                Debug.LogError($"Não achei {TemplateScenePath} (a cena de exemplo do template), usada como base com câmera e luz 2D.");
                return;
            }

            EnsureFolders();
            EnsureTagsAndLayers();

            Sprite circle = CreateSprite("Circle", 64, 64, 64f, FilterMode.Bilinear, CirclePixel);
            Sprite ring = CreateSprite("Ring", 128, 128, 64f, FilterMode.Bilinear, RingPixel);
            Sprite triangle = CreateSprite("Triangle", 64, 64, 64f, FilterMode.Bilinear, TrianglePixel);
            // 512 x 288 pixels a 16 pixels por unidade = 32 x 18 unidades, a área visível da câmera.
            Sprite stars = CreateSprite("Stars", 512, 288, 16f, FilterMode.Point, StarPixel);

            Material bridgeMaterial = CreateBridgeMaterial();
            EnemyData droneData = GetOrCreateDroneData();

            // Parte da cena de exemplo (já tem câmera e Global Light 2D) copiada para Game.unity.
            if (File.Exists(ScenePath))
                AssetDatabase.DeleteAsset(ScenePath);
            AssetDatabase.CopyAsset(TemplateScenePath, ScenePath);
            EditorSceneManager.OpenScene(ScenePath);

            ConfigureCamera();
            CreateSpriteObject("Background", stars, new Vector2(0f, 0f), 1f, Color.white, "Background", 0);
            CreateSpriteObject("Portal", ring, Waypoints[0], 2.2f, new Color(0.7f, 0.4f, 1f), "Ships", 0);

            LightBridge bridge = CreateBridge(bridgeMaterial);
            CreateMotherShip(circle);

            GameObject dronePrefab = CreateDronePrefab(triangle, droneData);
            GameObject droneInstance = (GameObject)PrefabUtility.InstantiatePrefab(dronePrefab);
            droneInstance.name = "Drone_Teste";
            droneInstance.transform.position = Waypoints[0];
            SetReference(droneInstance.GetComponent<Enemy>(), "testBridge", bridge);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Cena Game montada em {ScenePath}. Comprimento da ponte: {bridge.TotalLength}. Dê Play: o Drone_Teste deve percorrer a ponte e tirar 1 de vida da nave-mãe.");
        }

        // ---------- Pastas, tags e layers ----------

        private static void EnsureFolders()
        {
            foreach (string folder in new[] { "Assets/_Project/Scenes", ArtFolder, DataFolder, PrefabFolder, MaterialFolder })
            {
                if (AssetDatabase.IsValidFolder(folder))
                    continue;

                string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }

        private static void EnsureTagsAndLayers()
        {
            foreach (string tag in new[] { "Enemy", "MotherShip" })
            {
                if (Array.IndexOf(InternalEditorUtility.tags, tag) < 0)
                    InternalEditorUtility.AddTag(tag);
            }

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            // Sorting Layers, na ordem do fundo para a frente (depois da Default).
            SerializedProperty sortingLayers = tagManager.FindProperty("m_SortingLayers");
            foreach (string layerName in SortingLayerNames)
            {
                if (HasSortingLayer(sortingLayers, layerName))
                    continue;

                sortingLayers.InsertArrayElementAtIndex(sortingLayers.arraySize);
                SerializedProperty item = sortingLayers.GetArrayElementAtIndex(sortingLayers.arraySize - 1);
                item.FindPropertyRelative("name").stringValue = layerName;
                // O inserido copia o último; o ID precisa ser único. Hash do nome = o mesmo nos 3 PCs.
                item.FindPropertyRelative("uniqueID").intValue = Animator.StringToHash(layerName);
                item.FindPropertyRelative("locked").boolValue = false;
            }

            // Layer "Enemy" (as torres usam para buscar inimigos). Layers de usuário vão de 8 a 31.
            SerializedProperty layers = tagManager.FindProperty("layers");
            bool hasEnemyLayer = false;
            for (int i = 0; i < layers.arraySize; i++)
                hasEnemyLayer |= layers.GetArrayElementAtIndex(i).stringValue == "Enemy";

            if (!hasEnemyLayer)
            {
                for (int i = 8; i < layers.arraySize; i++)
                {
                    SerializedProperty slot = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(slot.stringValue))
                    {
                        slot.stringValue = "Enemy";
                        break;
                    }
                }
            }

            tagManager.ApplyModifiedProperties();
        }

        private static bool HasSortingLayer(SerializedProperty sortingLayers, string layerName)
        {
            for (int i = 0; i < sortingLayers.arraySize; i++)
            {
                if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                    return true;
            }
            return false;
        }

        // ---------- Sprites provisórios (gerados por código; trocar pela arte final depois) ----------

        private delegate Color32 PixelFunc(int x, int y, int width, int height);

        private static Sprite CreateSprite(string fileName, int width, int height, float pixelsPerUnit, FilterMode filter, PixelFunc pixel)
        {
            string path = $"{ArtFolder}/{fileName}.png";

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x, y, width, height);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.filterMode = filter;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color32 CirclePixel(int x, int y, int w, int h)
        {
            float dx = x + 0.5f - w / 2f, dy = y + 0.5f - h / 2f;
            return Math.Sqrt(dx * dx + dy * dy) <= w / 2f ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        }

        private static Color32 RingPixel(int x, int y, int w, int h)
        {
            float dx = x + 0.5f - w / 2f, dy = y + 0.5f - h / 2f;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            bool inside = distance <= w / 2f && distance >= w / 2f * 0.75f;
            return inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        }

        // Triângulo apontando para cima (os sprites do Enemy apontam para cima: spriteAngleOffset = -90).
        private static Color32 TrianglePixel(int x, int y, int w, int h)
        {
            float halfWidth = (1f - (y + 0.5f) / h) * w / 2f;
            bool inside = Math.Abs(x + 0.5f - w / 2f) <= halfWidth;
            return inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
        }

        // Estrelas espalhadas por um hash dos pixels: sempre o mesmo céu, sem precisar de Random.
        private static Color32 StarPixel(int x, int y, int w, int h)
        {
            int hash = unchecked((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;
            if (hash % 600 != 0)
                return new Color32(255, 255, 255, 0);

            byte brightness = (byte)(110 + (hash / 600) % 145);
            return new Color32(brightness, brightness, 255, 255);
        }

        // ---------- Material, dados e prefab ----------

        private static Material CreateBridgeMaterial()
        {
            string path = $"{MaterialFolder}/LightBridge.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            // Sem luz (unlit): a ponte "brilha" com a cor do LineRenderer.
            Shader shader = Shader.Find("Sprites/Default");
            var material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static EnemyData GetOrCreateDroneData()
        {
            string path = $"{DataFolder}/Drone.asset";
            var existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null)
                return existing;

            // Os valores padrão do EnemyData já são os do Drone.
            var data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static GameObject CreateDronePrefab(Sprite sprite, EnemyData data)
        {
            var go = new GameObject("Drone");
            go.transform.localScale = Vector3.one * 0.6f;
            go.tag = "Enemy";
            SetLayer(go, "Enemy");

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.45f, 0.25f);
            renderer.sortingLayerName = "Enemies";

            // Kinematic: anda por código, mas os triggers continuam funcionando.
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            var enemy = go.AddComponent<Enemy>();
            SetReference(enemy, "data", data);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/Drone.prefab");
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------- Objetos da cena ----------

        private static void ConfigureCamera()
        {
            Camera cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f; // 32 x 18 unidades em 16:9
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.03f, 0.09f);
        }

        private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector2 position, float scale, Color color, string sortingLayer, int order)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return go;
        }

        private static LightBridge CreateBridge(Material material)
        {
            var go = new GameObject("LightBridge_A");
            LightBridge bridge = go.AddComponent<LightBridge>(); // o RequireComponent já adiciona o LineRenderer

            for (int i = 0; i < Waypoints.Length; i++)
            {
                var waypoint = new GameObject($"WP_{i:00}");
                waypoint.transform.SetParent(go.transform, false);
                waypoint.transform.position = Waypoints[i];
            }

            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.startWidth = line.endWidth = 0.35f;
            line.numCornerVertices = 6;
            line.numCapVertices = 6;
            line.startColor = line.endColor = new Color(0.3f, 0.9f, 1f);
            line.sortingLayerName = "Bridges";

            bridge.Rebuild();
            return bridge;
        }

        private static void CreateMotherShip(Sprite sprite)
        {
            // Sprite de 1 unidade com escala 4 = 4 unidades de diâmetro, centro em x = 13,5.
            // A borda esquerda fica em x = 11,5, antes do último waypoint (x = 12).
            GameObject go = CreateSpriteObject("MotherShip", sprite, new Vector2(13.5f, 0f), 4f, new Color(0.35f, 0.6f, 1f), "Ships", 1);
            go.tag = "MotherShip";

            var collider = go.AddComponent<CircleCollider2D>(); // antes do MotherShip, que exige um Collider2D
            collider.isTrigger = true;
            collider.radius = 0.5f;

            go.AddComponent<MotherShip>();
        }

        // ---------- Utilitários ----------

        private static void SetLayer(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
                Debug.LogWarning($"A layer {layerName} não existe. Crie em Edit → Project Settings → Tags and Layers.");
            else
                go.layer = layer;
        }

        // Preenche um campo [SerializeField] private pelo nome, como o Inspector faria.
        private static void SetReference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
