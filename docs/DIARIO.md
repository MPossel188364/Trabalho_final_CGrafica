# Diário de desenvolvimento

Uma entrada por passo, a mais recente no fim. Cada entrada diz o que foi feito,
por que daquele jeito e como testar. Padrões gerais ficam no `CLAUDE.md`.

---

## 2026-10-29 — Preparação do repositório

**Feito:** `.gitignore` e `.gitattributes` de Unity na raiz do repo.

**Por quê:**
- O Unity gera pastas enormes e recriáveis (`Library/`, `Temp/`, `Logs/`,
  `obj/`) e arquivos de IDE (`*.csproj`, `*.sln`). Se entrarem no Git, o
  repositório incha e cada integrante gera conflitos em arquivos que nem editou.
  Builds (`.exe`) também ficam de fora: vão para o GitHub Releases.
- Os arquivos `.meta` **entram**: é neles que o Unity guarda o ID de cada asset.
  Sem eles, as referências entre prefabs, cenas e scripts quebram no PC dos colegas.
- O `.gitattributes` força final de linha LF (Mac e Windows no mesmo repo) e
  marca cenas/prefabs para o `UnityYAMLMerge`, a ferramenta do Unity que resolve
  conflitos nesses arquivos melhor que o merge de texto comum.

---

## 2026-10-29 — Mapa, parte 1: a ponte de luz (`LightBridge.cs`)

**Feito:** `Assets/_Project/Scripts/Enemies/LightBridge.cs`.

**O que faz:** os waypoints da ponte são os objetos filhos do `LightBridge`, na
ordem da Hierarchy. O script lê a posição deles, passa para o `LineRenderer`
(que desenha a linha) e calcula o comprimento total do caminho.

**Por quê assim:**
- **Uma fonte só para o desenho e para a lógica.** O inimigo vai seguir os
  mesmos pontos que a linha desenha; se alguém mexer no caminho, os dois mudam
  juntos e nunca ficam diferentes.
- **Waypoints como filhos (Transforms)** em vez de uma lista de coordenadas no
  Inspector: dá para arrastá-los na Scene view com o mouse, e a ordem é a da
  Hierarchy, que é fácil de ver e reorganizar.
- **`[ExecuteAlways]`** faz o script rodar também no editor, então a linha se
  redesenha enquanto o waypoint é arrastado. Durante o jogo o caminho é fixo, por
  isso o recálculo no `Update` só acontece com `!Application.isPlaying`.
- **`[RequireComponent(typeof(LineRenderer))]`** adiciona o `LineRenderer`
  sozinho ao colocar o script num objeto e impede removê-lo por engano.
- **Comprimento total visível no Inspector** (`totalLength`): prova o requisito
  do PDF de caber 30 inimigos enfileirados (30 × 0,6 = 18 unidades); o nosso
  caminho tem 59,5.
- **`OnDrawGizmos`** desenha um círculo amarelo em cada waypoint, só na Scene
  view, para enxergar os pontos mesmo antes de a linha ter material.

**Como testar (quando a cena existir):** criar um objeto vazio `LightBridge_A`,
adicionar o componente, criar os filhos `WP_00`…`WP_09` nas posições da tabela
do `CLAUDE.md` e arrastar um deles: a linha deve acompanhar e o `Total Length`
mudar.

---

## 2026-10-29 — Projeto Unity criado e colocado na raiz do repo

**Feito:** projeto criado pelo Unity Hub com o Unity **6000.3.27f1** e o template
2D (pacote `com.unity.template.2d-cross-platform`). As pastas `Assets`,
`Packages` e `ProjectSettings` foram movidas para a raiz do repositório,
junto com a `Assets/_Project` que já existia.

**Por quê:**
- **Raiz do repo = raiz do projeto.** Quem clonar abre a pasta direto no Hub, sem
  subpasta para lembrar, e o `.gitignore` (que espera `Library/` na raiz)
  funciona sem ajuste.
- **O template já traz o que o plano pede:** URP 17.3 (necessário para o Bloom),
  Input System e uGUI. Nesta versão do Hub, o "Universal 2D" do doc de design
  aparece com esse nome de pacote.
