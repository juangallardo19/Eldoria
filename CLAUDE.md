# Eldoria — Contexto del Proyecto para Claude

> **CLAUDE.md está en:** `C:\Users\juang\Eldoria\CLAUDE.md` — leer siempre al iniciar conversación.
> **Regla de código:** Siempre que sea posible, aplicar patrones de diseño (Singleton, Observer,
> State, Command, Strategy…) y documentarlos con un comentario que explique cuál patrón es y por qué.

## Proyecto
Juego 2D estilo Metroidvania desarrollado en **Unity 2022.3 LTS** como proyecto universitario
para la asignatura **Diseño de Interfaces**. El foco actual es toda la capa de UI/menús;
el gameplay (plataformeo, biomas, bosses) es trabajo futuro.

## Directorios clave
| Ruta | Descripción |
|------|-------------|
| `C:\Users\juang\Eldoria\` | Proyecto Unity (fuente) |
| `C:\Users\juang\OneDrive\Documentos\Universidad\DisenoDeInterfaces\Juego\assets\` | Assets crudos (Aseprite, MP4, audio) |
| `C:\Users\juang\OneDrive\Documentos\Universidad\DisenoDeInterfaces\Juego\eldoria\` | Build compilado del juego |

## Flujo de pantallas
```
[MainMenu]
    ├─ Jugar     → [SlotsScreen]  — 4 slots de guardado
    │                  ├─ Slot vacío  → [Intro] video cinemático + subtítulos → [Game]
    │                  └─ Slot lleno → Continuar directo a [Game] | Borrar (con confirmación)
    ├─ Opciones  → [Settings]     — dos columnas: pestañas izq / panel der
    └─ Salir     → modal confirmación
