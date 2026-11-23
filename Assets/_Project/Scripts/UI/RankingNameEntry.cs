using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaGuard
{
    /// <summary>
    /// Campo "seu nome" + botão "Salvar" na tela de fim, mostrado só quando a partida entrou
    /// no ranking. A partida já entrou com o último nome usado (o GameManager chama o
    /// Ranking.Submit no fim); aqui o jogador só troca o nome daquela linha.
    ///
    /// Não fica na cena: o HUD acrescenta este componente no EndPanel quando precisa, e ele
    /// monta o campo por código, copiando o visual dos elementos que já existem na tela de fim
    /// (fonte do texto de pontos e botão "Jogar de novo"). Assim não há passo manual no editor.
    /// </summary>
    public class RankingNameEntry : MonoBehaviour
    {
        // Linha do campo, abaixo do texto de pontos; os botões de baixo descem para abrir espaço.
        // Texto de pontos ocupa de -10 a -70; a linha fica de -110 a -190; os botões descem 130
        // (Jogar de novo em -290, Menu principal em -390, ainda longe da borda em -540).
        private const float RowY = -150f;
        private const float ButtonsShift = -130f;
        private static readonly Vector2 FieldSize = new Vector2(480f, 80f);
        private static readonly Vector2 SaveButtonSize = new Vector2(240f, 80f);
        private static readonly Color DarkText = new Color32(0x1A, 0x22, 0x38, 0xFF); // texto sobre botão da Kenney

        private int rankingPosition;
        private TMP_InputField input;
        private Button saveButton;
        private TMP_Text saveLabel;
        private bool saved;

        /// <summary>
        /// Monta o campo. <paramref name="fontSource"/> empresta a fonte; <paramref name="buttonTemplate"/>
        /// é copiado para fazer o "Salvar"; <paramref name="buttonsBelow"/> descem para dar lugar à linha.
        /// </summary>
        public void Setup(int position, TMP_Text fontSource, Button buttonTemplate, params Button[] buttonsBelow)
        {
            rankingPosition = position;

            foreach (Button button in buttonsBelow)
            {
                if (button != null)
                    ((RectTransform)button.transform).anchoredPosition += new Vector2(0f, ButtonsShift);
            }

            Image templateImage = buttonTemplate.GetComponent<Image>();
            float gap = 20f;
            float fieldX = -(SaveButtonSize.x + gap) / 2f;
            float buttonX = (FieldSize.x + gap) / 2f;

            input = CreateInputField(fontSource.font, templateImage, new Vector2(fieldX, RowY));
            CreateSaveButton(buttonTemplate, new Vector2(buttonX, RowY));

            // Já com o cursor no campo e o nome selecionado: é só digitar por cima e dar Enter.
            input.Select();
            input.ActivateInputField();
        }

        private void Save()
        {
            if (saved)
                return;

            saved = true;
            Ranking.SetName(rankingPosition, input.text);
            input.text = Ranking.CleanName(input.text); // mostra como ficou salvo (sem espaços, com limite)
            input.interactable = false;
            saveButton.interactable = false;
            saveLabel.text = "Salvo!";
            AudioManager.PlayClick();
        }

        // ---------- Montagem por código ----------

        private TMP_InputField CreateInputField(TMP_FontAsset font, Image templateImage, Vector2 position)
        {
            var root = new GameObject("NameInput", typeof(RectTransform), typeof(Image));
            // Desligado durante a montagem: o TMP_InputField não pode "acordar" (OnEnable)
            // antes de saber qual é o texto dele.
            root.SetActive(false);
            root.transform.SetParent(transform, false);
            Place((RectTransform)root.transform, position, FieldSize);

            // Mesmo fundo dos botões da Kenney (branco, borda colorida, 9-slice).
            var background = root.GetComponent<Image>();
            background.sprite = templateImage.sprite;
            background.type = Image.Type.Sliced;

            // Área do texto com máscara: nome comprido não vaza para fora da caixa.
            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(root.transform, false);
            var areaRect = (RectTransform)area.transform;
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(24f, 8f);
            areaRect.offsetMax = new Vector2(-24f, -8f);

            TMP_Text placeholder = CreateText(area.transform, "Placeholder", font, "Seu nome");
            placeholder.color = new Color(DarkText.r, DarkText.g, DarkText.b, 0.45f);
            TMP_Text text = CreateText(area.transform, "Text", font, string.Empty);

            var field = root.AddComponent<TMP_InputField>();
            field.targetGraphic = background;
            field.textViewport = areaRect;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = font;
            field.pointSize = 40f;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = Ranking.MaxNameLength;
            field.text = Ranking.LastPlayerName;
            field.onSubmit.AddListener(_ => Save()); // Enter também salva

            root.SetActive(true);
            return field;
        }

        private void CreateSaveButton(Button template, Vector2 position)
        {
            GameObject copy = Instantiate(template.gameObject, transform);
            copy.name = "SaveNameButton";
            Place((RectTransform)copy.transform, position, SaveButtonSize);

            saveButton = copy.GetComponent<Button>();
            saveButton.onClick = new Button.ButtonClickedEvent(); // a cópia não pode reiniciar a partida
            saveButton.onClick.AddListener(Save);

            saveLabel = copy.GetComponentInChildren<TMP_Text>();
            saveLabel.text = "Salvar";
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font, string value)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = obj.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = 40f;
            text.color = DarkText;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.text = value;
            return text;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