- **Force Text e Visible Meta Files** já vêm como padrão no Unity 6
  (`m_SerializationMode: 2` em `ProjectSettings/EditorSettings.asset`), então não
  foi preciso mudar nada.
- `*.slnx` (arquivo de solução que o Unity 6.3 gera para a IDE) foi adicionado
  ao `.gitignore`: cada PC gera o seu.

**Problema encontrado:** o Hub mostrava "project not found". O editor estava
parado esperando o aceite dos termos de uso, numa janela que tinha ficado
escondida. Registrado no `CLAUDE.md`.

---

## 2026-10-29 — Mapa, parte 2: cena Game com câmera, ponte, portal e nave-mãe

**Feito (arquivos de configuração):**
- `SampleScene` renomeada e movida para `Assets/_Project/Scenes/Game.unity`,
  junto com o `.meta` (mantém o mesmo ID e continua na lista de cenas do build,
  em `ProjectSettings/EditorBuildSettings.asset`).
- `ProjectSettings/TagManager.asset`: tags `Enemy` e `MotherShip`; layer 6 =
  `Enemy`; sorting layers `Background`, `Default`, `Bridges`, `Ships`,
  `Enemies`, `Projectiles`, `Effects` (de trás para frente).

**Feito (no editor, pelo João):** câmera, `LightBridge_A` com 10 waypoints,
`Portal_A` e `Mothership`. Valores na tabela "Mapa" do `CLAUDE.md`.

**Por quê:**
- **Tag × layer.** A tag é um rótulo que o código confere depois de uma colisão
  (o inimigo vai perguntar "encostei na `MotherShip`?"). A layer é um filtro da
  física: a torre vai buscar alvos com `OverlapCircle` só na layer `Enemy`, sem
  testar asteroides e projéteis.
- **Sorting layers.** Em 2D tudo está em z = 0; quem decide o que fica na frente
  é a ordem das sorting layers. `Default` fica logo após o fundo para que um
  sprite esquecido sem layer não suma atrás das estrelas.
- **Câmera ortográfica size 9.** O size é metade da altura visível: 18 unidades
  de altura, 32 de largura em 16:9. É o tamanho de mapa do plano.
- **Materiais do URP.** A linha usa `Sprite-Unlit-Default` (brilha sozinha, não
  depende de luz); portal e nave-mãe usam `Sprite-Lit-Default` (reagem à Global
  Light 2D e a luzes futuras). Os materiais embutidos antigos funcionam hoje,
  mas não respondem ao Bloom da etapa de efeitos.
- **Formas provisórias** (círculo e quadrado do Unity) no lugar dos sprites:
  permitem testar posições e escala agora; a arte entra depois sem mudar a lógica.

**Problemas encontrados:**
- Ao arrastar um material para o campo do Sprite Renderer, às vezes o Unity
  ignora a soltura sem avisar. Conferir o campo depois de soltar.
- No seletor de materiais, os assets de pacotes não aparecem por padrão. Mais
  fácil: abrir a pasta em Packages na janela Project e arrastar de lá.

**Como verificar:** abrir a cena Game; o Inspector de `LightBridge_A` deve
mostrar `Total Length` = 59,5. Arrastar um waypoint (tecla W, setas na Scene
view) e a linha acompanha.

---

## 2026-11-04 — Inimigo e nave-mãe, parte 1: andar pela ponte e chegar na base

**Feito:** `Scripts/Data/EnemyData.cs`, `Scripts/Enemies/Enemy.cs` e
`Scripts/Core/MotherShip.cs`.

**O que faz:**
- `EnemyData`: um asset com vida, velocidade, dano à base, créditos e pontos de
  um tipo de inimigo. Os valores padrão são os do Drone.
- `Enemy`: `Initialize(ponte, multiplicadores)` coloca o inimigo no `wp_00`. A cada
  frame ele anda até o próximo waypoint com `Vector2.MoveTowards`, gira o sprite
  para a direção do movimento e soma a distância percorrida. Tem `TakeDamage` e os
  eventos `Died` e `ReachedBase`.
