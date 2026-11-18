# Space Defense — contexto para IA

**Nome do jogo: Space Defense** (até 18/11 era *Nebula Guard*; o nome antigo ficou só por
dentro: namespace `NebulaGuard`, menu **NebulaGuard → …** das ferramentas e a chave do ranking).

Tower defense 2D no espaço, feito em Unity para a disciplina CCC300 (UPF, 2026/2),
em trio. Entrega e apresentação em **24/11/2026**.

- **Plano completo (fonte da verdade):** documento de design em
  https://claude.ai/artifact/9BfguFbPifwWN8jNRcu7mF — requisitos do PDF, torres,
  inimigos, ondas, divisão do trio e cronograma. Este arquivo não repete o plano:
  registra o que **já foi feito** e os **padrões** que o código segue.
- **Diário:** `docs/DIARIO.md` — cada passo, com o que foi feito e por quê. Leia
  antes de mexer numa área; escreva nele ao terminar um passo.

## Como trabalhamos com IA

O grupo pediu para avançar **uma parte por vez**, com a IA explicando como fez e
por que escolheu aquele jeito (o professor pergunta individualmente sobre
qualquer parte). Não gerar o jogo inteiro de uma vez. Ao terminar um passo:

1. Adicionar uma entrada em `docs/DIARIO.md` (o quê, por quê, como testar).
2. Atualizar a seção **Estado atual** abaixo.
3. Se surgiu um padrão novo, registrá-lo em **Padrões** abaixo.

## Versão e ferramentas

- **Unity 6000.3.27f1** (Unity 6.3 LTS), template **Universal 2D** (URP).
  O doc de design citava 6000.0.x; usamos a 6.3 LTS, que era a recomendada pelo
  Hub quando o projeto foi criado. **Os três PCs precisam da mesma versão.**
- Módulo **Windows Build Support (Mono)** no Hub: necessário para gerar o `.exe`
  a partir de um Mac.
- Editor de código: VS Code com a extensão Unity.
- **A raiz do repositório é a raiz do projeto Unity** (`Assets/`, `Packages/`,
  `ProjectSettings/` ficam direto nela). Para abrir: Unity Hub → Add → Add project
  from disk → a pasta do repo clonado.
- Na primeira abertura do editor, o Unity pede para aceitar os termos de uso
  numa janela que pode ficar escondida atrás das outras. Enquanto ela não for
  aceita, o projeto não é criado e o Hub mostra "project not found".

## Estrutura de pastas

Todo asset nosso fica em `Assets/_Project/` (o `_` mantém a pasta no topo e
separa do que pacotes e o template criam):

```
Assets/_Project/
  Scripts/
    Core/      GameManager, GameSession, GameSettings, MotherShip, Ranking, AudioManager
    Data/      TowerData, EnemyData, DifficultyData
    Enemies/   Enemy, LightBridge, WaveSpawner
    Towers/    TowerShip, Projectile, BuildSlot, BuildController
    UI/        HUD, MainMenu, BuildMenuUI
    Effects/   CameraShake, ParallaxLayer, DeathEffect, EffectSpawner, PortalVortex
    Editor/    BuildSlotSetup, HudStyleSetup, MainMenuSetup, HardModeSetup, EffectsSetup, PortalSetup (só existem no editor, não entram no build)
  Data/        assets .asset (ScriptableObjects) de naves, inimigos e dificuldades
  Prefabs/     naves, inimigos, projéteis; Effects/ (Explosao)
  Scenes/      MainMenu, Game, Sandbox/ (cena de teste de cada um)
  Art/         sprites (Towers, Enemies, Projectiles, Base, Effects), UI (botões e ícones), Fonts
  Audio/       música e efeitos
  Materials/   pontes de luz, partículas
```

Fora de `Assets/`, a pasta `tools/` guarda scripts auxiliares que o Unity não importa
(ex.: `gerar_icone_moeda.py`, que desenha o `Art/UI/IconCredits.png`, `gerar_particula.py` e
`gerar_portal.py`, que desenham o `Art/Effects/Particle.png` e o `PortalSwirl.png`).

## Padrões de código

