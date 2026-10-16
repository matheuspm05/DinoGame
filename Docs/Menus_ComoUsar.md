# Menu Principal e Pausa

## Executar

1. Abra `Assets/Scenes/MainMenu.unity` na Unity e entre em Play.
2. **Novo Jogo** inicia `Level01_Nave` desde o início.
3. No nível, **ESC** abre/fecha a pausa. O menu também permite continuar, consultar controles, reiniciar, retornar ao Menu Principal e sair.
4. No Editor, **Sair** encerra Play. Na build, encerra o aplicativo.

A Scene List está configurada com `MainMenu` em primeiro e `Level01_Nave` em segundo. Se o Build Profile ativo tem uma lista própria, ela também recebe essa ordem. Nenhuma configuração manual de OnClick é necessária.

## Editar a interface

- MainMenu: expanda `UI_MenuPrincipal` na Hierarchy. Seus painéis, botões e textos são objetos normais da cena.
- Pausa: expanda `UI_Pausa` na nave. `PauseOverlay` fica inativo fora da pausa; ative-o temporariamente no Inspector para editar sua aparência.
- Para que ajustes da pausa sejam reutilizados em futuras gerações, edite `Assets/Prefabs/UI/PauseMenu.prefab` em Prefab Mode, ou aplique ao prefab os overrides desejados da instância.
- Os Canvas usam **Screen Space - Overlay**, **Canvas Scaler / Scale With Screen Size**, referência **1920x1080** e Match **0.5**.
- Foram usados `Text` e a fonte nativa `LegacyRuntime.ttf`, sem importar recursos do TextMeshPro.
- Botões usam `Image`, `Button`, `Color Tint` e eventos OnClick persistentes, visíveis no Inspector. O EventSystem usa `InputSystemUIInputModule`.

O painel de controles informa **WASD**, **mouse**, **Shift esquerdo**, **Espaço**, **Ctrl (segurar) — Agachar** e **ESC**. Ctrl esquerdo ou direito reduz a cápsula de 1,8 m para 1,1 m, baixa suavemente a câmera e limita a velocidade a 1,8 m/s. Soltar Ctrl levanta o jogador quando há espaço acima. Enquanto agachado, correr e pular ficam bloqueados. A pausa preserva a postura; ao continuar, o controlador consulta novamente Ctrl e o espaço livre. Esses parâmetros ficam no Inspector do jogador. Interação ainda não está implementada.

## Scripts

| Arquivo | Responsabilidade |
|---|---|
| `Assets/Scripts/Runtime/UI/MainMenuController.cs` | Novo Jogo, controles e saída do menu principal. |
| `Assets/Scripts/Runtime/UI/PauseMenuController.cs` | ESC, pausa, bloqueio do controlador existente e navegação da pausa. |
| `Assets/Scripts/Runtime/UI/ControlsPanelController.cs` | Abrir controles e voltar à página anterior. |
| `Assets/Scripts/Runtime/UI/MenuSceneActions.cs` | Carregar cenas com tempo normal e encerrar a aplicação. |
| `Assets/Scripts/Editor/UI/MenuSceneSetup.cs` | Instalar as interfaces e configurar a Scene List. |
| `Assets/Scripts/Editor/UI/MenuPlayModeValidation.cs` | Validar os fluxos em Play com entrada virtual, sem salvar mudanças de gameplay. |

O `NaveWalkthroughController` original continua responsável pela movimentação e câmera. Sua antiga ação de ESC foi substituída pelo menu, e foi acrescentada uma guarda para ignorar entradas com `timeScale` zero. A pausa desativa temporariamente esse mesmo componente e o reativa ao continuar.

`Time.timeScale` volta a 1 ao continuar, reiniciar, trocar de cena ou sair. A UI permanece responsiva porque o EventSystem e o Input System não dependem do tempo escalado para os cliques.

## Integração com a nave

O gerador recebeu somente a chamada `MenuSceneSetup.EnsurePauseMenu(scene)` após criar o jogador. Ela instancia o prefab existente, conecta o controlador e garante um único EventSystem. A arquitetura/layout do gerador não foi alterada, e a nave não foi regenerada para instalar os menus.

**Tools > Configurar Menus** instala interfaces faltantes e configura a Scene List. Não recria um MainMenu ou prefab de pausa que já existe. **Tools > Gerar Level 01** continua sendo o comando de reconstrução completa da nave; ele não é necessário para instalar/editar os menus.

## Verificação

**Tools > Validar Menus em Play** executa os fluxos e escreve `Docs/Menus_Validacao.txt`. As capturas da Game View ficam em `Docs/Menu_Previas`. O teste restaura a cena inicial de Play anteriormente configurada e usa alterações de entrada apenas em memória, sem modificar o asset de Input Actions. Durante o teste, os dispositivos físicos de teclado/mouse ficam temporariamente desativados apenas no Input System da Unity para evitar interferência nos eventos virtuais; são restaurados ao sair de Play. O teste também verifica Ctrl esquerdo/direito, redução da cápsula/câmera, pés no piso, bloqueio de pulo agachado, teto baixo e preservação da postura na pausa. O teto de validação existe somente em Play e não é salvo na cena.

Validação executada na Unity **6000.0.81f1**, com resultado **APROVADO**: cliques reais do Input System, ESC, bloqueio de movimento/rotação, controles durante a pausa, Continuar, Reiniciar Nível, retorno ao menu, novo início e Sair encerrando Play. A compilação dos scripts também passou com e sem `UNITY_EDITOR`. O encerramento do executável deve ser conferido na build.

A comparação da cena antes/depois confirmou que os **992 transforms**, **181 BoxColliders**, **CharacterController**, câmera e **307 instâncias de prefabs** originais permaneceram iguais. O Editor acrescentou seus componentes auxiliares URP durante a serialização; a arquitetura, as posições e os parâmetros do jogador foram preservados.

Também teste manualmente:

1. Novo Jogo → ande, corra, olhe e pule. Segure Ctrl para agachar, tente correr/pular, solte para levantar. Confira o bloqueio de levantar sob obstáculos baixos e a retomada após ESC.
2. ESC → tente mover/girar; o jogador deve permanecer parado e o cursor visível.
3. Abra Controles, clique Voltar e pressione ESC para continuar.
4. Pause outra vez e use Continuar.
5. Ande pelo nível, pause e reinicie; a posição inicial deve ser restaurada.
6. Volte ao Menu Principal e inicie novamente.
7. Confira a Game View em 1280x720, 1920x1080 e 1024x768.
8. Teste Sair na build; no Editor, o efeito esperado é parar Play.

Referências de API utilizadas na configuração da Unity 6: [BuildProfile](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Build.Profile.BuildProfile.html) e [EditorBuildSettings.scenes](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/EditorBuildSettings-scenes.html).