- `MotherShip`: guarda a vida. Quando um inimigo toca o trigger dela
  (`OnTriggerEnter2D`), lê o dano do inimigo, manda o inimigo sumir e desconta a
  vida. Avisa por `HealthChanged` (para o HUD) e `Destroyed` (para o GameManager).

**Por quê assim:**
- **Números em ScriptableObject.** Drone, Caça e Couraçado usam o mesmo script
  `Enemy` com três assets diferentes. Os multiplicadores do Difícil entram como
  parâmetros do `Initialize`, então não precisa duplicar os assets.
- **O inimigo não sabe de créditos, pontos nem HUD.** Ele só avisa por evento
  (`Died`, `ReachedBase`); quem escuta (WaveSpawner, GameSession) decide o que
  fazer. Assim cada classe tem uma responsabilidade só.
- **O caminho vem da `LightBridge`.** O inimigo pede `GetWaypoint(i)`, então anda
  exatamente pela linha que está desenhada.
- **`MoveTowards` + `Time.deltaTime`.** A velocidade é em unidades por segundo,
  igual em qualquer FPS, e o `MoveTowards` nunca passa do alvo, então o inimigo
  não "ultrapassa" um waypoint. Na curva a sobra do passo é descartada, o que dá
  menos de 0,05 unidade por frame.
- **`DistanceTravelled`.** As torres vão usar para saber quem é o "primeiro da fila".
- **`Rigidbody2D` Kinematic no inimigo.** O `OnTriggerEnter2D` só dispara se pelo
  menos um dos dois objetos tem `Rigidbody2D`. Como o inimigo anda por código, ele
  é Kinematic: a física não o empurra, mas o trigger funciona. A nave-mãe não
  precisa de `Rigidbody2D`.
- **A `MotherShip` reconhece o inimigo pelo componente** (`TryGetComponent<Enemy>`),
  não pela tag: errar o nome de uma tag não dá erro nenhum.
- **`IsAlive`** impede o inimigo de causar dano duas vezes se dois triggers
  acontecerem no mesmo frame.
- **Provisório:** a vida da nave-mãe (20) está no Inspector até existir o
  `DifficultyData`, e o campo `Test Bridge` do `Enemy` existe só para testar sem
  `WaveSpawner`. Os dois saem quando o `WaveSpawner` for feito.

**Como testar (a cena Game, com `LightBridge_A` e `Mothership`, vem do PR da cena):**
1. Em `Assets/_Project/Data/`, botão direito → Create → NebulaGuard → Enemy Data,
   com o nome `Drone`. Os valores padrão já são os do Drone.
2. Crie o prefab do Drone: um sprite de tamanho cerca de 0,6, com os componentes
   `Enemy` (o `Rigidbody2D` entra como Kinematic sozinho) e `CircleCollider2D` com
   Is Trigger ligado. Arraste o asset `Drone` para o campo Data, use o Sorting Layer
   `Enemies` e salve em `Assets/_Project/Prefabs/`.
3. No objeto `Mothership` da cena, adicione o script `MotherShip` (o `BoxCollider2D`
   entra junto) e ligue **Is Trigger**. Atenção: essa nave-mãe (centro x = 13,8,
   escala 2,5 × 4) ainda não tinha collider, e um collider do tamanho do sprite
   começaria em x = 12,55, depois do `wp_09` (x = 12): o drone pararia antes de
   tocar e o Console avisaria. Ponha **Offset X = -0,5** no collider: ele anda 1,25
   unidade para a esquerda e a borda esquerda vai a x = 11,3, antes do `wp_09`.
4. Arraste o prefab para a cena e, no campo `Test Bridge` dessa instância, arraste
   o `LightBridge_A`. Dê Play.
5. O drone deve aparecer no `wp_00`, andar o zigue-zague inteiro (59,5 ÷ 2,5 ≈ 24 s)
   e sumir ao encostar na nave-mãe, cuja `Current Health` cai de 20 para 19. Se ele
   parar no fim da ponte, o Console avisa que o trigger não o pegou.
6. O `TakeDamage` do inimigo será testado quando os projéteis existirem.
