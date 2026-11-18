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

---

## 2026-11-15 — Naves-torre, parte 2: Bombardeiro e Cruzador

**Feito:**
- `TowerData.cs`: campo `splashRadius` (raio da explosão; 0 = sem dano em área) e
  um valor novo, `MostClustered`, no `TargetingMode`.
- `Projectile.cs`: o `Launch` ganhou o parâmetro opcional `newSplashRadius`. Com
  raio maior que 0, o projétil explode ao tocar o primeiro inimigo e os outros
  inimigos dentro do raio levam o mesmo dano.
- `TowerShip.cs`: mira `MostClustered` (método `FindMostClustered`) e repasse do
  raio ao projétil.
- Assets e prefabs novos: `Data/Bombardeiro.asset`, `Data/Cruzador.asset`,
  `Prefabs/Bombardeiro.prefab`, `Prefabs/Missil_Bombardeiro.prefab`,
  `Prefabs/Cruzador.prefab` e `Prefabs/Raio_Cruzador.prefab`.

**Valores** (do documento de design; são o ponto de partida do balanceamento):

| Nave | Custo | Alcance | Tiros/s | Dano | Velocidade do projétil | Mira |
| --- | --- | --- | --- | --- | --- | --- |
| Bombardeiro | 80 | 3 | 0,6 | 20 em área (raio 1,2) | 8 | onde há mais inimigos juntos |
| Cruzador | 120 | 7 | 0,5 | 40 | 30 | o inimigo com mais vida |

**Por quê assim:**
- **O Caça não mudou.** O raio da explosão tem padrão 0 e o `MostClustered` foi
  acrescentado no fim do enum, então o número salvo no `Caca.asset` continua
  valendo o mesmo. `Caca.asset`, `Caca.prefab`, `Laser_Caca.prefab` e a cena
  `Game.unity` não foram tocados.
- **Mesmo `TowerShip` e mesmo `Projectile` para as três naves.** O que muda de uma
  nave para outra são os números (assets) e o visual (prefab). O Cruzador não
  precisou de código novo: a mira `Strongest` (mais vida) já existia.
- **O dano em área fica no projétil, não na nave.** Quem sabe onde a explosão
  acontece é o projétil, no momento em que toca o inimigo. O inimigo tocado leva o
  dano uma vez e é pulado na explosão, para não levar duas.
- **Mira do Bombardeiro.** Para cada inimigo no alcance, conta quantos inimigos
  (ele mesmo incluído) estão dentro do raio da explosão e escolhe o que tem o
  maior número. Empate: o mais adiantado na ponte. São poucos inimigos no alcance,
  então a conta dupla por frame não pesa.
- **A busca da explosão usa só a layer `Enemy`**, igual à busca das torres.
- **Os prefabs e assets foram criados por cópia** dos do Caça e do `Laser_Caca`,
  trocando só nome, escala, cor, números e as referências entre eles. Foi feito
  sem o Unity aberto, e as referências (GUIDs e fileIDs) foram conferidas por
  script.
- **A cena não foi editada.** Se algum prefab novo tiver problema ao importar, o
  fluxo do Caça continua funcionando. As naves entram no menu quando são
  adicionadas à lista do `BuildController` (passo 3 abaixo).

**Ainda não tem (fica para a etapa de efeitos):** efeito visual na explosão e
rastro de fumaça no míssil (o plano os coloca na etapa de efeitos), som, e sprites
próprios: as naves novas usam o mesmo sprite do Caça, com escala e cor diferentes
(Bombardeiro larga e laranja, Cruzador comprido e azul).

**Não compilado nem testado no Unity** (escrito sem o editor aberto): a primeira
abertura do projeto é o teste.

**Como testar:**
1. Abra o projeto no Unity (6000.3.27f1) e espere a importação. O Console não deve
   mostrar erro vermelho. Se mostrar, pare aqui e copie a mensagem.
2. Em `Assets/_Project/Data`, clique em `Bombardeiro`: Cost 80, Range 3, Splash
   Radius 1,2, Targeting `MostClustered` e Projectile Prefab `Missil_Bombardeiro`.
   Em `Assets/_Project/Prefabs`, clique em `Bombardeiro` e `Cruzador`: o `TowerShip`
   deve mostrar o campo Data preenchido, sem "Missing".
