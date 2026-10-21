# Arquivos do polimento

## Criados

- `Assets/Scripts/Runtime/Gameplay/Audio/PauseAwareAudio.cs`
- `Assets/Scripts/Runtime/Gameplay/Audio/PlayerFootstepAudio.cs`
- `Assets/Scripts/Runtime/Gameplay/Audio/DoorPresentation.cs`
- `Assets/Scripts/Runtime/Gameplay/Audio/CompanionChirps.cs`
- `Assets/Scripts/Runtime/Gameplay/Companion/SparrowAnimationController.cs`
- `Assets/Scripts/Runtime/Gameplay/VFX/EnergyActivationVFX.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01PolishSetup.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01PolishSetup.Audio.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01PolishSetup.VFX.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01PolishSetup.Collisions.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01PolishAudit.cs`
- `Assets/Scripts/Editor/Level01/Gameplay/Level01GameplayValidation.Polish.cs`
- `Assets/VFX/Prefabs/VFX_EnergyActivation.prefab`
- `Assets/VFX/Prefabs/VFX_DoorSteam.prefab`
- `Assets/VFX/Materials/VFX_EnergyActivation_URP.mat`
- `Assets/VFX/Materials/VFX_DoorSteam_URP.mat`
- `Assets/VFX/Particles/SoftParticle.asset`
- `Assets/Scenes/Backups/Level01_Antes_Polimento.unity`
- Pastas Audio e VFX ausentes e seus metadados, gerados pela Unity.
- `Docs/Guias/Polimento_Nivel01.md`
- Relatórios, manifesto de áudio, capturas e cópias anteriores em `Docs/Relatorios/Polimento_Nivel01`, `Docs/Previas/Polimento_Nivel01` e `Docs/Backups/Antes_Polimento`.

## Modificados

- `Assets/Scenes/Level01_Nave_Expanded.unity`: componentes/fontes de áudio, adoção do Sparrow, instâncias VFX e correções de colliders. Os 1.434 renderizadores anteriores do cenário permanecem nas mesmas posições, rotações e escalas; o pet foi levado para o local de espera.
- `Runtime/Gameplay/Companion/PetFollower.cs`: espera no local configurado, adoção por IInteractable, remoção do alvo e decolagem com folga do piso; acompanhamento anterior preservado.
- `Runtime/Gameplay/Level01/DoorController.cs`: evento OpeningStarted apenas ao iniciar abertura não imediata, sem trocar sua lógica de Translate.
- `Runtime/Gameplay/Level01/LevelCompleteTrigger.cs`: identifica somente o CharacterController real; OnTriggerStay e presença no mesmo volume recuperam entrada perdida, mantendo a condição dos três terminais no gerente.
- `Editor/Level01/Generation/Level01NaveGenerator.Expanded.cs`: chamada final da integração de polimento para futuras gerações. O gerador não foi executado nesta tarefa.
- `Editor/Level01/Gameplay/Level01GameplayValidation.cs`: novo conjunto de testes, aproximações adequadas aos móveis sólidos e relatório atual em Polimento_Nivel01.
- `Editor/Level01/Gameplay/Level01AdjustmentsAudit.cs`: aceita somente o pequeno trigger de adoção enquanto espera e considera uma aproximação acessível da mesa.
- `README.md`, `Docs/README.md`, `Docs/Guias/Scripts.md`, `Organizacao_Projeto.md` e `Ajustes_Nivel01.md`: links e explicações atualizados.

Os caminhos abreviados Runtime/ e Editor/ acima partem de `Assets/Scripts/`.

## Movidos com GUID preservado

- `Assets/kenney_sci-fi-sounds` → `Assets/Audio/Libraries/Kenney_SciFi`.
- `Assets/kenney_interface-sounds` → `Assets/Audio/Libraries/Kenney_Interface`.
- Seis clipes dessas bibliotecas foram classificados em Doors, Interactions, Alarms e Ambient, com prefixo Kenney.
- O manifesto `Audio_Caminhos_GUIDs.csv` comprova os 173 clipes originais e seus caminhos atuais. Não houve cópia de bibliotecas ou exclusão de clipes.

Os arquivos de áudio e suas licenças conservam o conteúdo original. Os assets visuais dos fornecedores, scripts de movimentação, menus, Input System, requisitos dos puzzles, Scene List e configurações globais não foram alterados.
