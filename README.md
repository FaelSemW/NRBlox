# ⚡ NRBlox — Next-Gen Open-Source Roblox Engine Controller

<p align="center">
  <a href="https://github.com/FaelSemW/NRBlox/releases/download/v3.0.0/NRBlox.exe"><img src="https://img.shields.io/badge/Download-NRBlox.exe%20(v3.0.0)-2EA44F?style=for-the-badge&logo=windows&logoColor=white" alt="Download NRBlox.exe"></a>
  <a href="https://discord.gg/M8j8JnQSCx"><img src="https://img.shields.io/badge/Discord-Comunidade%20Oficial-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Discord Oficial"></a>
  <a href="https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Home.md"><img src="https://img.shields.io/badge/GitHub%20Wiki-Documenta%C3%A7%C3%A3o%20Completa-00E5FF?style=for-the-badge&logo=github&logoColor=black" alt="Wiki Oficial"></a>
  <img src="https://img.shields.io/badge/Linux-NRBlox%20Native%20Core%20(C%2FC%2B%2B)-FCC624?style=for-the-badge&logo=linux&logoColor=black" alt="Linux Engine">
  <img src="https://img.shields.io/badge/Licen%C3%A7a-MIT%20(100%25%20Open%20Source)-10B981?style=for-the-badge" alt="MIT License">
</p>

O **NRBlox** é uma plataforma 100% open-source de inicialização, gerenciamento de **FastFlags**, aplicação de **Mods** e monitoramento de servidores em tempo real para o Roblox, composta por **dois launchers nativos independentes e projetados especificamente para cada sistema operacional**:

- 🪟 **NRBlox Windows Edition (`NRBlox.exe` em [`windows/`](./windows))**: Aplicativo Desktop **100% nativo em C# / WPF** (sem abas de navegador ou Electron) com renderização segura via CPU (`RenderMode.SoftwareOnly`), layout Fluent em 2 janelas (**Launch Menu** + **Settings Window**), alternância entre **Light Mode** e **Dark Mode**, **Instalador/Atualizador Automático de Pacotes da CDN do Roblox** (`setup.rbxcdn.com` com cache MD5), Multi-Instância (`ROBLOX_singletonMutex`), geolocalização de servidores e Discord Rich Presence via Named Pipe.
- 🐧 **NRBlox Linux Edition (`nrblox-linux` + `nrblox-core` em [`linux/`](./linux))**: Launcher e Runtime **100% próprios para Linux (X11 & Wayland)**, sem dependência de wrappers de terceiros! Inclui o binário nativo em C **`nrblox-core`** ([`nrblox_linux_core.c`](./linux/runtime/nrblox_linux_core.c)), a reimplementação open-source em C++ do **`WebView2Loader.dll` & NTDLL/Registry Cloak** ([`nrblox_wine_cloak.cpp`](./linux/runtime/nrblox_wine_cloak.cpp)), e o **Instalador Direto da CDN do Roblox (`RobloxLinuxCdnInstaller`)** que baixa e extrai os 21 pacotes oficiais com barra de progresso em MB em tempo real, rodando com **Vulkan / DXVK**, **Esync/Fsync**, **Feral GameMode** e **MangoHud**.

---

## 📖 Wiki Oficial & Comunidade

Toda a documentação detalhada está disponível diretamente na nossa **Wiki no GitHub**:

- 🏠 [**Página Inicial da Wiki (Visão Geral)**](https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Home.md)
- 📦 [**Guia de Instalação (Windows & Linux)**](https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Instalacao-Windows-e-Linux.md)
- 🚀 [**FastFlags & Otimização Gráfica do Motor**](https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/FastFlags-e-Otimizacao-de-Motor.md)
- 🎨 [**Mods, Cursores Clássicos, Sons e Fontes**](https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Mods-Cursores-Sons-e-Fontes.md)
- 📡 [**Monitor de Servidores, Geolocalização IP & Discord RPC**](https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Monitor-de-Servidores-e-Discord-RPC.md)
- 🏗️ [**Arquitetura Nativa (Windows WPF + Linux Engine + Open-Source Runtime)**](https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Arquitetura-Nativa-Windows-e-Linux.md)

💬 **Participe da nossa Comunidade Oficial no Discord:**  
👉 **[https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)**

---

## 🚀 Como Usar

