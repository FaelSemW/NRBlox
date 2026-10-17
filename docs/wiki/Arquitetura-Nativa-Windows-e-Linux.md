# 🏗️ Arquitetura Nativa — Windows WPF, Linux Engine & Runtime Open-Source

💬 **Comunidade no Discord:** [https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)

O **NRBlox** foi projetado com código 100% aberto sob a licença MIT, separando completamente o launcher do Windows e o launcher do Linux para respeitar as particularidades de cada sistema operacional.

---

## 🪟 1. Arquitetura do Launcher Windows (`windows/`)
Construído inteiramente em **C# com Windows Presentation Foundation (`PresentationFramework.dll`, `PresentationCore.dll`, `WindowsBase.dll`)**:
- **`windows/src/Core/NRBloxConfig.cs`**: Serializador JSON nativo, gerenciador de caminhos, `FastFlagEngine` e `ModEngine` com suporte a manifesto de backup `.nrblox_orig`.
- **`windows/src/Core/NRBloxServices.cs`**:
  - `NativeDiscordRpcClient`: Implementação binária do protocolo Discord IPC via `NamedPipeClientStream`.
  - `ActivityWatcherService`: Leitor assíncrono de logs com `FileShare.ReadWrite | FileShare.Delete` e resolução geográfica HTTPS.
  - `SystemIntegrationService`: Controle do mutex `ROBLOX_singletonMutex` para Multi-Instância, registro de protocolos em `HKCU\Software\Classes` e verificação de integridade.
- **`windows/src/UI/NRBloxTheme.cs`**: Motor de temas dinâmicos (`ResourceDictionary`) com suporte em tempo real a **Obsidian Dark Mode** e **Alabaster Light Mode**.
- **`windows/src/UI/NRBloxBootstrapperWindow.cs`**: Janela nativa de inicialização do cliente.
- **`windows/src/UI/NRBloxMainWindow.cs`**: Interface principal em estilo Bento Grid com barra de comando horizontal.

---

## 🐧 2. Arquitetura do Launcher & Runtime Linux (`linux/`)
Desenvolvido especificamente para o ecossistema Linux (**X11 / Wayland**) sem depender de wrappers de terceiros:
- **`linux/runtime/nrblox_linux_core.c`**: Binário nativo em C (`nrblox-core`) que isola o prefixo `~/.local/share/nrblox-linux/prefix`, configura `WINEESYNC=1`, `WINEFSYNC=1`, `Vulkan / DXVK` e despacha `RobloxPlayerBeta.exe` diretamente da pasta da versão.
- **`linux/runtime/nrblox_wine_cloak.cpp`**: Implementação open-source em C++ do `WebView2Loader.dll` combinada com normalização de variáveis de ambiente e registro Windows 10 Build 19045.
- **`linux/src/nrblox_linux_app.py`**:
  - `RobloxLinuxCdnInstaller`: Baixa e extrai os 21 pacotes `.zip` oficiais direto de `setup.rbxcdn.com` com verificação de cache MD5.
  - `LinuxEngineController`: Sincroniza `ClientAppSettings.json`, aplica o NTDLL/Registry Cloak e coordena `gamemoderun` e `mangohud`.
  - `NRBloxLinuxApp`: Interface Fluent de 2 janelas (**Launch Menu** + **Settings Window**) e janela de **Bootstrapper** com barra de progresso em MB em tempo real, com suporte a **Light Mode** e **Dark Mode**.

---

## 🧬 3. Runtime Open-Source para `version-11f155f3450f4c8e` (`roblox-version-11f155f3450f4c8e/`)
- **`NRBloxPlayerLauncher.exe` (`open-source-runtime/NRBloxPlayerLauncher.cs`)**: Substituto open-source direto para o lançador padrão da pasta `version-11f155f3450f4c8e`, garantindo que `AppSettings.xml` e `ClientSettings\ClientAppSettings.json` estejam sempre íntegros antes de iniciar `RobloxPlayerBeta.exe`.
- **`open-source-runtime/WebView2Loader_OpenSource.cpp`**: Implementação C++ open-source das 5 funções exportadas da biblioteca `WebView2Loader.dll` (`CompareBrowserVersions`, `CreateCoreWebView2Environment`, `CreateCoreWebView2EnvironmentWithOptions`, `GetAvailableCoreWebView2BrowserVersionString`, `GetAvailableCoreWebView2BrowserVersionStringWithOptions`), eliminando falhas de inicialização em ambientes sem EdgeUpdate ou sob Wine/Proton no Linux.
