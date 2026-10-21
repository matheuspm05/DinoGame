# Polimento audiovisual do Nível 1

Abra `Assets/Scenes/MainMenu.unity` e pressione Play. Novo Jogo continua usando `Level01_Nave_Expanded`. A nave não foi reconstruída.

## O que mudou

- O Sparrow espera no chão em **(1,12; 0,116; 6,35)**, depois da porta inicial. Mire nele e pressione **F** para adotar. É opcional. Depois da adoção o alvo de interação desliga e o pet decola suavemente para acompanhar o jogador.
- O controller original `AC_Sparrow` permanece atribuído. `Idle_A`, `Idle_B` e `Idle_C` variam a cada 3–7 segundos enquanto espera. Depois de adotar, usa o único clipe `Fly`, sem alternar poses terrestres durante o voo.
- As quatro portas de progressão disparam som 3D e duas pequenas rajadas de vapor quando começam a abrir. A lógica de abertura continua em `DoorController`.
- A instalação bem-sucedida da bateria dispara partículas ciano e uma confirmação eletrônica.
- A saída externa emite um aviso eletrônico por **4 segundos** ao receber a terceira ativação. A luz branca continua funcionando. O trigger de conclusão verifica também a permanência/presença da cápsula real no mesmo volume, evitando perder uma entrada do CharacterController; ainda exige três terminais e passagem pela saída.
- Foram alinhados colisores à geometria atual de paredes, caixas, equipamentos e móveis. Mesas usam tampo e pés; marcos de portas usam laterais e verga. Pisos, 141 tetos e as quatro folhas móveis foram conservados/verificados.
- O terceiro marcador do cartão foi ajustado à superfície da mesa que havia sido movida manualmente. O único cartão continua sendo sorteado em execução.

## Áudios disponíveis e limitações

A inspeção pela AssetDatabase encontrou **173 AudioClips**, todos dos dois pacotes Kenney. O pacote adicional com falas/anúncios **não foi encontrado importado neste projeto**.

| Uso | Arquivo atual | Origem |
|---|---|---|
| Porta inicial e saída externa | `Audio/SFX/Doors/Kenney_doorOpen_000.ogg` | Kenney Sci-fi Sounds |
| Laboratório | `Audio/SFX/Doors/Kenney_doorOpen_001.ogg` | Kenney Sci-fi Sounds |
| Hangar | `Audio/SFX/Doors/Kenney_doorOpen_002.ogg` | Kenney Sci-fi Sounds |
| Instalar bateria | `Audio/SFX/Interactions/Kenney_confirmation_001.ogg` | Kenney Interface Sounds |
| Alerta externo | `Audio/SFX/Alarms/Kenney_error_006.ogg` | Kenney Interface Sounds |
| Ambiente discreto da energia | `Audio/Ambient/Kenney_engineCircular_000.ogg` | Kenney Sci-fi Sounds |

Todos os caminhos da tabela são relativos a `Assets/`. O alerta é um aviso eletrônico curto repetido por quatro segundos; não é uma fala ou sirene de um pacote inexistente.

**Não foram encontrados passos reais nem cantos de pássaro adequados.** Não foram usados ruídos eletrônicos nessas funções.

Os sistemas estão preparados:
- No jogador, `PlayerFootstepAudio.footsteps`: atribua passos reais quando importados. A frequência acompanha o deslocamento efetivo, corrida e contato com o chão; parado ou no ar não gera passos.
- Em `CompanionPet/CompanionChirps.clips`: atribua cantos reais. Só toca após adotar, em intervalos sorteados de 8–20 segundos, com volume discreto e sem repetição imediata quando houver mais de um clipe.
- Ambos os arrays começam vazios intencionalmente. Atribuir clipes pelo Inspector basta; não é preciso modificar o código.

## Organização editável

```text
Assets/
  Audio/
    SFX/Footsteps/
    SFX/Doors/
    SFX/Interactions/
    SFX/Alarms/
    SFX/Bird/
    Ambient/
    Music/
    Libraries/Kenney_SciFi/
    Libraries/Kenney_Interface/
  VFX/
    Particles/SoftParticle.asset
    Materials/
    Prefabs/VFX_EnergyActivation.prefab
    Prefabs/VFX_DoorSteam.prefab
  Scripts/Runtime/Gameplay/
    Audio/
    Companion/
    VFX/
```

