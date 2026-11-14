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

---

## 2026-11-08 — Inimigo e nave-mãe, parte 2: o Drone na cena e o teste até a base

**Feito (no editor, pelo João):**
- `Data/Drone.asset`: o `EnemyData` do Drone, com os valores padrão (vida 15,
  velocidade 2,5, dano 1, 5 créditos, 10 pontos).
- `Prefabs/Drone.prefab`: sprite Triangle do Unity, escala 0,6, Sorting Layer
  `Enemies`, `Sprite-Lit-Default`; componentes `Enemy` (com o asset no campo Data),
  `Rigidbody2D` Kinematic e `CircleCollider2D` trigger.
- Cena Game: componente `MotherShip` no `Mothership`, com `BoxCollider2D` trigger e
  **Offset X = -0,5**; uma instância do Drone com `Test Bridge` = `LightBridge_A`.

**Resultado:** no Play o drone sai do portal, percorre o zigue-zague (~24 s), some
ao tocar a nave-mãe e a `Current Health` cai de 20 para 19. Fecha o teste que
estava pendente desde a entrada de 04/11.

**Por quê assim:**
- **Offset X = -0,5 no collider da nave-mãe.** A nave-mãe está em x = 13,8 com
  largura 2,5: um collider do tamanho do sprite começaria em x = 12,55, e a ponte
  termina em x = 12 (a borda do drone chega a 12,3). O offset é em unidades locais,
  antes da escala: -0,5 × 2,5 = -1,25 no mundo, e a borda vai a x = 11,3.
- **Triângulo:** aponta para cima, o que casa com o `spriteAngleOffset = -90` do
  `Enemy` (a ponta segue a direção do movimento).
- **`Test Bridge` só na instância da cena,** não no prefab: um prefab é um asset do
  projeto e não pode referenciar objeto de uma cena (o Unity mostra "Type mismatch").
  Quem vai ligar inimigo e ponte por código é o `WaveSpawner`.
- **Fora do Play o drone fica onde foi solto** na cena: quem o leva ao `wp_00` é o
  `Initialize`, chamado no `Start()`, que só roda no Play.

**Problemas encontrados:**
- **O arquivo `.cs` precisa ter o mesmo nome da classe.** O script `EnemyData.cs`
  foi renomeado por engano para `Drone.cs` (em vez do asset). O Unity deixou de
  reconhecer o asset como `EnemyData` e o campo Data do `Enemy` recusava o arquivo.
  Voltar o nome resolveu; o `.meta` (com o GUID) foi junto, então nada se perdeu.
- **"Type mismatch" no `Test Bridge`:** era o prefab (ou o Prefab Mode) sendo
  editado, e não a instância da cena. Ver acima.
- **Um drone parado no meio da tela durante o Play:** sobrou na cena o objeto
  original usado para criar o prefab, sem `Test Bridge`. Sem `Initialize`, o
  `IsAlive` fica falso e o `Update` não faz nada. Foi apagado.

**Como testar:** abrir a cena Game, dar Play e seguir o passo 5 da entrada de
04/11. Para ver a `Current Health` durante o jogo, pôr o Inspector em modo Debug
(menu ⋮ no canto do Inspector).

---

## 2026-11-08 — Naves-torre, parte 1: o Caça atirando no Drone

**Feito (código):** `Scripts/Data/TowerData.cs`, `Scripts/Towers/TowerShip.cs` e
`Scripts/Towers/Projectile.cs`.

**Feito (no editor, pelo João):**
- `Data/Caca.asset` (TowerData com os valores do Caça) e `Prefabs/Laser_Caca.prefab`
  (Capsule verde 0,12 × 0,4, `Sprite-Unlit-Default`, Sorting Layer `Projectiles`,
  `Projectile` + `Rigidbody2D` Kinematic + `CircleCollider2D` trigger).
- `Prefabs/Caca.prefab`: Triangle verde, escala 0,8, Sorting Layer `Ships`,
  `TowerShip` com o asset `Caca` e um filho `Muzzle` em (0, 0,5), a ponta da nave.
