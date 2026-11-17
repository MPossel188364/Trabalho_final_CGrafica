using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace NebulaGuard
{
    /// <summary>
    /// Menu que aparece ao lado do slot clicado. Slot livre: um botão por nave (desenho, nome
    /// e custo) e o saldo. Slot ocupado: um botão de venda com o valor devolvido.
    /// Só desenha e avisa qual botão foi apertado (OptionChosen, SellChosen); quem cobra,
    /// posiciona e vende é o BuildController. O Canvas, o painel e os botões são criados por
    /// código; no Inspector ficam só a fonte e as imagens (a ferramenta
    /// NebulaGuard → Aplicar estilo ao HUD já preenche). Sem elas, o menu usa cores lisas.
    /// </summary>
    public class BuildMenuUI : MonoBehaviour
    {
        private const float PanelOffset = 50f;
        private const float ButtonHeight = 68f;
        private const float ButtonMinWidth = 380f;
        private const float AffordableAlpha = 1f;
        private const float UnaffordableAlpha = 0.45f;

        private static readonly Color PanelColor = new Color(0.04f, 0.08f, 0.19f, 0.92f);
        private static readonly Color FallbackBuyColor = new Color(0.12f, 0.35f, 0.5f, 1f);
        private static readonly Color FallbackSellColor = new Color(0.55f, 0.16f, 0.16f, 1f);
        private static readonly Color Gold = new Color(1f, 0.83f, 0.29f);

        // Os botões da Kenney são brancos com borda colorida: o texto dentro deles é escuro.
        private static readonly Color ButtonText = new Color(0.1f, 0.13f, 0.22f);
        private static readonly Color ButtonCost = new Color(0.62f, 0.42f, 0f);
        private static readonly Color TooExpensive = new Color(0.8f, 0.15f, 0.12f);

        [Header("Estilo (opcional)")]
        [Tooltip("Font Asset do TextMeshPro (kenvector_future SDF).")]
        [SerializeField] private TMP_FontAsset font;
        [Tooltip("Botão de compra (Art/UI/ButtonBlue), com borda para 9-slice.")]
        [SerializeField] private Sprite buyButtonSprite;
        [Tooltip("Botão de venda (Art/UI/ButtonRed).")]
        [SerializeField] private Sprite sellButtonSprite;
        [Tooltip("Ícone da moeda (Art/UI/IconCredits).")]
        [SerializeField] private Sprite coinIcon;

        /// <summary>Disparado com o índice da nave escolhida (posição na lista do BuildController).</summary>
        public event Action<int> OptionChosen;

        /// <summary>Disparado quando o jogador aperta "Vender" no menu de um slot ocupado.</summary>
        public event Action SellChosen;

        // Um botão do menu e o que muda nele quando o saldo muda.
        private class Option
        {
            public Button Button;
            public CanvasGroup Group;
            public TMP_Text Cost;
            public Color CostColor;
            public int Price;
        }

        private RectTransform canvasRect;
        private RectTransform panel;
        private TMP_Text titleLabel;
        private TMP_Text creditsLabel;
        private readonly List<Option> options = new List<Option>();
        private int credits;

        private void Awake()
        {
            EnsureEventSystem();
            BuildCanvas();
            panel.gameObject.SetActive(false);
        }

        /// <summary>Abre o menu de compra ao lado do ponto do mundo, com um botão por nave da lista.</summary>
        public void Show(Vector3 worldPosition, IReadOnlyList<TowerShip> towers, int currentCredits)
        {
            ClearOptions();
            titleLabel.text = "Construir";

            for (int i = 0; i < towers.Count; i++)
            {
                int index = i; // cópia para o botão lembrar o próprio índice
                TowerData data = towers[i].Data;
                AddOption(TowerSprite(towers[i]), data.DisplayName, data.Cost.ToString(), data.Cost,
                    buyButtonSprite, FallbackBuyColor, () => OptionChosen?.Invoke(index));
            }

            Open(worldPosition, currentCredits);
        }

        /// <summary>Abre o menu de venda de um slot ocupado: um botão com o valor que volta.</summary>
        public void ShowSell(Vector3 worldPosition, TowerShip tower, int refund, int currentCredits)
        {
            ClearOptions();
            titleLabel.text = tower.Data.DisplayName;

            // Preço 0: vender sempre está liberado, com qualquer saldo.
            AddOption(TowerSprite(tower), "Vender", $"+{refund}", 0,
                sellButtonSprite, FallbackSellColor, () => SellChosen?.Invoke());

            Open(worldPosition, currentCredits);
        }

        public void Hide()
        {
            panel.gameObject.SetActive(false);
        }

        /// <summary>Atualiza o saldo mostrado (e quais botões estão liberados) enquanto o menu está aberto.</summary>
        public void SetCredits(int currentCredits)
        {
            credits = currentCredits;
            if (panel.gameObject.activeSelf)
                RefreshState();
        }

        private void Open(Vector3 worldPosition, int currentCredits)
        {
            credits = currentCredits;
            RefreshState();

            panel.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            PlaceAt(worldPosition);
        }

        // Nave que não dá para pagar: botão desligado, apagado e com o custo em vermelho.
        private void RefreshState()
        {
            creditsLabel.text = credits.ToString();
            foreach (Option option in options)
            {
                bool affordable = credits >= option.Price;
                option.Button.interactable = affordable;
                option.Group.alpha = affordable ? AffordableAlpha : UnaffordableAlpha;
                if (option.Price > 0)
                    option.Cost.color = affordable ? option.CostColor : TooExpensive;
            }
        }

        // Posiciona o painel logo acima do slot, sem sair da tela.
        private void PlaceAt(Vector3 worldPosition)
        {
            Vector2 screen = Camera.main.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);

            Rect area = canvasRect.rect;
            Vector2 size = panel.rect.size;
            local.y += PanelOffset;
            local.x = Mathf.Clamp(local.x, area.xMin + size.x / 2f, area.xMax - size.x / 2f);
            local.y = Mathf.Clamp(local.y, area.yMin, area.yMax - size.y);
            panel.anchoredPosition = local;
        }

        private static Sprite TowerSprite(TowerShip tower)
        {
            var renderer = tower.GetComponentInChildren<SpriteRenderer>();
            return renderer != null ? renderer.sprite : null;
        }

        // ---------- Montagem da interface por código ----------

        private void BuildCanvas()
        {
            var canvasObject = new GameObject("BuildMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = canvasObject.GetComponent<RectTransform>();

            var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelObject.transform.SetParent(canvasObject.transform, false);
            panelObject.GetComponent<Image>().color = PanelColor;

            panel = panelObject.GetComponent<RectTransform>();
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f); // âncora no centro do Canvas
            panel.pivot = new Vector2(0.5f, 0f);                          // cresce para cima a partir do slot

            var layout = panelObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 16);
            layout.spacing = 10f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = panelObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Cabeçalho: título à esquerda, saldo (moeda + número) à direita.
            RectTransform header = CreateRow(panel, "Header", 40f, new RectOffset(4, 4, 0, 0));
            titleLabel = CreateText(header, "Construir", 26, Color.white, TextAlignmentOptions.MidlineLeft, flexible: true);
            CreateIcon(header, coinIcon, 26f);
            creditsLabel = CreateText(header, "0", 26, Gold, TextAlignmentOptions.MidlineRight, flexible: false);
        }

        /// <summary>Um botão: [desenho da nave] [nome] [moeda] [valor].</summary>
        private void AddOption(Sprite icon, string label, string value, int price, Sprite background, Color fallbackColor, Action onClick)
        {
            var buttonObject = new GameObject($"Option_{options.Count}", typeof(RectTransform), typeof(Image), typeof(Button),
                typeof(CanvasGroup), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            buttonObject.transform.SetParent(panel, false);

            var image = buttonObject.GetComponent<Image>();
            Color textColor = Color.white;
            Color costColor = Gold;
            if (background != null)
            {
                image.sprite = background;
                image.type = Image.Type.Sliced; // cantos fixos, miolo estica (borda definida no sprite)
                image.color = Color.white;
                textColor = ButtonText; // fundo claro: texto escuro
                costColor = ButtonCost;
            }
            else
            {
                image.color = fallbackColor; // fundo escuro: texto claro
            }

            var element = buttonObject.GetComponent<LayoutElement>();
            element.minHeight = ButtonHeight;
            element.minWidth = ButtonMinWidth;

            var row = buttonObject.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(14, 20, 8, 8);
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            var rect = buttonObject.GetComponent<RectTransform>();
            CreateIcon(rect, icon, 48f);
            CreateText(rect, label, 26, textColor, TextAlignmentOptions.MidlineLeft, flexible: true);
            CreateIcon(rect, coinIcon, 24f);
            TMP_Text cost = CreateText(rect, value, 26, costColor, TextAlignmentOptions.MidlineRight, flexible: false);

            var button = buttonObject.GetComponent<Button>();
            // A transparência do CanvasGroup já mostra o botão desligado; o tom cinza do
            // Button escureceria a imagem duas vezes.
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.95f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            options.Add(new Option
            {
                Button = button,
                Group = buttonObject.GetComponent<CanvasGroup>(),
                Cost = cost,
                CostColor = costColor,
                Price = price,
            });
        }

        private void ClearOptions()
        {
            foreach (Option option in options)
            {
                // O Destroy só acontece no fim do frame; desativar antes tira o botão
                // do cálculo de layout deste mesmo frame.
                option.Button.gameObject.SetActive(false);
                Destroy(option.Button.gameObject);
            }

            options.Clear();
        }

        private static RectTransform CreateRow(RectTransform parent, string name, float height, RectOffset padding)
        {
            var rowObject = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowObject.transform.SetParent(parent, false);
            rowObject.GetComponent<LayoutElement>().minHeight = height;

            var row = rowObject.GetComponent<HorizontalLayoutGroup>();
            row.padding = padding;
            row.spacing = 8f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return rowObject.GetComponent<RectTransform>();
        }

        private TMP_Text CreateText(RectTransform parent, string content, float size, Color color, TextAlignmentOptions alignment, bool flexible)
        {
            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            if (font != null)
                text.font = font; // sem fonte definida, o TMP usa a padrão dele
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false; // o clique é do botão, não do texto

            // Flexível = ocupa o espaço que sobra na linha (empurra o valor para a direita).
            textObject.GetComponent<LayoutElement>().flexibleWidth = flexible ? 1f : 0f;
            return text;
        }

        private static void CreateIcon(RectTransform parent, Sprite sprite, float size)
        {
            if (sprite == null)
                return;

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            iconObject.transform.SetParent(parent, false);

            var image = iconObject.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var element = iconObject.GetComponent<LayoutElement>();
            element.preferredWidth = size;
            element.preferredHeight = size;
        }

        // Os botões só funcionam com um EventSystem na cena. Cria um se ninguém criou ainda
        // (o projeto usa o novo Input System, então o módulo de entrada é o dele).
        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