- **Namespace `NebulaGuard`** em todos os scripts.
- **Nomes de classes, métodos e variáveis em inglês** (os mesmos do doc de
  design: `Enemy`, `TowerShip`, `LightBridge`…); **comentários e documentação em
  português**.
- **Um componente por responsabilidade.** Ex.: `LightBridge` só sabe o caminho;
  quem anda nele é o `Enemy`.
- Campos editáveis no Inspector: `[SerializeField] private`, não `public`. O que
  outros scripts precisam ler é exposto por propriedade só de leitura
  (`public float TotalLength => totalLength;`).
- Números de balanceamento (vida, dano, custo, velocidade) em
  **ScriptableObjects** (`Scripts/Data`), nunca fixos no código.
- Avisar a interface por **eventos C#** (`System.Action`), sem o script de jogo
  conhecer o HUD.
- Todo movimento multiplicado por `Time.deltaTime`; pausa via `Time.timeScale = 0`.
- Dano em área: `SplashRadius` no `TowerData` (0 = sem área). O `Projectile` explode no
  primeiro inimigo que toca e tira o mesmo dano dos vizinhos no raio, buscando só na
  layer `Enemy`. A mira `MostClustered` escolhe o inimigo com mais vizinhos nesse raio.
- **Brilho = Bloom**: brilha o que for mais claro que o Threshold (0,6) do `GameVolumeProfile`.
  Para algo brilhar, cor clara e material `Sprite-Unlit-Default`; para não brilhar, cor mais
  escura. O HUD (Canvas Screen Space - Overlay) não passa pelo pós-processamento.
- **Tamanho de nave = escala do prefab** (Caça 1,3, Bombardeiro 1,5, Cruzador 1,6; Drone 0,7,
  Caça alienígena 0,9, Couraçado 1,25). Não mexe no alcance (`TowerData`). Nave-torre não tem
  collider: o clique acerta o do `BuildSlot`, que precisa cobrir a nave inteira.
- **Sprite novo:** importa como Sprite a 100 px por unidade (≈ 1 unidade por nave); no Sprite
  Renderer, **Color branca** (a cor multiplica a imagem) e escala igual em X e Y. Os inimigos
  da Kenney apontam para baixo: `Sprite Angle Offset = 90` no `Enemy` (as naves-torre, para
  cima, usam -90). Collider de projétil: o raio é multiplicado pela escala.
- **Luz 2D só ilumina as Sorting Layers marcadas** em *Target Sorting Layers*. Objeto com
  `Sprite-Lit-Default` numa layer sem luz fica **preto**. O Global Light 2D está em **All**.
- **Materiais sempre do URP**, nunca os embutidos do pipeline antigo
  (`Sprites-Default`, `Default-Line`): esses não respondem ao Bloom nem às luzes
  2D. Ficam em Packages → Universal RP → Runtime → Materials.
  `Sprite-Unlit-Default` para o que brilha sozinho (pontes, lasers, portais);
  `Sprite-Lit-Default` para objetos sólidos (naves, nave-mãe, asteroides).
- Campo que guarda um prefab é tipado pelo componente que ele precisa ter
  (`Projectile projectilePrefab`, não `GameObject`): o Inspector recusa prefab errado.
- `Data/` só tem assets de dados (`.asset`); objetos vão em `Prefabs/`. Mudança feita
  numa instância de prefab na cena (ex.: filho novo) só vai para o prefab com
  **Overrides → Apply**.
- **Créditos e pontos só mudam pelo `GameSession`** (`TrySpend`, `AddCredits`,
  `AddScore`); a interface escuta `CreditsChanged` e `ScoreChanged`. Todo inimigo gerado
  precisa de `session.Register(enemy)` (o `WaveSpawner` deve chamar), senão não dá
  créditos ao morrer.
- **Construir** = `BuildController` (clique) → `BuildMenuUI` (escolha) → `GameSession`
  (cobra) → `BuildSlot` (instancia). O custo vem do `TowerData` da nave. Nave nova no
  jogo = acrescentar o prefab em `Tower Prefabs` do `BuildController`.