As bibliotecas foram movidas com `AssetDatabase.MoveAsset`; o conteúdo dos arquivos, metadados, GUIDs e licenças foram preservados. Os seis clipes utilizados ficam nas categorias acima; os demais continuam nas bibliotecas, sem duplicação. O relatório `Audio_Caminhos_GUIDs.csv` relaciona cada caminho original ao atual e ao GUID.

Os VFX usam Particle System nativo, máscara suave criada no projeto e `Universal Render Pipeline/Particles/Unlit`. Não têm colisores, loops, luzes adicionais ou plugins. Energia termina em aproximadamente 3 segundos; vapor termina em até 2 segundos. Nenhuma configuração global de URP foi alterada.

## Scripts novos

| Script | Responsabilidade |
|---|---|
| `PauseAwareAudio` | Pausa e retoma somente a AudioSource local, sem deixar estado global de áudio entre cenas. |
| `PlayerFootstepAudio` | Mede velocidade e chão após o movimento, calcula cadência e sorteia passos/pitch quando houver clipes. |
| `DoorPresentation` | Escuta a abertura do DoorController, toca som/vapor e encerra o alerta externo no prazo. |
| `CompanionChirps` | Agenda cantos opcionais somente após adoção. |
| `SparrowAnimationController` | Alterna idles existentes e faz transição suave para Fly. |
| `EnergyActivationVFX` | Observa a instalação real da bateria e reproduz o efeito uma única vez. |
| `Level01PolishSetup` e seus arquivos parciais | Ferramenta idempotente de integração, organização de áudio, VFX e colisões. |
| `Level01PolishAudit` | Verifica fontes reais, GUIDs, materiais, componentes únicos e preservação da decoração. |
| `Level01GameplayValidation.Polish` | Complementa os testes existentes de progressão com adoção, cadência, áudio, partículas, pausa e reset. |

`PetFollower` agora também implementa o contrato `IInteractable` existente. `DoorController` ganhou apenas um evento para avisar o início da abertura. O movimento do jogador e as condições dos puzzles continuam nos scripts anteriores.

## Ferramentas e integração futura

- **Tools → Gameplay Level 01 → Integrar audio VFX e adocao** aplica o polimento à cena existente. Não chama o gerador da nave. Execute fora de Play.
- **Tools → Gameplay Level 01 → Auditar audio VFX e adocao** gera a auditoria.
- **Tools → Gameplay Level 01 → Validar progressao completa em Play** executa interação, física, puzzles, adoção, VFX/áudio e reinício. O estado temporário do teste é descartado.
- **Tools → Validar Menus em Play** verifica os menus e controles anteriores.

O gerador expandido chama a integração do polimento no final de futuras gerações. **Gerar novamente a nave ainda substitui o layout editado manualmente; não é necessário para usar este trabalho.** A cena funcional anterior ao polimento está em `Assets/Scenes/Backups/Level01_Antes_Polimento.unity`.

## Teste manual

1. Pegue a caixa com F e acerte o alvo. Confira som e vapor na abertura da primeira porta.
2. No corredor, observe os idles do Sparrow. Ele deve continuar esperando se você passar sem adotar.
3. Mire no pássaro e pressione F. Confira mensagem, decolagem, voo lateral e passagem por portas/cantos.
4. Colete e instale a bateria. Confira partículas ciano, confirmação e abertura do laboratório.
5. Encontre o cartão, use o leitor e confira som/vapor do hangar.
6. Ative os três terminais. Confira alerta por quatro segundos, vapor e luz branca. A fase só termina ao atravessar a saída.
7. Durante efeitos, pause com ESC: áudio, partículas, jogador e pet devem parar; os botões continuam ativos.
8. Reinicie: pássaro volta a esperar, portas/puzzles/efeitos voltam ao início e o cartão é sorteado novamente.

A avaliação auditiva de volumes, balanço e transições precisa de uma jogada humana com som. Revise colisões compostas nas extremidades de braços mecânicos e estantes e teste uma build no computador de apresentação. As capturas e resultados automatizados ficam em `Docs/Relatorios/Polimento_Nivel01` e `Docs/Previas/Polimento_Nivel01`.
