using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Ponto de ancoragem: um lugar onde o jogador pode posicionar uma nave-torre.
    /// Só sabe se está livre e como instanciar a nave nele. Quem decide cobrar créditos e
    /// quem detecta o clique é o BuildController.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BuildSlot : MonoBehaviour
    {
        [Tooltip("Sprite do círculo que marca o ponto. Some quando uma nave ocupa o slot.")]
        [SerializeField] private SpriteRenderer marker;

        private TowerShip occupant;

        /// <summary>Livre = nenhuma nave posicionada (ou a nave foi destruída).</summary>
        public bool IsFree => occupant == null;

        public TowerShip Occupant => occupant;

        private void Update()
        {
            if (marker != null)
                marker.enabled = IsFree;
        }

        /// <summary>
        /// Instancia a nave no centro do slot. Devolve false se o slot já está ocupado
        /// ou se o prefab é nulo.
        /// </summary>
        public bool TryPlace(TowerShip towerPrefab)
        {
            if (!IsFree || towerPrefab == null)
                return false;

            occupant = Instantiate(towerPrefab, transform.position, Quaternion.identity);
            return true;
        }

        // Chamado ao adicionar o componente: já liga o marcador do próprio objeto.
        private void Reset()
        {
            marker = GetComponent<SpriteRenderer>();
        }
    }
}