- Uma instância do Caça na cena em (-6, 0), o corredor entre as colunas x = -9 e -3.
- `Prefabs/Drone.prefab` passou para a layer `Enemy` e a tag `Enemy`.

**O que faz:**
- `TowerData`: custo, alcance, disparos por segundo, dano, velocidade do projétil,
  velocidade de giro, modo de mirar e o prefab do projétil. Padrões = Caça (50, 3,5,
  3/s, 5).
- `TowerShip`, a cada frame: busca inimigos com `Physics2D.OverlapCircle` filtrando a
  layer `Enemy`; escolhe o alvo (Caça: maior `DistanceTravelled`, o primeiro da fila);
  gira com `Quaternion.RotateTowards`; atira quando a recarga (`1 / fireRate`) acabou
  e a mira está a menos de 10° do alvo. Desenha o alcance como gizmo com a nave
  selecionada.
- `Projectile`: persegue o alvo enquanto ele vive (se morrer, segue reto e some em
  2 s); ao tocar um inimigo (`OnTriggerEnter2D`) chama `TakeDamage` e some.

**Resultado:** o drone (15 de vida) leva 3 tiros de 5 e é destruído antes de chegar
à base. Primeiro teste do `Enemy.TakeDamage`.

**Por quê assim:**
- **Modo de mirar como `enum` + `switch`,** e não a interface `ITargetStrategy` do doc
  de design: com poucos critérios fixos, cabe num campo do asset e é mais simples de
  explicar. O critério do Bombardeiro ("mais inimigos juntos") entra quando ele for feito.
- **Filtro por layer no `OverlapCircle`:** a física só devolve colliders da layer
  `Enemy`, sem testar projéteis nem a nave-mãe. `useTriggers = true` porque os
  inimigos usam collider trigger.
- **Lista reaproveitada** (`List<Collider2D>` criada uma vez) para não alocar memória a
  cada frame em cada nave.
- **Só atira mirando:** com giro limitado em graus por segundo, a nave vira aos poucos;
  atirar antes de mirar mandaria o laser para o lado errado.
- **Campo `Projectile Prefab` tipado como `Projectile`** (e não `GameObject`): o Unity
  só aceita ali um prefab que tenha o componente, então não dá para ligar um objeto
  que não sabe voar nem causar dano.
- **Projétil persegue o alvo:** o drone faz curvas de 90°; um tiro reto mirado na
  posição atual erraria nas curvas. Acerta qualquer inimigo que tocar no caminho.

**Problemas encontrados:**
- **Drone na layer Default:** a torre não o enxergava. Prefab passou para a layer `Enemy`.
- **Prefab salvo sem o componente:** o `Laser_Caca` virou prefab antes de receber o
  `Projectile`, e o campo do TowerData o recusava (o tipo não batia).
- **Dois arquivos chamados `Caca` na pasta `Data`:** o asset (TowerData) e um prefab da
  nave criado ali sem querer. O campo Data do `TowerShip` ficou vazio e a nave não
  fazia nada (`data == null` → `return`). O prefab foi movido para `Prefabs/`.
- **Muzzle só na cena:** o filho foi criado depois do prefab, então era um *override*
  da instância. **Overrides → Apply All** copiou para o prefab.

**Como testar:** abrir a cena Game e dar Play. O Caça gira para o drone quando ele
passa pelas colunas x = -9 e -3, dispara lasers verdes e o drone some após 3
acertos; a `Current Health` da nave-mãe continua em 20 (ver em modo Debug).

---

## 2026-11-10 — Construção, parte 1: créditos, slots e menu de escolha

**Feito:** `Scripts/Core/GameSession.cs`, `Scripts/Towers/BuildSlot.cs`,
`Scripts/Towers/BuildController.cs`, `Scripts/UI/BuildMenuUI.cs` e a ferramenta
`Scripts/Editor/BuildSlotSetup.cs`.

**O que faz:**
- `GameSession`: guarda créditos (100 no início, provisório) e pontos. `Register(enemy)`
  escuta `Died` (soma `CreditsReward` e `ScoreReward` do `EnemyData`) e `ReachedBase`
  (não dá nada). `TrySpend` só gasta se houver saldo. Avisa por `CreditsChanged` e
  `ScoreChanged`.
