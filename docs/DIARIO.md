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