- **Vender** = clique num slot ocupado → `BuildMenuUI.ShowSell` → `BuildSlot.TryClear` →
  `GameSession.AddCredits`. A fração devolvida é o `Sell Refund` do `DifficultyData` (1 no
  Fácil), repassada pelo `GameManager` em `buildController.Begin`.
- **`BuildMenuUI` monta o próprio Canvas e botões por código** (uGUI + TextMeshPro, com
  `HorizontalLayoutGroup` em cada botão) e cria um `EventSystem` se a cena não tiver nenhum.
  A fonte e as imagens ficam em campos do componente, preenchidos pela ferramenta
  **Aplicar estilo ao HUD**; sem elas, o menu volta às cores lisas.
- **Botões da Kenney são brancos com borda colorida:** texto dentro deles é **escuro**
  (`1A2238`); texto branco só sobre fundo escuro. O
  projeto usa só o **novo Input System**: ler mouse/teclado com `Mouse.current` e
  `Keyboard.current`, nunca `Input.GetKey`/`Input.mousePosition`.
- Scripts de ferramenta do editor ficam em `Scripts/Editor/`, no namespace
  `NebulaGuard.EditorTools`, e **só acrescentam** à cena (nunca recriam `Game.unity`):
  o grupo edita a cena à mão e uma ferramenta que a recria apagaria o trabalho dos outros.
- Ao duplicar um prefab de inimigo ou nave (`Cmd + D`), **trocar o campo Data**: a cópia
  continua apontando para o asset do original. Mudanças em ScriptableObject só vão para o
  disco com **File → Save Project** (o `Cmd + S` salva a cena).
- **A dificuldade da partida mora no `GameManager`**: ele aplica os números
  (`session.Begin`, `motherShip.Begin`) e começa as ondas (`waveSpawner.Begin`). Quem
  precisar saber a dificuldade lê `GameManager.Difficulty`.
- **A dificuldade vem do menu por `GameSettings.SelectedDifficulty`** (classe estática: sobrevive
  à troca de cena). O `GameManager` a usa no `Awake`; se for nula (cena Game aberta direto no
  editor), vale a do campo `Difficulty` dele. Cena nova que dependa do `GameManager` precisa
  estar no Build Settings; o **MainMenu é o índice 0** (o `.exe` abre por ele).
- **Pontos = pontos do inimigo × `ScoreMultiplier` da dificuldade**, aplicado no
  `GameSession.AddScore`; por isso HUD, tela de fim e ranking mostram o mesmo número.
- **Ranking** (`Core/Ranking.cs`, classe estática, top 5 em `PlayerPrefs`): o `GameManager` chama
  `Ranking.Submit` ao fim de **toda** partida (vitória ou derrota) e guarda a posição em
  `LastRankingPosition`, que o HUD mostra. Partida com 0 pontos não entra; empate fica atrás de
  quem já estava.
- **Quantas pontes a partida usa vem da dificuldade** (`BridgeCount`: Fácil 1, Difícil 2). O
  `WaveSpawner` liga só essas e desliga as outras junto com o portal delas (`Second Bridge
  Objects`). Ponte nova = filha de nada: o `LightBridge` trata **todo filho como waypoint**, então
  o portal não pode ser filho da ponte.
- **O Esc é só do `GameManager`** (fecha o menu de construção, senão pausa). Tela nova que
  precise do Esc deve passar por ele, não ler a tecla por conta própria.
- **O `HUD` é montado no editor** (`HUD_Canvas`, uGUI + **TextMeshPro**, Canvas Scaler
  1920×1080) e só escuta eventos; dado que muda todo frame (cronômetro) é lido no `Update`.
  Ao trocar a âncora de um elemento, segurar **Alt + Shift** ou redigitar posição e tamanho
  (o Unity mantém a posição antiga). Texto TMP grande: **Wrapping = Disabled**.
  O estilo (posições, cores, ícones, botões) é aplicado por **NebulaGuard → Aplicar estilo ao
  HUD** (`Editor/HudStyleSetup.cs`): mudar o layout = mudar os números na ferramenta e rodar de
  novo. Ela acha os objetos pelo nome e só cria o que falta. Fonte: `kenvector_future SDF`.
  Botões da Kenney: sprite com borda 12 (9-slice) e **Image Type = Sliced**. Animação com o
  jogo parado (`timeScale = 0`) usa `Time.unscaledDeltaTime`.