3. Abra a cena Game e selecione `BuildController` na Hierarchy. Em **Tower Prefabs**,
   mude o Size de 1 para 3. O Element 0 continua `Caca`; arraste `Bombardeiro` para
   o Element 1 e `Cruzador` para o Element 2.
4. Dê Play e clique num slot livre: o menu deve mostrar Caça — 50, Bombardeiro — 80
   e Cruzador — 120, com desligados os botões que não cabem no saldo. O Fácil começa
   com 100 créditos, então no começo o Cruzador aparece desligado; ele libera depois
   que os drones destruídos (+5 cada) somarem o saldo.
5. **Caça (nada pode ter mudado):** compre e confira que atira e mata o drone como
   antes.
6. **Bombardeiro:** posicione perto de onde os drones passam juntos. O míssil laranja
   persegue o alvo e, ao acertar, **todos** os drones dentro do raio (1,2) levam 20
   de dano ao mesmo tempo. O drone tem 25 de vida, então o primeiro míssil deixa o
   grupo com 5 e o segundo o elimina junto. Para conferir, compare com o Caça: ele
   só machuca um drone por vez.
7. **Cruzador:** o raio azul sai de longe (alcance 7) e, quando há inimigos de vidas
   diferentes no alcance, ele atira primeiro no de mais vida (na onda 3, o
   Couraçado, com 150, antes dos drones e do caça alienígena). Cada tiro tira 40,
   então um drone (25 de vida) morre com um tiro.
8. Deu certo: salve a cena (Ctrl+S no Windows, Cmd+S no Mac) e commite
   `Assets/_Project/Scenes/Game.unity`. O diff deve ter só duas linhas novas na lista
   do `BuildController`. Se mexer nos números dos assets das naves ao balancear, use
   **File → Save Project** (a cena salva não grava ScriptableObject). Se algo deu
   errado, desfaça o passo 3 e o jogo volta a ser o de antes.

## 2026-11-17 — Bombardeiro e Cruzador no menu de construção

**O que foi feito:** as naves do Matheus (entrada de 15/11) entraram no jogo. A lista
`Tower Prefabs` do `BuildController` na cena passou de 1 para 3 elementos: `Caca`,
`Bombardeiro` e `Cruzador`. Nenhuma linha de código mudou.

**Por quê assim:** o `BuildMenuUI` cria um botão por prefab da lista dentro de um
`VerticalLayoutGroup`, então a terceira nave já aparece no menu sem mexer na interface. É o
padrão "nave nova = acrescentar o prefab em `Tower Prefabs`" funcionando como previsto.

**Testado:** as três naves aparecem no menu com o custo e podem ser compradas. O
Bombardeiro acerta vários inimigos de uma vez quando eles passam juntos e o Cruzador
alcança longe com tiro lento e forte. Nenhum erro no Console: os assets e prefabs escritos
sem o editor aberto estavam corretos.

## 2026-11-17 — Ponte de luz arredondada e mais grossa; astronauta cortado

**O que foi feito:**
- `LightBridge.cs`: campos `Corner Radius` (raio das curvas, 1,5 na cena) e
  `Corner Segments` (pedaços retos de cada curva, 10). O `Rebuild` troca cada waypoint do
  meio por uma curva antes de desenhar a linha.
- Na cena, o `Line Renderer` do `LightBridge_A` ficou com largura 0,85 (antes 0,15) e
  6 *cap vertices* (pontas arredondadas).
- O astronauta saiu do plano (doc de design e CLAUDE.md): o PDF não o exige e o tempo que
  sobra até a entrega vai para o visual.

**Como a curva é feita:** em cada quina, a curva começa `radius` unidades antes dela, no
trecho reto de chegada, e termina `radius` unidades depois, no trecho de saída. Entre os
dois pontos é uma **curva de Bézier quadrática** com a quina como ponto de controle:
`P(t) = (1-t)²·início + 2(1-t)t·quina + t²·fim`, com `t` de 0 a 1. Ela sai na direção do
trecho de chegada e chega na direção do de saída, por isso não há tranco na troca entre reta
e curva. O raio é limitado à metade do trecho mais curto, para duas curvas vizinhas não se
sobreporem.

**Por quê assim:**
- **A curva é calculada no caminho, não só no desenho.** O Line Renderer tem a opção
  *Corner Vertices*, mas ela só arredonda a malha da linha; os inimigos continuariam virando
  em ângulo reto, por fora da curva. Como o `LightBridge` entrega os mesmos pontos para a
  linha e para o `Enemy` (`GetWaypoint`), os dois seguem a mesma curva. O `Enemy` não mudou:
  ele só recebe mais pontos, mais próximos.