- `BuildSlot`: um ponto de ancoragem. Sabe se está livre (`IsFree`) e instancia a nave no
  centro dele (`TryPlace`). O círculo ciano some quando a nave ocupa o slot.
- `BuildController`: a cada clique, converte o mouse para o mundo e testa com
  `Physics2D.OverlapPoint` se há um `BuildSlot` livre ali. Se há, abre o menu; senão,
  fecha. Ao escolher uma nave: `TrySpend(custo)` e depois `TryPlace`. Esc também fecha.
- `BuildMenuUI`: painel junto ao slot com um botão por nave ("Caça — 50") e o saldo.
  Botão fica desligado quando o saldo não cobre o custo e se atualiza ao vivo.
- `BuildSlotSetup` (menu **NebulaGuard → Adicionar slots de construção**): cria o prefab
  `BuildSlot`, 6 slots nos corredores (x = -6, 0 e 6; y = ±2), o `GameSession` e o
  `BuildController` na cena Game, e remove o Caça solto.

**Por quê assim:**
- **Uma peça por responsabilidade.** O slot não cobra, o `GameSession` não conhece
  slot nem menu, o menu não sabe construir. Só o `BuildController` conversa com os
  três; trocar o menu por outro não mexe nas regras.
- **O custo vem do `TowerData`** (`prefab.Data.Cost`): o número continua em ScriptableObject
  e o menu lê da própria nave. Naves novas entram só acrescentando o prefab na lista
  `Tower Prefabs`, com o tipo `TowerShip` (o Inspector recusa prefab errado).
- **Gastar antes de posicionar, devolver se falhar:** evita construir sem pagar. O
  `TryPlace` só falha se o slot foi ocupado no meio tempo, caso em que o dinheiro volta.
- **Clique em UI é ignorado pelo controlador** (`IsPointerOverGameObject`): senão apertar
  um botão do menu contaria também como clique no mundo e fecharia o menu.
- **Novo Input System:** o projeto está com `activeInputHandler: 1` (só o novo), então
  `Input.mousePosition` daria erro. Usa `Mouse.current` e `Keyboard.current`.
- **Menu montado por código:** Canvas, painel e botões nascem no `Awake`, e um
  `EventSystem` é criado se não houver (botões não funcionam sem ele). Evita um prefab
  de UI para montar à mão; a contrapartida é que mudar o visual exige mexer no código.
  O HUD (item D) pode usar o mesmo `EventSystem`.
- **Slots nos corredores, a 2 unidades do centro:** ficam longe dos trechos horizontais
  da ponte e a 3 das colunas vizinhas, dentro do alcance de 3,5 do Caça.
- **Ferramenta só acrescenta.** O Game.unity agora é editado à mão por mais de uma
  pessoa; uma ferramenta que o recriasse apagaria o trabalho dos outros. Rodar de novo
  não duplica nada (se `BuildSlots`, `GameSession` e `BuildController` já existem, mantém).
- **Provisório:** `GameSession.Start` registra os inimigos que já estão na cena (o Drone
  de teste) para ele dar créditos ao morrer. Sai quando o `WaveSpawner` chamar `Register`.

**Como testar (no Unity 6000.3.27f1):**
1. Abra o projeto e espere compilar sem erros no Console.
2. Menu **NebulaGuard → Adicionar slots de construção** e confirme. A cena Game ganha os 6
   círculos ciano, o `GameSession` e o `BuildController`; o Caça solto some.
3. Dê **Play** e, antes de o drone chegar (cerca de 24 s), clique num círculo: o menu
   mostra "Créditos: 100" e o botão "Caça — 50".
4. Clique no botão: o Caça aparece no slot e o círculo some. Reabra o menu em outro slot: o
   saldo é 50.
5. O Caça atira no drone; ao morrer o saldo sobe 5 (de 50 para 55).
6. Com saldo menor que 50 o botão fica cinza. Esc ou clique fora fecha o menu.
7. Commite `Game.unity`, `Prefabs/BuildSlot.prefab` e os `.meta`.