- **Image sem sprite aparece como um quadrado branco**: objeto de UI que sobrou de um teste
  aparece como "fundo branco".
- `[ExecuteAlways]` só em componentes que precisam se atualizar no editor (ex.:
  a ponte de luz redesenhando ao arrastar waypoints), sempre separando o que roda
  só no editor com `if (!Application.isPlaying)`.
- **Efeitos de partículas:** um prefab só (`Prefabs/Effects/Explosao`), tocado por
  `EffectSpawner.Play(prefab, posição, tamanho)`. O Particle System usa **Scaling Mode =
  Hierarchy** (o tamanho é a escala do objeto), **Stop Action = Destroy** (some sozinho),
  material `Sprite-Unlit-Default`, Sorting Layer `Effects` e o desenho da partícula no módulo
  **Texture Sheet Animation** (modo Sprites). Efeito de morte = componente `DeathEffect` no
  prefab do inimigo (escuta o `Died` e o `ReachedBase`); de impacto = campo `Impact Effect Prefab` do `Projectile`.
- **Rastro de projétil:** Particle System filho em espaço **World**, ligado no campo `Trail` do
  `Projectile`. O projétil some sempre pelo `Despawn`, que solta o rastro antes de se apagar
  (não usar `Destroy(gameObject, tempo)` no projétil).
- **Tremida de tela:** `CameraShake.AddTrauma(0..1)` na Main Camera; hoje é chamada pelos
  eventos da `MotherShip`.
- Inimigos e projéteis avisam o resto do jogo por **eventos de instância**
  (`Died`, `ReachedBase`); quem reage (créditos, pontos, HUD) assina o evento.
- Triggers reconhecem o outro objeto pelo **componente** (`TryGetComponent<Enemy>`),
  não pela tag. Quem se move por código usa `Rigidbody2D` **Kinematic**: sem
  `Rigidbody2D` em pelo menos um dos lados, o `OnTriggerEnter2D` não dispara.

## Padrões de cena

- Câmera **ortográfica, size 9** → área visível de 32 × 18 unidades em 16:9
  (x de -16 a 16, y de -9 a 9). Tudo em z = 0.
- O topo da tela (y > 6) fica livre para o HUD.
- Fluxo da esquerda para a direita: portais à esquerda, nave-mãe à direita.
- **Sorting Layers** (de trás para frente): `Background`, `Bridges`, `Ships`,
  `Enemies`, `Projectiles`, `Effects`. A UI fica em Canvas próprio.
- Tags: `Enemy` nos inimigos, `MotherShip` na base. Layer `Enemy` para a busca
  das torres com `Physics2D.OverlapCircle`.

## Mapa (cena Game)

Ponte de luz principal (Fácil), objeto `LightBridge_A` com estes waypoints filhos,
em ordem (o `LightBridge` usa a ordem da Hierarchy, não o nome). As quinas viram curvas
(`Corner Radius` = 1,5), então os inimigos passam perto dos waypoints do meio, mas não por
cima deles. Comprimento total com as curvas: **≈ 57 unidades** (o requisito de 30 inimigos
enfileirados pede 18).

| Waypoint | Posição (x, y) | Observação |
| --- | --- | --- |
| wp_00 | (-13.4, 4.5) | borda do portal (o redemoinho tem raio ≈ 0,9; a linha não passa por cima dele) |
| wp_01 | (-9, 4.5) | |
| wp_02 | (-9, -5) | |
| wp_03 | (-3, -5) | |
| wp_04 | (-3, 4.5) | |
| wp_05 | (3, 4.5) | |
| wp_06 | (3, -5) | |
| wp_07 | (9, -5) | |
| wp_08 | (9, 0) | |
| wp_09 | (13.8, 0) | centro da nave-mãe: o inimigo entra no casco antes de bater |

Objetos da cena `Assets/_Project/Scenes/Game.unity` até agora:

