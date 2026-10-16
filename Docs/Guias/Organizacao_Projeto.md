# Organização do projeto

```text
Assets/
├── Scenes/                         MainMenu, nave original e nave expandida
├── Scripts/
│   ├── Runtime/                    Código usado durante o jogo
│   │   ├── Player/                 Movimento, câmera e agachamento
│   │   ├── Environment/            Placas 3D do cenário
│   │   └── UI/                     Menu principal, pausa e controles
│   └── Editor/                     Ferramentas exclusivas do Editor
│       ├── Level01/
│       │   ├── Generation/         Geradores, layout, decoração e conferência da montagem
│       │   └── Validation/         Auditoria dos modelos e percurso em Play
│       ├── UI/                     Criação/configuração das interfaces
│       └── Validation/             Testes de menus, entrada e movimentação
├── Prefabs/UI/                     Prefab reutilizável do menu de pausa
├── Materials/                      Materiais locais por versão do nível
├── Shaders/                        Shaders próprios
├── Input/                          Asset do novo Input System
├── Settings/                       Configurações de renderização existentes
├── ThirdParty/                     Pacotes importados, com seus arquivos originais
│   ├── AK Studio Art/              Surveillance Camera
│   ├── Barking_Dog/                3D Free Modular Kit
│   ├── BrokenVector/               Low Poly Storage Pack e shaders do pacote
│   ├── DenysAlmaral/               City People
│   ├── Modular SciFi MegaKit[Standard]/
│   └── ScifiOfficeLite/            Free Sci-Fi Office Pack
└── TutorialInfo/                   Conteúdo inicial do template da Unity

Docs/
├── Referencias/                    PDFs do trabalho
├── Guias/                          Como usar e explicação dos scripts
├── Relatorios/                     Assets e resultados dos testes
├── Plantas/                        Planta da nave
└── Previas/                        Imagens do projeto
```

Os scripts mantêm seus nomes e namespaces. Foram movidos pela API `AssetDatabase.MoveAsset`, com seus `.meta`, preservando os GUIDs usados pelas cenas e prefabs. Os arquivos dos pacotes foram agrupados sem alterar seu conteúdo. Os caminhos utilizados pelos geradores, pelos testes e pelos relatórios foram atualizados para a nova estrutura.

As cenas e o prefab de pausa permanecem nos mesmos caminhos. Nenhum cenário foi regenerado para organizar as pastas. Os comandos do menu **Tools** permanecem com os mesmos nomes.

`Packages` e `ProjectSettings` ficam na raiz porque fazem parte da estrutura da Unity. `Library`, `Temp`, `Logs`, `obj`, `UserSettings` e arquivos `.csproj`/`.slnx` são gerados ou gerenciados pelas ferramentas; não foram movidos nem excluídos.

Para entender cada arquivo C#, consulte [Scripts.md](Scripts.md). Os scripts de terceiros ficam dentro dos respectivos pacotes, separados do código próprio.
