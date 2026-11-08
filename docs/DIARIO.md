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
