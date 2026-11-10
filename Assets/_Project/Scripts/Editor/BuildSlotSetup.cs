using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NebulaGuard.EditorTools
{
    /// <summary>
    /// Ferramenta de editor: acrescenta à cena Game o que o sistema de construção precisa,
    /// em NebulaGuard → Adicionar slots de construção. Cria o prefab do slot, 6 slots nos
    /// corredores da ponte, o GameSession e o BuildController, e remove o Caça solto
    /// (provisório até existirem os slots).
    /// Só acrescenta: o que já existe na cena (slots, GameSession, BuildController) é
    /// mantido, então rodar de novo não duplica nada nem apaga edições.
    /// Fica numa pasta "Editor": só existe dentro do Unity e não entra no build do jogo.
    /// </summary>
    public static class BuildSlotSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string SlotPrefabPath = "Assets/_Project/Prefabs/BuildSlot.prefab";
        private const string TowerPrefabPath = "Assets/_Project/Prefabs/Caca.prefab";

        // GUID do Sprite-Unlit-Default do URP (o mesmo material das pontes de luz).
        private const string UnlitMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

        // Dois slots em cada corredor entre as colunas da ponte (x = -6, 0 e 6), longe dos
        // trechos horizontais da ponte e dentro do alcance (3,5) das colunas vizinhas.
        private static readonly Vector2[] SlotPositions =
        {
            new Vector2(-6f, 2f), new Vector2(-6f, -2f),
            new Vector2(0f, 2f),  new Vector2(0f, -2f),
            new Vector2(6f, 2f),  new Vector2(6f, -2f),
        };

        [MenuItem("NebulaGuard/Adicionar slots de construção")]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("Adicionar slots de construção",
                    "Vai abrir a cena Game e acrescentar os slots, o GameSession e o BuildController. " +
                    "O Caça solto na cena será removido. Continuar?", "Continuar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.OpenScene(ScenePath);

            var towerPrefab = AssetDatabase.LoadAssetAtPath<TowerShip>(TowerPrefabPath);
            if (towerPrefab == null)
            {
                Debug.LogError($"Não achei o prefab {TowerPrefabPath}.");
                return;
            }

            BuildSlot slotPrefab = GetOrCreateSlotPrefab();
            CreateSlots(slotPrefab);
            GameSession session = GetOrCreateSession();
            CreateController(session, towerPrefab);
            RemoveLooseTowers();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Debug.Log("Slots de construção adicionados à cena Game. Dê Play: clique num círculo ciano, escolha o Caça no menu e ele é posicionado (-50 créditos).");
        }

        // ---------- Prefab do slot ----------

        private static BuildSlot GetOrCreateSlotPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<BuildSlot>(SlotPrefabPath);
            if (existing != null)
                return existing;

            var go = new GameObject("BuildSlot");
            go.transform.localScale = Vector3.one * 2f;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"); // círculo provisório
            renderer.color = new Color(0.25f, 0.9f, 1f, 0.4f);
            renderer.sortingLayerName = "Ships";
            renderer.sortingOrder = -1;

            // Sem luz: o marcador brilha com a própria cor, como as pontes.
            string materialPath = AssetDatabase.GUIDToAssetPath(UnlitMaterialGuid);
            if (string.IsNullOrEmpty(materialPath))
                Debug.LogWarning("Não achei o Sprite-Unlit-Default do URP. Arraste o material à mão no prefab BuildSlot.");
            else
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            // Trigger: só serve para o clique detectar o slot, sem interferir na física.
            var collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = renderer.sprite.bounds.extents.x;

            var slot = go.AddComponent<BuildSlot>();
            SetReference(slot, "marker", renderer);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, SlotPrefabPath);
            Object.DestroyImmediate(go);
            return saved.GetComponent<BuildSlot>();
        }

        // ---------- Objetos da cena ----------

        private static void CreateSlots(BuildSlot slotPrefab)
        {
            if (GameObject.Find("BuildSlots") != null)
            {
                Debug.Log("O objeto BuildSlots já existe na cena: slots mantidos.");
                return;
            }

            var parent = new GameObject("BuildSlots");
            for (int i = 0; i < SlotPositions.Length; i++)
            {
                var slot = (BuildSlot)PrefabUtility.InstantiatePrefab(slotPrefab);
                slot.name = $"Slot_{i + 1:00}";
                slot.transform.SetParent(parent.transform, false);
                slot.transform.position = SlotPositions[i];
            }
        }

        private static GameSession GetOrCreateSession()
        {
            var session = Object.FindFirstObjectByType<GameSession>();
            if (session != null)
                return session;

            return new GameObject("GameSession").AddComponent<GameSession>();
        }

        private static void CreateController(GameSession session, TowerShip towerPrefab)
        {
            var controller = Object.FindFirstObjectByType<BuildController>();
            BuildMenuUI menu;

            if (controller == null)
            {
                var go = new GameObject("BuildController");
                menu = go.AddComponent<BuildMenuUI>();
                controller = go.AddComponent<BuildController>();
            }
            else
            {
                menu = controller.GetComponent<BuildMenuUI>();
                if (menu == null)
                    menu = controller.gameObject.AddComponent<BuildMenuUI>();
            }

            SetReference(controller, "session", session);
            SetReference(controller, "menu", menu);

            // A lista de naves só é preenchida se estiver vazia, para não apagar edições.
            var serialized = new SerializedObject(controller);
            SerializedProperty towers = serialized.FindProperty("towerPrefabs");
            if (towers.arraySize == 0)
            {
                towers.arraySize = 1;
                towers.GetArrayElementAtIndex(0).objectReferenceValue = towerPrefab;
                serialized.ApplyModifiedProperties();
            }
        }

        // O Caça solto em (-6, 0) era provisório "até o BuildSlot". Agora a nave nasce nos slots.
        private static void RemoveLooseTowers()
        {
            foreach (TowerShip tower in Object.FindObjectsByType<TowerShip>(FindObjectsSortMode.None))
            {
                if (tower.name != "Caca")
                    continue;

                Debug.Log("Removendo o Caça solto da cena (agora a nave é construída nos slots).");
                Object.DestroyImmediate(tower.gameObject);
            }
        }

        // ---------- Utilitário ----------

        // Preenche um campo [SerializeField] private pelo nome, como o Inspector faria.
        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
