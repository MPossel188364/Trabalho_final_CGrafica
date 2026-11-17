using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Ponte de luz: o caminho que os inimigos percorrem do portal até a nave-mãe.
    /// Os waypoints são os objetos filhos deste, na ordem em que aparecem na Hierarchy.
    /// As quinas entre waypoints viram curvas (raio em Corner Radius). O mesmo conjunto de
    /// pontos já arredondado desenha a linha (LineRenderer) e guia os inimigos, então o
    /// caminho visual e o lógico nunca ficam diferentes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class LightBridge : MonoBehaviour
    {
        [Tooltip("Raio das curvas nas quinas, em unidades. 0 = quinas em ângulo reto. É reduzido sozinho onde o trecho é curto demais.")]
        [Min(0f)] [SerializeField] private float cornerRadius = 1.2f;

        [Tooltip("Quantos pedaços retos formam cada curva. Mais = curva mais lisa.")]
        [Range(2, 24)] [SerializeField] private int cornerSegments = 10;

        [Tooltip("Comprimento total do caminho, calculado automaticamente. O requisito de 30 inimigos enfileirados pede pelo menos 18 unidades (30 x 0,6).")]
        [SerializeField] private float totalLength;

        private LineRenderer line;
        private Vector3[] waypoints = new Vector3[0];
        private Vector3[] points = new Vector3[0];

        /// <summary>Quantidade de pontos do caminho já arredondado (mais que os waypoints filhos).</summary>
        public int WaypointCount => points.Length;
        public float TotalLength => totalLength;

        /// <summary>Ponto do caminho já arredondado. O inimigo anda de um para o outro.</summary>
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

        /// <summary>Lê a posição dos filhos, arredonda as quinas e atualiza a linha e o comprimento.</summary>
        public void Rebuild()
        {
            int count = transform.childCount;
            if (waypoints.Length != count)
                waypoints = new Vector3[count];
            for (int i = 0; i < count; i++)
                waypoints[i] = transform.GetChild(i).position;

            points = BuildRoundedPath(waypoints);

            totalLength = 0f;
            for (int i = 1; i < points.Length; i++)
                totalLength += Vector3.Distance(points[i - 1], points[i]);

            line.useWorldSpace = true;
            line.positionCount = points.Length;
            line.SetPositions(points);
        }

        /// <summary>
        /// Troca cada quina (waypoint do meio) por uma curva. A curva começa um pouco antes
        /// da quina e termina um pouco depois, sempre em cima dos dois trechos retos, então
        /// a ponte continua passando pelos mesmos corredores.
        /// </summary>
        private Vector3[] BuildRoundedPath(Vector3[] corners)
        {
            if (corners.Length < 3 || cornerRadius <= 0f)
                return (Vector3[])corners.Clone();

            int middle = corners.Length - 2;
            var result = new Vector3[2 + middle * (cornerSegments + 1)];
            int n = 0;

            result[n++] = corners[0];
            for (int i = 1; i < corners.Length - 1; i++)
            {
                Vector3 previous = corners[i - 1];
                Vector3 corner = corners[i];
                Vector3 next = corners[i + 1];

                // Até metade de cada trecho: duas curvas vizinhas nunca se sobrepõem.
                float inLength = Vector3.Distance(previous, corner);
                float outLength = Vector3.Distance(corner, next);
                float radius = Mathf.Min(cornerRadius, inLength * 0.5f, outLength * 0.5f);

                Vector3 start = corner + (previous - corner).normalized * radius;
                Vector3 end = corner + (next - corner).normalized * radius;

                // Curva de Bézier quadrática: sai de start, é "puxada" pela quina e chega em
                // end. Tangente aos dois trechos retos, então não há tranco na troca.
                for (int s = 0; s <= cornerSegments; s++)
                {
                    float t = (float)s / cornerSegments;
                    float u = 1f - t;
                    result[n++] = u * u * start + 2f * u * t * corner + t * t * end;
                }
            }
            result[n++] = corners[corners.Length - 1];

            return result;
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
