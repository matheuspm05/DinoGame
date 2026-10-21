# Ajustes de gameplay do Nível 1

> Atualização: o Sparrow agora espera para ser adotado com F. Áudio, VFX e revisão de colisões estão no [guia de polimento](Polimento_Nivel01.md).

Implementação sobre o projeto **C:/Users/matheus/Desktop/dino3**, que estava aberto no Unity Editor. A cena utilizada por Novo Jogo continua sendo **Assets/Scenes/Level01_Nave_Expanded.unity**.

O cenário não foi regenerado. Não foram movidos nem reorganizados os modelos decorativos existentes. O estado não salvo que estava aberto foi preservado em **Assets/Scenes/Backups/Level01_Antes_Ajustes.unity**; arquivos anteriores também estão em **Docs/Backups/Antes_Ajustes/**.

## Jogar

Abra MainMenu, entre em Play e clique em Novo Jogo.

1. Na contenção, olhe para a pequena caixa no chão e pressione **F**.
2. A caixa aparece no HoldPoint, abaixo e à direita da mira. Aponte para o alvo azul acima da porta e use **mouse esquerdo**.
3. Aproxime-se a alguns metros do alvo e mire um pouco acima se necessário: o lançamento tem gravidade real.
4. Cada lançamento cria uma reposição na mão. Um acerto válido torna o alvo verde, abre a porta e encerra o spawn.
5. Procure a bateria na posição atual organizada pela equipe, em Máquinas/Carga à esquerda do depósito.
6. Instale-a no console da sala de energia; o laboratório abre.
7. Procure o cartão em uma das três mesas verificadas do laboratório.
8. Use-o no leitor já posicionado próximo ao corredor de acesso ao hangar.
9. Ative os três terminais, em qualquer ordem.
10. Atravesse a luz branca atrás do portão externo. Só a passagem do jogador conclui a fase.

WASD, mouse, Shift, Espaço, Ctrl e ESC permanecem como antes. O painel dos dois menus inclui **F — Interagir / Pegar caixa** e **Mouse esquerdo — Arremessar caixa**.

## Hierarchy

Sob Level01_Nave_Expanded/Gameplay_Level01:

- **InitialRoomMechanics**: porta inicial, alvo e caixa inicial. Contém **ThrowingSystem** e o grupo de caixas arremessadas.
- **CardSpawnSystem**: três pontos de cartão, referências às mesas de apoio e trigger de entrada no laboratório.
- **CompanionPet**: prefab Sparrow original com PetFollower.
- **ExitLightEffect**: superfície branca e luz local sem sombras, inicialmente desativadas.

**HoldPoint** é filho da câmera existente do jogador. Não há outra câmera, jogador, EventSystem ou HUD. A página de conclusão e o Canvas existentes foram reutilizados.

## Hitboxes e preservação dos modelos

O console de energia e o leitor tinham seus modelos deslocados manualmente, enquanto os colliders continuavam nas coordenadas antigas. Os BoxColliders agora usam os limites locais reais dos renderizadores de cada console.

Bateria e cartão mantêm triggers pequenos próximos da malha; os consoles e as portas mantêm colisores sólidos. O raycast considera o primeiro sólido no caminho e ignora a camada nativa Ignore Raycast. O pet e a caixa segurada usam essa camada; seus colisores são desativados.

A bateria já instalada tem um BatterySocket referenciado pelo console real. Sua representação é encaixada somente ao instalar em runtime; o modelo oculto não foi movido no Editor.

A nova porta inicial utiliza uma folha própria porque a folha decorativa já presente é estreita e está recolhida lateralmente. A decoração foi preservada. A folha nova abre para a esquerda pelo lado interno da contenção, sem ocupar o vão depois de aberta.

O integrador compara posições, rotações e escalas de todos os renderizadores existentes antes/depois da operação. O relatório de integração registra essa preservação. Pontos de spawn, colliders e triggers são editáveis; não precisam deslocar modelos para corrigir interações.

## Scripts de execução novos

| Arquivo em Assets/Scripts/Runtime/Gameplay | Responsabilidade |
|---|---|
| Throwing/IntroThrowingSystem.cs | Estado do desafio inicial, coleta, array de prefabs, spawn no HoldPoint, intervalo, lançamento e encerramento após acerto. |
| Throwing/ThrownObject.cs | Rigidbody/collider, estado segurado ou lançado, impulso, torque, detecção contínua e remoção por duração. |
| Throwing/ThrowablePickup.cs | Implementa IInteractable para coletar somente a primeira caixa usando F. |
| Throwing/ImpactButton.cs | Recebe a colisão no mesmo GameObject do collider do alvo. Aceita somente uma caixa efetivamente lançada pelo sistema correto. |
| Level01/CardSpawnSystem.cs | Valida apoios/limites/espaço livre e sorteia a posição do único cartão existente. |
| Level01/LaboratoryEntryTrigger.cs | Atualiza o objetivo ao jogador entrar no laboratório energizado. |
| Companion/PetFollower.cs | Voo suave, animação Fly original, checagens de obstáculos, trilha curta de acompanhamento e recuperação fora da vista. |

## Scripts existentes modificados

- **Level01GameManager.cs**: referências aos novos sistemas; objetivos iniciais/visita; encaixe da bateria; ativação da luz após 3/3. Continua sendo a fonte de verdade dos puzzles de bateria/cartão/terminais.
- **PlayerInteraction.cs**: exclui Ignore Raycast e mostra a indicação de arremesso enquanto há uma caixa segurada.
- **DoorController.cs**: usa Translate para aplicar o deslocamento limitado até o destino no espaço do suporte. Mantém folha e collider juntos.
- **Level01GameplaySetup.cs**: reutilização interna de helpers de geometria; oito linhas de controles.
- **MenuSceneSetup.cs**: futuras criações dos menus também incluem arremesso.
- **Level01NaveGenerator.Expanded.cs**: chama a nova integração depois da instalação do gameplay, sem alterar layout ou decoração.
- **Level01GameplayValidation.cs**: roteiro completo atualizado para o novo desafio, posições atuais e cartão aleatório.

O controlador de movimento e os controladores dos menus não foram substituídos. Não foram alterados o Input Actions, configurações globais de Layers/colisões, pacotes originais ou Scene List.

## Ferramenta de integração

**Assets/Scripts/Editor/Level01/Gameplay/Level01AdjustmentsSetup.cs**

Comando: **Tools > Gameplay Level 01 > Integrar arremesso pet e ajustes**.

Executa sobre a cena existente, corrige hitboxes e só cria sistemas ausentes. Reexecutar não acrescenta outro pet, cartão, porta inicial, alvo ou HUD. Referências incompletas são denunciadas em vez de apagar e reconstruir objetos.

Não use o comando de geração completa da nave para aplicar os ajustes. A geração completa continua substituindo o cenário gerado e pode perder edições manuais, como já acontecia. A chamada de integração garante que uma geração futura também receba os novos sistemas.

## Prefabs e materiais

- **Assets/Prefabs/Gameplay/Level01/Throwing/Caixa_Crate3.prefab**, baseado no Prop_Crate3.fbx real do Modular SciFi MegaKit.
- **Assets/Prefabs/Gameplay/Level01/Throwing/Caixa_Storage.prefab**, baseado no Crate_01.prefab real do Low Poly Storage Pack.
- Porta inicial: Door_Simple.fbx do MegaKit.
- Alvo: Prop_Light_Small.fbx do MegaKit.
- Pet: **Assets/Quirky Series Ultimate/FREE/Prefabs/Sparrow.prefab**, Sparrow – Quirky Series, Omabuarts Studio.
- Cartão e bateria: os objetos/prefabs existentes foram preservados, incluindo Arcadia_Verso.

Cópias materiais próprias ficam em Assets/Materials/Gameplay/Level01:
Alvo_Impacto_URP, Sparrow_URP e Saida_Luz_Branca. A cópia do Sparrow conserva o atlas, a cor e a emissão do shader SoftSurface do fornecedor; o material original não é modificado.

## Sparrow: animações reais e movimento

O prefab possui Animator e **AC_Sparrow.controller**. O FBX Sparrow_Animations contém **Fly, Walk, Run, Idle_A, Idle_B, Idle_C**, além de outros clipes do pacote.

O pet usa **Base Layer.Fly**, incluindo quando paira. Oscilações pequenas de posição produzem o idle de voo; não foi criada uma animação de asas, caminhada ou personagem humano. Os clipes terrestres foram identificados, mas não são executados no ar. Essas animações do pet não comprovam automaticamente os requisitos de animação do protagonista.

O tamanho é reduzido para um pássaro pequeno (escala 0,28 no modelo). Ele mantém um alvo lateral à câmera, sem ocupar a mira. SphereCasts impedem avanço através de sólidos; uma trilha limitada de posições do jogador ajuda em cantos. Se ficar longe/preso, é ocultado brevemente e recuperado em um ponto livre atrás do jogador, fora do campo frontal. Não há NavMesh, combate ou interação com puzzles.

## Onde demonstrar os requisitos técnicos

| Recurso | Uso real |
|---|---|
| Rigidbody | ThrownObject.body, prefab de caixa com massa 0,65 kg e gravidade. |
| AddForce / ForceMode.Impulse | ThrownObject.Launch: impulso na direção da câmera. |
| AddTorque | ThrownObject.Launch: rotação aleatória pequena na caixa física. |
| Translate | DoorController.Update: movimento real da folha até a posição final. |
| Rotação | PetFollower.LateUpdate e torque dos projéteis. |
| Array de prefabs | IntroThrowingSystem.throwablePrefabs: dois modelos compatíveis. |
| Random.Range | SpawnHeldObject escolhe o prefab; CardSpawnSystem.TrySpawn escolhe um ponto válido. |
| Instantiate / função de spawn | IntroThrowingSystem.SpawnHeldObject cria uma instância no HoldPoint. |
| Layers | Camada nativa 2, Ignore Raycast, usada no pet e na caixa segurada; Default nos projéteis lançados. |
| Colliders / colisão | OnCollisionEnter no alvo; BoxColliders sólidos em portas, caixas e consoles. |
| Triggers | Cartão/bateria para raycast; entrada do laboratório e região de conclusão identificam somente o jogador. |
| Animator / AnimationClip | Sparrow com controller original e clipe Fly comprovado. |

Não há chamadas artificiais apenas para preencher a lista. As funções novas têm comentários XML sobre finalidade, argumentos, retorno e processamento.

## Parâmetros úteis no Inspector

Em IntroThrowingSystem:
- Throw Impulse: 11.
- Throw Torque: 0,08.
- Throw Interval: 0,45 segundo.
- Projectile Lifetime: 12 segundos.
- Maximum Projectiles: 8.
- Throwable Prefabs: array com os dois prefabs locais.

Em CardSpawnSystem, os pontos e seus colliders de apoio podem ser substituídos por outros locais previamente verificados. O sorteio ocorre em Start a cada carregamento/reinício. Não há promessa de posição diferente em todas as partidas.

Em PetFollower, ajuste Follow Offset, Smooth Time, Maximum Speed e Obstacle Radius conforme a apresentação. Conserve Flight State como Base Layer.Fly enquanto usar o controller original.

## Testes e limitações

Use **Tools > Gameplay Level 01 > Validar progressao completa em Play**.
O resultado atual fica em Docs/Relatorios/Ajustes_Nivel01/Testes_Play.txt. As capturas ficam em Docs/Previas/Ajustes_Nivel01.

O validador usa F/mouse/ESC pelo Input System, colisão física de um lançamento real, percurso do CharacterController, negativos dos puzzles, sorteios repetidos do mesmo cartão, animação Fly, pausa e reinício. Reposicionamentos de testes negativos ocorrem somente em Play, sem salvar estado.

Também execute **Tools > Validar Menus em Play** para os cliques, corrida, salto e agachamento existentes.

Faça uma partida humana para ajustar a dificuldade do arremesso, descobrir os três locais do cartão e observar o pet em cantos/móveis. Os testes de esfera não garantem que cada ponta de asa nunca encoste visualmente em um recorte do cenário. A recuperação é deliberadamente discreta, sem simular navegação aérea complexa.

A saída usa uma superfície URP Unlit branca de alta intensidade, independente da luz ambiente, e uma luz local sem sombras. Bloom não foi instalado nem habilitado globalmente. O visual depende da exposição/resolução da máquina da equipe. O executável final precisa de smoke test; não há nova configuração manual de referências se a cena integrada for utilizada.


## Auditoria e evidências finais

**Tools > Gameplay Level 01 > Auditar ajustes e Sparrow** executa Level01AdjustmentsAudit.cs.

A auditoria confere referências, componentes únicos, prefabs físicos, controller/atlas originais, apoios do cartão e o trigger final. Uma grade de cápsulas no laboratório verifica os três locais de aproximação e suas linhas de visão. A cena anterior é aberta somente para comparação e fechada sem salvar.

O relatório em Docs/Relatorios/Ajustes_Nivel01/Auditoria.txt confirmou **1.430 renderizadores anteriores** com posição, rotação e escala iguais ao backup. O relatório em Testes_Play.txt confirmou a execução de Fly e a mudança de rotação da asa entre frames reais, além do percurso completo dos puzzles. A imagem Sparrow_Preview.png é uma prévia estática do modelo; não representa uma gravação animada.

O teste de menus também passou após incluir o arremesso. A tela final usa fundo opaco e mensagens temporárias receberam contorno, mantendo legibilidade sobre a luz branca.