```

## Escenas (`Assets/Scenes/`)
| Escena | Estado | Descripción |
|--------|--------|-------------|
| MainMenu | ✅ Funcional | Video BG loop + música + logo + 3 botones |
| Settings | 🔧 En progreso | Opciones con 3 pestañas |
| SlotsScreen | ✅ Funcional | 4 slots de guardado |
| Intro       | ✅ Implementada | Video cinemático intro + subtítulos + skip |
| SampleScene | — | Escena de prueba de gameplay |

## Estructura de Settings (4 pestañas)
| Pestaña | Secciones | Controles |
|---------|-----------|-----------|
| **GRÁFICOS** | Pantalla | ResolutionSelector, ScreenModeSelector, FpsSelector, VsyncToggle, QualitySelector |
| | Visual | BrightnessSlider, ContrastSlider, SaturationSlider |
| | Accesibilidad | ColorBlindToggle → ColorBlindOptionsGroup (TypeSelector + IntensitySlider) |
| **SONIDO** | — | MasterVolumeSlider, MusicSlider, SFXSlider, VoicesSlider, UISlider |
| **CONTROLES** | — | KeyRebindUI (click en slot → presiona tecla → se reasigna) |
| **AJUSTES** | — | LanguageSelector (Español / English) |

Controles UI usados: `SelectionControl` (←valor→), `Slider`, `Toggle`.
`SelectionControl.cs` en `Assets/Scripts/` — script propio, reemplaza TMP_Dropdown.

## Scripts (`Assets/Scripts/`)
| Script | Patrón | Descripción |
|--------|--------|-------------|
| `AudioManager.cs` | Singleton | Volumen música/SFX, persiste entre escenas (DontDestroyOnLoad) |
| `SceneFader.cs` | Singleton | Transición con fade negro entre escenas |
| `SaveManager.cs` | Singleton | Carga/guarda partidas en JSON (4 slots) |
| `MainMenuManager.cs` | — | Botones Play/Options/Quit del menú principal |
| `SettingsManager.cs` | Observer + State | Pantalla de opciones: 4 pestañas, eventos estáticos |
| `SlotsScreenManager.cs` | State Machine + Observer | UI de selección/creación de partidas; `_selectedSlot` es el estado, botones globales reaccionan al estado |
| `KeyRebindUI.cs` | — | Reasignación de teclas con PlayerPrefs |
| `BackgroundVideoManager.cs` | Singleton | VideoPlayer persistente entre escenas; renderiza a VideoRenderTexture |
| `SliderFillReveal.cs` | Observer | Efecto "before/after" en sliders: revela imagen sin estirarla. Se añade al Slider junto al componente Slider de Unity. Auto-detecta el Fill Image via `slider.fillRect` antes de desconectarlo. |
| `VideoLoopController.cs` | — | DEPRECADO — reemplazado por BackgroundVideoManager |
| `PlayerController.cs` | State Machine | Movimiento walk/run (Shift toggle), salto Z (coyote+buffer), dash bloqueado hasta hasDash=true, double jump, wall slide/jump, float. Radio de detección escala-aware. Expone estado para PlayerAnimator |
| `PlayerAnimator.cs`   | Observer | Lee estado de PlayerController, actualiza parámetros del Animator (Speed, IsGrounded, IsRunning, IsJumping, IsFalling, IsDashing, IsWallSlide, Hurt, Die) |
| `SaveData.cs` | — | Modelo de datos de partida guardada |
| `CameraFollow.cs` | — | Cámara sigue al jugador |
| `IntroVideoManager.cs` | Command + Observer | Reproduce video intro, muestra subtítulos cronometrados, skip manteniendo tecla; al terminar carga "Game" |
| `SubtitleData.cs` | — | ScriptableObject con entradas {startTime, endTime, text}; crear desde Assets → Create → Eldoria → Subtitle Data |
| `RoomStructure.cs` | Value Object | Elemento de colisión de sala (Ground/Platform/Wall/Ceiling/OneWay). BoxCollider2D + overlay de gizmos en color por tipo. OneWay auto-añade PlatformEffector2D |

## Assets en el proyecto Unity (`Assets/UI/Sprites/`)
| Carpeta | Archivos |
|---------|----------|
| `Buttons/` | normalButton, hoverButton, pressButton, settingsButtons (Normal/Hover/Press) |
| `Sliders/` | slider0% (fondo vacío), slider100%2 (fill/relleno), sliderButton (handle), sliders |
| `Toggle/` | toggleOn, toggleOff |
| `Containers/` | Container1.png ← copiado desde assets externos |
| `Logo/` | logo2.png |

## Opciones de Gráficos — valores fijos

| Control | Opciones | Notas |
|---------|----------|-------|
| Resolución | 1920×1080 / 1600×900 / 1200×675 | Lista fija en `FixedResolutions` |
| Modo pantalla | Pantalla completa / Sin bordes / Ventana | `ExclusiveFullScreen / FullScreenWindow / Windowed` |
| Límite FPS | 120 / 144 / Sin límite | `FpsValues = {120, 144, -1}` · default Sin límite |
| VSync | Toggle on/off | `QualitySettings.vSyncCount` |

## Configuración de sliders en Settings — Sección Visual
Los sliders de Brillo, Saturación y Contraste usan sprites personalizados:
- **Background**: sprite `slider0%` — imagen del slider vacío
- **Fill**: sprite `slider100%2` — imagen del slider lleno (revelada progresivamente)
- **Handle**: sprite `sliderButton`

Cada slider tiene el componente `SliderFillReveal` adjunto. Este:
1. Desconecta `slider.fillRect` para que Unity no redimensione el Fill
2. Configura `Image.Type = Filled / Horizontal / Left` en el Fill
3. Actualiza `fillAmount` al mover el slider (efecto cortina sin stretch)

Jerarquía (los 3 son idénticos):
```
Canvas/RightPanel/GraficosPanel/Scroll View/Viewport/Content/
  Row_Brillo/Slider        ← SliderFillReveal + Slider (instanceId 37648)
    Background             ← Image: slider0%
    Fill Area              ← (instanceId 38092)
      Fill                 ← Image: slider100%2 (instanceId 37944)
    Handle Slide Area
      Handle               ← Image: sliderButton

  Row_Saturacion/Slider    ← instanceId 38086, Fill instanceId 37714
  Row_Contraste/Slider     ← instanceId 37990, Fill instanceId 38218
