# My LifeCam Controller: design

**Data:** 2026-11-05
**Objetivo:** app Windows com painel completo de controlos da webcam (Microsoft LifeCam Studio), sobretudo para fixar o foco, que tem tendência a desfocar em modo automático.

## Decisões

| Decisão | Escolha | Porquê |
| --- | --- | --- |
| Linguagem/UI | C# + WinForms, .NET 9 | O runtime Desktop 9 já está instalado; acesso COM nativo; sem dependências. |
| API da câmara | DirectShow `IAMCameraControl` + `IAMVideoProcAmp` | Expõe todos os controlos UVC (incluindo foco) e funciona mesmo com a câmara em uso por outra app. A API WinRT `MediaCapture` precisa de acesso exclusivo para mudar definições. |
| Interop | Escrita à mão (sem DirectShow.NET) | Só precisamos de ~10 interfaces, e a biblioteca está abandonada. |
| Pré-visualização | Grafo DirectShow (CaptureGraphBuilder2 + Video Renderer) dentro de um Panel | Sem dependências; falha de forma controlada se a câmara estiver ocupada. |
| Persistência | JSON em `%APPDATA%`, só os controlos alterados pelo utilizador | Não sobrepõe valores que o utilizador nunca mexeu. |
| Arranque | Chave `HKCU\...\Run` com `--apply` (sem janela, tenta durante 30 s) | Resolve a LifeCam voltar ao autofoco após reiniciar; não precisa de admin. |

## Componentes

- **Core/DirectShow/Interop.cs**: declarações COM.
- **Core/Camera**: `CameraDeviceInfo` (enumerar câmaras e abrir o filtro), `CameraSession` (ler/escrever controlos, janela do driver), `PreviewGraph`, `PropertyCatalog`/`PropertyMath` (lógica pura).
- **Core/Settings**: `SettingsStore` (JSON atómico, tolerante a ficheiros corrompidos), `SettingsApplier` (aplica valores; escreve manual primeiro e depois ativa o auto).
- **App**: `MainForm`, `PropertyRow`, `ApplyMode` (`--apply`), `StartupRegistration`.

## Erros

- Câmara ocupada: a pré-visualização mostra uma mensagem e os controlos continuam ativos.
- Valor recusado pelo driver: mensagem na barra de estado, sem crash.
- Câmara desligada: o polling para e a app pede para atualizar a lista.
- Ficheiro de definições corrompido: começa com definições vazias.

## Testes

- Unitários: `Snap`, formatação, catálogo, persistência e applier (com um alvo falso).
- Hardware (`Category=Hardware`): enumerar, ler todos os controlos dentro do intervalo, mudar e repor o brilho. Passam sem fazer nada quando não há câmara.

## Fora do âmbito (v1)

Perfis múltiplos, ícone na área de notificação, reaplicar automaticamente quando outra app repõe o autofoco.
