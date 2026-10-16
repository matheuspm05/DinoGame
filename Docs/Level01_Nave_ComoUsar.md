# Level 01 — estrutura da nave

## Organização do projeto

```text
Assets/
  Scripts/
    Editor/              # Ferramentas da Unity: gerador do nível
    Runtime/             # Controle de visita e placas do cenário
  Materials/Level01/     # Materiais locais da nave
  Shaders/Level01/       # Shader das placas
  Input/                # Input Actions
  Scenes/               # Level01_Nave e SampleScene
  Settings/             # Configurações URP existentes
  ThirdParty/           # Barking_Dog, DenysAlmaral e Modular SciFi MegaKit
```

Os scripts dos fornecedores continuam dentro dos respectivos pacotes em `ThirdParty`. Os scripts desenvolvidos pela equipe ficam em `Scripts`. As movimentações preservam os arquivos `.meta` e seus GUIDs.

## Gerar e visitar

1. Abra este projeto na Unity 6000.0.81f1 e aguarde a importação/compilação.
2. Fora do Play Mode, use **Tools > Gerar Level 01**.
3. Se houver uma cena aberta com alterações, salve-a quando solicitado. O gerador não edita a SampleScene.
4. A ferramenta salva e abre **Assets/Scenes/Level01_Nave.unity**.
5. Entre em Play e clique na janela Game. **WASD** move, **mouse** olha, **Shift** corre e **Espaço** salta. **Esc** libera o cursor; clique para capturá-lo novamente.
6. Saia de Play para editar os objetos normalmente. O visitante é provisório e pode ser removido quando o personagem definitivo for implementado.

Gerar novamente substitui as edições manuais de Level01_Nave, após confirmação. Não regerar sobre uma cena já decorada manualmente sem guardar uma cópia dela. Materiais adaptados pertencem somente a `Assets/Materials/Level01`; os materiais originais são preservados. Build Settings, Input Settings e Graphics Settings não são alterados.

## Planta e dimensões

Piso na altura Y = 0; altura interna = 3,4 m. Cápsula de visita: 1,8 m de altura e 0,30 m de raio; câmera: 1,65 m. Passagens principais: 3 m; conexões da energia/hangar: 4 m.

```text
                     [ HANGAR 12 x 12 ]
                              |
                    [ SEGURANÇA 8 x 8 ]
                              |
     [ DEPÓSITO 8 x 8 ]---[ LABORATÓRIO 8 x 8 ]
              |
 [ ENERGIA 8 x 8 ]---[ CORREDOR 4 x 8 ]
              |
     [ CONTENÇÃO 8 x 8 ]

```

| Setor | Centro X/Z (m) |
|---|---|
| Contenção | 0 / 0 |
| Corredor | 0 / 8 |
| Energia | -8 / 8 |
| Depósito | 0 / 16 |
| Laboratório | 8 / 16 |
| Segurança | 8 / 24 |
| Hangar | 8 / 34 |

Cada setor tem grupos de arquitetura, iluminação e decoração. Objetos `RESERVADO_*` são marcadores vazios, sem puzzles, triggers ou missões. Portas internas ficam estacionadas abertas. A porta externa do hangar permanece fechada; a saída/visão do planeta será desenvolvida depois.

## Assets reais usados pelo código

- `Assets/ThirdParty/Barking_Dog/3D Free Modular Kit/Prefabs/`: `Floor_01`, `Roof_01`, `Door_Left_01`, `Door_Arch_01` e `Fan_01`.
- `Assets/ThirdParty/Modular SciFi MegaKit[Standard]/Modular SciFi MegaKit[Standard]/FBX (Unity)/`:
  - `Walls/WallAstra_Straight` e `TopSimple_Straight`;
  - `Platforms/Door_Frame_Square` e `Door_DarkMetal`;
  - `Columns/Column_Pipes`;
  - `Props/Prop_Computer`, `Prop_AccessPoint`, `Prop_Crate3`, `Prop_Crate4`, `Prop_Barrel_Large`, `Prop_Cable_3`, `Prop_Vent_Small`, `Prop_Vent_Wide`, `Prop_Light_Wide` e `Prop_ItemHolder`.

O gerador consulta `AssetDatabase.FindAssets`, confirma os caminhos e instancia com `PrefabUtility.InstantiatePrefab`. Suportes `Ajuste` normalizam a geometria pelos limites dos renderizadores; os modelos continuam vinculados às fontes. Não há GUIDs de fornecedores escritos no código.

Pisos, paredes e grandes objetos têm um BoxCollider simplificado por módulo. Colisores importados são desativados somente nas instâncias utilizadas, evitando colisores ativos duplicados. Molduras e pequenos detalhes não têm colisão. Corpos rígidos, animações, scripts de demonstração, luzes e câmeras importados são desativados somente nessas instâncias.

## Verificações

O código foi compilado com Roslyn e referências locais da Unity 6000.0.81f1, sem erros. A geração também foi executada no Unity Editor já aberto: a cena foi salva com 307 instâncias vinculadas a 20 modelos distintos, e passou em 243 amostras do trajeto, sem scripts ausentes. As quatro prévias renderizadas em URP ficam em `Docs/Level01_Previas`. O teste manual de movimentação em Play ainda deve ser feito pela equipe.

Ao gerar, a ferramenta testa o caminho completo, incluindo ida/volta da sala de energia, com amostras a cada 25 cm. Confere piso, espaço para uma cápsula humana e scripts ausentes. Se houver falha, informa o ponto no Console e não salva a nova cena. Uma geração bem-sucedida escreve `Docs/Level01_Validacao.txt`, com contagens e caminhos efetivamente usados.

Verifique em Scene/Game:

- paredes e tetos sem frestas visíveis, normais invertidas ou proporções estranhas;
- molduras realmente vazadas e folhas abertas fora da circulação;
- altura visual dos pisos e pés do jogador, pois a colisão é simplificada;
- materiais sem rosa, transparência da contenção e legibilidade das placas;
- luminosidade das salas e transições; as luzes são realtime, sem bake;
- a curva depósito/laboratório e os acessos da energia e do hangar;
- o salto sem atravessar o teto e a câmera sem cortar objetos próximos.

Algumas peças recebem escala não uniforme para compatibilizar os dois kits. A forma exata dos encaixes ainda exige revisão no Editor. Materiais locais usam faces duplas para permitir ver as paredes dos dois lados. Placas usam shader URP próprio e `NaveWorldLabel`, que atualiza o atlas da fonte sem deixar textos atravessarem paredes. O baú articulado `Prop_Chest` apresentou escala inconsistente ao reabrir a cena e foi substituído por `Prop_Crate4`; seu asset original não foi alterado. A cápsula é uma composição provisória de suporte/moldura; o hangar ainda não tem nave, exterior ou cutscene. Esta etapa não busca concluir a meta final de 70 assets distintos.
