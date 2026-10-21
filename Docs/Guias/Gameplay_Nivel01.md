# Gameplay do Nível 1

Atualização do fluxo: consulte [Ajustes do Nível 1](Ajustes_Nivel01.md) para o desafio inicial de arremesso, cartão aleatório, Sparrow e saída iluminada. Este guia descreve a integração base dos puzzles.

A cena de jogo continua sendo `Assets/Scenes/Level01_Nave_Expanded.unity`. O cenário foi integrado no lugar, sem executar a geração completa. O jogador, seu controlador, câmera, MainMenu, PauseMenu e Input System foram reutilizados.

## Executar e apresentar

1. Abra `Assets/Scenes/MainMenu.unity` e pressione Play.
2. Clique em **Novo Jogo**. WASD move; mouse olha; Shift corre; Espaço pula; Ctrl agacha; **F interage**; ESC pausa/continua.
3. Olhe para o objeto a até 3 m. A mensagem inferior informa a ação disponível. Paredes e portas sólidas interrompem o raycast.
4. Siga o objetivo no canto superior esquerdo. Os três terminais podem ser ativados em qualquer ordem.

| Etapa | Localização / referência | Posição mundial |
|---|---|---|
| Bateria | Atravesse a contenção e entre no depósito. Vire à esquerda para Máquinas/Carga. A célula cilíndrica fica sobre um suporte na metade norte da sala. | (-14, 0.81, 28) |
| Instalação | Volte ao corredor inicial e entre na sala de Energia. O painel fica na metade norte. A célula instalada aparece ao lado dele. | (-12, 0, 12) |
| Laboratório | Porta no lado direito do depósito. A instalação da bateria abre essa porta automaticamente. | (10, 0, 21) |
| Cartão | Mesa oeste do laboratório, próxima à entrada, na borda dianteira entre o computador e a bandeja. | (14, 0.914, 16) |
| Leitor | Segurança, perto da parede norte, à direita do caminho para o hangar e atrás da bancada. | (22.5, 0, 44.1) |
| Entrada do hangar | Depois de Segurança. O cartão no leitor abre essa porta automaticamente. | (20, 0, 49) |
| Terminal 1 | Hangar: lado esquerdo, primeira metade — Energia auxiliar. | (16, 0, 54) |
| Terminal 2 | Hangar: lado direito, região central — Segurança. | (24, 0, 60) |
| Terminal 3 | Hangar: lado esquerdo, região posterior — Saída externa. | (16, 0, 66) |
| Saída / conclusão | Portão ao fundo do hangar. Abre com 3/3; caminhe até o pequeno deck depois do portão. | Portão (20, 0, 70.7); trigger (20, 1.3, 73) |

A sala de Máquinas existente funciona como o desvio de carga à esquerda do depósito; não foi criada uma sala de puzzles nova. A bateria não está atrás de uma porta que dependa dela. O cartão fica atrás da porta que depende somente da bateria.

## Scripts e responsabilidades

Os sete scripts de execução ficam em `Assets/Scripts/Runtime/Gameplay/`:

| Arquivo | Responsabilidade |
|---|---|
| `Interaction/IInteractable.cs` | Contrato comum: obter a mensagem e executar uma interação. Não é componente de GameObject. |
| `Interaction/PlayerInteraction.cs` | Componente adicional no jogador existente. Lê F pelo novo Input System, lança um raio da câmera até 3 m, respeita o primeiro obstáculo e bloqueia ações na pausa/conclusão. |
| `Level01/Level01GameManager.cs` | Única fonte de verdade do progresso: bateria, energia, cartão, hangar, três terminais e conclusão. Atualiza os objetos/objetivos e solicita a abertura das portas. Não persiste entre cenas. |
| `Level01/Level01Interactable.cs` | Componente reutilizado nos cinco tipos de objeto: bateria, instalação, cartão, leitor e terminal. O campo Kind escolhe a ação; os terminais usam índices 0, 1 e 2. Não mantém progresso paralelo. |
| `Level01/DoorController.cs` | Desloca a folha móvel junto com seu BoxCollider sólido. Guarda posição fechada e deslocamento de abertura, editáveis no Inspector. |
| `UI/ObjectiveHUD.cs` | Objetivo, mira, indicação F, mensagens temporárias e contador 0/3 a 3/3. Oculta gameplay na pausa e mostra a página final. |
| `Level01/LevelCompleteTrigger.cs` | Detecta o CharacterController depois da saída e solicita conclusão. O gerente só aceita após três terminais e saída liberada. |