**Ainda não testado:** escrito sem acesso ao Unity. Se der erro de compilação ou o menu
não aparecer, o ponto de partida é o Console.

---

## 2026-11-13 — Construção, parte 2: slots na cena e primeiro teste no Unity

**Feito (no editor, pelo João):** rodado o menu **NebulaGuard → Adicionar slots de
construção** na cena Game. A cena ganhou `BuildSlots` (6 slots, instâncias do novo
`Prefabs/BuildSlot.prefab`), `GameSession` (100 créditos) e `BuildController`
(`Tower Prefabs` = Caça); o Caça solto em (-6, 0) saiu.

**Resultado do teste (passos da entrada de 10/11):**
- Clicar num slot abre o menu com o saldo e "Caça — 50"; escolher posiciona o Caça
  no slot e desconta 50.
- Com o saldo abaixo de 50 o botão fica desligado e não dá para comprar.
- O drone destruído pelo Caça dá +5 créditos (`GameSession.Register` → `Died`).

Fecha o item que estava pendente: o código do PR #7 agora está testado e a cena com
os slots está no repositório.

**Como testar:** abrir a cena Game, dar Play, clicar num círculo ciano e comprar o
Caça; ver o saldo cair de 100 para 50 e subir 5 quando o drone morre.

---

## 2026-11-13 — Ondas, parte 1: `DifficultyData` e `WaveSpawner`

**Feito (código):** `Scripts/Data/DifficultyData.cs` e `Scripts/Enemies/WaveSpawner.cs`.
Removidos os dois provisórios que o spawner substitui: o campo `Test Bridge` (e o
`Start`) do `Enemy` e o registro automático de inimigos no `GameSession.Start`.

**Feito (no editor, pelo João):**
- `Data/Facil.asset` (DifficultyData): intervalo 1,2 s, preparação 10 s,
  multiplicadores 1,0; ondas de 6, 7 e 5 drones (o plano pede caças e couraçado
  alienígenas nas ondas 2 e 3, que ainda não existem).
- Cena Game: objeto `WaveSpawner` (Difficulty = `Facil`, Bridges = `LightBridge_A`,
  Session = `GameSession`). O `Drone` de teste saiu da cena.
- `Data/Drone.asset`: vida 15 → **25** (balanceamento em teste, ver abaixo).

**O que faz:**
- `DifficultyData`: um asset por dificuldade, com as ondas (lista de grupos
  "prefab + quantidade"), o intervalo entre inimigos, a preparação antes de cada onda
  e os multiplicadores de vida e velocidade. Os outros números do plano (créditos
  iniciais, vida da nave-mãe, tempo máximo, multiplicador de pontos) entram quando quem
  os usa existir.
- `WaveSpawner`: uma corrotina percorre as ondas. Para cada uma: espera a preparação,
  solta os inimigos um a um e espera até todos terem morrido ou chegado à base. Em cada
  inimigo chama `Initialize` (ponte + multiplicadores) e `session.Register`. Avisa por
  `WaveCountdownStarted`, `WaveStarted`, `WaveCleared` e `AllWavesCleared`, para o HUD e o
  `GameManager` futuros.

**Por quê assim:**
- **Corrotina:** a sequência "espera, solta, espera acabar" fica escrita em ordem, como
  uma receita, em vez de espalhada em contadores no `Update`. `WaitForSeconds` respeita o
  `Time.timeScale`, então a pausa do jogo também pausa as ondas.
- **Contagem dos vivos pelos eventos** `Died` e `ReachedBase`: o spawner não precisa
  procurar inimigos na cena; os dois casos contam como "saiu de cena".
- **Ondas no asset da dificuldade,** não no spawner: o mesmo `WaveSpawner` roda o Fácil e
  o Difícil trocando só o asset. Com duas pontes (Difícil), os inimigos se alternam.
- **Preparação também antes da primeira onda:** é o tempo de posicionar as naves. O
  plano ganhou uma contagem regressiva no topo da tela para isso (HUD, futuro), ligada
  ao `WaveCountdownStarted`.

