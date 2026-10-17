# 🎨 Mods, Cursores Clássicos, Sons e Fontes

💬 **Comunidade no Discord:** [https://discord.gg/M8j8JnQSCx](https://discord.gg/M8j8JnQSCx)

O **NRBlox** inclui um gerenciador de modificações com **backup transacional automático (`.nrblox_orig`)**. Isso significa que você pode ativar e desativar qualquer modificação a qualquer momento sem corromper os arquivos originais do cliente.

---

## 🖱️ 1. Cursores Clássicos (`From2006` e `From2013`)
Substitui automaticamente `content/textures/Cursors/KeyboardMouse/ArrowCursor.png` e `ArrowFarCursor.png`:
- **`Default`**: Mantém o cursor moderno padrão do cliente.
- **`From2006`**: Restaura o cursor clássico chanfrado de 2006.
- **`From2013`**: Restaura o cursor clássico de 2013.

---

## 🔊 2. Sons Clássicos de Personagem (2013)
Quando ativado, o NRBlox substitui os arquivos de áudio em `content/sounds/`:
- `action_footsteps_plastic.mp3` ⬅ `OldWalk.mp3`
- `action_jump.mp3` ⬅ `OldJump.mp3`
- `action_get_up.mp3` ⬅ `OldGetUp.mp3`
- `action_falling.mp3`, `action_jump_land.mp3`, `action_swim.mp3`, `impact_water.mp3` ⬅ `Empty.mp3`

---

## 🏛️ 3. Cenário Clássico do Editor de Avatar
Quando ativado, injeta `OldAvatarBackground.rbxl` em `ExtraContent/places/Mobile.rbxl`, restaurando o clássico cenário iluminado ao visualizar ou editar seu personagem.

---

## 🔤 4. Fontes Tipográficas Customizadas (`.ttf` / `.otf`)
Você pode selecionar qualquer arquivo de fonte `.ttf` ou `.otf` do seu computador para substituir a tipografia padrão da interface do cliente, e restaurar a fonte original a qualquer momento com 1 clique.

---

## 📂 5. Pasta de Modificações Customizadas (`data/Modifications`)
Qualquer textura, som ou asset colocado dentro de `data/Modifications/` (no Windows) ou `~/.local/share/nrblox-linux/modifications/` (no Linux) seguindo a hierarquia de pastas do cliente (`content/...`, `ExtraContent/...`) é sincronizado automaticamente antes de cada inicialização.
