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
- `[ExecuteAlways]` só em componentes que precisam se atualizar no editor (ex.:
  a ponte de luz redesenhando ao arrastar waypoints), sempre separando o que roda
  só no editor com `if (!Application.isPlaying)`.

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
em ordem. Comprimento total: **59,5 unidades** (o requisito de 30 inimigos
enfileirados pede 18).

| Waypoint | Posição (x, y) | Observação |
| --- | --- | --- |
| WP_00 | (-14, 4.5) | saída do portal |
| WP_01 | (-9, 4.5) | |
| WP_02 | (-9, -5) | |
| WP_03 | (-3, -5) | |
| WP_04 | (-3, 4.5) | |
| WP_05 | (3, 4.5) | |
| WP_06 | (3, -5) | |
| WP_07 | (9, -5) | |
| WP_08 | (9, 0) | |
| WP_09 | (12, 0) | entrada da nave-mãe (x ≈ 13,5) |

Os corredores entre as colunas da ponte (x ≈ -6, 0 e 6) são onde ficarão os
pontos de ancoragem das torres. A segunda ponte (Difícil) ainda não foi desenhada.

## Estado atual

- [x] `.gitignore` e `.gitattributes` de Unity na raiz
- [x] `LightBridge.cs` — caminho por waypoints + LineRenderer
- [x] Projeto Unity criado (6000.3.27f1, template 2D com URP) na raiz do repo
- [ ] Primeiro commit do projeto no GitHub
- [ ] Cena Game com câmera, fundo, portal, ponte de luz e nave-mãe
- [ ] Pontos de ancoragem
