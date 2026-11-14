# Nebula Guard — contexto para IA

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
    Core/      GameManager, GameSession, MotherShip, Ranking, AudioManager
    Data/      TowerData, EnemyData, DifficultyData
    Enemies/   Enemy, LightBridge, WaveSpawner
    Towers/    TowerShip, Projectile, BuildSlot
    Player/    Astronaut
    UI/        HUD, MainMenu, BuildMenuUI
    Effects/   CameraShake, ParallaxLayer
    Editor/    BuildSlotSetup (só existe no editor, não entra no build)
  Data/        assets .asset (ScriptableObjects) de naves, inimigos e dificuldades
  Prefabs/     naves, inimigos, projéteis, efeitos
  Scenes/      MainMenu, Game, Sandbox/ (cena de teste de cada um)
  Art/         sprites
  Audio/       música e efeitos
  Materials/   pontes de luz, partículas
```

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
- **`BuildMenuUI` monta o próprio Canvas e botões por código** (uGUI + `Text` legado,
  fonte `LegacyRuntime.ttf`) e cria um `EventSystem` se a cena não tiver nenhum. O
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
- **O Esc é só do `GameManager`** (fecha o menu de construção, senão pausa). Tela nova que
  precise do Esc deve passar por ele, não ler a tecla por conta própria.
- **O `HUD` é montado no editor** (`HUD_Canvas`, uGUI + **TextMeshPro**, Canvas Scaler
  1920×1080) e só escuta eventos; dado que muda todo frame (cronômetro) é lido no `Update`.
  Ao trocar a âncora de um elemento, segurar **Alt + Shift** ou redigitar posição e tamanho
  (o Unity mantém a posição antiga). Texto TMP grande: **Wrapping = Disabled**.
- `[ExecuteAlways]` só em componentes que precisam se atualizar no editor (ex.:
  a ponte de luz redesenhando ao arrastar waypoints), sempre separando o que roda
  só no editor com `if (!Application.isPlaying)`.
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
em ordem (o `LightBridge` usa a ordem da Hierarchy, não o nome). Comprimento total: **59,5 unidades** (o requisito de 30 inimigos
enfileirados pede 18).

| Waypoint | Posição (x, y) | Observação |
| --- | --- | --- |
| wp_00 | (-14, 4.5) | saída do portal |
| wp_01 | (-9, 4.5) | |
| wp_02 | (-9, -5) | |
| wp_03 | (-3, -5) | |
| wp_04 | (-3, 4.5) | |
| wp_05 | (3, 4.5) | |
| wp_06 | (3, -5) | |
| wp_07 | (9, -5) | |
| wp_08 | (9, 0) | |
| wp_09 | (12, 0) | entrada da nave-mãe (x ≈ 13,5) |

Objetos da cena `Assets/_Project/Scenes/Game.unity` até agora:

| Objeto | Posição | Escala | Sorting Layer | Material | Observação |
| --- | --- | --- | --- | --- | --- |
| Main Camera | (0, 0, -10) | — | — | — | ortográfica, size 9, fundo `0B0E1A` |
| Global Light 2D | (0, 0) | — | — | — | do template, intensidade 1 |
| LightBridge_A | (0, 0) | 1 | Bridges | Sprite-Unlit-Default | linha ciano `3FE0FF`, largura 0,15, 4 corner vertices |
| Portal_A | (-14.8, 4.5) | 1.5 | Ships | Sprite-Lit-Default | círculo roxo provisório |
| Mothership | (13.8, 0) | (2.5, 4) | Ships | Sprite-Lit-Default | quadrado cinza provisório, tag `MotherShip`, `MotherShip` + `BoxCollider2D` trigger com Offset X = -0,5 |
| BuildSlots | (0, 0) | 1 | — | — | pai dos 6 slots `Slot_01`…`Slot_06`, instâncias de `Prefabs/BuildSlot` (círculo ciano provisório, `BuildSlot` + `CircleCollider2D` trigger) |
| GameSession | (0, 0) | — | — | — | créditos e pontos; os créditos iniciais vêm do `DifficultyData` pelo `GameManager` |
| GameManager | (0, 0) | — | — | — | Difficulty = `Data/Facil`; referências a `WaveSpawner`, `Mothership`, `GameSession` e `BuildController` |
| WaveSpawner | (0, 0) | — | — | — | Bridges = `LightBridge_A`, Session = `GameSession`; a dificuldade vem do `GameManager` (`Begin`); gera os inimigos (nenhum inimigo fica na cena antes do Play) |
| BuildController | (0, 0) | — | — | — | `BuildController` + `BuildMenuUI`; `Tower Prefabs` = Caça |
| HUD_Canvas | — | — | — | — | Canvas Scaler 1920×1080 (match 0,5), componente `HUD`; filhos `TopBar`, `CountdownText`, `PausePanel`, `EndPanel` |
| EventSystem | — | — | — | — | `InputSystemUIInputModule` |

O Caça solto em (-6, 0) saiu da cena: a nave agora nasce nos slots.

Posições dos slots (dois por corredor, y = ±2): (-6, ±2), (0, ±2) e (6, ±2).
Sprites provisórios são formas do Unity (2D Object → Sprites) até entrarem os da
Kenney.

Os corredores entre as colunas da ponte (x ≈ -6, 0 e 6) são onde ficarão os
pontos de ancoragem das torres. A segunda ponte (Difícil) ainda não foi desenhada.

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
- [ ] Fundo com estrelas (parallax)