**Balanceamento (em teste):** com o Drone de 15 de vida, um Caça (3 tiros × 5 = 15 de
dano por segundo) matava cada drone em ~1 s, antes de o próximo chegar (1,2 s): nenhum
passava da primeira torre, e com 100 créditos já dá para comprar duas. Com 25 de vida
são ~1,7 s por drone, mais que o intervalo, então a fila se acumula. Dano e custo do Caça
ficaram iguais (valem para o jogo todo, e o Caça ainda vai enfrentar inimigos de 40 e 150).
Valor final a confirmar no Play; depois, atualizar a tabela de inimigos do plano.

**Como testar:** dar Play na cena Game. 10 s sem nada (comprar as naves), depois 6
drones em fila; com a onda eliminada, mais 10 s e a onda 2, depois a 3. No Inspector do
`WaveSpawner` em modo Debug dá para ver `Current Wave` e `aliveInWave`.

---

## 2026-11-13 — Ondas, parte 2: Caça alienígena, Couraçado e balanceamento

**Feito (no editor, pelo João), sem código novo:**
- `Data/CacaAlienigena.asset` e `Data/Couracado.asset` (EnemyData), duplicados do Drone.
- `Prefabs/CacaAlienigena.prefab` (Triangle vermelho, escala 0,8) e
  `Prefabs/Couracado.prefab` (Capsule roxa, escala 0,9 × 1,4), duplicados do Drone:
  herdam a layer e a tag `Enemy`, o `Rigidbody2D` Kinematic e o collider trigger.
- `Data/Facil.asset`: onda 1 = 6 drones; onda 2 = 4 drones + 3 caças alienígenas;
  onda 3 = 3 drones + 2 caças alienígenas + 1 couraçado.
- Balanceamento (valores finais, testados nas três ondas):

| | Vida | Velocidade | Dano à base | Créditos | Pontos |
| --- | --- | --- | --- | --- | --- |
| Drone | 25 (era 15) | 2,5 | 1 | 5 | 10 |
| Caça alienígena | 40 | 2 (era 1,6) | 5 (era 2) | 10 | 25 |
| Couraçado alienígena | 150 | 1,5 (era 0,8) | 5 | 30 | 80 |

  e o **Caça (nave)** passou de 5 para **3 de dano** (3 tiros/s → 9 de dano por segundo).
  O plano foi atualizado com esses números.

**Por quê assim:**
- **Nenhum script novo:** o `Enemy` é genérico; o que diferencia um tipo do outro são os
  números (`EnemyData`) e a aparência (prefab). É o motivo de usar ScriptableObjects.
- **Duplicar o prefab do Drone** em vez de montar do zero: a cópia já vem com os
  detalhes que deram trabalho (layer, tag, Kinematic, trigger).
- **Formas diferentes de longe:** tamanho e cor distinguem os inimigos; o Couraçado é
  comprido na vertical porque o `Enemy` trata "para cima" como a frente do sprite.
- **Caça de 5 para 3 de dano:** comparando com as outras naves do plano, o Caça rendia
  15 de dano por segundo por 50 créditos (3,0 a cada 10 créditos), quase o dobro do
  Bombardeiro (1,5) e do Cruzador (1,7). Com 3 de dano fica em 1,8: todas as naves
  passam a valer a compra, e o Caça continua sendo a de tiro rápido.
- **Inimigos mais rápidos:** com a torre mais fraca, a velocidade maior do caça
  alienígena e do couraçado faz eles passarem menos tempo no alcance das naves.

**Problemas encontrados:**
- **Prefab duplicado continua apontando para os dados do original:** os dois prefabs
  novos ficaram com `Data = Drone`, então o primeiro teste tinha três "drones" de cores
  diferentes. Ao duplicar um prefab, trocar o campo Data é obrigatório.
- **Valores trocados entre dois assets** (dano, créditos e pontos do caça alienígena e do
  couraçado invertidos): conferidos e corrigidos antes do commit.
- **Asset alterado não ia para o disco:** `Cmd + S` salva a cena; para gravar mudanças em
  ScriptableObjects, **File → Save Project**.

