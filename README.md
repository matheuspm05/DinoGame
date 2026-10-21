# Ecos do Cretáceo: Projeto Retorno

Projeto acadêmico em Unity 6, URP e novo Input System.

## Como baixar e jogar

1. Instale o **Git**, o **Git LFS** e o **Unity Hub**.
2. Pelo Unity Hub, instale a Unity **6000.0.81f1**.
3. Abra o terminal e execute:

   ```bash
   git lfs install
   git clone https://github.com/matheuspm05/DinoGame.git
   cd DinoGame
   git lfs pull
   ```

4. Aguarde o download terminar. No Unity Hub, adicione a pasta `DinoGame` e abra o projeto.
5. Aguarde a importação e abra `Assets/Scenes/MainMenu.unity`.
6. Pressione **Play** e clique em **Novo Jogo**.

## Documentação

- [Gameplay do Nível 1: puzzles, itens e testes](Docs/Guias/Gameplay_Nivel01.md)
- [Mapa da documentação](Docs/README.md)
- [Para que serve cada script](Docs/Guias/Scripts.md)
- [Como usar o nível expandido](Docs/Guias/Level01_Expanded_ComoUsar.md)

Código próprio em `Assets/Scripts`; pacotes externos em `Assets/ThirdParty`. Cenas, prefabs, materiais, shaders e entrada permanecem em suas pastas próprias dentro de `Assets`.

`Packages` e `ProjectSettings` fazem parte da estrutura da Unity. `Library`, `Temp`, `Logs`, `obj`, `UserSettings` e os arquivos de solução/projeto são gerenciados pelas ferramentas e não foram reorganizados.