- **Os waypoints continuam sendo as quinas.** Para mudar o mapa, ainda é só arrastar os
  filhos; a curva é refeita sozinha no editor (`[ExecuteAlways]`).
- O comprimento caiu de 59,5 para ≈ 55 unidades, bem acima das 18 que o requisito dos 30
  inimigos enfileirados pede.

**Como testar:** na Scene view, as quinas aparecem arredondadas; ao mudar o `Corner Radius`
a ponte se refaz na hora. No Play, os inimigos fazem a curva em cima da linha.

## 2026-11-17 — Visual, parte 1: Bloom e Vignette

**O que foi feito:**
- Main Camera: **Rendering → Post Processing** ligado (estava desligado; sem isso nenhum
  efeito de Volume aparece).
- `Global Volume` na cena Game, com o perfil `Assets/Settings/GameVolumeProfile.asset`
  (junto dos outros assets de render do template):
  - **Bloom:** Threshold 0,6, Intensity 1,5, Scatter 0,7;
  - **Vignette:** Intensity 0,3.
- Nenhuma linha de código: é configuração do URP. Atende ao requisito de pós-processamento
  do PDF.

**Como o Bloom funciona:** depois que a cena é desenhada, o URP separa os pixels mais claros
que o **Threshold**, borra essa imagem em várias resoluções (o **Scatter** controla o quanto o
borrão se espalha) e soma o resultado por cima da imagem original com a força da
**Intensity**. Como o fundo é quase preto (`0B0E1A`), só o que é claro ganha halo: a ponte
ciano, os lasers e o portal. A **Vignette** escurece as bordas da tela e puxa o olhar para o
centro.

**Por quê assim:**
- **Global Volume:** vale para a câmera inteira, em qualquer posição. Um Volume local só
  aplicaria o efeito dentro de um collider.
- **Threshold 0,6:** abaixo disso as naves cinza e o fundo também brilhariam e a imagem
  ficaria lavada; acima, a ponte ciano (azul no máximo, verde em 0,88) quase não brilharia.
- **O HUD não brilha:** está num Canvas *Screen Space - Overlay*, desenhado depois do
  pós-processamento, então os textos continuam nítidos.

**Como testar:** Play na cena Game: a ponte, os lasers e o portal ganham halo; desligar o
`Global Volume` (checkbox no Inspector) mostra a diferença.

## 2026-11-17 — Visual, parte 2: fundo de estrelas com parallax

**O que foi feito:**
- `Scripts/Effects/ParallaxLayer.cs`: uma camada de estrelas. No `Start` cria as estrelas
  como objetos filhos (posição, tamanho e brilho sorteados); no `Update` move todas para a
  esquerda e devolve para a direita a que sai da área.
- Na cena Game, objeto `Background` com três camadas:

| Camada | Estrelas | Tamanho | Brilho | Velocidade | Sorting Order |
| --- | --- | --- | --- | --- | --- |
| `Stars_Far` (tom azulado `AFC8FF`) | 150 | 0,05 – 0,10 | 0,2 – 0,45 | 0,15 | 0 |
| `Stars_Mid` | 80 | 0,08 – 0,15 | 0,4 – 0,7 | 0,4 | 1 |
| `Stars_Near` | 30 | 0,12 – 0,22 | 0,7 – 1 | 0,9 | 2 |

**Por quê assim:**
- **Parallax:** objetos distantes parecem se mover mais devagar que os próximos. Camadas
  com velocidade, tamanho e brilho crescentes fazem o olho ler profundidade num jogo 2D.
- **Repetição sem emenda:** as estrelas cobrem uma área de 34 × 20 (a tela tem 32 × 18).
  A que passa da borda esquerda ganha `+ largura` no x e reaparece à direita, fora da vista;
  o fundo é infinito com um número fixo de objetos, sem criar nem destruir nada.
- **Sprite gerado por código** (`Texture2D` de 32 × 32 com alfa caindo do centro para a
  borda): nenhuma imagem importada, e uma única textura (`static`) serve a todas as camadas.
- **`System.Random` com semente:** o céu é igual em toda partida e o sorteio não mexe no
  `UnityEngine.Random` usado pelo resto do jogo.
