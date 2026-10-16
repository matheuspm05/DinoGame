# Para que serve cada script

O projeto tem dois grupos: **Runtime**, que funciona durante o jogo, e **Editor**, que contém ferramentas utilizadas dentro da Unity para montar e testar o projeto. Os scripts abaixo são os nossos; códigos dos pacotes ficam em `Assets/ThirdParty`.

## Scripts que funcionam no jogo — Runtime

| Pasta | Script | Para que serve |
|---|---|---|
| `Runtime/Player` | [NaveWalkthroughController.cs](../../Assets/Scripts/Runtime/Player/NaveWalkthroughController.cs) | Controla o personagem: WASD, mouse, corrida, salto, gravidade, colisão pelo CharacterController e agachamento com Ctrl. Abaixa a câmera e verifica o teto antes de levantar. É o controlador que já estava funcionando. |
| `Runtime/Environment` | [NaveWorldLabel.cs](../../Assets/Scripts/Runtime/Environment/NaveWorldLabel.cs) | Mantém a textura da fonte atualizada nas placas 3D da nave. Evita que os textos percam os caracteres quando a Unity reconstrói o atlas da fonte. Funciona também no Editor. |
| `Runtime/UI` | [MainMenuController.cs](../../Assets/Scripts/Runtime/UI/MainMenuController.cs) | Recebe os cliques do Menu Principal: Novo Jogo, Controles e Sair. Prepara o cursor e fecha o painel de controles com ESC. |
| `Runtime/UI` | [PauseMenuController.cs](../../Assets/Scripts/Runtime/UI/PauseMenuController.cs) | Abre/fecha a pausa com ESC, congela o tempo, desativa temporariamente o controlador existente e libera o cursor. Executa Continuar, Controles, Reiniciar Nível, Menu Principal e Sair. Reinicia a cena atual. |
| `Runtime/UI` | [ControlsPanelController.cs](../../Assets/Scripts/Runtime/UI/ControlsPanelController.cs) | Alterna entre o menu e o painel dos comandos. Controla o botão Voltar e a seleção dos botões. Serve tanto ao Menu Principal quanto à pausa. |
| `Runtime/UI` | [MenuSceneActions.cs](../../Assets/Scripts/Runtime/UI/MenuSceneActions.cs) | Reúne ações compartilhadas: carregar cenas com o tempo normal e encerrar o jogo. Guarda o nome da cena usada por Novo Jogo. É uma classe estática: não é adicionada a GameObjects. |

Os controladores de UI recebem os eventos **OnClick** dos botões do Canvas. O jogador usa o **novo Input System**. A pausa reutiliza o controlador existente, sem criar outro personagem.

## Ferramentas para construir o nível — Editor/Level01/Generation

| Script | Para que serve |
|---|---|
| [Level01NaveGenerator.cs](../../Assets/Scripts/Editor/Level01/Generation/Level01NaveGenerator.cs) | Contém o gerador da nave original e funções compartilhadas: localizar modelos, instanciar prefabs, ajustar tamanhos, converter materiais locais para URP, criar colisores, placas e prévias. O comando **Tools → Gerar Level 01** recria a referência original. |
| [Level01NaveGenerator.Expanded.cs](../../Assets/Scripts/Editor/Level01/Generation/Level01NaveGenerator.Expanded.cs) | Coordena a geração da versão expandida: confirma os caminhos reais, seleciona os modelos, cria a cena separada, copia o jogador existente e conecta a pausa. **Tools → Gerar Level 01 Expandido** executa essa montagem. |
| [Level01NaveGenerator.ExpandedLayout.cs](../../Assets/Scripts/Editor/Level01/Generation/Level01NaveGenerator.ExpandedLayout.cs) | Define o mapa: posições e dimensões das salas, corredores, pisos, tetos, paredes, passagens, luzes e rota de circulação. É o arquivo principal para mudar o formato e o tamanho da nave. |
| [Level01NaveGenerator.ExpandedDecoration.cs](../../Assets/Scripts/Editor/Level01/Generation/Level01NaveGenerator.ExpandedDecoration.cs) | Distribui mesas, computadores, caixas, tubulações, equipamentos e decoração por setor. Mantém as proporções dos props e apoia objetos sobre mesas/paletes. É o arquivo principal para alterar a composição dos ambientes. |
| [Level01NaveGenerator.ExpandedValidation.cs](../../Assets/Scripts/Editor/Level01/Generation/Level01NaveGenerator.ExpandedValidation.cs) | Faz a conferência durante a geração: piso contínuo, espaço para a cápsula humana, conectividade dos setores, componentes únicos, materiais e iluminação. Também captura as prévias renderizadas. |

