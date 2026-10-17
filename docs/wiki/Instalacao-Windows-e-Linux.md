# 📦 Guia de Instalação — Windows & Linux

💬 **Comunidade no Discord:** [https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)

O **NRBlox** possui dois launchers independentes, projetados especificamente para cada sistema operacional.

---

## 🪟 1. Instalação e Uso no Windows (`NRBlox.exe`)

O launcher Windows é um executável **100% nativo em C# / WPF**, sem necessidade de instalar runtimes externos pesados ou abrir abas de navegador.

### Executando o NRBlox
1. Abra diretamente o executável **`NRBlox.exe`** na raiz do projeto (ou em `windows/bin/NRBlox.exe`).
2. Na aba **`🛠️ Sistema & Sobre`**, clique em:
   - **Registrar Protocolos**: Registra `roblox-player://`, `roblox://` e `nrblox://` no Registro do Windows (`HKCU\Software\Classes`) para que o botão **Play** do site do Roblox inicie sempre através do NRBlox.
   - **Criar Atalhos**: Cria os atalhos `NRBlox` e `Jogar Roblox (NRBlox)` na sua Área de Trabalho.

### Compilando a partir do Código-Fonte no Windows
O NRBlox utiliza o compilador nativo do Windows (`csc.exe` + assemblies WPF):
```powershell
powershell -ExecutionPolicy Bypass -File .\windows\build-windows.ps1
```

### Argumentos de Linha de Comando (Windows)
- `NRBlox.exe` — Abre o painel de controle nativo do NRBlox (Dark/Light Mode).
- `NRBlox.exe -player` — Inicia diretamente o cliente Roblox com a janela do Bootstrapper.
- `NRBlox.exe -studio` — Inicia diretamente o Roblox Studio.
- `NRBlox.exe --verify` — Exibe o relatório de integridade da instalação no console.
- `NRBlox.exe --register-protocols` — Registra silenciosamente os handlers de protocolo da web.

---

## 🐧 2. Instalação e Uso no Linux (`nrblox-linux`)

O launcher Linux (`linux/nrblox-linux`) é uma aplicação nativa desenvolvida especificamente para distribuições Linux (**X11 e Wayland**), integrando-se diretamente com **Sober (Flatpak)**, **Vinegar** e **Wine64 / Proton-GE**.

### Instalação Automática (XDG Desktop + MIME Protocols)
```bash
cd linux
chmod +x install-linux.sh nrblox-linux
./install-linux.sh
```
O instalador:
1. Copia o motor do NRBlox Linux para `~/.local/share/nrblox-linux`.
2. Cria o comando global `~/.local/bin/nrblox-linux`.
3. Registra o arquivo `.desktop` em `~/.local/share/applications/nrblox-linux.desktop` e associa os protocolos `x-scheme-handler/roblox-player`, `x-scheme-handler/roblox` e `x-scheme-handler/nrblox` via `xdg-mime`.

### Runners Suportados no Linux
- **Sober (`org.vinegarhq.Sober` via Flatpak)**: Recomendado para máxima performance em jogos. O NRBlox sincroniza automaticamente suas FastFlags em `~/.var/app/org.vinegarhq.Sober/config/sober/config.json`.
- **Vinegar**: Suporte completo para Player e Roblox Studio via Wine-GE.
- **Prefixo Wine64 / Proton Customizado**: Permite rodar diretamente a pasta `version-11f155f3450f4c8e` com `WINEESYNC=1`, `WINEFSYNC=1`, `gamemoderun` e `mangohud`.