- **Material `Sprite-Unlit-Default` e Sorting Layer `Background`:** a estrela brilha sozinha
  (não depende das luzes 2D) e fica atrás de tudo. As da camada da frente passam do Threshold
  do Bloom e ganham halo.
- **`Time.deltaTime`:** o fundo para na pausa, junto com o resto.

**Como testar:** Play na cena Game. O céu desliza devagar para a esquerda, a camada da
frente mais rápido que as de trás; Esc pausa e o fundo para. As estrelas só existem no
Play (são criadas no `Start`).

## 2026-11-17 — Visual, parte 3: sprites da Kenney

**O que foi feito:** as formas provisórias viraram sprites do pacote **Space Shooter
(Remastered)** da Kenney (licença CC0, domínio público). Só as 11 imagens usadas foram
copiadas para `Assets/_Project/Art/`, com nomes do jogo; `Art/CREDITOS.txt` registra a origem
de cada uma.

| Objeto | Imagem | Escala |
| --- | --- | --- |
| Caça / Bombardeiro / Cruzador | `playerShip1/2/3_blue` | 0,8 / 1 / 1,1 |
| Drone / Caça alienígena / Couraçado | `enemyRed1/2/4` | 0,6 / 0,8 / 1,1 |
| Laser do Caça / Raio do Cruzador | `laserBlue01` / `laserBlue16` | 1 |
| Míssil do Bombardeiro | `laserRed16` | 1 |
| Nave-mãe / Portal (cena) | `ufoBlue` / `ufoRed` | 3 / 1,8 |

**Por quê assim:**
- **Lados por cor:** naves do jogador azuis, inimigos laranja-avermelhados. Dá para saber
  quem é quem sem ler nada.
- **Formato pela função:** o Caça é a nave pequena, o Bombardeiro a mais larga, o Cruzador a
  mais comprida; entre os inimigos, o Drone é o menor e o Couraçado o mais robusto.
- **Só as imagens usadas** vão para o repositório (não as 295 do pacote), com nomes nossos:
  `Art/Towers/Caca.png` diz mais que `playerShip1_blue.png`.
- **Cor branca no Sprite Renderer:** a cor multiplica a imagem; as cores dos provisórios
  escureceriam os sprites.
- **Escala igual em X e Y:** os provisórios eram esticados para imitar formatos.

**Problemas no caminho:**
- **Inimigos de ré:** as naves inimigas do pacote apontam para baixo. `Sprite Angle Offset`
  do `Enemy` passou de -90 para **90**.
- **Collider dos projéteis:** o raio é multiplicado pela escala do objeto; com a escala de
  0,12 para 1, o raio caiu para 0,15 (senão o laser acertaria a um metro de distância).
- **Tudo preto:** o `Global Light 2D` do template iluminava só a Sorting Layer `Default`.
  Sprites com `Sprite-Lit-Default` nas layers `Ships` e `Enemies` não recebiam luz nenhuma, e
  cor × luz zero = preto. As formas provisórias eram escuras e o problema passou despercebido.
  Correção: **Target Sorting Layers = All**.

**Como testar:** Play na cena Game: naves e inimigos coloridos, inimigos andando de frente
pela ponte, tiros acertando só ao encostar, inimigos tirando vida da nave-mãe.

## 2026-11-17 — Venda de naves

**O que foi feito:** clicar numa nave já posicionada abre o menu com o botão
**"Vender <nave> (+valor)"**; vendendo, a nave sai, o slot fica livre e os créditos voltam.
- `BuildController`: clique em slot livre → menu de compra (como antes); em slot ocupado →
  menu de venda. `OnSellChosen` lê o valor, chama `BuildSlot.TryClear` e devolve os créditos
  com `GameSession.AddCredits`.
- `BuildSlot.TryClear()`: destrói a nave e libera o slot (o marcador volta a aparecer).
- `BuildMenuUI.ShowSell(...)` e o evento `SellChosen`: um botão vermelho de venda. A criação
  de botão virou um método só (`AddButton(texto, custo, cor, ação)`), usado pela compra e
  pela venda.
- `DifficultyData.sellRefund` (**Sell Refund**, de 0 a 1; 1 = devolve tudo). O `GameManager`
  repassa para o `BuildController` com `Begin`, como já fazia com créditos e vida.
- Prefab `BuildSlot`: raio do collider de 0,1 para 0,35 (0,7 no mundo, com a escala 2), para
  o clique em qualquer parte da nave acertar o slot.

