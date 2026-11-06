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

## 2026-11-04 — Inimigo e nave-mãe, parte 1: andar pela ponte e chegar na base

**Feito:** `Scripts/Data/EnemyData.cs`, `Scripts/Enemies/Enemy.cs` e
`Scripts/Core/MotherShip.cs`.

**O que faz:**
- `EnemyData`: um asset com vida, velocidade, dano à base, créditos e pontos de
  um tipo de inimigo. Os valores padrão são os do Drone.
- `Enemy`: `Initialize(ponte, multiplicadores)` coloca o inimigo no `WP_00`. A cada
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

**Como testar (precisa da cena Game, com `LightBridge_A` e a nave-mãe):**
1. Em `Assets/_Project/Data/`, botão direito → Create → NebulaGuard → Enemy Data,
   com o nome `Drone`. Os valores padrão já são os do Drone.
2. Crie o prefab do Drone: um sprite de tamanho cerca de 0,6, com os componentes
   `Enemy` (o `Rigidbody2D` entra como Kinematic sozinho) e `CircleCollider2D` com
   Is Trigger ligado. Arraste o asset `Drone` para o campo Data, use o Sorting Layer
   `Enemies` e salve em `Assets/_Project/Prefabs/`.
3. Na nave-mãe, adicione o script `MotherShip`. O collider (Is Trigger) precisa
   cobrir o último waypoint: com o `WP_09` em x = 12, a borda esquerda do collider
   deve ficar em x = 11,5 ou menos.
4. Arraste o prefab para a cena e, no campo `Test Bridge` dessa instância, arraste
   o `LightBridge_A`. Dê Play.
5. O drone deve aparecer no `WP_00`, andar o zigue-zague inteiro (59,5 ÷ 2,5 ≈ 24 s)
   e sumir ao encostar na nave-mãe, cuja `Current Health` cai de 20 para 19. Se ele
   parar no fim da ponte, o Console avisa que o trigger não o pegou.
6. O `TakeDamage` do inimigo será testado quando os projéteis existirem.

---

## 2026-11-06 — Cena Game, parte 1: montagem automática (`GameSceneBuilder.cs`)

**Feito:** `Assets/_Project/Scripts/Editor/GameSceneBuilder.cs`, uma ferramenta do
editor. Em **NebulaGuard → Montar cena Game** ela cria, de uma vez:
- as Sorting Layers (`Background`, `Bridges`, `Ships`, `Enemies`, `Projectiles`,
  `Effects`), as tags `Enemy` e `MotherShip` e a layer `Enemy`;
- sprites provisórios gerados por código (círculo, anel, triângulo e estrelas) em
  `Assets/_Project/Art/` e o material da ponte;
- a cena `Scenes/Game.unity`, copiada da cena de exemplo (que já tem câmera e luz 2D),
  com a câmera ortográfica size 9, o fundo, o portal, a `LightBridge_A` com os 10
  waypoints da tabela do `CLAUDE.md` e a nave-mãe;
- o asset `Data/Drone.asset`, o prefab `Prefabs/Drone.prefab` e um `Drone_Teste` na
  cena, já apontando para a ponte pelo campo `Test Bridge`.

**Por quê assim:**
- **Script em vez de cliques manuais.** Montar tudo à mão no Inspector são dezenas de
  passos fáceis de errar (um waypoint fora do lugar, um collider pequeno). O script
  repete sempre o mesmo resultado, e os outros dois do trio podem recriar a cena se
  um conflito de merge a estragar.
- **A cena nasce da `SampleScene`** em vez de uma cena vazia: já vem com a câmera e a
  `Global Light 2D` do URP, que seriam trabalhosas de criar por código.
- **Pasta `Editor`.** O Unity só compila esse código no editor; ele usa a API do
  editor (`AssetDatabase`, `SerializedObject`) e quebraria o build do `.exe`.
- **`SerializedObject` para preencher campos privados** (`data`, `testBridge`): faz o
  mesmo que arrastar no Inspector, sem tornar os campos `public`.
- **Nave-mãe:** sprite de 1 unidade com escala 4 e collider trigger de raio 0,5 (2
  unidades no mundo). Centro em x = 13,5, então a borda esquerda fica em x = 11,5,
  antes do último waypoint (x = 12), como pede o passo 3 da entrada de 2026-11-04.
- **Triângulo apontando para cima** no Drone, para casar com `spriteAngleOffset = -90`
  do `Enemy`.
- **IDs das Sorting Layers vindos do hash do nome** (`Animator.StringToHash`): o Unity
  copia o ID da camada anterior ao inserir, o que duplicaria IDs. Com o hash, o ID é
  único e igual em qualquer PC.
- Rodar de novo recria a cena (pede confirmação) mas mantém o `Drone.asset`, para não
  perder balanceamento editado.

**Como testar:**
1. Abra o projeto no Unity 6000.3.27f1 e aguarde compilar (sem erros no Console).
2. Menu **NebulaGuard → Montar cena Game**. O Console mostra "Cena Game montada" e o
   comprimento da ponte, que deve ser **59,5**.
3. Dê **Play**. O `Drone_Teste` sai do portal (roxo, à esquerda), anda o zigue-zague
   pela linha ciano e some ao tocar a nave-mãe (azul, à direita), em cerca de 24 s.
4. Selecione `MotherShip` na Hierarchy: `Current Health` deve cair de 20 para 19.
5. Se o drone parar antes da base, o Console avisa que o trigger não o pegou.
6. Commite `Game.unity`, `Art/`, `Data/`, `Prefabs/`, `Materials/` e os `.meta`.

**Ainda não testado:** escrito sem acesso ao Unity. Se aparecer erro de compilação ou
o teste falhar, o ponto de partida é o Console.