| Objeto | Posição | Escala | Sorting Layer | Material | Observação |
| --- | --- | --- | --- | --- | --- |
| Main Camera | (0, 0, -10) | — | — | — | ortográfica, size 9, fundo `0B0E1A`, Post Processing ligado |
| Global Light 2D | (0, 0) | — | — | — | do template, intensidade 1, **Target Sorting Layers = All** |
| LightBridge_A | (0, 0) | 1 | Bridges | Sprite-Unlit-Default | linha ciano `3FE0FF`, largura 0,85, 6 cap vertices; Corner Radius 1,5, Corner Segments 10 |
| Portal_A | (-14.8, 4.5) | 1.8 | Ships | Sprite-Unlit-Default | redemoinho: miolo escuro + filhos `Swirl_Outer`, `Swirl_Inner`, `Sparks` e `PortalVortex` (**NebulaGuard → Trocar visual dos portais**) |
| LightBridge_B | (0, 0) | 1 | Bridges | Sprite-Unlit-Default | **só no Difícil**; cópia da A com 2 waypoints: (-13.4, 0) → (13.8, 0), reta. Criada por **Adicionar ponte do Difícil** |
| Portal_B | (-14.8, 0) | 1.8 | Ships | Sprite-Lit-Default | **só no Difícil**; cópia do Portal_A, em `Second Bridge Objects` do `WaveSpawner` |
| Mothership | (13.8, 0) | 3 | Ships | Sprite-Lit-Default | sprite `Art/Base/Mothership` (Kenney), tag `MotherShip`, `MotherShip` + `BoxCollider2D` trigger pequeno no centro (Size 0,4 × 0,4, Offset 0; 1,2 × 1,2 no mundo) |
| BuildSlots | (0, 0) | 1 | — | — | pai dos 6 slots `Slot_01`…`Slot_06`, instâncias de `Prefabs/BuildSlot` (círculo ciano provisório, `BuildSlot` + `CircleCollider2D` trigger com raio 0,5, que cobre a nave posicionada) |
| GameSession | (0, 0) | — | — | — | créditos e pontos; os créditos iniciais vêm do `DifficultyData` pelo `GameManager` |
| GameManager | (0, 0) | — | — | — | Difficulty = `Data/Facil`; referências a `WaveSpawner`, `Mothership`, `GameSession` e `BuildController` |
| WaveSpawner | (0, 0) | — | — | — | Bridges = `LightBridge_A`, Session = `GameSession`; a dificuldade vem do `GameManager` (`Begin`); gera os inimigos (nenhum inimigo fica na cena antes do Play) |
| BuildController | (0, 0) | — | — | — | `BuildController` + `BuildMenuUI`; `Tower Prefabs` = Caça, Bombardeiro, Cruzador |
| HUD_Canvas | — | — | — | — | Canvas Scaler 1920×1080 (match 0,5), componente `HUD`; filhos `TopBar`, `CountdownText`, `PausePanel`, `EndPanel` |
| EventSystem | — | — | — | — | `InputSystemUIInputModule` |
| Global Volume | — | — | — | — | perfil `Assets/Settings/GameVolumeProfile` (Bloom: Threshold 0,6, Intensity 1,5, Scatter 0,7; Vignette 0,3). A Main Camera tem **Post Processing** ligado |
| Background | (0, 0) | 1 | Background | Sprite-Unlit-Default | fundo de estrelas: filhos `Stars_Far`, `Stars_Mid`, `Stars_Near` com `ParallaxLayer` (velocidades 0,15 / 0,4 / 0,9; Sorting Order 0 / 1 / 2); as estrelas são criadas no `Start` |

O Caça solto em (-6, 0) saiu da cena: a nave agora nasce nos slots.

Posições dos slots (dois por corredor, y = ±2): (-6, ±2), (0, ±2) e (6, ±2).
Sprites: pacote **Space Shooter (Remastered)** da Kenney (CC0). Só as imagens usadas foram
copiadas para `Art/` (com nomes do jogo); `Art/CREDITOS.txt` diz de qual arquivo do pacote veio
cada uma. Os slots de construção ainda são círculos provisórios.

