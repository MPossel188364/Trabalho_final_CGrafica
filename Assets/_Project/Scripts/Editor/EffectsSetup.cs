using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NebulaGuard.EditorTools
{
    /// <summary>
    /// Ferramenta de editor: NebulaGuard → Adicionar explosões e tremida.
    ///  - Cria o prefab Prefabs/Effects/Explosao: um Particle System com um clarão (filho
    ///    Flash) e faíscas que saem do centro, perdem velocidade, mudam de amarelo para
    ///    vermelho e somem. O desenho de cada partícula é Art/Effects/Particle.png.
    ///  - Põe o DeathEffect nos três inimigos (explosão maior no Couraçado).
    ///  - Liga a explosão como efeito de impacto nos três projéteis (no míssil do
    ///    Bombardeiro ela fica do tamanho do raio de dano).
    ///  - Põe o CameraShake na Main Camera da cena Game, ligado à nave-mãe.
    /// Só acrescenta: prefab que já existe não é recriado (ajustes feitos no Inspector
    /// ficam) e campo já preenchido não é trocado. Para recriar a explosão do zero, apague o
    /// prefab e rode de novo.
    /// </summary>
    public static class EffectsSetup
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        internal const string ParticleSpritePath = "Assets/_Project/Art/Effects/Particle.png";
        private const string EffectsFolder = "Assets/_Project/Prefabs/Effects";
        private const string ExplosionPath = EffectsFolder + "/Explosao.prefab";
        private const string Prefabs = "Assets/_Project/Prefabs/";

        // GUID do Sprite-Unlit-Default do URP: as partículas brilham sozinhas e pegam o Bloom.
        private const string UnlitMaterialGuid = "9dfc825aed78fcd4ba02077103263b40";

        // Tamanho da explosão de cada inimigo (1 = a do prefab, ~1 unidade de raio).
        private static readonly (string prefab, float size)[] Enemies =
        {
            ("Drone", 0.7f),
            ("CacaAlienigena", 0.9f),
            ("Couracado", 1.5f),
        };

        // Tamanho do impacto dos tiros sem área. O míssil usa o raio de dano no lugar deste.
        private static readonly (string prefab, float size)[] Projectiles =
        {
            ("Laser_Caca", 0.25f),
            ("Missil_Bombardeiro", 0.4f),
            ("Raio_Cruzador", 0.45f),
        };

        [MenuItem("NebulaGuard/Adicionar explosões e tremida")]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("Adicionar explosões e tremida",
                    "Vai criar o prefab da explosão, ligá-lo nos inimigos e projéteis e pôr a tremida " +
                    "na câmera da cena Game (a cena é salva no final). Continuar?", "Continuar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Sprite particle = PrepareSprite(ParticleSpritePath, "gerar_particula.py");
            if (particle == null)
                return;

            ParticleSystem explosion = GetOrCreateExplosion(particle);
            if (explosion == null)
                return;

            foreach (var (prefab, size) in Enemies)
                EditPrefab(prefab, root =>
                {
                    // "??" não serve com objetos do Unity (o null deles é "falso"): testa com if.
                    var effect = root.GetComponent<DeathEffect>();
                    if (effect == null)
                        effect = root.AddComponent<DeathEffect>();
                    var serialized = new SerializedObject(effect);
                    SetIfEmpty(serialized, "explosionPrefab", explosion, "size", size);
                });

            foreach (var (prefab, size) in Projectiles)
                EditPrefab(prefab, root =>
                {
                    var projectile = root.GetComponent<Projectile>();
                    if (projectile == null)
                        return;
                    var serialized = new SerializedObject(projectile);
                    SetIfEmpty(serialized, "impactEffectPrefab", explosion, "impactEffectSize", size);
                });

            AddCameraShake();

            AssetDatabase.SaveAssets();
            Debug.Log("Explosões e tremida prontas. Dê Play: inimigo destruído explode, o míssil explode do tamanho da área de dano e a câmera treme quando um inimigo chega à nave-mãe.");
        }

        // ---------- Auxiliares (também usados pelo PortalSetup) ----------

        /// <summary>Importa um PNG gerado pelos scripts de tools/ como sprite único, a 100 px por unidade.</summary>
        internal static Sprite PrepareSprite(string path, string generatorScript)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                Debug.LogError($"Não achei {path}. Gere com: python3 tools/{generatorScript} {path}");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        internal static Material LoadUnlitMaterial()
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(UnlitMaterialGuid);
            var material = string.IsNullOrEmpty(materialPath) ? null : AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
                Debug.LogError("Não achei o Sprite-Unlit-Default do URP.");
            return material;
        }

        // ---------- Prefab da explosão ----------

        private static ParticleSystem GetOrCreateExplosion(Sprite particle)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ParticleSystem>(ExplosionPath);
            if (existing != null)
                return existing;

            Material material = LoadUnlitMaterial();
            if (material == null)
                return null;

            if (!AssetDatabase.IsValidFolder(EffectsFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Effects");

            var root = new GameObject("Explosao");
            BuildSparks(root.AddComponent<ParticleSystem>(), particle, material);

            var flash = new GameObject("Flash");
            flash.transform.SetParent(root.transform, false);
            BuildFlash(flash.AddComponent<ParticleSystem>(), particle, material);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ExplosionPath);
            Object.DestroyImmediate(root);
            return saved.GetComponent<ParticleSystem>();
        }

        /// <summary>
        /// Faíscas: 24 partículas de uma vez (burst), saindo em círculo do centro.
        /// O arrasto (drag) freia cada uma, então a explosão "abre" rápido e para; a cor vai
        /// de amarelo claro (brilha com o Bloom) a vermelho escuro (não brilha) e some.
        /// </summary>
        private static void BuildSparks(ParticleSystem ps, Sprite particle, Material material)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // a duração só muda com ele parado

            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f), new Color(1f, 0.65f, 0.25f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy; // segue a escala do objeto (tamanho do efeito)
            main.stopAction = ParticleSystemStopAction.Destroy;     // apaga o objeto quando a última partícula morre
            main.maxParticles = 64;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)24) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle; // círculo no plano XY: espalha para todos os lados da tela
            shape.radius = 0.1f;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f; // sem teto de velocidade; só o arrasto abaixo importa
            limit.drag = 4f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = Gradient(
                new[] { new GradientColorKey(new Color(1f, 1f, 0.85f), 0f), new GradientColorKey(new Color(1f, 0.55f, 0.15f), 0.35f), new GradientColorKey(new Color(0.6f, 0.1f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) });

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.2f)));

            ConfigureLook(ps, particle, material, sortingOrder: 1);
        }

        /// <summary>Clarão: uma partícula grande e branca que cresce e some em 0,15 s, atrás das faíscas.</summary>
        private static void BuildFlash(ParticleSystem ps, Sprite particle, Material material)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.15f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = 0.15f;
            main.startSpeed = 0f;
            main.startSize = 1.6f;
            main.startColor = new Color(1f, 1f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 1;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });

            var shape = ps.shape;
            shape.enabled = false; // nasce no centro

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = Gradient(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1f)));

            ConfigureLook(ps, particle, material, sortingOrder: 0);
        }

        /// <summary>
        /// O desenho da partícula vem do módulo Texture Sheet Animation (modo Sprites): assim o
        /// material continua sendo o Sprite-Unlit-Default do URP, sem criar material novo.
        /// </summary>
        internal static void ConfigureLook(ParticleSystem ps, Sprite particle, Material material, int sortingOrder, string sortingLayer = "Effects")
        {
            var sheet = ps.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites;
            sheet.AddSprite(particle);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = sortingOrder;
        }

        internal static Gradient Gradient(GradientColorKey[] colors, GradientAlphaKey[] alphas)
        {
            var gradient = new Gradient();
            gradient.SetKeys(colors, alphas);
            return gradient;
        }

        // ---------- Inimigos e projéteis ----------

        private static void EditPrefab(string name, System.Action<GameObject> edit)
        {
            string path = $"{Prefabs}{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                Debug.LogWarning($"Não achei {path}; pulei.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        /// <summary>Preenche o efeito e o tamanho só se o efeito ainda estiver vazio (não desfaz ajustes).</summary>
        private static void SetIfEmpty(SerializedObject serialized, string effectField, Object effect, string sizeField, float size)
        {
            var effectProperty = serialized.FindProperty(effectField);
            if (effectProperty.objectReferenceValue != null)
                return;

            effectProperty.objectReferenceValue = effect;
            serialized.FindProperty(sizeField).floatValue = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Câmera ----------

        private static void AddCameraShake()
        {
            var scene = EditorSceneManager.OpenScene(GameScenePath);

            Camera camera = Camera.main;
            var motherShip = Object.FindFirstObjectByType<MotherShip>();
            if (camera == null || motherShip == null)
            {
                Debug.LogError("Não achei a Main Camera (tag MainCamera) ou a MotherShip na cena Game; a tremida não foi adicionada.");
                return;
            }

            var shake = camera.GetComponent<CameraShake>();
            if (shake == null)
                shake = Undo.AddComponent<CameraShake>(camera.gameObject);

            var serialized = new SerializedObject(shake);
            var motherShipProperty = serialized.FindProperty("motherShip");
            if (motherShipProperty.objectReferenceValue == null)
            {
                motherShipProperty.objectReferenceValue = motherShip;
                serialized.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