As três ferramentas permanentes ficam em `Assets/Scripts/Editor/Level01/Gameplay/`:

| Arquivo | Responsabilidade |
|---|---|
| `Level01GameplaySetup.cs` | Integra puzzles/HUD na nave já criada, preserva prefabs, corrige tetos/folhas e atualiza os controles dos dois menus. A instalação existente é reutilizada. |
| `Level01GameplayValidation.cs` | Teste completo em Play com F/ESC via dispositivos virtuais, CharacterController real, estados negativos, percurso físico, conclusão, Novo Jogo e reinício. Restaura os dispositivos/configurações temporárias ao sair. |
| `Level01GameplayAudit.cs` | Audita referências, scripts ausentes, modelos, materiais, colisores e OnClick. Renderiza as duas faces do cartão em uma PreviewScene temporária. |

Todas as funções novas têm comentários XML sobre finalidade, argumentos, retorno e processamento.

## Organização e edição manual

```text
Assets/Scripts/Runtime/Gameplay/
  Interaction/
  Level01/
  UI/
Assets/Scripts/Editor/Level01/Gameplay/
Assets/Prefabs/Gameplay/Level01/
  Bateria_Energia.prefab
  Cartao_ARCADIA.prefab
Assets/Materials/Gameplay/Level01/
Assets/Scenes/Backups/Level01_Antes_Gameplay.unity
Docs/Guias/Gameplay_Nivel01.md
Docs/Relatorios/Gameplay/
Docs/Previas/Gameplay/
Docs/Backups/Antes_Gameplay/
```

Na Hierarchy, os novos sistemas estão em **Level01_Nave_Expanded > Gameplay_Level01**:

- **Itens_e_Paineis**: bateria, suporte, painel, cartão, leitor e terminais.
- **Portas_de_Progressao**: portas controladas, fechamentos e deck de saída.
- **Canvas_Gameplay**: HUD e Conclusao, com Text, Image e Button editáveis.
- **Conclusao_Apos_Porta_Externa**: trigger de término.

O script PlayerInteraction foi acrescentado ao jogador existente; seu controlador não foi reescrito. Para mover um terminal, selecione o seu objeto pai, incluindo console, indicador e collider. Os terminais voltados ao eixo central facilitam a leitura.

O Canvas usa **Screen Space - Overlay**, **Scale With Screen Size**, referência **1920 × 1080**, textos nativos e botão com **OnClick persistente**. Reutiliza o EventSystem/InputSystemUIInputModule existente. Não utiliza UI Toolkit. A pausa não é recriada.

## Prefabs e cartão

- Bateria: `Assets/Energy/Type 1/EnergyCell_1/Prefabs/EnergyCell_1_1.prefab`, pacote Energy Cells Free realmente importado.
- Consoles: `.../FBX (Unity)/Props/Prop_AccessPoint.fbx`, Modular SciFi MegaKit.
- Indicadores: `.../Props/Prop_Light_Small.fbx`.
- Suporte: `.../Props/Prop_ItemHolder.fbx`.
- Folhas novas: `.../Platforms/Door_Simple.fbx`.
- Saída: a folha Door_DarkMetal já presente na nave foi reaproveitada.
- Deck: Floor_01 e Roof_01 de Barking Dog; fechamentos com WallAstra_Straight.
- Cartão: `Assets/Cartao_ARCADIA_Unity/Cartao_Arcadia.obj`, com três submalhas reais.