**Por quê assim:**
- **Devolução de 100%** (decisão do grupo): o jogador pode reorganizar as naves quando
  quiser. Fica num ScriptableObject, não fixo no código, porque é número de balanceamento: o
  Difícil pode devolver menos mudando só o asset. O `Facil.asset` não precisou mudar: campo
  novo que não existe no asset assume o valor padrão do script (1).
- **O valor é lido antes de destruir a nave:** depois do `TryClear` o objeto não existe mais.
- **Mesma divisão de papéis da compra:** o menu só mostra e avisa, o slot só guarda a nave,
  o `GameSession` é o único que mexe nos créditos, e o `BuildController` liga os três.
- **O clique é no slot, não na nave:** as naves não têm collider (nem precisam: os inimigos
  são achados pelo `OverlapCircle` da nave). Aumentar o collider do slot resolve o clique sem
  mexer nos prefabs das naves.

**Como testar:** comprar um Caça (−50), clicar nele e vender (+50): a nave some e o círculo
do slot volta; clicar no slot vazio abre de novo o menu de compra; Esc fecha o menu de venda.

## 2026-11-17 — Visual, parte 4: estilo do HUD

**O que foi feito:**
- **Fonte Kenvector Future** (do pacote da Kenney, com todos os acentos do português) em
  todos os textos do HUD, como *Font Asset* do TextMeshPro (`Art/Fonts/kenvector_future SDF`).
- **Barra do topo transparente**, com um ícone ao lado de cada número: cápsula verde (vida),
  nave inimiga (onda), **moeda com "$"** (créditos) e estrela (pontos). Os textos mostram só o
  número; o ícone diz o que é.
- `HUD.cs`:
  - a barra de vida muda de cor (verde → amarelo → vermelho) por um `Gradient`;
  - nos últimos 30 s o cronômetro pulsa em vermelho;
  - a tela de fim mostra o motivo (`GameManager.EndReason`, ex.: "A nave-mãe foi destruída")
    e aparece com fade e um leve zoom.
- Botões "Continuar" (azul) e "Jogar de novo" (verde) com as imagens da Kenney.
- `Editor/HudStyleSetup.cs`: menu **NebulaGuard → Aplicar estilo ao HUD**, que aplica tudo
  isso na cena de uma vez e salva.
- `tools/gerar_icone_moeda.py`: o pacote não tem moeda; o script desenha o ícone pixel a pixel.

**Por quê assim:**
- **Ferramenta de editor em vez de cliques:** eram ~40 ajustes de posição, cor e sprite; à mão
  é fácil errar ou esquecer de salvar (aconteceu). A ferramenta acha cada objeto pelo nome,
  só cria o que falta (rodar de novo não duplica), entra no Undo e salva a cena. Mudar o layout
  depois = mudar um número nela e rodar de novo.
- **9-slice nos botões:** a imagem tem 222 × 39 px e o botão 360 × 80. Com borda de 12 px, os
  cantos ficam do tamanho original e só o miolo estica, então o arredondado não deforma.
- **Fade com `Time.unscaledDeltaTime`:** no fim da partida o `timeScale` é 0, e o
  `Time.deltaTime` também; a animação usaria o tempo do jogo e ficaria parada.
- **Curva de saída no fade** (`1 - (1 - t)²`): começa rápido e desacelera, que é como uma
  janela entrando parece natural.
- **Cor da vida e alerta do tempo:** o jogador percebe o perigo sem ler o número.

**Problemas no caminho:**
- A cena não estava salva quando fui conferir: só a fonte tinha entrado.
- "Fundinho branco" atrás do ícone de vida: era um objeto `Image` sem sprite que sobrou de
  um teste (Image sem sprite é desenhada como um quadrado branco). Apagado.
- O primeiro ícone de vida (escudo prateado) era claro e parecia um quadrado; trocado pela
  cápsula verde.

**Como testar:** Play: barra do topo com ícones e números; perder vida deixa a barra amarela
e depois vermelha; Esc mostra o botão azul "Continuar"; no fim aparece o título, o motivo,
os pontos e o botão verde, com fade.

## 2026-11-17 — Visual, parte 5: menu de compra e venda no estilo novo

