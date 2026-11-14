using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NebulaGuard
{
    /// <summary>
    /// Liga o clique do jogador à construção: clicar num slot livre abre o menu; escolher
    /// uma nave no menu cobra os créditos (GameSession) e posiciona a nave (BuildSlot).
    /// Cada peça faz só a parte dela; este componente só conversa com as três.
    /// </summary>
    public class BuildController : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private BuildMenuUI menu;

        [Tooltip("Naves que o jogador pode construir, na ordem do menu. O custo vem do TowerData de cada uma.")]
        [SerializeField] private TowerShip[] towerPrefabs;

        private Camera cam;
        private BuildSlot selected;

        /// <summary>Verdadeiro enquanto o menu de construção está aberto.</summary>
        public bool IsMenuOpen => selected != null;

        private void Awake()
        {
            cam = Camera.main;
        }

        private void OnEnable()
        {
            menu.OptionChosen += OnOptionChosen;
            session.CreditsChanged += menu.SetCredits;
        }

        private void OnDisable()
        {
            menu.OptionChosen -= OnOptionChosen;
            session.CreditsChanged -= menu.SetCredits;
        }

        private void Update()
        {
            // O Esc é tratado pelo GameManager (fecha este menu ou pausa o jogo),
            // para as duas coisas não acontecerem no mesmo aperto.
            Mouse mouse = Mouse.current;

            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            // Clique num botão do menu: quem trata é o próprio botão.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(world);

            if (hit != null && hit.TryGetComponent(out BuildSlot slot) && slot.IsFree)
                OpenMenu(slot);
            else
                CloseMenu();
        }

        private void OpenMenu(BuildSlot slot)
        {
            selected = slot;
            menu.Show(slot.transform.position, towerPrefabs, session.Credits);
        }

        public void CloseMenu()
        {
            selected = null;
            menu.Hide();
        }

        private void OnOptionChosen(int index)
        {
            if (selected == null || !selected.IsFree || index < 0 || index >= towerPrefabs.Length)
                return;

            TowerShip prefab = towerPrefabs[index];
            int cost = prefab.Data.Cost;

            if (!session.TrySpend(cost))
                return;

            // Se por algum motivo a nave não foi posicionada, devolve os créditos.
            if (!selected.TryPlace(prefab))
                session.AddCredits(cost);

            CloseMenu();
        }
    }
}
