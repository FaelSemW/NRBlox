# 📦 Guia de Instalação — Windows & Linux

💬 **Comunidade no Discord:** [https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)

O **NRBlox** possui dois launchers independentes, projetados especificamente para cada sistema operacional.

---

## 🪟 1. Instalação e Uso no Windows (`NRBlox.exe`)

O launcher Windows é um executável **100% nativo em C# / WPF**, sem necessidade de instalar runtimes externos pesados ou abrir abas de navegador.

### Executando o NRBlox
1. Abra diretamente o executável **`NRBlox.exe`** na raiz do projeto (ou em `windows/bin/NRBlox.exe`).
2. Na janela inicial (**Launch Menu**), você pode:
   - **Launch Roblox**: Inicia o Roblox imediatamente. Caso o Roblox não esteja instalado no PC (ou se **Force Roblox reinstallation** estiver marcado na aba *Bootstrapper*), o NRBlox consulta `clientsettingscdn.roblox.com`, baixa automaticamente os 21 pacotes `.zip` oficiais de `setup.rbxcdn.com` com verificação de cache MD5, extrai tudo em `%LocalAppData%\Roblox\Versions\version-...`, aplica suas FastFlags e Mods e abre o jogo.
   - **Launch Roblox Studio**: Inicia ou instala automaticamente o Roblox Studio.
   - **Configure settings**: Abre o painel completo de configurações (`Integrations`, `Bootstrapper`, `Mods`, `Engine Settings`, `Appearance`, `Shortcuts`, `NRBlox` e `About`).

### Compilando a partir do Código-Fonte no Windows
O NRBlox utiliza o compilador nativo do Windows (`csc.exe` + assemblies WPF):
```powershell
powershell -ExecutionPolicy Bypass -File .\windows\build-windows.ps1
```

### Argumentos de Linha de Comando (Windows)
- `NRBlox.exe` — Abre o menu de inicialização rápida do NRBlox (**Launch Menu**).
- `NRBlox.exe -settings` — Abre diretamente a janela de configurações (**Settings Window**).
- `NRBlox.exe -player` — Inicia diretamente o cliente Roblox com a janela do Bootstrapper (baixando/extraindo automaticamente caso ausente).
- `NRBlox.exe -studio` — Inicia diretamente o Roblox Studio.
- `NRBlox.exe --install-player` — Baixa, valida (MD5) e extrai todos os pacotes da versão atual do Roblox via linha de comando.
- `NRBlox.exe --verify` — Exibe o relatório de integridade da instalação no console.
- `NRBlox.exe --register-protocols` — Registra silenciosamente os handlers de protocolo `roblox-player:`, `roblox:` e `nrblox:` no Registro do Windows.

---

## 🐧 2. Instalação e Uso no Linux (Via CLI em 1 Comando)

No Linux (**X11 e Wayland**), a instalação é feita diretamente via **CLI** com um único comando no terminal. Ele configura tudo sozinho e já abre a janela do launcher (com o mesmo layout de 2 janelas do Windows + janela de Bootstrapper):

```bash
curl -fsSL https://raw.githubusercontent.com/FaelSemW/NRBlox/main/linux/install-linux.sh | bash
```

### O que o instalador CLI faz automaticamente:
1. Detecta sua distribuição (`apt`, `pacman`, `dnf` ou `zypper`) e garante que `python3`, `python3-tk`, `gcc`, `mingw-w64`, `wine64`, `curl` e `xdg-utils` estejam instalados.
2. Compila localmente o runner nativo em C **`nrblox-core`** (`linux/runtime/nrblox_linux_core.c`) e a biblioteca open-source **`WebView2Loader.dll` & NTDLL Cloak** (`linux/runtime/nrblox_wine_cloak.cpp`).
3. Baixa o motor do NRBlox Linux para `~/.local/share/nrblox-linux` e cria os comandos globais `nrblox` e `nrblox-linux` em `~/.local/bin`.
4. Cria o atalho no menu de aplicativos (`~/.local/share/applications/nrblox-linux.desktop`) e na Área de Trabalho, registrando os protocolos `roblox-player://`, `roblox://` e `nrblox://` via `xdg-mime`.
5. **Abre automaticamente a janela do NRBlox** assim que a instalação termina!

### Como o NRBlox Linux Baixa e Executa o Jogo
- **Instalador Direto da CDN (`RobloxLinuxCdnInstaller`)**: Ao clicar em **Launch Roblox**, se o cliente ainda não estiver instalado em `~/.local/share/nrblox-linux/Versions/version-...`, a janela do Bootstrapper do NRBlox Linux baixa e extrai automaticamente os **21 pacotes `.zip` oficiais** direto de `setup.rbxcdn.com` com verificação de hash MD5!
- **Runtime Nativo Próprio (`nrblox-core` + `WebView2Loader.dll` Open-Source)**: Gerencia um prefixo isolado (`~/.local/share/nrblox-linux/prefix`), injeta o `WebView2Loader.dll` open-source, limpa chaves de fingerprint do registro (`HKLM\Software\Wine`) e inicia o cliente com `WINEESYNC=1`, `WINEFSYNC=1`, `Vulkan / DXVK`, `gamemoderun` e `mangohud`.
