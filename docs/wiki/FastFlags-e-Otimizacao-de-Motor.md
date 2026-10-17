# 🚀 FastFlags & Otimização Gráfica do Motor

💬 **Comunidade no Discord:** [https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)

O **NRBlox** gerencia de forma segura e transacional o arquivo `ClientSettings\ClientAppSettings.json` dentro da versão ativa do cliente (`%LocalAppData%\Roblox\Versions\version-...` no Windows e `~/.local/share/nrblox-linux/Versions/version-...` no Linux).

---

## ⚡ Presets Integrados na Interface

| Recurso | FastFlag Subjacente | Descrição |
| :--- | :--- | :--- |
| **Desbloqueador de FPS** | `DFIntTaskSchedulerTargetFps` | Define o limite máximo de quadros do Task Scheduler (`60`, `120`, `144`, `165`, `240`, `360`, `999`). |
| **API Gráfica Direct3D 11** | `FFlagDebugGraphicsPreferD3D11` | Força o backend nativo Direct3D 11 no Windows. |
| **API Gráfica Direct3D 10** | `FFlagDebugGraphicsPreferD3D11FL10` | Usa Feature Level 10 para GPUs integradas mais antigas. |
| **API Gráfica Vulkan** | `FFlagDebugGraphicsPreferVulkan` + `FFlagDebugGraphicsDisableDirect3D11` | Ativa o renderizador Vulkan de baixa sobrecarga (ideal para GPUs modernas e para Linux RADV/ANV/NVIDIA). |
| **API Gráfica OpenGL** | `FFlagDebugGraphicsPreferOpenGL` | Renderizador alternativo OpenGL. |
| **Iluminação Voxel (Fase 1)** | `DFFlagDebugRenderForceTechnologyVoxel` | Força iluminação em grade Voxel para ganho máximo de FPS. |
| **Iluminação ShadowMap (Fase 2)** | `FFlagDebugForceFutureIsBrightPhase2` | Equilíbrio entre sombras dinâmicas e desempenho. |
| **Iluminação Future (Fase 3)** | `FFlagDebugForceFutureIsBrightPhase3` | Ativa reflexos especulares e iluminação física avançada. |
| **Anti-Aliasing (MSAA)** | `FIntDebugForceMSAASamples` | Força amostragem MSAA em `1x`, `2x`, `4x` ou `8x`. |
| **Qualidade de Texturas** | `DFFlagTextureQualityOverrideEnabled` + `DFIntTextureQualityOverride` | Fixa o nível de resolução das texturas (`0` Baixa até `3` Ultra). |
| **Correção de Escala DPI** | `DFFlagDisableDPIScale` | Impede que o Windows aplique desfoque por escalonamento em telas de alta resolução. |
| **Tela Cheia Alt+Enter** | `FFlagHandleAltEnterFullscreenManually` | Habilita o modo de tela cheia exclusivo ao pressionar `Alt + Enter`. |
| **Bloqueio de Telemetria** | `FFlagDebugDisableTelemetryEphemeralCounter`, `EventIngest`, `Point`, `V2Counter` | Desativa o envio de contadores efêmeros de telemetria durante o jogo. |

---

## 🛠️ Editor Customizado de FastFlags

Na aba **FastFlags**, você também pode:
- Adicionar qualquer flag customizada informando o nome (`FFlag...`, `DFFlag...`, `FInt...`, `DFInt...`, `FString...`) e seu valor.
- Remover flags individualmente com 1 clique.
- Abrir e editar diretamente o arquivo `ClientAppSettings.json`.
