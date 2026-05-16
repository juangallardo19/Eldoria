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
    │                  ├─ Slot vacío  → nueva partida
    │                  └─ Slot lleno → Continuar | Borrar (con confirmación)
    ├─ Opciones  → [Settings]     — dos columnas: pestañas izq / panel der
    └─ Salir     → modal confirmación
```

## Escenas (`Assets/Scenes/`)
| Escena | Estado | Descripción |
|--------|--------|-------------|
| MainMenu | ✅ Funcional | Video BG loop + música + logo + 3 botones |
| Settings | 🔧 En progreso | Opciones con 3 pestañas |
| SlotsScreen | ✅ Funcional | 4 slots de guardado |
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
| `SlotsScreenManager.cs` | — | UI de selección/creación de partidas |
| `KeyRebindUI.cs` | — | Reasignación de teclas con PlayerPrefs |
| `BackgroundVideoManager.cs` | Singleton | VideoPlayer persistente entre escenas; renderiza a VideoRenderTexture |
| `SliderFillReveal.cs` | Observer | Efecto "before/after" en sliders: revela imagen sin estirarla. Se añade al Slider junto al componente Slider de Unity. Auto-detecta el Fill Image via `slider.fillRect` antes de desconectarlo. |
| `VideoLoopController.cs` | — | DEPRECADO — reemplazado por BackgroundVideoManager |
| `PlayerController.cs` | — | Control del personaje (gameplay, WIP) |
| `SaveData.cs` | — | Modelo de datos de partida guardada |
| `CameraFollow.cs` | — | Cámara sigue al jugador |

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