Os corredores entre as colunas da ponte (x ≈ -6, 0 e 6) são onde ficarão os
pontos de ancoragem das torres. A segunda ponte (Difícil) é uma reta em y = 0: passa entre as
duas fileiras de slots (y = ±2), a 2 unidades de cada uma, dentro do alcance de todas as naves
(Caça 3,5). Tem ~26 unidades, contra ≈ 55 da ponte A: é o caminho curto e rápido.

## Naves-torre (valores atuais)

Os números moram nos assets de `Data/`, que são a fonte da verdade; esta tabela é só uma
cópia para consulta e pode ficar para trás se alguém balancear. O doc de design traz valores
iniciais um pouco diferentes (o Caça foi rebalanceado de 5 para 3 de dano).

| Nave | Custo | Alcance | Tiros/s | Dano | Raio da explosão | Vel. do projétil | Giro (°/s) | Mira |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Caça | 50 | 3,5 | 3 | 3 | 0 (sem área) | 14 | 540 | `First` (o mais perto da nave-mãe) |
| Bombardeiro | 80 | 3 | 0,6 | 20 | 1,2 | 8 | 180 | `MostClustered` (o do maior grupo) |
| Cruzador | 120 | 7 | 0,5 | 40 | 0 (sem área) | 30 | 270 | `Strongest` (o de mais vida) |

Prefabs em `Prefabs/`: as naves `Caca`, `Bombardeiro` e `Cruzador`, e os projéteis
`Laser_Caca`, `Missil_Bombardeiro` e `Raio_Cruzador`. As três estão na lista
`Tower Prefabs` do `BuildController` da cena.

## Estado atual

