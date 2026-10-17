# ⚡ Bem-vindo à Wiki Oficial do NRBlox

**Desenvolvido por [FaelSemW](https://github.com/FaelSemW) • Licença MIT (100% Open Source)**  
💬 **Comunidade Oficial no Discord:** [https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)

---

## O que é o NRBlox?

O **NRBlox** é um ecossistema de inicialização, otimização de motor (`FastFlags`), gerenciamento de modificações (`Mods`) e telemetria de servidores para o cliente Roblox, construído com **dois launchers nativos independentes e dedicados para cada sistema operacional**:

1. **🪟 NRBlox Windows Edition (`NRBlox.exe`)**:
   - Aplicativo Desktop **100% Nativo em C# / WPF** (zero navegador, zero Electron).
   - Interface **Bento Grid** com alternância instantânea entre **Modo Escuro (Dark Mode)** e **Modo Claro (Light Mode)**.
   - Janela nativa de Bootstrapper, bloqueio de versão (`version-11f155f3450f4c8e`), suporte a **Múltiplas Instâncias (`ROBLOX_singletonMutex`)**, injeção direta em `ClientAppSettings.json`, geolocalização de servidores UDMUX e Discord Rich Presence via Named Pipe.

2. **🐧 NRBlox Linux Edition (`nrblox-linux`)**:
   - Launcher Desktop **100% Nativo para Linux (X11 & Wayland)**.
   - Gerenciamento dedicado de runners Linux: **Sober (`org.vinegarhq.Sober` Flatpak)**, **Vinegar** e **Prefixos Wine64 / Proton-GE**.
   - Controle direto do ambiente gráfico e kernel Linux: seleção de driver **Vulkan ICD (`AMD RADV`, `Intel ANV`, `NVIDIA`)**, **Feral GameMode (`gamemoderun`)**, **MangoHud (`mangohud --dlsym`)**, **Gamescope**, **Esync/Fsync** e **NVIDIA PRIME Render Offload**.
   - Sincronização simultânea de FastFlags e Asset Overlays com o `config.json` do Sober e `ClientAppSettings.json` do Wine, além de Discord Rich Presence via socket `AF_UNIX`.

---

## 📚 Índice da Wiki

1. [**Guia de Instalação (Windows & Linux)**](./Instalacao-Windows-e-Linux.md)
2. [**FastFlags & Otimização Gráfica do Motor**](./FastFlags-e-Otimizacao-de-Motor.md)
3. [**Mods, Cursores Clássicos, Sons e Fontes**](./Mods-Cursores-Sons-e-Fontes.md)
4. [**Monitor de Servidores, Geolocalização IP & Discord RPC**](./Monitor-de-Servidores-e-Discord-RPC.md)
5. [**Arquitetura Nativa (Windows WPF + Linux Engine + Open-Source Runtime)**](./Arquitetura-Nativa-Windows-e-Linux.md)

---

## 💬 Comunidade & Suporte

Para dúvidas, sugestões de novas FastFlags, presets visuais e atualizações do projeto, entre no nosso servidor oficial do Discord:
👉 **[Entrar na Comunidade NRBlox no Discord](https://discord.gg/M8j8JnQSCx)**