**Como testar:** Play na cena Game e jogar as três ondas comprando Caças com os créditos
ganhos. Na onda 2 os caças alienígenas passam da primeira linha; na onda 3 o couraçado
exige várias naves cobrindo o caminho.

---

## 2026-11-14 — Partida: `GameManager` (estados, cronômetro, pausa, vitória e derrota)

**Feito (código):** `Scripts/Core/GameManager.cs`, e ajustes em `DifficultyData`,
`WaveSpawner`, `GameSession`, `MotherShip` e `BuildController`.

**Feito (no editor, pelo João):** objeto `GameManager` na cena Game (Difficulty =
`Data/Facil`, e as referências a `WaveSpawner`, `Mothership`, `GameSession` e
`BuildController`). O `Data/Facil` ganhou créditos iniciais 100, vida da nave-mãe 20 e
tempo máximo 240 s.

**O que faz:**
- Estados `Preparing` (contagem antes da primeira onda) → `Playing` ⇄ `Paused` →
  `Victory` ou `Defeat`, avisados pelo evento `StateChanged`.
- No `Start`, aplica a dificuldade (`session.Begin(créditos)`, `motherShip.Begin(vida)`)
  e só então chama `waveSpawner.Begin(dificuldade)`.
- Cronômetro: começa na primeira onda (`WaveStarted`); zerou = derrota.
- Vitória: `AllWavesCleared` do `WaveSpawner`. Derrota: `Destroyed` da `MotherShip` ou
  o tempo. No fim: para as ondas, bloqueia a construção, `Time.timeScale = 0` e escreve o
  resultado no Console (as telas de fim são do HUD).
- Esc: com o menu de construção aberto, fecha o menu; senão pausa/retoma. Na pausa não
  dá para construir.

**Por quê assim:**
- **O `GameManager` só escuta e decide.** Quem sabe a vida é a `MotherShip`, quem sabe
  as ondas é o `WaveSpawner`; ele assina os eventos deles. Nenhum dos dois conhece o
  `GameManager`.
- **A dificuldade mora em um lugar só.** Antes o `WaveSpawner` tinha o campo; agora o
  `GameManager` recebe o `DifficultyData` e repassa (`Begin`). Quando o menu principal
  existir, a escolha do jogador vai para um lugar só.
- **Números da partida no `DifficultyData`** (créditos, vida, tempo), como pede o padrão
  de balanceamento em ScriptableObject. Os campos antigos no Inspector do `GameSession` e
  da `MotherShip` ficaram só como reserva para cenas sem `GameManager`. Os créditos
  continuam 100 (o plano dizia 150), porque o balanceamento de 13/11 foi testado com 100.
- **Aplicar no `Start`, não no `Awake`:** o `Start` roda depois do `Awake` de todos os
  objetos da cena, então `GameSession` e `MotherShip` já se inicializaram e o `GameManager`
  sobrescreve com os valores da dificuldade.
- **Pausa com `Time.timeScale = 0`:** movimento, tiros, `WaitForSeconds` das ondas e o
  cronômetro usam o tempo do jogo, então tudo para junto. O `Update` continua rodando,
  por isso o Esc funciona na pausa.
- **Um dono só para o Esc:** se o `BuildController` e o `GameManager` lessem o Esc, no
  mesmo frame um fecharia o menu e o outro, já sem menu aberto, pausaria. O Esc agora é só
  do `GameManager`, que pergunta ao `BuildController` se o menu está aberto (`IsMenuOpen`).
- **Construção desligada na pausa e no fim** (`buildController.enabled = false`): com o
  tempo parado o `Update` continua rodando, e daria para comprar naves com o jogo pausado.
- **Vale o primeiro motivo de fim:** o último inimigo pode destruir a nave-mãe no mesmo
  frame em que a onda acaba; o segundo aviso é ignorado.
- **`Time.timeScale = 1` no `Awake`:** quando houver "Jogar de novo" (recarregar a cena),
  o tempo estaria parado desde o fim da partida anterior.

**Testado:** Esc fecha o menu e depois pausa/retoma; vitória ao fim das 3 ondas; derrota
com a vida da nave-mãe em 2 e derrota por tempo com o limite em 20 s (valores de teste
desfeitos depois).

