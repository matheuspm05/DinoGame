# Validação final do polimento

Projeto: C:/Users/matheus/Desktop/dino3  
Unity: 6000.0.81f1 — URP — novo Input System  
Cena: Assets/Scenes/Level01_Nave_Expanded.unity

## Resultado

- Scripts compilados e executados no Unity Editor.
- Progressão completa em Play: **APROVADO**, relatório de 21/10/2026 01:20:55.
- **849 movimentos físicos** da cápsula, incluindo contenção, carga/máquinas, energia, laboratório, segurança, hangar e saída.
- Regressão de menus/controles em Play: **APROVADO**, relatório de 21/10/2026 01:21:58.
- Auditoria estática e preservação de referências: **APROVADO**.
- Reaplicação da integração: **zero colisores auxiliares adicionais** e mesmos componentes, sem acúmulo de instâncias.
- **1.434 renderizadores do cenário** comparados ao backup, com posição, rotação e escala preservadas. O pet recebeu o novo local de espera autorizado.
- **173 AudioClips originais preservados**, com GUIDs conferidos individualmente e sem duplicação.
- Um jogador, HUD, EventSystem e AudioListener.
- Zero scripts ausentes. Nenhum MeshCollider ou Rigidbody novo para decoração.

## Cobertura em Play

- Quatro portas sólidas bloqueiam o CharacterController fechado e deixam o percurso aberto depois.
- 141 módulos de teto conferidos e subida da cápsula contra tetos nos nove setores.
- Arremesso com física real, erro/acerto do alvo, reposição e remoção das caixas.
- Pássaro começa esperando no chão; idles variam; F adota uma única vez; alvo desliga; decola; o Fly movimenta as asas reais.
- Pausa bloqueia F, arremesso, jogador, pet, animações e HUD de gameplay.
- Cadência de passos lê caminhada/corrida e suspende no ar/parado. **Não há áudio de passos atribuído**, por falta de clipes adequados.
- Bateria exige coleta e continua restaurando energia/liberando laboratório.
- Partículas ciano reais e duas rajadas de vapor disparadas pela abertura; ambas encerram automaticamente.
- Fontes 3D reproduzem áudio; pausa congela fonte ativa e partículas; continuar retoma o áudio que estava tocando.
- Todos os pontos do cartão têm apoio; testes de acesso/linha de visão dos três pontos foram aprovados; sorteios preservam um único cartão.
- Cartão/leitor e terminais continuam funcionando, sem contagem duplicada.
- Terceiro terminal inicia portão/luz/alerta, sem concluir sozinho. Alerta encerra após quatro segundos e não dispara novamente em SetOpen repetido.
- Passagem do jogador na saída conclui; retomada, Novo Jogo, Reiniciar e Menu Principal restauram o fluxo/estados.
- Regressão dos menus inclui cliques por raycast, movimentação, câmera, corrida, salto, agachamento, teto baixo, ESC, controles, reinício e Sair no Editor.

## Colisões

Foram revistos 572 objetos arquitetônicos/sólidos da raiz gerada e 38 modelos avulsos da cena. A revisão reconfigurou 290 objetos conforme sua geometria atual.

A auditoria registra 902 colisores sólidos habilitados contra 648 no backup. O detalhamento dos componentes e volumes fica em Integracao.txt e Auditoria.txt. Colisores antigos desalinhados foram desativados na instância da cena, preservando os arquivos dos fornecedores. Auxiliares usam nomes Colisao_Solida, Colisao_Pe, Colisao_Lateral e Colisao_Verga. Os colisores das portas continuam na folha móvel.

O terceiro marcador do cartão acompanha agora a mesa real em (26,06; 0,906; 23,28). Não foi movido nenhum móvel ou modelo decorativo.

A conclusão antes dependia somente de OnTriggerEnter e falhou de forma intermitente em uma rodada. O mesmo LevelCompleteTrigger passou a conferir também permanência e presença da cápsula real no volume existente, mantendo a validação de três terminais no gerente. A rodada final após essa correção foi aprovada.

## Evidências

- Testes_Play.txt: sequência completa e resultado.
- ../Menus/Menus_Validacao.txt: regressão dos menus.
- Auditoria.txt: modelos, componentes, recursos, GUIDs e comparação com backup.
- Audio_Caminhos_GUIDs.csv: origem/destino/GUID dos 173 clipes.
- Integracao.txt: objetos/colliders configurados e reaplicação sem duplicação.
- ../../Previas/Polimento_Nivel01/: capturas reais do Sparrow esperando/voando, energia e vapor.
- Arquivos.md: arquivos criados, modificados e movidos.

## Limitações e revisão humana

O pacote adicional de falas/anúncios não foi encontrado importado. Também não foram encontrados passos reais nem cantos adequados. Os respectivos arrays estão vazios intencionalmente, prontos no Inspector. O alerta usa Kenney error_006, um aviso eletrônico repetido por quatro segundos, sem anúncio de voz.

As capturas foram inspecionadas visualmente para o pet, partículas ciano e vapor. A reprodução foi verificada pelo estado das AudioSources no Editor; o balanço auditivo de volume, pitch, sobreposição e emendas do ambiente precisa de uma jogada humana com som.

Revisar cantos estreitos do acompanhamento, extremidades de braços mecânicos/estantes e pequenas diferenças das caixas de colisão em relação às silhuetas. Não foi gerada nem testada uma build standalone. Não há configuração manual necessária para portas, partículas, adoção, menus ou puzzles. Passos/cantos precisam de importação e atribuição dos clipes adequados.