**O que foi feito:** `BuildMenuUI.cs` reescrito com TextMeshPro e as imagens da Kenney.
- Cabeçalho: "Construir" (ou o nome da nave, na venda) e o saldo com a moeda.
- Um botão por nave: **desenho da nave** (lido do `SpriteRenderer` do próprio prefab), nome e
  custo ao lado da moeda. Nave que não dá para pagar: botão apagado (45% de opacidade), sem
  clique e com o custo em vermelho; acende sozinho se os créditos chegarem com o menu aberto.
- Venda: botão vermelho com a nave, "Vender" e "+valor".
- Campos novos no componente (fonte, botão de compra, botão de venda, moeda), preenchidos
  pela ferramenta **NebulaGuard → Aplicar estilo ao HUD**.
- `BuildController`: `ShowSell` passou a receber a nave (para mostrar o desenho dela).

**Por quê assim:**
- **Continua montado por código:** o número de botões depende da lista `Tower Prefabs`;
  nave nova aparece no menu sem mexer na cena.
- **`HorizontalLayoutGroup` em cada botão** enfileira ícone, nome, moeda e valor; o nome tem
  `flexibleWidth = 1`, ocupa o que sobra e empurra o valor para a borda direita.
- **`CanvasGroup` para apagar o botão:** muda a opacidade do botão e de tudo dentro dele de
  uma vez. O tom cinza que o `Button` aplica quando desligado foi neutralizado
  (`disabledColor` branco), senão a imagem escureceria duas vezes.
- **Ícones e textos com `raycastTarget = false`:** o clique atravessa para o botão.
- **Funciona sem as imagens:** se os campos estiverem vazios, volta às cores lisas e à fonte
  padrão do TMP, em vez de dar erro.

**Problema no caminho:** os botões da Kenney são **brancos com borda colorida**, e o texto
branco sumia. Texto dentro dos botões passou a ser escuro (nome em `1A2238`, custo em dourado
escuro); a ferramenta aplicou o mesmo aos botões "Continuar" e "Jogar de novo" do HUD.

**Como testar:** Play; clicar num slot vazio mostra as 3 naves com desenho e custo; com 100
créditos o Cruzador (120) aparece apagado e com custo vermelho; comprar um Caça e clicar
nele mostra o botão vermelho "Vender +50".

## 2026-11-17 — Menu principal, Ranking e Sobre (código)

**O que foi feito:**
- `Core/GameSettings.cs`: classe estática com `SelectedDifficulty`. O menu escreve, o
  `GameManager` lê no `Awake`.
- `Core/Ranking.cs`: top 5 em `PlayerPrefs` (JSON via `JsonUtility`). `Submit(pontos, dificuldade)`
  devolve a posição (1 a 5) ou 0. Partida com 0 pontos não entra; empate fica atrás de quem
  já estava. O `GameManager` chama `Submit` ao fim de toda partida e o HUD mostra a posição.
- `DifficultyData.ScoreMultiplier` (Fácil ×1, Difícil ×2), aplicado em `GameSession.AddScore`.
- `UI/MainMenu.cs`: Jogar (escolha da dificuldade), Ranking (com "Limpar ranking"), Sobre e Sair,
  tudo montado por código, como o `BuildMenuUI`.
- `HUD`: botão "Menu principal" na pausa e na tela de fim; a linha de pontos mostra "novo recorde!"
  ou "#N no ranking".
- `Editor/MainMenuSetup.cs` (**NebulaGuard → Montar cena MainMenu**): cria `MainMenu.unity`
  (câmera, fundo de estrelas, objeto `MainMenu`), põe as duas cenas no Build Settings
  (MainMenu = índice 0) e acrescenta os botões "Menu principal" na cena Game.

**Por quê assim:**
- **Classe estática para a dificuldade:** é o jeito mais simples de um dado sobreviver à troca de
  cena, sem objeto `DontDestroyOnLoad`.
- **Multiplicador dentro do `GameSession`:** HUD, tela de fim e ranking leem o mesmo número.
- **Ferramenta de editor que só acrescenta:** o grupo edita a `Game.unity` à mão; recriar a cena
  apagaria o trabalho dos outros.
- `Time.timeScale = 1` antes de carregar qualquer cena, senão o jogo abriria parado se a saída
  fosse pela pausa.

**Como testar (nada disso foi rodado no Unity):** rodar a ferramenta; Play na MainMenu; Jogar →
Fácil → partida; Esc → "Menu principal" volta; perder/vencer e ver a linha do ranking; abrir
Ranking e Sobre. **Escrever os nomes do grupo** no `About Text` do objeto `MainMenu`. Conferir se
ã, ç e ó aparecem na fonte Kenvector (se virarem quadrados, falta fonte reserva).

