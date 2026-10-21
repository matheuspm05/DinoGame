# Arquivos dos ajustes do Nível 1

Projeto implementado: C:/Users/matheus/Desktop/dino3 (projeto que estava aberto no Editor).
Cena principal preservada: Assets/Scenes/Level01_Nave_Expanded.unity.

## Novos scripts

- Assets/Scripts/Runtime/Gameplay/Throwing/IntroThrowingSystem.cs
- Assets/Scripts/Runtime/Gameplay/Throwing/ThrownObject.cs
- Assets/Scripts/Runtime/Gameplay/Throwing/ThrowablePickup.cs
- Assets/Scripts/Runtime/Gameplay/Throwing/ImpactButton.cs
- Assets/Scripts/Runtime/Gameplay/Level01/CardSpawnSystem.cs
- Assets/Scripts/Runtime/Gameplay/Level01/LaboratoryEntryTrigger.cs
- Assets/Scripts/Runtime/Gameplay/Companion/PetFollower.cs
- Assets/Scripts/Editor/Level01/Gameplay/Level01AdjustmentsSetup.cs
- Assets/Scripts/Editor/Level01/Gameplay/Level01AdjustmentsAudit.cs

Funções e responsabilidades: [guia completo](../../Guias/Ajustes_Nivel01.md).

## Scripts modificados

| Arquivo | Alteração |
|---|---|
| Runtime/Gameplay/Level01/Level01GameManager.cs | Objetivos novos, visita ao laboratório, encaixe da bateria e luz final. |
| Runtime/Gameplay/Interaction/PlayerInteraction.cs | Camada Ignore Raycast e mensagem de arremesso. |
| Runtime/Gameplay/Level01/DoorController.cs | Translate real para mover a folha junto do collider. |
| Editor/Level01/Gameplay/Level01GameplaySetup.cs | Helpers reutilizados internamente e painel com oito comandos. |
| Editor/UI/MenuSceneSetup.cs | Controles novos nas futuras criações. |
| Editor/Level01/Generation/Level01NaveGenerator.Expanded.cs | Uma chamada à integração nova após o gameplay base. O gerador não foi executado. |
| Editor/Level01/Gameplay/Level01GameplayValidation.cs | Teste completo com física, spawn, voo, pausa e progressão atualizados. |

Todos os caminhos acima ficam sob Assets/Scripts.

## Outros arquivos

- Modificados: Assets/Scenes/Level01_Nave_Expanded.unity, Assets/Scenes/MainMenu.unity e Assets/Prefabs/UI/PauseMenu.prefab.
- Criados: Assets/Prefabs/Gameplay/Level01/Throwing/Caixa_Crate3.prefab e Caixa_Storage.prefab.
- Criados: materiais próprios Alvo_Impacto_URP, Sparrow_URP e Saida_Luz_Branca em Assets/Materials/Gameplay/Level01. O adaptador também cria uma cópia URP do material legado da caixa do Storage Pack.
- Backup do estado aberto: Assets/Scenes/Backups/Level01_Antes_Ajustes.unity.
- Backups de arquivos e do validador anterior: Docs/Backups/Antes_Ajustes.
- Guia: Docs/Guias/Ajustes_Nivel01.md.
- Relatórios: Docs/Relatorios/Ajustes_Nivel01.
- Capturas: Docs/Previas/Ajustes_Nivel01.
- Índices de documentação atualizados e metadados .meta criados pela Unity.

Não foram movidos assets ou alterados arquivos originais dos fornecedores. O controlador de movimento, os controladores dos menus, SampleScene, Level01_Nave, Input Actions e configurações globais foram preservados.

## Validação

Testes_Play.txt registra o fluxo integral aprovado no Unity, incluindo mudança real da pose da asa durante Fly, arremesso/impacto físico, sorteios, pausa e reinícios.

Auditoria.txt registra componentes únicos, fontes reais, apoio/alcance dos três pontos do cartão e comparação independente dos 1.430 renderizadores anteriores com o backup.

A regressão dos menus em ../Menus/Menus_Validacao.txt também foi aprovada.

A build final não foi gerada. Ajuste humano de dificuldade, vistoria de pontas de asas nos cantos e teste no executável permanecem recomendados.