```

**Fuentes:** Minecraft.ttf · Perfect DOS VGA 437.ttf (también como TMP Asset: MinecraftTMP)
**Audio:** Celestial Kingdom, Enchanted Ruins, Ethereal Waters, Lost Temples, Mystical Forest, Peaceful Village (.ogg)
**Video:** BgEldoriaStartScreen.mp4

## Patrones de diseño utilizados
- **Singleton** — `AudioManager`, `SceneFader`, `SaveManager`, `BackgroundVideoManager` persisten con `DontDestroyOnLoad`
- **Observer** — `SettingsManager` expone eventos estáticos (`OnMusicVolumeChanged`, etc.) para desacoplar sistemas
- **State Machine** — `SettingsManager.ShowPanel()` gestiona qué panel/tab está activo

## PlayerPrefs keys
| Key | Tipo | Descripción |
|-----|------|-------------|
| `MusicVolume` | float | Volumen música (0–1) |
| `SFXVolume` | float | Volumen efectos (0–1) |
| `MuteMusic` | int (0/1) | Silenciar música |
| `MuteSFX` | int (0/1) | Silenciar efectos |
| `Fullscreen` | int (0/1) | Pantalla completa |
| `VSync` | int (0/1) | Sincronización vertical |
| `Quality` | int (0/1/2) | Calidad gráfica (Bajo/Medio/Alto) |
| `Resolution` | int | Índice de resolución en Screen.resolutions |
| `Key_<actionId>` | string | Teclas reasignadas (ej: Key_Jump) |

## Configuración de la escena Settings (Unity Editor)
Para que `SettingsManager.cs` funcione, la jerarquía de la escena Settings debe tener:

```
[Canvas]
  ├─ Background (RawImage con VideoRenderTexture)   ← mismo video que MainMenu
  ├─ LeftColumn (columna de pestañas)
  │    ├─ AudioTabButton     (Button → Image: settingsButtonsNormal)
  │    ├─ GraphicsTabButton  (Button → Image: settingsButtonsNormal)
  │    └─ ControlsTabButton  (Button → Image: settingsButtonsNormal)
  ├─ RightPanel (Image: Container1.png)
  │    ├─ AudioPanel
  │    │    ├─ MusicSlider   (Slider)
  │    │    ├─ SFXSlider     (Slider)
  │    │    ├─ MuteMusicToggle (Toggle: toggleOn/toggleOff)
  │    │    └─ MuteSFXToggle   (Toggle: toggleOn/toggleOff)
  │    ├─ GraphicsPanel
  │    │    ├─ FullscreenToggle (Toggle)
  │    │    ├─ VSyncToggle      (Toggle)
  │    │    ├─ ResolutionDropdown (TMP_Dropdown)
  │    │    └─ QualityDropdown   (TMP_Dropdown)
  │    └─ ControlsPanel
  │         └─ [KeyRebindUI component con sus BindingEntries]
  └─ BackButton (Button)

[AudioManager]   ← prefab persistente (ya existe en MainMenu, DontDestroyOnLoad)
[SceneFader]     ← prefab persistente (ya existe en MainMenu, DontDestroyOnLoad)
```

**Notas de configuración:**
- `SettingsManager` → Tab Active Sprite = `settingsButtons.png` | Tab Normal Sprite = `settingsButtonsNormal.png`
- Cada Button de pestaña → Transition = Sprite Swap → Highlighted = `settingsButtonsHover` | Pressed = `settingsButtonsPress`
- La música continúa automáticamente porque `AudioManager` usa `DontDestroyOnLoad`
- El video lo gestiona `BackgroundVideoManager` (Singleton DontDestroyOnLoad desde MainMenu).
  La escena Settings solo necesita un RawImage con VideoRenderTexture — sin VideoPlayer propio.

## Layout de la escena Settings — LeftColumn
```
LeftColumn
  ├─ BackButton          ← ancla arriba-izquierda, separado del grupo de tabs
  │                         (Pos Y negativo desde el top, ej: -40)
  ├─ [Espacio flexible]  ← LayoutElement con Flexible Height = 1
  ├─ TabGroup            ← Vertical Layout Group, centrado
  │    ├─ AudioTabButton
  │    ├─ GraphicsTabButton
  │    └─ ControlsTabButton
  └─ [Espacio flexible]  ← LayoutElement con Flexible Height = 1
