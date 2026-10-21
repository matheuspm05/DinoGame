# Arquivos do gameplay — Nível 1

## Criados

- `Assets/Scripts/Runtime/Gameplay/Interaction/IInteractable.cs`
- `Assets/Scripts/Runtime/Gameplay/Interaction/PlayerInteraction.cs`
- `Assets/Scripts/Runtime/Gameplay/Level01/Level01GameManager.cs`
- `Assets/Scripts/Runtime/Gameplay/Level01/Level01Interactable.cs`
- `Assets/Scripts/Runtime/Gameplay/Level01/DoorController.cs`
- `Assets/Scripts/Runtime/Gameplay/Level01/LevelCompleteTrigger.cs`
- `Assets/Scripts/Runtime/Gameplay/UI/ObjectiveHUD.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01GameplaySetup.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01GameplayValidation.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01GameplayAudit.cs`
- `Assets/Prefabs/Gameplay/Level01/Bateria_Energia.prefab`
- `Assets/Prefabs/Gameplay/Level01/Cartao_ARCADIA.prefab`
- Materiais URP em `Assets/Materials/Gameplay/Level01/`: bateria, frente/verso/borda do cartão, indicador e adaptação local da célula.
- Backup do estado aberto do Editor: `Assets/Scenes/Backups/Level01_Antes_Gameplay.unity`.
- `Docs/Guias/Gameplay_Nivel01.md`, relatórios em `Docs/Relatorios/Gameplay/` e capturas em `Docs/Previas/Gameplay/`.
- Backups dos três arquivos de cena/prefab anteriores em `Docs/Backups/Antes_Gameplay/`.
- Metadados `.meta` gerados pela própria Unity.

## Modificados

| Arquivo | Alteração |
|---|---|
| `Assets/Scenes/Level01_Nave_Expanded.unity` | Integração no lugar: puzzles, HUD, folhas móveis, colisôes de tetos/portas e deck de conclusão. |
| `Assets/Scenes/MainMenu.unity` | Linha F — Interagir no painel de controles. |
| `Assets/Prefabs/UI/PauseMenu.prefab` | Mesma atualização de controles, preservando botões e callbacks. |
| `Assets/Scripts/Editor/Level01/Generation/Level01NaveGenerator.Expanded.cs` | Uma chamada de integração depois da validação da arquitetura e antes de salvar futuras gerações. |
| `Assets/Scripts/Editor/Level01/Validation/Level01ExpandedPlayValidation.cs` | Abre as portas apenas durante o passeio de geometria; a progressão inicialmente fechada tem validador próprio. |
| `Assets/Scripts/Editor/UI/MenuSceneSetup.cs` | Futuras criações de menus incluem F e sete linhas com espaçamento adequado. |
| `Assets/Scripts/Editor/Validation/MenuPlayModeValidation.cs` | Confere a interação anunciada no painel de controles. |
| `README.md`, `Docs/README.md`, guias de scripts/menus/expansão | Links e orientações atualizados para o gameplay atual. |

O controlador de movimento, os controladores dos menus, o asset de entrada, os pacotes de terceiros, Level01_Nave e SampleScene foram preservados. Não houve geração completa do cenário nem instalação de pacotes.

## Evidências

- `Testes_Play.txt`: testes completos de progressão executados na Unity.
- `Auditoria_Cena.txt`: referências, componentes únicos, fontes, URP e folhas sem sobreposição nas paredes, nas poses fechada/aberta.
- `../Menus/Menus_Validacao.txt`: regressão dos botões e dos controles existentes.
- `../../Previas/Gameplay/Cartao_Lado_A.png` e `Cartao_Lado_B.png`: frente e verso realmente renderizados do prefab.
- `../../Previas/Gameplay/02_Conclusao.png`: tela final.

O teste do executável final e uma partida humana para avaliar descoberta dos itens/iluminação permanecem recomendados. As validações executadas usam o Editor; não foi produzida uma build nesta tarefa.
