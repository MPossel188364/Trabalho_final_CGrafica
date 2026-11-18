using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NebulaGuard.EditorTools
{
    /// <summary>
    /// Ferramenta de editor: NebulaGuard → Trocar visual dos portais.
    /// Em cada objeto "Portal_..." da cena Game (Portal_A e, se existir, Portal_B):
    ///  - o sprite do próprio portal (o disco voador da Kenney) vira um miolo escuro e
    ///    esfumado, o "buraco";
    ///  - ganha dois filhos com o redemoinho (Art/Effects/PortalSwirl.png): Swirl_Outer, roxo e
    ///    maior, e Swirl_Inner, rosa, menor e girando mais rápido;
    ///  - ganha um filho Sparks, um Particle System de faíscas que saem do centro girando;
    ///  - ganha o PortalVortex, que gira e pulsa as duas camadas.
    /// Filhos que já existem (pelo nome) não são recriados. O HardModeSetup cria o Portal_B
    /// copiando o Portal_A, então rodar esta ferramenta antes ou depois dá no mesmo.
    /// </summary>
    public static class PortalSetup
    {
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string SwirlSpritePath = "Assets/_Project/Art/Effects/PortalSwirl.png";

        // O portal tem escala 1,8 na cena, e os filhos herdam: os tamanhos abaixo são
        // locais. Diâmetro no mundo = 2,56 (imagem) × escala local × 1,8.
        private const float OuterScale = 0.48f; // ≈ 2,2 unidades
        private const float InnerScale = 0.30f; // ≈ 1,4 unidade

        // Roxo e rosa: claros o bastante para passar do Threshold do Bloom (0,6) e brilhar, e
        // diferentes do ciano da ponte e das naves do jogador (o portal é do inimigo).
        private static readonly Color OuterColor = new Color(0.69f, 0.30f, 1f);    // B04CFF
        private static readonly Color InnerColor = new Color(1f, 0.48f, 0.9f, 0.9f); // FF7AE6
        private static readonly Color CoreColor = new Color(0.07f, 0.02f, 0.13f);   // quase preto, roxo

        [MenuItem("NebulaGuard/Trocar visual dos portais")]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("Trocar visual dos portais",
                    "Vai trocar o disco voador dos portais da cena Game por um redemoinho animado " +
                    "(a cena é salva no final). Continuar?", "Continuar", "Cancelar"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Sprite swirl = EffectsSetup.PrepareSprite(SwirlSpritePath, "gerar_portal.py");
            Sprite particle = EffectsSetup.PrepareSprite(EffectsSetup.ParticleSpritePath, "gerar_particula.py");
            Material material = EffectsSetup.LoadUnlitMaterial();
            if (swirl == null || particle == null || material == null)
                return;

            var scene = EditorSceneManager.OpenScene(GameScenePath);

            int count = 0;
            // Inclui inativos: o Portal_B pode estar desligado na cena.
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.parent != null || !t.name.StartsWith("Portal_"))
                    continue;

                RestylePortal(t.gameObject, swirl, particle, material);
                count++;
            }

            if (count == 0)
            {
                Debug.LogError("Não achei nenhum objeto Portal_... na raiz da cena Game.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"{count} portal(is) trocado(s). Dê Play para ver o redemoinho girando; velocidades e pulsação ficam no PortalVortex.");
        }

        private static void RestylePortal(GameObject portal, Sprite swirl, Sprite particle, Material material)
        {
            Undo.RegisterFullObjectHierarchyUndo(portal, "Trocar visual do portal");

            // O próprio portal vira o miolo escuro, atrás de tudo (esconde as estrelas no meio).
            var core = portal.GetComponent<SpriteRenderer>();
            if (core != null)
            {
                core.sprite = particle;
                core.color = CoreColor;
                core.sharedMaterial = material;
                core.sortingLayerName = "Ships";
                core.sortingOrder = 0;
            }

            Transform outer = GetOrCreateLayer(portal, "Swirl_Outer", swirl, material, OuterColor, OuterScale, 1);
            Transform inner = GetOrCreateLayer(portal, "Swirl_Inner", swirl, material, InnerColor, InnerScale, 2);
            GetOrCreateSparks(portal, particle, material);

            var vortex = portal.GetComponent<PortalVortex>();
            if (vortex == null)
            {
                vortex = Undo.AddComponent<PortalVortex>(portal);
                var serialized = new SerializedObject(vortex);
                var layers = serialized.FindProperty("layers");
                layers.arraySize = 2;
                SetLayer(layers.GetArrayElementAtIndex(0), outer, 50f);
                SetLayer(layers.GetArrayElementAtIndex(1), inner, 130f); // a de dentro gira mais rápido
                serialized.ApplyModifiedProperties();
            }
        }

        private static void SetLayer(SerializedProperty element, Transform layer, float degreesPerSecond)
        {
            element.FindPropertyRelative("layer").objectReferenceValue = layer;
            element.FindPropertyRelative("degreesPerSecond").floatValue = degreesPerSecond;
        }

        private static Transform GetOrCreateLayer(GameObject portal, string name, Sprite sprite, Material material,
            Color color, float scale, int sortingOrder)
        {
            Transform existing = portal.transform.Find(name);
            if (existing != null)
                return existing;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Criar camada do portal");
            go.transform.SetParent(portal.transform, false);
            go.transform.localScale = Vector3.one * scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sharedMaterial = material;
            renderer.sortingLayerName = "Ships";
            renderer.sortingOrder = sortingOrder;
            return go.transform;
        }

        /// <summary>
        /// Faíscas que nascem perto do centro e saem girando: Velocity over Lifetime com
        /// Radial (afasta do centro) e Orbital Z (gira em volta do centro). Rodam sem parar
        /// (Looping) e no espaço local, então acompanham o portal.
        /// </summary>
        private static void GetOrCreateSparks(GameObject portal, Sprite particle, Material material)
        {
            if (portal.transform.Find("Sparks") != null)
                return;

            var go = new GameObject("Sparks");
            Undo.RegisterCreatedObjectUndo(go, "Criar faíscas do portal");
            go.transform.SetParent(portal.transform, false);

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 1f), new Color(0.8f, 0.55f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 14f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.15f;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.radial = 0.5f;   // para fora
            velocity.orbitalZ = 2.5f; // girando, no mesmo sentido do redemoinho
            // O módulo exige as três curvas de velocidade no mesmo modo (aqui, constante).
            velocity.x = 0f;
            velocity.y = 0f;
            velocity.z = 0f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = EffectsSetup.Gradient(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });

            EffectsSetup.ConfigureLook(ps, particle, material, sortingOrder: 3, sortingLayer: "Ships");
        }
    }
}