Materiais próprios de frente, verso e borda ficam em `Assets/Materials/Gameplay/Level01/`. O verso usa **Arcadia_Verso.png**; a frente usa **Arcadia_Frente.png**. As UVs existentes já mapeiam corretamente as duas faces; não foi necessário modificar o OBJ ou as imagens. O prefab mantém o modelo original como referência e usa escala 3 para facilitar a identificação sobre a mesa.

Prévia comprovada: `Docs/Previas/Gameplay/Cartao_Lado_A.png` e `Cartao_Lado_B.png`. Materiais originais dos fornecedores não foram editados.

## Colisões e alterações pontuais da arquitetura

- **141 módulos de teto** receberam BoxCollider sólido de **0.24 m** na espessura acima do limite interno. X/Z seguem o piso modular correspondente. Colisores antigos de filhos permanecem desabilitados; não foram sobrepostos colisores ativos.
- As folhas decorativas recolhidas e portas visuais fechadas receberam BoxCollider ajustado ao modelo. Portais/arquitraves abertos não receberam uma caixa bloqueando seu vão.
- As portas de laboratório, hangar e saída têm collider sólido na própria folha móvel. O collider acompanha a translação para fora do vão. As folhas de entrada ficam 25 cm à frente da parede para deslizar ao lado dela; as partes móveis não usam flags estáticas. A conclusão também aceita a passagem quando o portão liberado ainda está terminando de abrir.
- Atrás da saída existia parede contínua. Somente os três módulos centrais e seus acabamentos foram preservados **inativos**, criando um vão de 7 m com pequenos fechamentos laterais. Um deck de 4 m permite atravessar o trigger com piso e teto contínuos.
- A saída é encerramento da fase; não há planeta externo ou próximo nível.

O estado não salvo que estava aberto no Editor foi preservado em `Assets/Scenes/Backups/Level01_Antes_Gameplay.unity`. Há também cópias dos arquivos anteriores em `Docs/Backups/Antes_Gameplay/`. A cena original Level01_Nave e a SampleScene não foram substituídas.

## Integração futura sem perder os puzzles

**Não é preciso gerar a nave para jogar esta versão. A cena expandida já está salva.**

- **Tools > Gameplay Level 01 > Integrar puzzles e corrigir colisoes**: aplica a integração à cena existente. Ao encontrar a instalação pronta, preserva os itens e não duplica jogador, HUD, portas, EventSystem ou gerente. Corrige os colisores e atualiza os controles.
- **Tools > Gameplay Level 01 > Validar progressao completa em Play**: roda o teste completo e escreve `Docs/Relatorios/Gameplay/Testes_Play.txt`.
- **Tools > Gameplay Level 01 > Auditar cena e visualizar cartao**: gera `Auditoria_Cena.txt` e as prévias do cartão.

O gerador expandido foi alterado pontualmente: chama `InstallIntoScene` depois da validação da arquitetura e antes de salvar. Uma reconstrução futura recria os puzzles. **O comando de geração continua substituindo a geometria gerada e suas edições manuais, como antes; não o use somente para atualizar os puzzles.** O passeio de geometria abre as portas apenas no estado temporário de Play para conferir os setores; o novo validador de gameplay testa as portas inicialmente fechadas.

Os scripts MenuSceneSetup e MenuPlayModeValidation foram atualizados para a interação F. MainMenuController, PauseMenuController, MenuSceneActions, ControlsPanelController e NaveWalkthroughController continuam sendo os sistemas existentes.

## Verificação manual recomendada

O relatório automatizado descreve o que foi efetivamente testado no Unity. Os testes de física percorrem a rota com a cápsula real e acionam F/ESC pelo Input System; não simulam um jogador humano decidindo os rumos.

Antes da apresentação, faça uma partida sem reposicionamentos: confirme a facilidade de encontrar a bateria/cartão, a orientação dos terminais e o conforto da iluminação nas resoluções do computador da equipe. Confira também uma build: os testes de Play exercitam a navegação das cenas e o reset, mas não substituem um teste do executável final. Não há configuração manual de referências obrigatória após abrir o projeto e aguardar a importação.
