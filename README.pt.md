# My LifeCam Controller

[English](README.md) · **Português**

App para Windows que controla todas as definições de uma webcam (foco, exposição, zoom, brilho, balanço de brancos…), feita para a **Microsoft LifeCam Studio** mas compatível com qualquer webcam USB (UVC).

Nasceu de um problema concreto: o autofoco da LifeCam Studio "procura" o foco e a imagem fica desfocada de vez em quando. Com esta app desligas o **Auto** do foco, fixas o valor à mão e ele fica guardado.

## Download

Descarrega o `MyLifeCamController.exe` da [última versão](https://github.com/V-1per/MyLifeCamController/releases/latest). É um ficheiro único, sem instalação. Precisa do [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0).

## O que faz

- Lista as câmaras ligadas e mostra **todos os controlos que a câmara suporta**, com slider e caixa **Auto** quando existe modo automático.
- **Pré-visualização** ao vivo (se outra app estiver a usar a câmara, os controlos continuam a funcionar).
- Mostra em tempo real o que os modos automáticos estão a fazer (por exemplo, o valor do foco a mudar).
- **Guarda automaticamente** as alterações por câmara e volta a aplicá-las quando abres a app.
- **Aplicar ao iniciar o Windows**: a LifeCam volta ao autofoco quando reinicias o PC; esta opção reaplica as tuas definições em cada arranque.
- Botão **Definições do driver…** que abre a janela nativa do driver.
- **Repor predefinições** volta tudo aos valores de fábrica.
- **Português / English**: escolhe o idioma no canto superior (por omissão segue o idioma do Windows).

## Requisitos

- Windows 10/11
- [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (para correr)
- .NET 9 SDK (só para compilar)

## Compilar e correr

```powershell
dotnet build -c Release
dotnet run --project src/MyLifeCamController -c Release
```

Gerar um executável único em `publish/`:

```powershell
dotnet publish src/MyLifeCamController -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
```

> Ativa "Aplicar ao iniciar o Windows" a partir do executável na pasta onde ele vai ficar: o arranque guarda o caminho do executável.

## Testes

```powershell
dotnet test                                  # todos
dotnet test --filter Category!=Hardware      # só os que não precisam de câmara
```

Os testes de hardware usam a câmara real: leem os controlos e mudam o brilho por um instante, repondo o valor original no fim. Se não houver câmara, passam sem fazer nada.

## Linha de comandos

| Comando | O que faz |
| --- | --- |
| `MyLifeCamController.exe` | Abre a janela. |
| `MyLifeCamController.exe --apply` | Aplica as definições guardadas a todas as câmaras ligadas, sem janela (espera até 30 s que a câmara apareça). Escreve um registo em `%APPDATA%\MyLifeCamController\apply.log`. |

## Como funciona

A app fala diretamente com o driver através das interfaces DirectShow `IAMCameraControl` (foco, exposição, zoom, pan, tilt…) e `IAMVideoProcAmp` (brilho, contraste, balanço de brancos…). A interop COM está escrita à mão em `src/MyLifeCamController.Core/DirectShow/Interop.cs`, sem bibliotecas externas.

```
src/
  MyLifeCamController.Core/   lógica sem interface: interop, câmara, pré-visualização, definições
  MyLifeCamController/        app WinForms (janela, linhas de controlo, modo --apply, arranque)
tests/
  MyLifeCamController.Tests/  testes xUnit (lógica pura + câmara real)
```

As definições ficam em `%APPDATA%\MyLifeCamController\settings.json`. Só são guardados os controlos que mudaste.

## Limitações conhecidas

- Algumas apps (Discord, browsers) podem repor o autofoco quando começam a usar a câmara. Se acontecer, abre a app e as tuas definições voltam a ser aplicadas.
- As definições são guardadas por porta USB: se ligares a câmara a outra porta, o Windows vê-a como outro dispositivo.

## Inspiração

- [WebCam-Settings](https://github.com/Simon311/WebCam-Settings): janela nativa do driver
- [CameraControlLib](https://github.com/marcellarius/CameraControlLib): controlo via DirectShow
- [cfg-cam](https://github.com/SuslikV/cfg-cam): guardar e repor definições
