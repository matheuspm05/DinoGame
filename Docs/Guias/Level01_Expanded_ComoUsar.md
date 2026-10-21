# Nível 1 — Nave expandida

A implementação está em `Assets/Scenes/Level01_Nave_Expanded.unity`. A nave original foi preservada em `Assets/Scenes/Level01_Nave.unity`. O controlador existente foi copiado integralmente para a nova cena; seu script, parâmetros, câmera, corrida, salto e agachamento foram mantidos.

## Abrir e jogar

1. Abra `Assets/Scenes/MainMenu.unity` e entre em Play.
2. **Novo Jogo** inicia `Level01_Nave_Expanded`.
3. WASD movimenta, mouse olha, Shift esquerdo corre, Espaço pula, Ctrl esquerdo/direito agacha enquanto pressionado, F interage, ESC pausa/continua.
4. **Reiniciar Nível** recarrega a cena em que o menu de pausa está. Isso mantém o reinício correto tanto na nave expandida quanto na referência original.
5. **Menu Principal** retorna ao menu; Novo Jogo permite iniciar novamente.

A Scene List é MainMenu → Level01_Nave_Expanded → Level01_Nave. MainMenu continua sendo a entrada da aplicação. Não há configuração manual de OnClick nem novos pacotes para instalar.

## Layout e ambientação

| Setor | Dimensões internas | Identidade |
|---|---|---|
| 01 Contenção | 12 × 10 m | Berço tecnológico, estação de observação, iluminação fria. |
| 02 Corredores | Conexões curtas, vão livre de 3 a 4 m | Acesso claro aos setores, ventilação, cabos e câmera decorativa. |
| 03 Energia | 14 × 12 m | Armários elétricos, estruturas de tubulação, servidores, luz âmbar e reserva para o puzzle. |
| 04 Depósito | 16 × 16 m | Prateleiras, palete, caixas, barris e contêineres agrupados nas laterais. |
| 05 Laboratório | 20 × 16 m | Quatro bancadas, computadores, cadeiras, braços mecânicos, divisórias e armazenamento técnico; luz azul. |
| 06 Segurança | 14 × 12 m | Duas estações de monitoramento, telas, servidor, câmera e espaço do terminal do hangar. |
| 07 Hangar | 26 × 22 m | Cinco baias de carga sobre plataformas baixas, estruturas, contêineres e portão externo fechado. Pé-direito de 6 m. |
| 08 Máquinas | 10 × 16 m | Ventiladores superiores, tubulações, suportes e quadros industriais; luz de alerta. Desvio curto pelo depósito. |
| 09 Manutenção | 12 × 10 m | Bancada, suportes, armários e equipamentos; luz verde suave. Desvio pela segurança. |

Veja a [planta](../Plantas/Level01_Expanded_Planta.svg) e as nove renderizações em `Docs/Previas/Nivel01_Expandido/`. A rota principal segue contenção → corredores → depósito → laboratório → segurança → hangar. Energia, máquinas e manutenção são explorações laterais. A nave agora inclui puzzles de bateria, cartão e terminais. Veja o [guia de gameplay](Gameplay_Nivel01.md) para a rota completa; o tempo de exploração precisa ser conferido numa partida da equipe.

## Assets e contagem

Foram comprovados **82 modelos visuais distintos**, em **1.005 instâncias de cenário**, de cinco pacotes. O inventário de origem é separado da contagem de uso.

- [Relatório detalhado](../Relatorios/Nivel01_Expandido/Level01_Expanded_Assets.md): nome, caminho original, pacote, categoria, setores e quantidade de cada modelo.
- [CSV](../Relatorios/Nivel01_Expandido/Level01_Expanded_Assets.csv) e [JSON](../Relatorios/Nivel01_Expandido/Level01_Expanded_Assets.json): dados da cena e hashes de geometria, incluindo as origens das malhas.
- [Inventário dos pacotes](../Relatorios/Nivel01_Expandido/Level01_Expanded_Inventario.md): pastas reais, modelos, prefabs, materiais, texturas e pacotes ausentes.

As instâncias são encontradas por `PrefabUtility`, e as geometrias equivalentes são agrupadas. Platform_3Plates/Platform_DarkPlates, Platform_Metal2/Platform_Squares e os dois prefabs de braço mecânico Office foram agrupados, contando uma vez por conjunto geométrico. Cores, escala, rotação, posição, formatos alternativos, UI, jogador, materiais, placas de texto e objetos vazios não aumentam o total. A contagem pode ser deliberadamente conservadora.

## Edição e geração segura

Todos os objetos existem na Hierarchy e podem ser editados. Os setores ficam sob `Level01_Nave_Expanded`, com arquitetura/iluminação separadas e props identificados pelo modelo de origem. Os pivôs dos setores ficam nos seus centros, sem alterar a posição dos elementos. Marcadores `RESERVADO_...` indicam futuros pontos de gameplay; não têm scripts ou triggers.

**Tools → Gerar Level 01 Expandido** recria somente a versão expandida. Pede confirmação antes de substituir suas edições manuais. Repetir a geração substitui a cena, sem acrescentar uma segunda nave. A geração batch recusa substituir uma candidata que esteja aberta, evitando sobrescrita inesperada; o comando de menu administra o fechamento e reabertura com confirmação.

**Tools → Gerar Level 01** continua sendo o gerador da referência original. Ele não é necessário para usar a expansão. **Tools → Configurar Menus** conecta a pausa da versão expandida e configura a Scene List; não reconstrói o ambiente.

`Level01NaveGenerator.cs` foi dividido por responsabilidade usando partes da mesma classe, sem criar um novo sistema de execução:

| Script | Responsabilidade |
|---|---|
| `Level01NaveGenerator.Expanded.cs` | Caminhos reais, inventário selecionado, geração isolada e cópia do jogador existente. |
| `Level01NaveGenerator.ExpandedLayout.cs` | Salas, conexões, arquitetura, portas abertas, iluminação e rota. |
| `Level01NaveGenerator.ExpandedDecoration.cs` | Composição de cada setor e apoio dos objetos sobre mesas/paletes. |
| `Level01NaveGenerator.ExpandedValidation.cs` | Checagem estrutural, grade navegável, materiais e capturas URP. |
| `Level01ExpandedAudit.cs` | Contagem por geometria da cena real e exportação dos relatórios. |
| `Level01ExpandedPlayValidation.cs` | Percurso automatizado com a cápsula real em Play. |

O gerador compartilhado recebeu medição da pose real para modelos com `SkinnedMeshRenderer`, evitando escala incorreta no ventilador. Os materiais adaptados ficam em `Assets/Materials/Level01_Expanded/`; os materiais dos fornecedores permanecem intactos. Cópias locais mantêm mapas de base, normal, metal, oclusão e emissão quando disponíveis, com adaptação dos vidros transparentes.

## Validação e limites

Validação executada na Unity **6000.0.81f1** em 15/10/2026, com resultado **APROVADO** para percurso em Play e fluxo completo dos menus na expansão. A cápsula realizou 724 movimentos físicos, percorrendo os acessos e desvios. Compilação verificada tanto para Editor quanto para o código de runtime sem `UNITY_EDITOR`. A conferência independente do YAML encontrou 1.005 instâncias de cenário e um prefab de pausa, excluído da contagem visual. Os 2.248 arquivos protegidos conferidos permaneceram idênticos, incluindo a nave original e os arquivos dos fornecedores.

- `Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Validacao.txt`: rota e desvios com 741 amostras, conectividade em grade, setores, componentes e materiais.
- `Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Play.txt`: percurso físico em Play; o teste usa o CharacterController da cena e descarta o estado ao sair.
- `Docs/Relatorios/Menus/Menus_Validacao.txt`: fluxo real dos menus, novo início, corrida, salto, Ctrl, teto baixo, ESC, controles, reinício, retorno ao menu e saída do Play.
- `Docs/Relatorios/Nivel01_Expandido/Level01_Expanded_Preservacao.txt`: conferência dos arquivos protegidos e estabilidade de contagem entre gerações.

Comandos para repetir: **Tools → Validar Nave Expandida em Play** e **Tools → Validar Menus em Play**. O teste de menus utiliza dispositivos virtuais e restaura os dispositivos reais ao sair de Play. Nenhum dos testes salva alterações de gameplay na cena.

O cenário usa 25 luzes sem sombras em tempo real; renderizadores estáticos são marcados para batching e não projetam sombras. Não foram adicionados efeitos pesados, scripts de comportamento nos props ou colisores MeshCollider. Os colisores importados são desativados nas instâncias; sólidos recebem uma caixa local, sem duplicar colisão ativa. Molduras e detalhes elevados mantêm os vãos livres. As plataformas baixas do hangar permanecem nas laterais.

Ainda é recomendável conferir manualmente a movimentação próxima a móveis e plataformas: as caixas de colisão são mais simples que as pernas de mesa e recortes dos modelos. A grade pode registrar pequenos bolsões isolados atrás de mobiliário; todos os centros e entradas de setor são alcançáveis. As renderizações foram revisadas, mas não substituem uma vistoria manual de todos os ângulos. O desempenho em executável e outros computadores ainda precisa ser medido; o teste realizado usa o Editor.

Na expansão inicial de 15/10, Energy Cells Free, Free Sockets and Switches, Planet Earth Free e Hand Painted Nature Kit LITE não foram encontrados. Energy Cells Free e o cartão ARCADIA foram importados posteriormente e já estão integrados aos puzzles. Não houve download ou instalação. City People está importado e ficou fora da cena. Não foi encontrada cápsula científica pronta específica: a contenção usa um berço tecnológico com os modelos disponíveis. A saída do hangar agora abre após três terminais e leva a um pequeno deck de conclusão. Há puzzles simples de bateria/cartão/terminais; continuam fora de escopo inventário complexo, inimigos, combate, cutscenes e save. Os relatórios de expansão de 15/10 registram a ambientação anterior; os testes atuais de gameplay estão em Docs/Relatorios/Gameplay.

## Outros arquivos modificados

- `Assets/Scripts/Editor/Level01/Generation/Level01NaveGenerator.cs`: classe parcial, destino separado de materiais e medição correta de malhas deformadas; preservação dos mapas/transparência na conversão local para URP.
- `Assets/Scripts/Runtime/UI/MenuSceneActions.cs`: Novo Jogo utiliza a cena expandida após sua validação.
- `Assets/Scripts/Runtime/UI/PauseMenuController.cs`: reinicia a cena que contém o menu, atendendo às duas versões da nave.
- `Assets/Scripts/Editor/UI/MenuSceneSetup.cs`: configuração da expansão e da Scene List, mantendo a original como referência.
- `Assets/Scripts/Editor/Validation/MenuPlayModeValidation.cs`: testes adicionais de corrida/salto e sincronização dos passos com frames reais durante carregamento.
- `ProjectSettings/EditorBuildSettings.asset`: MainMenu em primeiro, expandida em segundo e original em terceiro.
- `Docs/Guias/Menus_ComoUsar.md`, relatório de validação e prévias dos menus: documentação/registro atualizados para a nova versão.

Novos assets têm seus arquivos `.meta` gerados pela própria Unity. Os auxiliares temporários da sessão de desenvolvimento foram removidos. Não houve alteração do controlador do jogador, dos assets originais de terceiros, da SampleScene, dos pacotes instalados ou das configurações globais de Input/URP.
