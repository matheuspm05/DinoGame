# Inventário dos pacotes importados

Dados obtidos por nomes, caminhos e metadados. Modelos são carregados pela Unity com AssetDatabase/PrefabUtility; texturas e modelos não foram abertos manualmente como arquivos binários. Contagens abaixo são inventário de arquivos, separado da contagem de modelos utilizados na cena.

| Pacote | Pasta real | FBX | Prefabs | Materiais | Texturas |
|---|---|---:|---:|---:|---:|
| Barking Dog — 3D Free Modular Kit | `Assets/ThirdParty/Barking_Dog/3D Free Modular Kit` | 17 | 22 | 3 | 4 |
| Modular SciFi MegaKit | `Assets/ThirdParty/Modular SciFi MegaKit[Standard]` | 378 | 0 | 0 | 24 |
| Free Sci-Fi Office Pack | `Assets/ThirdParty/ScifiOfficeLite` | 33 | 46 | 28 | 107 |
| Surveillance Camera | `Assets/ThirdParty/AK Studio Art/Surveillance Camera` | 1 | 1 | 2 | 5 |
| Low Poly Storage Pack | `Assets/ThirdParty/BrokenVector/LowPolyStoragePack` | 0 | 87 | 15 | 5 |
| City People FREE Samples | `Assets/ThirdParty/DenysAlmaral/CityPeople` | 53 | 32 | 31 | 37 |

MegaKit inclui versões alternativas OBJ/glTF e FBX para outros programas. A geração usa exclusivamente FBX (Unity). Office e Storage têm variantes de cor/material: elas são agrupadas por geometria no relatório da cena. A câmera tem pacotes de compatibilidade de renderização anexos, que não foram instalados; foram criadas cópias locais dos materiais existentes em URP.

Não encontrados em Assets: **Energy Cells Free**, **Free Sockets and Switches**, **Planet Earth Free**, **Hand Painted Nature Kit LITE**. City People não é usado no ambiente desta etapa.

Arquitetura e elementos mecânicos vêm dos kits sci-fi; mobiliário e instrumentos vêm do Office; armários, paletes e contêineres industriais vêm de Storage. A câmera é decorativa. Natureza, Terra, personagens e variantes coloridas de um mesmo modelo não foram usados para aumentar a contagem.

Os materiais locais mantêm mapas de base, normal, emissão, metal e oclusão quando disponíveis. Vidros Standard transparentes são adaptados para URP transparente. Materiais originais permanecem preservados. Algumas superfícies dos kits são deliberadamente simples e os objetos Storage têm acabamento low poly.