**Como testar:** Play na cena Game. Inspector do `GameManager` em modo Debug mostra o
estado e o tempo restante; o fim da partida aparece no Console.

## 2026-11-14 — HUD: barra do topo, contagem, pausa e fim de partida

**O que foi feito:**
- `Scripts/UI/HUD.cs`: escuta os eventos do jogo e atualiza a interface. Barra do topo com
  vida da nave-mãe (barra + "atual/máx"), onda ("Onda 1/3"), cronômetro (m:ss), créditos e
  pontos; contagem "Onda N em 10, 9, 8…" antes de cada onda; tela de pausa com botão
  "Continuar"; tela de fim com "VITÓRIA" (verde) ou "DERROTA" (vermelho), pontos e botão
  "Jogar de novo".
- Na cena Game, `HUD_Canvas` montado à mão no editor (uGUI + TextMeshPro), com
  `EventSystem` usando o `InputSystemUIInputModule`. Importado o **TMP Essentials**
  (pasta `Assets/TextMesh Pro`, necessária para as fontes do TextMeshPro).
- Hierarquia: `TopBar` (`HealthBG` → `HealthFill`, `HealthText`; `WaveText`, `TimerText`,
  `CreditsText`, `ScoreText`), `CountdownText`, `PausePanel`, `EndPanel`.

**Por quê assim:**
- **Interface montada no editor, não por código** (diferente do `BuildMenuUI`): a barra e
  as telas são fixas, e no editor dá para ver e ajustar o layout sem dar Play. O script só
  recebe as referências.
- **O HUD só escuta.** `GameManager.StateChanged`, `WaveSpawner.WaveCountdownStarted` e
  `WaveStarted`, `GameSession.CreditsChanged` e `ScoreChanged`, `MotherShip.HealthChanged`.
  Nenhum script de jogo conhece o HUD; dá para tirar o HUD da cena e o jogo continua.
- **Cronômetro lido no `Update`:** muda todo frame, um evento por frame não ajudaria.
- **Assina no `OnEnable`:** roda antes de qualquer `Start`, então o HUD já está escutando
  quando o `GameManager.Start` aplica créditos e vida — a tela abre com os números certos.
- **"Onda 0/3" na contagem da primeira onda**, e não ao entrar em `Preparing`: nesse
  momento o `WaveSpawner` ainda não recebeu a dificuldade e `TotalWaves` é 0.
- **Botões ligados por código** (`onClick.AddListener`): menos um passo manual no Inspector.
- **"Jogar de novo" recarrega a cena** (`SceneManager.LoadScene`): tudo volta ao estado
  inicial sem código de "reset". O `Time.timeScale = 1` do `GameManager.Awake` destrava o
  tempo parado pelo fim da partida.
- **Âncoras:** a `TopBar` é top-stretch (presa ao topo, largura da tela); cada filho é
  preso à borda esquerda, ao centro ou à borda direita da barra. Com o Canvas Scaler em
  Scale With Screen Size (1920×1080), o HUD fica no lugar em qualquer resolução.

**Problemas no caminho:**
- Ao trocar a âncora sem **Alt + Shift**, o Unity mantém a posição antiga: a `TopBar`
  ficou com Pos Y -490 e 100 px de largura (uma caixa preta no meio). Correção: Left = 0,
  Right = 0, Pos Y = 0.
- "PAUSADO", "VITÓRIA" e "DERROTA" quebravam em várias linhas: texto TMP novo nasce com
  200 × 50. Correção: caixa maior e **Wrapping = Disabled**.
- O texto de um botão fica no filho `Text (TMP)`, não no objeto do botão.

**Como testar:** Play na cena Game. Contagem "Onda 1 em 10…" com o cronômetro parado em
4:00; ao começar a onda, "Onda 1/3" e o tempo corre; créditos e pontos mudam ao comprar e
matar; a barra de vida desce quando um inimigo chega à base; Esc abre a pausa e
"Continuar" volta; no fim aparece vitória ou derrota com os pontos e "Jogar de novo"
recomeça.
