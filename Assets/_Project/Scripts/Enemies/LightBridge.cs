using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Ponte de luz: o caminho que os inimigos percorrem do portal até a nave-mãe.
    /// Os waypoints são os objetos filhos deste, na ordem em que aparecem na Hierarchy.
    /// O mesmo conjunto de pontos desenha a linha (LineRenderer) e guia os inimigos,
    /// então o caminho visual e o lógico nunca ficam diferentes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class LightBridge : MonoBehaviour
    {
        [Tooltip("Comprimento total do caminho, calculado automaticamente. O requisito de 30 inimigos enfileirados pede pelo menos 18 unidades (30 x 0,6).")]
        [SerializeField] private float totalLength;

        private LineRenderer line;
        private Vector3[] points = new Vector3[0];

        public int WaypointCount => points.Length;
        public float TotalLength => totalLength;

        public Vector3 GetWaypoint(int index) => points[index];

        private void OnEnable()
        {
            line = GetComponent<LineRenderer>();
            Rebuild();
        }

        private void Update()
        {
            // No editor, recalcula sempre que a cena muda, para a linha acompanhar
            // quando um waypoint é arrastado. Durante o jogo o caminho é fixo.
            if (!Application.isPlaying)
                Rebuild();
        }

        /// <summary>Lê a posição dos filhos e atualiza a linha e o comprimento.</summary>
        public void Rebuild()
        {
            int count = transform.childCount;
            if (points.Length != count)
                points = new Vector3[count];

            totalLength = 0f;
            for (int i = 0; i < count; i++)
            {
                points[i] = transform.GetChild(i).position;
                if (i > 0)
                    totalLength += Vector3.Distance(points[i - 1], points[i]);
            }

            line.useWorldSpace = true;
            line.positionCount = count;
            line.SetPositions(points);
        }

        private void OnDrawGizmos()
        {
            // Marca cada waypoint na Scene view (não aparece no jogo).
            Gizmos.color = Color.yellow;
            for (int i = 0; i < transform.childCount; i++)
                Gizmos.DrawWireSphere(transform.GetChild(i).position, 0.25f);
        }
    }
}
