# 📡 Monitor de Servidores, Geolocalização IP & Discord RPC

💬 **Comunidade no Discord:** [https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)

O **NRBlox** conta com um motor integrado de telemetria de sessão em tempo real (`ActivityWatcherService`), permitindo saber exatamente em qual servidor você está conectado, sua localização física e reentrar na mesma instância com 1 clique.

---

## 🌍 1. Geolocalização de Servidores UDMUX
Ao entrar em uma experiência, o NRBlox lê em tempo real o arquivo de log ativo do cliente (`%LocalAppData%\Roblox\logs` no Windows ou `~/.local/share/nrblox-linux/prefix/drive_c/users/$USER/AppData/Local/Roblox/logs` no Linux) e extrai:
- **`PlaceId`** (ID do jogo)
- **`JobId`** (UUID da instância do servidor)
- **`Server IP` & `Port`** (Endereço UDMUX da instância)

Com o IP do servidor em mãos, o NRBlox consulta a localização geográfica (`Cidade`, `Estado/Região` e `País`), permitindo verificar se você caiu em um servidor próximo (ex: São Paulo, Miami, New Jersey, Frankfurt).

---

## 🔄 2. Histórico de Servidores & Reconexão Rápida
Todas as sessões recentes são salvas automaticamente em `ServerHistory.json`. Na aba **`📡 Servidor & RPC`**, você pode clicar no botão **`Reentrar`** de qualquer servidor listado para abrir diretamente a mesma instância (`roblox://experiences/start?placeId=...&gameInstanceId=...`).

---

## 💬 3. Discord Rich Presence Nativo (Zero Plugins)
O NRBlox possui seu próprio cliente IPC do Discord:
- **No Windows**: Conecta diretamente ao Named Pipe `\\.\pipe\discord-ipc-0`..`9`.
- **No Linux**: Conecta diretamente ao Unix Domain Socket `$XDG_RUNTIME_DIR/discord-ipc-0` (com suporte automático a Discord Flatpak e Vesktop).

---

## 🧩 4. Integrações Externas (Auto-Launch)
Na seção **Integrações Externas**, você pode cadastrar programas auxiliares (como overlays de FPS, contadores de ping ou utilitários de captura) para serem iniciados automaticamente sempre que o Roblox for aberto pelo NRBlox.
