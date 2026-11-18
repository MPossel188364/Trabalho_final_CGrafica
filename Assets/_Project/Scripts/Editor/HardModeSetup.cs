using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NebulaGuard.EditorTools
{
    /// <summary>
    /// Ferramenta de editor: acrescenta à cena Game o que o modo Difícil precisa, em
    /// NebulaGuard → Adicionar ponte do Difícil.
    ///  - LightBridge_B: uma linha reta em y = 0, do portal até a nave-mãe, curta e rápida
    ///    (a ponte A é a longa, em zigue-zague). Nasce como cópia da ponte A, então herda
    ///    material, cor, largura e camada de desenho.
    ///  - Portal_B: cópia do Portal_A, em (-14,8; 0).
    ///  - Liga os dois no WaveSpawner (Bridges e Second Bridge Objects). Quem decide se a ponte
    ///    B é usada é a dificuldade (Bridge Count): no Fácil ela e o portal ficam desligados.
    ///  - Se a cena MainMenu já existe, inclui o Dificil na lista de dificuldades dela.
    /// Só acrescenta: o que já existe (pela busca do nome) é mantido, então rodar de novo não
    /// duplica nada. Para mudar o caminho da ponte B, mexa nos waypoints dela na cena.
    /// Fica numa pasta "Editor": só existe dentro do Unity e não entra no build do jogo.
    /// </summary>
    public static class HardModeSetup
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string HardDataPath = "Assets/_Project/Data/Dificil.asset";

        // Reta em y = 0: passa entre as duas fileiras de slots (y = ±2), dentro do alcance
        // de todas as naves, e termina no mesmo ponto da ponte A (o centro da nave-mãe).
        // Começa na borda do portal (x = -13,4), não no centro, para a linha não aparecer por cima dele.
        private static readonly Vector2[] BridgeBPoints =
        {
            new Vector2(-13.4f, 0f),
            new Vector2(13.8f, 0f),
        };

        private static readonly Vector2 PortalBPosition = new Vector2(-14.8f, 0f);

        [MenuItem("NebulaGuard/Adicionar ponte do Difícil")]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("Adicionar ponte do Difícil",
                    "Vai abrir a cena Game e acrescentar a LightBridge_B e o Portal_B (ligados no WaveSpawner). " +
                    "As cenas Game e MainMenu são salvas no final. Continuar?", "Continuar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (!AddBridgeToGame())
                return;

            AddHardToMenu();

            AssetDatabase.SaveAssets();
            Debug.Log("Ponte do Difícil adicionada. Abra o menu, escolha Difícil e dê Play: os inimigos se alternam entre a ponte A (zigue-zague) e a B (reta).");
        }

        // ---------- Cena Game ----------

        private static bool AddBridgeToGame()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath);

            GameObject bridgeA = GameObject.Find("LightBridge_A");
            GameObject portalA = GameObject.Find("Portal_A");
            var spawner = Object.FindFirstObjectByType<WaveSpawner>();
            if (bridgeA == null || portalA == null || spawner == null)
            {
                Debug.LogError("Não achei LightBridge_A, Portal_A ou o WaveSpawner na cena Game.");
                return false;
            }

            LightBridge bridgeB = GetOrCreateBridgeB(bridgeA);
            GameObject portalB = GetOrCreatePortalB(portalA);

            var serialized = new SerializedObject(spawner);
            AddIfMissing(serialized.FindProperty("bridges"), bridgeB);
            AddIfMissing(serialized.FindProperty("secondBridgeObjects"), portalB);
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return true;
        }

        private static LightBridge GetOrCreateBridgeB(GameObject bridgeA)
        {
            GameObject existing = GameObject.Find("LightBridge_B");
            if (existing != null)
                return existing.GetComponent<LightBridge>();

            GameObject copy = Object.Instantiate(bridgeA);
            copy.name = "LightBridge_B";
            Undo.RegisterCreatedObjectUndo(copy, "Criar LightBridge_B");

            // A cópia veio com os waypoints da ponte A: troca pelos da B.
            for (int i = copy.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(copy.transform.GetChild(i).gameObject);

            for (int i = 0; i < BridgeBPoints.Length; i++)
            {
                var waypoint = new GameObject($"wp_{i:00}");
                waypoint.transform.SetParent(copy.transform, false);
                waypoint.transform.position = BridgeBPoints[i];
            }

            var bridge = copy.GetComponent<LightBridge>();
            bridge.Rebuild();
            return bridge;
        }

        private static GameObject GetOrCreatePortalB(GameObject portalA)
        {
            GameObject existing = GameObject.Find("Portal_B");
            if (existing != null)
                return existing;

            GameObject copy = Object.Instantiate(portalA);
            copy.name = "Portal_B";
            copy.transform.position = PortalBPosition;
            Undo.RegisterCreatedObjectUndo(copy, "Criar Portal_B");
            return copy;
        }

        // ---------- Menu ----------

        // O MainMenuSetup só preenche a lista de dificuldades quando ela está vazia; se o
        // menu foi montado antes do Dificil existir, é aqui que ele entra.
        private static void AddHardToMenu()
        {
            if (!File.Exists(MenuScenePath))
                return; // o MainMenuSetup, ao rodar, já acha o Dificil em Data/

            var hard = AssetDatabase.LoadAssetAtPath<DifficultyData>(HardDataPath);
            if (hard == null)
            {
                Debug.LogWarning($"Não achei {HardDataPath}: o Difícil não foi incluído no menu.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MenuScenePath);
            var menu = Object.FindFirstObjectByType<MainMenu>();
            if (menu == null)
                return;

            var serialized = new SerializedObject(menu);
            AddIfMissing(serialized.FindProperty("difficulties"), hard);
            serialized.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // Deixa a cena do jogo aberta, que é onde a ponte nova está.
            EditorSceneManager.OpenScene(GameScenePath);
        }

        // ---------- Utilitário ----------

        private static void AddIfMissing(SerializedProperty array, Object value)
        {
            for (int i = 0; i < array.arraySize; i++)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == value)
                    return;
            }

            array.arraySize++;
            array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = value;
        }
    }
}
