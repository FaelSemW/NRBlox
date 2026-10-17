# ⚡ NRBlox — Next-Gen Open-Source Roblox Engine Controller

<p align="center">
  <a href="https://discord.gg/M8j8JnQSCx"><img src="https://img.shields.io/badge/Discord-Comunidade%20Oficial-5865F2?style=for-the-badge&logo=discord&logoColor=white" alt="Discord Oficial"></a>
  <a href="https://github.com/FaelSemW/NRBlox/blob/main/docs/wiki/Home.md"><img src="https://img.shields.io/badge/GitHub%20Wiki-Documenta%C3%A7%C3%A3o%20Completa-00E5FF?style=for-the-badge&logo=github&logoColor=black" alt="Wiki Oficial"></a>
  <img src="https://img.shields.io/badge/Windows-WPF%20Nativo%20(.exe)-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows WPF">
  <img src="https://img.shields.io/badge/Linux-Sober%20%7C%20Vinegar%20%7C%20Wine64-FCC624?style=for-the-badge&logo=linux&logoColor=black" alt="Linux Engine">
  <img src="https://img.shields.io/badge/Licen%C3%A7a-MIT%20(100%25%20Open%20Source)-10B981?style=for-the-badge" alt="MIT License">
</p>

O **NRBlox** é uma plataforma 100% open-source de inicialização, gerenciamento de **FastFlags**, aplicação de **Mods** e monitoramento de servidores em tempo real para o Roblox, composta por **dois launchers nativos independentes e projetados especificamente para cada sistema operacional**:

- 🪟 **NRBlox Windows Edition (`NRBlox.exe` em [`windows/`](./windows))**: Aplicativo Desktop **100% nativo em C# / WPF** (sem abas de navegador ou Electron), com interface **Bento Grid**, alternância em tempo real entre **Modo Escuro (Dark Mode)** e **Modo Claro (Light Mode)**, janela própria de Bootstrapper, Multi-Instância (`ROBLOX_singletonMutex`), geolocalização de servidores e Discord Rich Presence via Named Pipe.
- 🐧 **NRBlox Linux Edition (`nrblox-linux` em [`linux/`](./linux))**: Launcher Desktop **100% nativo para Linux (X11 & Wayland)**, construído para gerenciar **Sober (Flatpak)**, **Vinegar** e **Prefixos Wine64 / Proton-GE**, com seleção de driver **Vulkan ICD (`RADV`, `ANV`, `NVIDIA`)**, **Feral GameMode (`gamemoderun`)**, **MangoHud**, **Gamescope**, **Esync/Fsync**, e Discord Rich Presence via socket `AF_UNIX`.

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

### 🪟 No Windows (`NRBlox.exe`)
1. Execute diretamente **`NRBlox.exe`** na raiz do repositório.
2. Alterne entre **🌙 Modo Escuro** e **☀️ Modo Claro** diretamente no topo da janela nativa.
3. Para compilar o executável nativo WPF a partir do código-fonte C#:
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\windows\build-windows.ps1
   ```

### 🐧 No Linux (`nrblox-linux`)
1. Execute diretamente o launcher nativo para Linux:
   ```bash
   chmod +x linux/nrblox-linux
   ./linux/nrblox-linux
   ```
2. Ou instale no sistema com integração completa de menu e protocolos `roblox-player://` (XDG):
   ```bash
   cd linux
   chmod +x install-linux.sh
   ./install-linux.sh
   ```

---

## 📂 Estrutura do Repositório

```text
NRBlox/
├── NRBlox.exe                           # Launcher Nativo Windows (C# WPF Desktop)
├── windows/                             # Código-Fonte Exclusivo do Launcher Windows (C# / WPF)
│   ├── build-windows.ps1                # Script de compilação nativa via csc.exe + WPF
│   └── src/
│       ├── Program.cs                   # Entry point Windows & CLI
│       ├── Core/
│       │   ├── NRBloxConfig.cs          # Configurações, FastFlagEngine & ModEngine (.nrblox_orig)
│       │   └── NRBloxServices.cs        # Discord RPC Named Pipe, ActivityWatcher & Registry
│       └── UI/
│           ├── NRBloxTheme.cs           # Motor de Temas Dinâmicos (Dark Mode & Light Mode)
│           ├── NRBloxBootstrapperWindow.cs # Janela Nativa do Bootstrapper
│           └── NRBloxMainWindow.cs      # Interface Principal Bento Grid em WPF
├── linux/                               # Código-Fonte Exclusivo do Launcher Linux (X11/Wayland)
│   ├── nrblox-linux                     # Executável do Launcher Linux
│   ├── install-linux.sh                 # Instalador XDG Desktop & MIME Protocols
│   ├── nrblox-linux.desktop             # Entrada Freedesktop (.desktop)
│   └── src/
│       └── nrblox_linux_app.py          # Interface Nativa Linux (Dark/Light) + Sober/Vinegar/Wine64
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