```
BackButton usa posicionamiento manual (sin Layout Group en el padre directo de LeftColumn).
Los TabButtons van dentro de un hijo `TabGroup` con su propio Vertical Layout Group.

## Patrones de datos utilizados
- **Array** — `SaveData[4]` en `SlotsScreenManager` para los 4 slots de partida (acceso O(1) por índice; tamaño fijo → array es la estructura correcta, no Lista ni Cola)

## SlotsScreen — arquitectura de botones
Jerarquía de cada Slot (Slot1..Slot4) dentro de `Canvas/SlotsRow`:
```
Slot1
  ├─ CardButton    ← Button (SpriteSwap) + Image (SlotEmptyNormal, color blanco)
  │                   Hermano 0: renderiza detrás del texto
  ├─ EmptyState    ← solo texto/ícono encima; SIN CardBg propio
  │    └─ QuestionMark (TMP)
  └─ OccupiedState ← activo cuando hay partida guardada; SIN CardBg propio
       ├─ LevelText (TMP)
       ├─ ZoneText  (TMP)
       └─ TimeText  (TMP)
```

**Principio de diseño:** CardButton ES la tarjeta visible. Unity gestiona automáticamente hover
(HighlightedSprite) y press (PressedSprite). Al hacer clic, `EventSystem.SetSelectedGameObject()`
mantiene el `SelectedSprite` (= hover) de forma persistente, igual que las pestañas de Settings.

**SpriteSwap por estado de slot:**
- Slot vacío:  Normal=`SlotEmptyNormal`  · Hover=`SlotEmptyHover`  · Press=`SlotEmptyPress`
- Slot lleno:  Normal=`SlotFilledNormal` · Hover=`SlotFilledHover` · Press=`SlotFilledPress`
  (cambiados en runtime por `ApplySlotSprites()` en `SlotsScreenManager.Start()`)

**Patrones en `SlotsScreenManager.cs`:**
- **State Machine** — `_selectedSlot` (int, -1 = ninguno) rastrea el slot activo
- **Observer** — `RefreshGlobalButtons()` actualiza BORRAR/SELECCIONAR en respuesta al estado
- Label del botón SELECCIONAR: `"NUEVA PARTIDA"` si slot vacío, `"CONTINUAR"` si slot lleno

**Estructura de datos:** `SaveData[4]` — array fijo de 4 entradas, acceso O(1) por índice.
No se usan pilas/colas; los slots no tienen semántica LIFO/FIFO.

**Generación de escena:** `Assets/Editor/SlotsSceneSetup.cs`
- `Eldoria/Setup Slots Scene` — reconstruye todo el Canvas desde cero
- `Eldoria/Wire All Slots References` — solo recablea refs + sprites sin tocar jerarquía

## SlotsScreen — bug crítico resuelto (2026-05-16)
**Causa raíz:** `AudioManager.musicSource` no asignado en inspector del MainMenu → `StopMusic()` lanzaba
`UnassignedReferenceException` → `SlotsScreenManager.Start()` abortaba antes de añadir listeners → todos los botones sin funcionar.

**Fix:** `AudioManager.StopMusic/PauseMusic/ResumeMusic` ahora hacen null-check de `musicSource`.

**Síntoma secundario:** Al jugar SlotsScreen directamente (sin MainMenu), `AudioManager.Instance` era null → null-safe → funcionaba. Al venir de MainMenu, Instance existía pero musicSource=null → crash. Asegurarse de asignar `musicSource` y `sfxSource` en el inspector del AudioManager del MainMenu.

## Log de progreso
- **2026-05-11** — Revisión inicial del proyecto. Mejorado `SettingsManager.cs`:
  añadidos mute toggles (música/SFX), VSync toggle, quality dropdown, eventos Observer.
  Importado `Container1.png` al proyecto Unity (`Assets/UI/Sprites/Containers/`).
  Creado este archivo `CLAUDE.md` para contexto persistente.
- **2026-05-11** — Creado `BackgroundVideoManager.cs` (Singleton): video persiste entre
  escenas sin reiniciarse. La escena Settings solo necesita RawImage con VideoRenderTexture.
  Layout del botón Volver documentado: arriba-izquierda separado de los tabs centrados.
- **2026-05-12** — Reescrito `SettingsManager.cs`: 5 pestañas (Gráficos, Sonido, Controles,
  Jugabilidad, Créditos), título dinámico del panel, botones APLICAR/RESTABLECER/VOLVER.
  Creado `Assets/Editor/SettingsSceneSetup.cs`: editor script que genera toda la jerarquía
  de la escena Settings desde Unity menu → Eldoria → Setup Settings Scene. Incluye creación
  automática del TMP Font Asset. Reversión: Ctrl+Z o File → Revert.
- **2026-05-12 (v3)** — Reescrito `SettingsSceneSetup.cs` con las siguientes mejoras:
  · Fuente cambiada a **Pixelatus** (`Assets/UI/Fonts/Pixelatus.ttf`).
  · Paneles con Container1.png usan `Color.white` (sin tinte/filtro oscuro).
  · Panel principal 1680×860 (antes 1100×560) para llenar mejor la pantalla 1920×1080.
  · Botones APLICAR/RESTABLECER/VOLVER movidos fuera de los containers, directamente
    sobre el MainPanel (sin fondo detrás de ellos).
  · Columnas izquierda y derecha dejan espacio inferior (BOT_H+GAP) para la fila de botones.
  · Tamaños de fuente: títulos=26, tabs=18, filas=18, botones=18.
  · `MainMenuManager.cs` ya tenía `OnOptions() → LoadScene("Settings")` — sin cambios.
- **2026-05-14** — Creado `SliderFillReveal.cs` (Observer): soluciona dos problemas en los
  sliders de la sección Visual (Brillo/Saturación/Contraste):
  1. Fill desbordaba el área del slider (sizeDelta +10px por defecto de Unity).
  2. La imagen del fill se estiraba al mover el slider en lugar de revelarse.
  Solución: desconectar `fillRect` del Slider, cambiar Image.Type a Filled/Horizontal/Left,
  y actualizar `fillAmount` vía `onValueChanged`. La configuración del RectTransform ocurre
  un frame después (coroutine con `yield return null`) para que el DrivenRectTransformTracker
  del Slider libere el lock. Componente añadido a los 3 sliders via MCP.
- **2026-05-14** — Corregidas opciones de Gráficos en `SettingsManager.cs`:
  resoluciones fijas (1920/1600/1200), modos de pantalla reordenados
  (Completa/Sin bordes/Ventana), FPS cambiado a 120/144/Sin límite (default: Sin límite).
  Corregido overflow de slider fills: `maskable = false` en las tres Image de Fill
  hacía que el RectMask2D del Viewport fuera ignorado. Solucionado via MCP y en
  `SliderFillReveal.cs` (ahora fuerza `maskable = true` en SetupNextFrame).
- **2026-05-14** — Corregido `BackgroundVideoManager.cs`: ahora crea un `RenderTexture` en
  runtime (`Awake`) si `targetTexture == null`. Esto garantiza que el VideoPlayer siempre
  renderice a textura en lugar de directo a pantalla, permitiendo que `BackgroundVideoDisplay`
  (en Settings) asigne esa textura al RawImage y el video se vea correctamente al navegar
  desde MainMenu → Settings. Escena guardada después de recompilación.
- **2026-05-16** — Implementada y reescrita escena `SlotsScreen`.
  · Bug crítico resuelto: `AudioManager.musicSource` no asignado → crash en `SlotsScreenManager.Start()` → todos los botones sin listeners. Fix: `StopMusic/PauseMusic/ResumeMusic` ahora hacen null-check.
  · Arquitectura final de slots: `CardButton` ES la tarjeta visible — `Button` + `Image` con `SpriteSwap`. Tres estados por sprite set (vacío/lleno): Normal/Hover/Press. Al clic, `EventSystem.SetSelectedGameObject()` mantiene estado hover persistente (igual que pestañas de Settings).
  · `SlotsScreenManager.cs` patrón State Machine (`_selectedSlot`) + Observer (`RefreshGlobalButtons`). Label SELECCIONAR dinámico: "NUEVA PARTIDA" o "CONTINUAR".
  · Estructura de datos: `SaveData[4]` array fijo, O(1) por índice.
  · Ambience Cave Sound Effect en `Canvas/Ambience` AudioSource (loop, volume=0.6).
  · `SlotsSceneSetup.cs`: menús `Eldoria/Setup Slots Scene` (reconstruye todo) y `Eldoria/Wire All Slots References` (solo recablea).
  **PENDIENTE:** Asignar `musicSource` y `sfxSource` en el inspector del AudioManager del MainMenu.
- **2026-05-17 (audio)** — Fix definitivo del AudioManager: auto-detección de AudioSources si inspector está vacío, `playOnAwake=false`, `PlayMusic()` explícito. MainMenuManager ahora llama `PlayMusic(menuMusic)` — campo `menuMusic` para asignar el clip en inspector. Cada escena controla su música: MainMenu/Settings = play, SlotsScreen/Intro/Game = stop.
- **2026-05-17 (gameplay)** — Assets importados: `Assets/Sprites/Kael/` (idle/run/walk/jump/fall/dash/slide/combo/death/hurt), `Assets/Sprites/Escenarios/` (Hub, Montañas, Paisajes, Plataformas), `Assets/Sprites/Enemigos/`. PlayerController reescrito con coyote time, jump buffer, gravedad mejorada, wall slide/jump, expone propiedades de estado. PlayerAnimator nuevo (Observer). GameSceneSetup.cs → `Eldoria/Setup Game Scene` monta HUB-01 (InteriorCasaKael) con Player, colliders, CameraFollow. **PENDIENTE:** (1) Crear layer "Ground" en Project Settings. (2) Crear Animator Controller de Kael en Unity (ver instrucciones abajo). (3) Asignar `menuMusic` en inspector de MainMenuManager. — Implementada escena `Intro` (cinemática de nueva partida).
  Video: `Assets/UI/Sprites/NewGame/NewGameVideo.mp4`. Sistema de subtítulos con `SubtitleData`
  ScriptableObject (entradas {startTime, endTime, text}). Skip manteniendo cualquier tecla (~1.2s).
  `IntroVideoManager.cs` patrones Command (ExitIntro) + Observer (loopPointReached). Editor script
  `IntroSceneSetup.cs` → menú `Eldoria/Setup Intro Scene`. Flujo: Nueva Partida → Intro → Game;
  Continuar Partida → Game directo.
  **PENDIENTE:** Crear asset `SubtitleData` desde Assets→Create→Eldoria→Subtitle Data,
  rellenar entradas con el guión del locutor y asignarlo al campo `subtitleData` en el inspector
  de `IntroVideoManager` (en Canvas de la escena Intro).
- **2026-05-17 (estructuras)** — Implementado sistema de hitboxes visuales para salas.
  · `RoomStructure.cs` (Value Object): BoxCollider2D + overlay de gizmos en color por tipo
    (Ground=gris, Platform=azul, Wall=naranja, Ceiling=verde, OneWay=amarillo). OneWay
    auto-añade PlatformEffector2D. Flag `showOverlayInGame` para debug en builds.
  · `RoomBuilderWindow.cs` (Editor): menú `Eldoria→Room Builder`. Crea estructuras por tipo,
    redimensiona vía `BoxCollider2D.size`, snap a grilla de tiles (1 tile = configurable en units).
    Lista todas las estructuras de la escena con botones Foco/Eliminar.
  · `GameSceneSetup.cs` actualizado: Ground/WallLeft/WallRight/Platform1-3 ahora llevan
    `RoomStructure` con su tipo correspondiente.
  **FLUJO DE USO:** Eldoria→Room Builder → selecciona tipo → "Crear en centro" → mueve en
  Scene view → en Inspector clic en "Edit Collider" (verde) para redimensionar arrastrando.
  Snap ALL al Grid para alinear a tiles de 16px (PPU=16 → 1.0 unit/tile).
- **2026-05-17 (puerta + Kael)** — `DoorExit.cs`: zona trigger en puerta → muestra "[ E ] SALIR"
  flotante (bobbing sinusoidal) cuando el Player entra, presiona E para cargar la escena destino.
  `GameSceneSetup` crea `DoorExit_Right` en x=10.5 (puerta derecha de InteriorCasaKael).
  `KaelSetup.cs` (Editor): menú `Eldoria→Setup Kael (Sprites + Animator)` ejecuta 4 fases:
  (1) importa todos los PNGs de Kael con PPU=16, Point, Multiple, slice 128×128, Pivot=Bottom;
  (2) crea AnimationClips en Assets/Animations/Kael/; (3) crea KaelAnimator.controller con
  8 estados (Idle/Run/Jump/Fall/Dash/WallSlide/Hurt/Death) + transiciones completas;
  (4) asigna controller al Animator del Player y pone tag "Player".
  **PENDIENTE MCP Unity:** herramientas desconectadas — reconectar desde Unity para uso directo.
- **2026-05-17 (Kael wiring + fixes)** — Completado el setup del Player via MCP + edición directa de YAML:
  · KaelAnimator.controller asignado al Animator del Player (m_Controller, guid ddf5cec5...).
  · PlayerController: groundCheck/wallCheckL/wallCheckR cableados a los hijos GroundCheck/WallCheckL/WallCheckR.
  · groundLayer seteado a m_Bits:256 (capa 8 = Ground) en el YAML de la escena.
  · Player localScale → (2,2,2) para escala doble visible.
  · `PlayerController.cs` actualizado: guarda `_baseScale` en Awake y lo usa al hacer flip horizontal
    (`transform.localScale = new Vector3(FacingDir * Abs(_baseScale.x), _baseScale.y, 1f)`)
    para preservar la escala Y cuando el personaje voltea. **Patrón:** ninguno nuevo; es corrección
    de bug — la escala base se almacena como Vector3 al inicio (estructura de datos simple).
  · `moveSpeed` subido de 6 → 10 (tanto en script como en escena).
  · **Bug fix build:** `TagManager.asset` tenía "Player" en el array de tags personalizados.
    "Player" es tag built-in de Unity → conflicto "Default GameObject Tag: Player already registered"
    → build fail con 5 errores. Fix: array de tags personalizados vacío (`tags: []`).
  · Scripts eliminados (ya no existen): `GameSceneSetup.cs`, `KaelSetup.cs`, `RoomBuilderWindow.cs`.
  · Animaciones Kael: 13 clips en Assets/Animations/Kael/. KaelAnimator con 12 estados y 11 params.
- **2026-05-18 (feel + desaparición al saltar)** — Ajustes de velocidad y fix de animación Jump:
  · `walkSpeed` 6→8, `runSpeed` 10→16, `jumpForce` 14→18, `fallMultiplier` 2.4→2.0, `lowJumpMult` 1.8→1.5.
    Personaje se siente más rápido y menos pesado. Saltos más altos con caída menos abrupta.
    Coyote/buffer time: 0.12→0.15 (ventana de salto más generosa).
  · **Bug desaparición al saltar:** estado Jump en KaelAnimator tenía `m_Motion: {fileID: 0}` (sin clip).
    Con `m_WriteDefaultValues: 1`, Unity reseteaba el sprite a su valor por defecto (null) → personaje
    invisible durante el salto. Fix: asignado Jump.anim (guid 909e23af...) al estado Jump.
    Ídem para Death state: asignado Death.anim (guid a0000001...d).
    Nota técnica: `update_component` MCP fue necesario para persistir walkSpeed/runSpeed porque
    `load_scene` auto-guarda el estado in-memory antes de cargar desde disco, sobreescribiendo
    ediciones directas en el YAML. Patrón de persistencia correcto: `update_component` → `save_scene`.
- **2026-05-18 (mecánicas de salto avanzadas)** — Tres nuevas mecánicas implementadas en `PlayerController.cs`:
  · **Caída acelerada (`HandleFastFall`):** presionar ↓ o S mientras `velocity.y < 0` aplica
    `fastFallAccel=30` hacia abajo cada frame. No actúa si está en suelo, dashing o floating.
  · **Salto variable (`HandleJumpHold`):** al presionar Z se aplica `jumpMinForce=13`. Manteniendo Z
    se añade `jumpHoldBoost=30·Δt` hasta alcanzar `jumpForce=18` o agotar `jumpHoldTime=0.3s`.
    Lanzar Z antes del timer da saltos bajos; mantener da saltos altos. `_jumpHolding` se cancela al
    soltar Z, tocar suelo, o superar el timer.
  · **Momentum bloqueado en aire (`_airSpeed`):** al saltar, `_airSpeed` se fija con la velocidad
    del modo actual (walk/run). En aire, `HandleMovement` usa `_airSpeed` en vez de calcular de nuevo
    → A/D cambia dirección pero no velocidad. Shift no puede toggle run mode en el aire
    (guarda condicional `if (IsGrounded)` antes del toggle). Al aterrizar, `_isJumpAirborne = false`
    restaura el control normal. Wall jump también fija `_airSpeed = wallJumpForce.x`.
  **Nuevos campos serializados:** `jumpMinForce=13`, `jumpHoldTime=0.3`, `jumpHoldBoost=30`, `fastFallAccel=30`.
- **2026-05-18 (movimiento + animaciones)** — Corrección completa del sistema de movimiento y animaciones de Kael:
  · **Bug raíz IsGrounded (fix definitivo):** `Physics2D.OverlapCircle` fallaba porque groundLayer
    era 0 en runtime (serializado como m_Bits:0) y el GroundCheck estaba demasiado alto para el
    radio original. Fix: cambio de detección a **`rb.GetContacts(_contacts)`** — lee los contactos
    reales del motor de física, sin depender de LayerMask ni de posición del GroundCheck.
    `normal.y > 0.5f` filtra suelos (normal vertical-arriba) de paredes (normal horizontal).
    Awake ahora hardcodea `groundLayer = 1 << 8` (layer 8 = Ground) sin condicional.
    `_contacts` es un `ContactPoint2D[8]` reutilizable (sin GC allocation por frame).
    **Patrón:** ninguno nuevo — es una corrección de arquitectura de detección (polling → evento físico).
  · **Sistema walk/run:** Eliminado `moveSpeed`, añadidos `walkSpeed=6` y `runSpeed=10`.
    `_runningMode` (bool privado): un tap de Shift lo activa/desactiva. Velocidad = `_runningMode ? runSpeed : walkSpeed`.
    `IsRunning = IsGrounded && moving && _runningMode`. Sin run mode = camina por defecto.
  · **Dash bloqueado por progresión:** `hasDash=false` (habilidad tardía). `HandleDash` hace early return
    si `!hasDash`. Shift es ahora el toggle de run; Dash también usa Shift pero nunca llega a activarse.
  · **Tecla de salto:** cambiada de Space → Z (en `jumpBufferCounter` y `ApplyBetterGravity`).
  · **PlayerAnimator:** añadido parámetro `IsRunning` (Bool) — hash pre-calculado, set en Update.
  · **KaelAnimator.controller:** añadido parámetro `IsRunning` (Bool, tipo 4). Walk state: asignado
    Walk.anim (guid a0000001b0000002c0000003d0000004). Nuevas transiciones:
    - 200020: Idle→Walk (Speed>0.1 AND IsRunning=false)
    - 200021: Walk→Idle (Speed<0.1)
    - 200022: Walk→Run (IsRunning=true AND Speed>0.1)
    - 200023: Run→Walk (IsRunning=false AND Speed>0.1)
    - 200024: Fall→Walk (IsGrounded=true AND Speed>0.1 AND IsRunning=false)
    Modificadas: Idle→Run (200010) + Fall→Run (200013) ahora exigen IsRunning=true.
    Fall→Idle (200012) conserva IsGrounded=true (correcto una vez el bug de detección está resuelto).