### 🪟 No Windows (`NRBlox.exe` — 1 Único Executável)
1. Baixe apenas o executável **[`NRBlox.exe` na página oficial de Releases](https://github.com/FaelSemW/NRBlox/releases/latest)** (ou execute `NRBlox.exe` na raiz do repositório).
2. Clique em **Launch Roblox** (se o Roblox ainda não estiver instalado no seu PC, o Bootstrapper do NRBlox baixa e extrai automaticamente todos os 21 pacotes oficiais da CDN do Roblox), **Launch Roblox Studio** ou **Configure settings**.
3. Para compilar o executável nativo WPF a partir do código-fonte C#:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\windows\build-windows.ps1
   ```

### 🐧 No Linux (Instalação Completa em 1 Comando via CLI)
Basta colar o comando abaixo no terminal. Ele instala as dependências nativas, compila o motor em C **`nrblox-core`** e o **`WebView2Loader.dll`** open-source em C++, registra os protocolos `roblox-player://`, cria o comando global `nrblox` e **já abre o launcher na tela com o mesmo layout do Windows**:
```bash
curl -fsSL https://raw.githubusercontent.com/FaelSemW/NRBlox/main/linux/install-linux.sh | bash
```
Depois de instalado, você pode abrir a qualquer momento pelo menu de aplicativos do sistema ou digitando no terminal:
```bash
nrblox
```

---

## 📂 Estrutura do Repositório

```text
NRBlox/
├── NRBlox.exe                           # Launcher Nativo Windows Standalone (C# WPF Desktop)
├── windows/                             # Código-Fonte Exclusivo do Launcher Windows (C# / WPF)
│   ├── build-windows.ps1                # Script de compilação nativa via csc.exe + WPF
│   └── src/
│       ├── Program.cs                   # Entry point Windows, RenderMode.SoftwareOnly & CLI
│       ├── Core/
│       │   ├── NRBloxConfig.cs          # Configurações, FastFlagEngine & ModEngine (.nrblox_orig)
│       │   └── NRBloxServices.cs        # RobloxPackageInstaller (CDN/MD5), Discord RPC & ActivityWatcher
│       └── UI/
│           ├── NRBloxTheme.cs           # Motor de Temas Fluent (Light Mode & Dark Mode)
│           ├── NRBloxMenuWindow.cs      # Janela Inicial de Menu Rápido (Launch / Studio / Settings / Wiki)
│           ├── NRBloxMainWindow.cs      # Janela Completa de Configurações (8 Abas + Test Mode)
│           └── NRBloxBootstrapperWindow.cs # Janela do Bootstrapper com Download/Extração Automática
├── linux/                               # Código-Fonte Exclusivo do Launcher & Runtime Linux
│   ├── nrblox-linux                     # Executável do Launcher Linux
│   ├── install-linux.sh                 # Instalador CLI de 1 Comando (Compila nrblox-core + WebView2Loader)
│   ├── nrblox-linux.desktop             # Entrada Freedesktop (.desktop)
│   ├── runtime/
│   │   ├── nrblox_linux_core.c          # Runner Nativo Linux em C (Prefixo Isolado, Vulkan/DXVK, Esync/Fsync)
│   │   └── nrblox_wine_cloak.cpp        # WebView2Loader.dll Open-Source & NTDLL/Environment Cloak em C++
│   └── src/
│       └── nrblox_linux_app.py          # Interface Fluent 2-Janelas + Instalador Direto da CDN do Roblox
├── roblox-version-11f155f3450f4c8e/     # Componentes Open-Source para a Versão Local do Cliente
│   ├── NRBloxPlayerLauncher.exe         # Lançador nativo open-source para version-11f155f3450f4c8e
│   └── open-source-runtime/
│       ├── NRBloxPlayerLauncher.cs      # Código-fonte C# do lançador
│       └── WebView2Loader_OpenSource.cpp # Implementação C++ open-source de WebView2Loader.dll
├── assets/                              # Ícones, Cursores (2006/2013), Sons Clássicos e Cenários
├── docs/wiki/                           # Páginas completas da Wiki Oficial
└── LICENSE                              # Licença MIT
```

---

## 👤 Autor

Projeto criado e mantido por **[FaelSemW](https://github.com/FaelSemW)**.  
Distribuído sob a [Licença MIT](./LICENSE).