## 2026-11-17 — Dificuldade Difícil: ponte extra, dados e ferramenta

**O que foi feito:**
- `DifficultyData.BridgeCount` e `WaveSpawner.UseBridges`: a dificuldade diz quantas pontes usar;
  as outras e o portal delas (`Second Bridge Objects`) ficam desligados. Com duas, os inimigos
  se alternam entre elas.
- `Data/Dificil.asset`: 5 ondas, 50 inimigos, nave-mãe com 15 de vida, 300 s, +25% de vida, +15% de
  velocidade, venda devolve 70%, pontos ×2, 2 pontes.
- `Editor/HardModeSetup.cs` (**NebulaGuard → Adicionar ponte do Difícil**): cria `LightBridge_B`
  (reta de (-14, 0) a (12, 0)) e `Portal_B` em (-14,8; 0) na cena Game, liga no `WaveSpawner` e
  inclui o Dificil na lista do menu, se ele já existir.

**Por quê assim:**
- **Ponte B reta em y = 0:** passa entre as duas fileiras de slots (y = ±2), dentro do alcance de
  todas as naves. Tem ~26 unidades contra ≈ 55 da A: o jogador defende dois caminhos, um longo
  e um curto e rápido, que é o que torna o Difícil diferente, e não só mais vida.
- **Quantidade de pontes na dificuldade, não na cena:** o Fácil continua com uma ponte sem a
  gente apagar nada da cena.
- **Tempo estimado:** com velocidade ×1,15, ~50 s por onda e 10 s de preparação, 5 ondas cabem
  nos 300 s com pouca folga.

**Atenção:** os números do `Dificil.asset` são um palpite, **nunca testados**. Jogar e balancear
(vida da nave-mãe, créditos, tamanho das ondas) antes da entrega.

**Como testar:** rodar a ferramenta; no menu, Jogar → Difícil; os inimigos saem dos dois portais
(A em zigue-zague, B reta); no Fácil, só o portal A aparece.

## 2026-11-17 — Menu e Difícil montados no Unity; naves maiores e mais créditos no Difícil

**O que foi feito:**
- Rodadas as ferramentas **Montar cena MainMenu** e **Adicionar ponte do Difícil**: existem
  `Scenes/MainMenu.unity` (índice 0 do Build Settings), `LightBridge_B` e `Portal_B` na cena Game.
  Nomes do grupo no `About Text`.
- Difícil: **créditos iniciais 100 → 150** (`Data/Dificil.asset`). No primeiro teste ele ficou
  difícil demais: com 100 só dava para uma nave antes da primeira onda vir pelos dois caminhos.
- Naves-torre bem maiores (escala no prefab): Caça 0,8 → 1,3; Bombardeiro 1 → 1,5;
  Cruzador 1,1 → 1,6.
- Inimigos um pouco maiores: Drone 0,6 → 0,7; Caça alienígena 0,8 → 0,9; Couraçado 1,1 → 1,25.
- `BuildSlot`: raio do collider 0,35 → 0,5 (com a escala 2 do slot, 1 unidade no mundo), para o
  clique de venda pegar a nave inteira, agora maior.

**Por quê assim:**
- **Só a escala muda, o alcance não:** alcance e dano ficam no `TowerData`; a nave maior é só
  visual. O `Muzzle` (ponta de onde sai o tiro) é filho da nave, então acompanha a escala e
  continua no bico.
- **Por que cabe:** o Cruzador, o maior, fica com ~1,6 × 1,2 unidades. Os slots estão a 4 unidades
  um do outro e a ~2,5 da borda da ponte, e a ponte B passa a 2 unidades deles: não encosta em nada.
- **Inimigos só um pouco:** o collider deles cresce junto (fica um pouco mais fácil de acertar);
  aumentar muito mudaria o balanceamento e embolaria a fila na ponte.
- **Collider do slot maior:** as naves não têm collider próprio; o clique acerta o slot embaixo.
  Com raio 0,7 no mundo, clicar na asa da nave grande caía fora.

**Como testar:** Play na cena MainMenu → Difícil: começa com 150 créditos. Construir as três naves
e ver que cabem nos corredores; clicar na ponta da asa abre o "Vender".