- [x] `.gitignore` e `.gitattributes` de Unity na raiz
- [x] `LightBridge.cs` — caminho por waypoints + LineRenderer
- [x] Projeto Unity criado (6000.3.27f1, template 2D com URP) na raiz do repo
- [x] Primeiro commit do projeto no GitHub (PR #1)
- [x] Tags, layer `Enemy` e sorting layers configuradas
- [x] Cena Game com câmera, portal, ponte de luz e nave-mãe (formas provisórias)
- [x] `EnemyData.cs`, `Enemy.cs` e `MotherShip.cs`
- [x] Drone na cena: `Data/Drone.asset`, `Prefabs/Drone.prefab`, `MotherShip` no
  `Mothership` (collider trigger com Offset X = -0,5) e teste do drone chegando na base
- [x] `TowerData.cs`, `TowerShip.cs`, `Projectile.cs`; Caça em (-6, 0) atirando no Drone
- [x] **Construção de naves (item A) — código pronto:** `GameSession.cs` (créditos e
  pontos), `BuildSlot.cs` (ponto de ancoragem), `BuildController.cs` (clique),
  `BuildMenuUI.cs` (menu de escolha com nome, custo e saldo) e a ferramenta
  `Editor/BuildSlotSetup.cs` (menu **NebulaGuard → Adicionar slots de construção**)
- [x] Construção testada no Unity: slots na cena, compra do Caça (100 → 50), botão
  desligado sem saldo e +5 créditos ao destruir o drone
- [x] Ondas: `DifficultyData.cs`, `WaveSpawner.cs`, `Data/Facil.asset` (3 ondas só de
  drones) e `WaveSpawner` na cena; provisórios `Test Bridge` e registro no
  `GameSession.Start` removidos
- [x] Caça alienígena e Couraçado alienígena (`EnemyData` + prefab) nas ondas 2 e 3 do Fácil
- [x] Balanceamento do Fácil testado (Caça com 3 de dano; valores no plano e no diário de 13/11)
- [x] `GameManager`: estados, cronômetro (240 s no Fácil), pausa com Esc, vitória e
  derrota; créditos, vida da nave-mãe e tempo vêm do `DifficultyData`
- [x] HUD (`UI/HUD.cs` + `HUD_Canvas` na cena): vida da nave-mãe, onda, cronômetro, créditos e
  pontos no topo; contagem antes de cada onda; telas de pausa e de fim com "Jogar de novo"
- [x] Bombardeiro e Cruzador: `TowerData.SplashRadius`, mira `MostClustered` e dano em área no
  `Projectile`; assets e prefabs (valores na tabela "Naves-torre"); ligados no menu de construção
  e testados no Unity
- [x] Ponte de luz arredondada (`LightBridge.cornerRadius`) e mais grossa (largura 0,85)
- [x] Bloom e Vignette: `Global Volume` na cena Game
- [x] Fundo com estrelas: `Effects/ParallaxLayer.cs`, 3 camadas no objeto `Background`
- [x] Sprites da Kenney nas naves, inimigos, projéteis, nave-mãe e portal (`Art/`)
- [x] Venda de naves: clique na nave → "Vender" devolve 100% do custo (`Sell Refund`)
- [x] Estilo do HUD: fonte Kenvector Future, barra do topo transparente com ícones (vida,
  onda, moeda, estrela), barra de vida que muda de cor, cronômetro em alerta nos últimos 30 s,
  botões da Kenney e tela de fim com motivo e fade (`Editor/HudStyleSetup.cs`)
- [x] Menu de compra e venda no mesmo estilo (TMP, botões da Kenney, desenho da nave, nave cara apagada)
- [x] **Menu principal, Ranking e Sobre — código pronto:** `UI/MainMenu.cs` (Jogar com escolha de
  dificuldade, Ranking, Sobre, Sair; montado por código), `Core/Ranking.cs`, `Core/GameSettings.cs`,
  multiplicador de pontos (`DifficultyData.ScoreMultiplier`), botão "Menu principal" na pausa e no
  fim (`HUD`) e a ferramenta `Editor/MainMenuSetup.cs` (**NebulaGuard → Montar cena MainMenu**)
- [x] Menu montado e testado no Unity: `Scenes/MainMenu.unity` (índice 0 do Build Settings), nomes
  do grupo no `About Text`
- [x] **Difícil — dados e código prontos:** `Data/Dificil.asset` (5 ondas, 50 inimigos, nave-mãe 15,
  300 s, +25% vida, +15% velocidade, venda 70%, pontos ×2), `DifficultyData.BridgeCount`, uso das
  pontes no `WaveSpawner` e a ferramenta `Editor/HardModeSetup.cs`
  (**NebulaGuard → Adicionar ponte do Difícil**)
- [x] Ponte do Difícil na cena (`LightBridge_B`, `Portal_B`); créditos iniciais do Difícil 100 → 150
  após o primeiro teste
- [x] Naves-torre maiores (Caça 1,3, Bombardeiro 1,5, Cruzador 1,6) e inimigos um pouco maiores;
  collider do `BuildSlot` com raio 0,5
- [ ] **Falta no Difícil:** continuar o balanceamento (vida da nave-mãe, tamanho das ondas) e
  atualizar a tabela de dificuldades do plano
- [x] **Explosões e tremida — código pronto:** `Effects/DeathEffect.cs`, `EffectSpawner.cs`,
  `CameraShake.cs`, efeito de impacto no `Projectile` e a ferramenta `Editor/EffectsSetup.cs`
  (**NebulaGuard → Adicionar explosões e tremida**)
- [x] Explosões e tremida testadas no Unity
- [x] Explosão também quando o inimigo bate na nave-mãe (`DeathEffect` escuta o `ReachedBase`)
- [x] **Portal novo — código pronto:** redemoinho animado (`PortalVortex`, `Art/Effects/PortalSwirl.png`)
  e a ferramenta `Editor/PortalSetup.cs` (**NebulaGuard → Trocar visual dos portais**)
- [x] Portal novo e pontas das pontes (início na borda do portal, fim no centro da nave-mãe) testados no Unity
- [x] **Rastro de fumaça do míssil — código pronto:** campo `Trail` no `Projectile` e o menu
  **NebulaGuard → Adicionar rastro de fumaça no míssil**
- [ ] Rodar a ferramenta do rastro no Unity e testar
- [x] Nome do jogo: **Space Defense** (título do menu e Product Name)
- [ ] Sons: SFX (tiro, explosão, compra, dano na base) e música (`AudioManager`)
- ~~Astronauta~~: cortado em 17/11 (o PDF não exige); o tempo vai para o visual
- [ ] Build Windows, vídeo demonstrativo e links no README
