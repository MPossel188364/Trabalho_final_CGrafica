using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Uma camada do fundo de estrelas. Espalha estrelas numa área um pouco maior que a tela
    /// e as faz deslizar para a esquerda; a que sai por um lado reaparece do outro, então o
    /// fundo nunca acaba. Várias camadas com velocidades diferentes dão a sensação de
    /// profundidade (parallax): o que está longe parece andar mais devagar.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Tooltip("Material do URP para as estrelas. Use Sprite-Unlit-Default: elas brilham sozinhas e pegam o Bloom.")]
        [SerializeField] private Material starMaterial;

        [Header("Estrelas")]
        [Min(1)] [SerializeField] private int starCount = 120;

        [Tooltip("Tamanho mínimo e máximo de cada estrela, em unidades.")]
        [SerializeField] private Vector2 sizeRange = new Vector2(0.04f, 0.09f);

        [Tooltip("Brilho mínimo e máximo (0 = invisível, 1 = cor cheia). Acima do Threshold do Bloom (0,6) a estrela ganha halo.")]
        [SerializeField] private Vector2 brightnessRange = new Vector2(0.3f, 0.7f);

        [SerializeField] private Color tint = Color.white;

        [Header("Movimento")]
        [Tooltip("Unidades por segundo para a esquerda. Camadas do fundo: devagar; da frente: mais rápido.")]
        [SerializeField] private float speed = 0.3f;

        [Tooltip("Área coberta pelas estrelas, centrada na câmera. A tela tem 32 x 18; um pouco a mais esconde a troca de lado.")]
        [SerializeField] private Vector2 area = new Vector2(34f, 20f);

        [Header("Desenho")]
        [Tooltip("Ordem dentro da Sorting Layer Background: camadas mais distantes com número menor.")]
        [SerializeField] private int sortingOrder;

        [Tooltip("Mesma semente = mesmas posições a cada partida.")]
        [SerializeField] private int seed = 1;

        // Uma textura só para todas as camadas: um círculo branco com borda suave.
        private static Sprite starSprite;

        private Transform[] stars;

        private void Start()
        {
            if (starMaterial == null)
                Debug.LogWarning($"{name}: falta o Star Material (Sprite-Unlit-Default).", this);

            CreateStars();
        }

        private void Update()
        {
            // Time.deltaTime: o fundo para junto com o jogo na pausa.
            float step = speed * Time.deltaTime;
            float left = transform.position.x - area.x * 0.5f;

            foreach (Transform star in stars)
            {
                Vector3 p = star.position;
                p.x -= step;
                if (p.x < left)
                    p.x += area.x; // saiu pela esquerda: volta pela direita
                star.position = p;
            }
        }

        private void CreateStars()
        {
            // Random próprio da camada, para não mexer no aleatório do resto do jogo.
            var random = new System.Random(seed);
            Sprite sprite = GetStarSprite();
            stars = new Transform[starCount];

            for (int i = 0; i < starCount; i++)
            {
                var star = new GameObject($"Star_{i:000}");
                star.transform.SetParent(transform, false);

                float x = Lerp(random, -area.x * 0.5f, area.x * 0.5f);
                float y = Lerp(random, -area.y * 0.5f, area.y * 0.5f);
                star.transform.localPosition = new Vector3(x, y, 0f);

                float size = Lerp(random, sizeRange.x, sizeRange.y);
                star.transform.localScale = new Vector3(size, size, 1f);

                var renderer = star.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                if (starMaterial != null)
                    renderer.sharedMaterial = starMaterial;
                renderer.sortingLayerName = "Background";
                renderer.sortingOrder = sortingOrder;

                float brightness = Lerp(random, brightnessRange.x, brightnessRange.y);
                renderer.color = new Color(tint.r * brightness, tint.g * brightness, tint.b * brightness, 1f);

                stars[i] = star.transform;
            }
        }

        private static float Lerp(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Gera por código um círculo branco de 32 x 32 pixels com a borda se apagando aos
        /// poucos. Assim o fundo não depende de nenhuma imagem importada.
        /// </summary>
        private static Sprite GetStarSprite()
        {
            if (starSprite != null)
                return starSprite;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - distance); // 1 no centro, 0 na borda
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }
            texture.Apply();

            // pixelsPerUnit = 32: o sprite mede 1 unidade, e a escala da estrela vira o tamanho dela.
            starSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return starSprite;
        }
    }
}