Esses cinco arquivos usam `partial class Level01NaveGenerator`: são partes da **mesma classe**, separadas para facilitar a leitura. Não são cinco geradores concorrentes, nem precisam ser adicionados a GameObjects. A geração substitui a cena correspondente depois da confirmação; edições manuais nessa versão podem ser substituídas ao gerar novamente.

## Auditoria e testes do nível — Editor/Level01/Validation

| Script | Para que serve |
|---|---|
| [Level01ExpandedAudit.cs](../../Assets/Scripts/Editor/Level01/Validation/Level01ExpandedAudit.cs) | Conta os modelos realmente presentes na cena e agrupa geometrias equivalentes. Produz os relatórios Markdown, CSV e JSON com nomes, caminhos, categorias, setores e quantidades. UI, materiais, objetos vazios e repetições não aumentam o total. É chamado pelo gerador. |
| [Level01ExpandedPlayValidation.cs](../../Assets/Scripts/Editor/Level01/Validation/Level01ExpandedPlayValidation.cs) | Percorre a rota usando o CharacterController real em Play para detectar bloqueios e quedas. Confere pausa/retomada e descarta o estado temporário ao terminar. Comando: **Tools → Validar Nave Expandida em Play**. |

## Configuração e teste dos menus — Editor

| Pasta | Script | Para que serve |
|---|---|---|
| `Editor/UI` | [MenuSceneSetup.cs](../../Assets/Scripts/Editor/UI/MenuSceneSetup.cs) | Cria interfaces faltantes usando Canvas, Panel, Button, Text e eventos OnClick. Conecta o menu de pausa ao jogador, garante um EventSystem por cena e configura a Scene List. Comando: **Tools → Configurar Menus**. |
| `Editor/Validation` | [MenuPlayModeValidation.cs](../../Assets/Scripts/Editor/Validation/MenuPlayModeValidation.cs) | Testa cliques, controles, corrida, salto, agachamento, teto baixo, ESC, reinício, retorno ao menu e saída do Play. Usa entradas virtuais durante o teste e restaura os dispositivos ao terminar. Comando: **Tools → Validar Menus em Play**. |

## Qual arquivo editar para cada tarefa

- **Velocidade, salto, câmera ou agachamento:** `NaveWalkthroughController.cs`; vários valores também ficam no Inspector do jogador.
- **Tamanho das salas e posição das conexões:** `Level01NaveGenerator.ExpandedLayout.cs`.
- **Posição de móveis e equipamentos:** `Level01NaveGenerator.ExpandedDecoration.cs`, ou os objetos da cena para ajustes manuais.
- **Modelos disponíveis no gerador:** `Level01NaveGenerator.Expanded.cs`.
- **Botões e ações dos menus:** os scripts de `Runtime/UI`.
- **Aparência dos menus:** os elementos do Canvas em `MainMenu.unity` e no prefab `Assets/Prefabs/UI/PauseMenu.prefab`.
- **Contagem dos assets:** `Level01ExpandedAudit.cs` e os relatórios em `Docs/Relatorios/Nivel01_Expandido`.

O ponto de entrada para jogar continua sendo `Assets/Scenes/MainMenu.unity`. As ferramentas de Editor não entram como componentes de gameplay na build.
