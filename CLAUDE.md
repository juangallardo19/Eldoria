# Eldoria — Contexto del Proyecto para Claude

> **CLAUDE.md está en:** `C:\Users\juang\Eldoria\CLAUDE.md` — **LEER SIEMPRE AL INICIAR**, incluso después de compactar la conversación.
> **Regla de código:** Siempre que sea posible, aplicar patrones de diseño (Singleton, Observer,
> State, Command, Strategy…) y documentarlos con un comentario que explique cuál patrón es y por qué.
> **Regla de preferencias globales:** Cuando el usuario diga "quiero que TODO X sea Y" o similar expresión universal,
> registrar como regla en este CLAUDE.md (ej: "todos los cielos deben comportarse igual que HV01_Exterior").
> **Regla de logging:** Solo registrar en CLAUDE.md cambios relevantes (nuevos sistemas, bugs no resueltos, reglas de diseño, decisiones de arquitectura). NO registrar cada pequeño fix o ajuste puntual — esos van al commit de git.

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

## Sprites de Escenarios (`Assets/Sprites/Escenarios/`)
> **BUSCAR SIEMPRE AQUÍ** cuando necesites fondos, cielos o plataformas de escenas de juego.

| Carpeta | Archivos | Uso |
|---------|----------|-----|
| `Hub/` | `CasaKael.png`, `InteriorCasaKael.png`, `HubCentral.png`, `HubCentralInterior.png`, `Hub03Interior.png`, **`hub04.png`**, `hub05.png`, `hub06.png`, `Casas2.png` | Fondos de salas del Hub. `hub04/05/06.png` = fondos de Zona A/B/C |
| `Montanas/` | `MTN01afueras.png`, `MTN01Interior.png`, `MTN02.png`, `mtn03-06.png`, `MTN08-12.png`, `AfuerasPreEntrada.png`, `PreMTN10-12.png` | Fondos bioma Montañas |
| `Paisajes/Hub/` | **`Dia.png`**, **`Noche.png`**, **`Amanecer.png`**, **`anochecer.png`** | Cielos de 4 estados para escenas del Hub. Path completo: `Assets/Sprites/Escenarios/Paisajes/Hub/` |
| `Paisajes/Montanas/` | `Dia.png`, `Noche.png`, `Amancer.png`, `Anochecer.png` | Cielos de 4 estados para Montañas. Path completo: `Assets/Sprites/Escenarios/Paisajes/Montanas/` |
| `Plataformas/hub/` | `ladrillos.png`, `PlataformaDerecha.png`, `PlataformaIzquierda.png`, `PlataformaLarga.png` | Tiles y plataformas del Hub |
| `Plataformas/Montanas/` | `Montanas.png`, `PlataformaLarga.png`, `plataformaMedianaDecorada.png`, `PlataformaPequenaDecorada.png` | Plataformas del bioma Montañas |
| `Paisajes/RefugioAMontanas/` | `Dia.png`, `Noche.png`, `Amanecer.png`, `Anochecer.png` | Cielos de 4 estados para HV07 |
| `Estructuras/Montanas/` | `EstructuraGrande1.png`, `EstructuraGrande2.png`, `EstructuraMediana1.png`, `EstructuraPequenaDesgastada1/2/3.png`, `CarpaDesgastada1.png` | Estructuras decorativas; las Desgastadas van hacia la izquierda |

**Dimensiones de referencia** (PPU=100 en todos):
- `hub04.png` / cielo Hub: 1672×941px → **16.72u × 9.41u** a escala 1 → a escala (5,5,1) = **83.6u × 47.05u** (sala HV04)
- `InteriorCasaKael.png`: 16.72×9.41u base, escala 2,2,1 → 33.44×18.82u

**Sprites de Kael** (`Assets/Sprites/Kael/`): idle, run, walk, jump, fall, dash, slide, combo, death, hurt (todos 128×128px, PPU=16)

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
| Settings | 🔧 En progreso | 4 pestañas. **Pendiente ejecutar 3 editor scripts** (ver §Pendientes) |
| SlotsScreen | ✅ Funcional | 4 slots de guardado |
| Intro | ✅ Implementada | Video cinemático intro + subtítulos + skip |
| HV01_Interior | ✅ Funcional | Interior casa de Kael — plataformas, CameraFollow FitRoom, DoorExit→Exterior |
| HV01_Exterior | 🔧 En progreso | Hub exterior — parallax, DayCycle, plataformas OneWay, DoorEntry→Interior |
| HV02_PlazaCentral | 🔧 En progreso | Plaza central — rampas, plataformas OneWay, 4 puertas a subzonas, FollowBounded |
| HV04 | 🔧 En progreso | Zona A — 83.6×47.05u, cielo parallax, FitRoom. Menú `Eldoria/Setup HV04`. |
| HV05 | 🔧 En progreso | Zona B — igual dimensiones que HV04. Menú `Eldoria/Setup HV05`. |
| HV06 | 🔧 En progreso | Zona C — igual dimensiones que HV04. Menú `Eldoria/Setup HV06`. |
| HV07 | ✅ Funcional | Corredor RefugioAMontanas (x=±90, suelo y=-30). Parallax 4 estados. 10 plataformas + 7 estructuras. SceneBoundary_Left→MTN01_Exterior(spawnId="default"), SceneBoundary_Right→HV01_Exterior. Menú `Eldoria/Setup HV07`. |
| MTN01_Exterior | ✅ Funcional | Afueras de las Montañas (x=±105, suelo y=-32). Parallax Montañas 4 estados. DoorExit→MTN01_Interior(spawnId="door"). SceneBoundary_Right→HV07(spawnId="mtn_left"). SpawnPoint "default"(x=40), "door"(x=-28). Menú `Eldoria/Setup MTN01_Exterior`. |
| MTN01_Interior | ✅ Funcional | Cueva interior (x=±58, suelo y=-16). FollowBounded + SteppedCameraBounds. Cielo 4 estados. DoorExit DERECHA(x=54)→MTN01_Exterior(spawnId="door"). SceneBoundary IZQUIERDA(x=-56)→MTN02(spawnId="mtn01_right"). SpawnPoint "door"(x=46), "mtn02_exit"(x=-50). 2 plataformas junto a puerta derecha. Menú `Eldoria/Setup MTN01_Interior`. |
| MTN02 | 🔧 En progreso | Ruinas (suelo y=-16.68, paredes x=±43.7, SceneBoundary x=±38.9). FollowBounded(boundsMin=(-49,-13), boundsMax=(49,0)) + SteppedCameraBounds(5 steps: x=-39/yMax=-7, x=-15/yMax=7, x=0/yMax=12, x=20/yMax=8, x=39/yMax=-5, todos yMin=-19). Cielo 4 estados (Sky scale=0.465). 2 plataformas OneWay: Plat_E_HighCenter(1) pos(5.31,-6.33) col(28×0.5), Plat_E_HighCenter(2) pos(16.24,-13.99) col(4.2×0.5). SceneBoundary DERECHA(x=38.9)→MTN01_Interior(spawnId="mtn02_exit"). IZQUIERDA(x=-38.8)→MTN03(spawnId="mtn02_right"). SpawnPoint "mtn01_right"(x=35.62,y=-13.88), "mtn03_exit"(x=-36.05,y=-14.4). Menú `Eldoria/Setup MTN02`. |
| MTN03 | ✅ Funcional | Santuario de Ara — calco exacto de MTN02 (mismas dimensiones, paredes, bounds, 10 plataformas OneWay user-placed). Fondo mtn03.png. SanctuaryAra en (0,-14.5): llama azul (partículas r=0,g=0.55,b=1,a=0.2), glow parpadeante (SanctuaryFlame), prompt "Descansar" al acercarse ≤6u. SceneBoundary DERECHA(x=38.9)→MTN02(spawnId="mtn03_exit"). IZQUIERDA(x=-38.8)→MTN04(spawnId="mtn03_right"). SpawnPoint "mtn02_right"(x=35.62,y=-13.88), "mtn04_exit"(x=-36.05,y=-14.4). Menú `Eldoria/Setup MTN03`. |
| MTN04 | 🔧 En progreso | Boca de las Cuevas — cúpula superior + pozo vertical central. Fondo mtn04.png. Paredes x=±50, suelo cúpula y=-10, techo y=+18, base pozo y=-55, pozo x=±7. FollowBounded + SteppedCameraBounds (7 pasos). 6 plataformas OneWay en cúpula + 3 en pozo. SceneBoundary DERECHA(x=48)→MTN03(spawnId="mtn04_exit"). SceneBoundary ABAJO(y=-54)→MTN05(spawnId="mtn04_bottom"). SpawnPoint "mtn03_right"(x=42,y=-6), "mtn05_exit"(x=0,y=-50). Menú `Eldoria/Setup MTN04`. |
| MTN05 | 🔧 En progreso | Galería de Cristal — cúpula amplia, suelo plano. Fondo mtn05.png. Paredes x=±52, suelo y=-12, techo y=+18 (hueco central x=±7 para pozo de MTN04). FollowBounded + SteppedCameraBounds (5 pasos). 8 plataformas OneWay. SceneBoundary ARRIBA(y=19, x=±7)→MTN04(spawnId="mtn05_exit"). SceneBoundary DERECHA(x=50)→MTN06(spawnId="mtn05_right"). SpawnPoint "mtn04_bottom"(x=0,y=16), "mtn06_exit"(x=44,y=-8). Menú `Eldoria/Setup MTN05`. |
| MTN06 | 🔧 En progreso | Laboratorio en Ruinas — cueva simétrica con dos torreones centrales y pasaje secreto bloqueado hacia MTN07. Fondo mtn06.png (scale=6.5). Paredes x=±54, suelo y=-14.5. Techo orgánico EdgeCollider2D (17 puntos, ajustar en Scene View). FollowBounded + SteppedCameraBounds (8 pasos). Ceiling_Gap_Blocker(pos=0,y=+16) bloquea físicamente el paso al MTN07. LockedZone_Top(pos=0,y=+11, trigger 10×8) muestra "— ZONA NO DESBLOQUEADA —" al entrar. SceneBoundary IZQUIERDA(x=-48)→MTN05(spawnId="mtn06_exit"). SceneBoundary DERECHA(x=+48)→MTN08(spawnId="mtn06_right"). SpawnPoint "mtn05_right"(x=-42,y=-13), "mtn08_exit"(x=+42,y=-13). Sky: estructura igual que MTN05 (Sky scale=0.485, hijos localScale=10). |
| MTN08 | 🔧 En progreso | Cruce de Vetas — cueva multinivel oscura sin cielo. Mecánica: CrystalHazard_Floor (trigger y=-17, ancho=130) → toca cristales → fade+teletransporte a última plataforma pisada -1 vida (CrystalRespawnManager). Fondo MTN08.png (scale=6.5). Paredes x=±55, suelo sólido y=-22. Techo orgánico EdgeCollider2D (13 puntos, ajustar en Scene View). FollowBounded boundsMin=(-50,-20), boundsMax=(50,18). 8 plataformas OneWay: Upper_Left(-35,-3,w22), Upper_Right(35,-3,w22), Mid_Left(-25,4,w18), Mid_Right(25,4,w18), Mid_Center(0,6,w20), High_Center(0,11,w24), Low_Left(-30,-11,w20), Low_Right(30,-11,w20). SceneBoundary IZQUIERDA(x=-48)→MTN06(spawnId="mtn08_exit"). SceneBoundary DERECHA(x=+48)→MTN09(spawnId="mtn08_right"). SpawnPoint "mtn06_right"(x=-44,y=-5), "mtn09_exit"(x=+44,y=-5). Sin Sky/DayCycle. |
| MTN09 | 🔧 En progreso | Antesala del Boss — cueva oscura en domo con Santuario de Ara al centro. Sin cielo. Fondo MTN09.png (scale=6.5). Paredes x=±52. Suelo escalonado EdgeCollider2D: (-52,-12)→(-12,-12)→(-12,-10)→(52,-10) (entrada izquierda 2u más baja). Techo domo EdgeCollider2D (11 puntos, pico y=+15 en centro). FollowBounded boundsMin=(-50,-20), boundsMax=(50,16). SanctuaryAra en (0,-7): llama azul, glow, prompt "Descansar". CrystalHazard_Bottom en (0,-19): zona de muerte al fondo. 5 plataformas OneWay: Plat_UpperLeft(-28,-2,w22), Plat_UpperRight(32,-3,w22), Plat_SanctuaryStep(0,-8.5,w12), Plat_MidLeft(-20,-6,w14), Plat_MidRight(20,-6,w14). SceneBoundary IZQUIERDA(x=-48)→MTN08(spawnId="mtn09_exit"). SceneBoundary DERECHA(x=+48)→MTN10(placeholder,spawnId="mtn09_right"). SpawnPoint "mtn08_right"(x=-44,y=-10), "mtn10_exit"(x=+44,y=-9). Menú `Eldoria/Setup MTN09`. Build Index=20. |

**SpawnPoints de referencia:**
- HV01_Interior: `default`(-11,-4), `door`(8.5,-4)
- HV01_Exterior: `door`(-48,-26), `left`(-75,-26), `right`(78,-30)
- HV02: `default`(0,5), `left`(-44,5)
- HV07: `default`, `right`

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
| `AudioManager.cs` | Singleton | Volumen música/SFX, persiste entre escenas (DontDestroyOnLoad). Auto-detecta AudioSources si inspector está vacío. |
| `SceneFader.cs` | Singleton | Transición con fade negro entre escenas |
| `SaveManager.cs` | Singleton | Carga/guarda partidas en JSON (4 slots). Bootstrap de GameSaveController en Awake. |
| `MainMenuManager.cs` | — | Botones Play/Options/Quit. Campo `menuMusic` → asignar clip en inspector. |
| `SettingsManager.cs` | Observer + State | 4 pestañas, eventos estáticos, localización integrada. `RefreshLocalizedSelectorOptions()` al cambiar idioma. |
| `SlotsScreenManager.cs` | State Machine + Observer | `_selectedSlot` es el estado; `RefreshGlobalButtons()` reacciona. Label SELECCIONAR dinámico: "NUEVA PARTIDA"/"CONTINUAR". |
| `KeyRebindUI.cs` | — | Reasignación de teclas con PlayerPrefs |
| `BackgroundVideoManager.cs` | Singleton | VideoPlayer persistente entre escenas; crea RenderTexture en Awake si `targetTexture==null`. |
| `SliderFillReveal.cs` | Observer | Efecto "before/after" en sliders visuales. Desconecta `fillRect`, usa `Image.Type=Filled`. |
| `PlayerController.cs` | State Machine | walk/run (Shift toggle), salto Z (coyote+buffer+variable), hasDash=false, double jump, wall slide/jump. Detección suelo via `rb.GetContacts`. `_baseScale` preserva escala Y al flip. |
| `PlayerAnimator.cs` | Observer | Parámetros: Speed, IsGrounded, IsRunning, IsJumping, IsFalling, IsDashing, IsWallSlide, Hurt, Die |
| `SaveData.cs` | — | Modelo de datos: `isEmpty`, `zoneName`, `sceneName`, `playTimeSeconds` |
| `SettingsManager.cs` actualización | — | Botón Back: si `PauseMenuManager.ReturnScene` tiene valor → carga esa escena (regreso desde pausa) y lo limpia; si no → carga MainMenu como antes. |
| `GameSaveController.cs` | Singleton + Observer + State | Autosave cada 30s. `ZoneNames` dict: HV01_Interior→"Casa de Kael", HV01_Exterior→"Exterior", HV02_PlazaCentral→"Plaza Central", HV04→"Zona A", HV05→"Zona B", HV06→"Zona C", HV07→"Camino a las Montañas", MTN01_Exterior→"Afueras de las Montañas", MTN01_Interior→"Entrada a las Montañas", MTN02→"Ruinas de las Laderas", MTN03→"La Bifurcación", MTN04→"Boca de las Cuevas", MTN05→"Galería de Cristal", MTN06→"Laboratorio en Ruinas", MTN08→"Cruce de Vetas", MTN09→"Antesala del Boss". |
| `IntroVideoManager.cs` | Command + Observer | Video intro, subtítulos cronometrados, skip (~1.2s manteniendo tecla). Carga "HV01_Interior" al terminar. |
| `SubtitleData.cs` | — | ScriptableObject {startTime, endTime, text}. Crear: Assets→Create→Eldoria→Subtitle Data |
| `SanctuaryFlame.cs` | State | Llama azul del Santuario de Ara. Estado Idle (parpadeo) / Near (prompt "Descansar" visible + emisión de partículas intensificada). Detección por distancia en Update. Referencias: `flameParticles`, `glowRenderer`, `promptText`. |
| `ParallaxBackground.cs` | Observer | Fórmula: `bg = cam*(1-f) + origin*f`. `parallaxFactor`(X), `parallaxFactorY`(Y, 1.0=estático). |
| `DayCycleController.cs` | State Machine | Night→Dawn→Day→Dusk→Night por tiempo total de partida (TotalPlayTime). phaseDuration=300s (5 min). CrossFade alpha entre fases. |
| `SteppedCameraBounds.cs` | Value Object | Límites de cámara con perfil escalonado para techos irregulares. Array de BoundsStep {x, yMin, yMax}. CameraFollow lo detecta en Start y lo usa con prioridad sobre CameraBoundsZone. Gizmo verde muestra el perfil en Scene View. |
| `OneWayPlatform.cs` | Strategy | Plataforma unidireccional. Usa `GetComponents<Collider2D>()` para ignorar TODOS los colliders. Chequeo horizontal evita bloqueo lateral. |
| `OneWayRamp.cs` | Strategy | Rampa unidireccional 45°. Proyecta en `transform.up/right`. RISING_THRESHOLD=2.0. PhysicsMaterial2D friction=2.0. |
| `DoorExit.cs` | Command | Trigger en puerta. `labelText` configurable. Presionar E → carga `targetScene` con SpawnId. |
| `CameraFollow.cs` | Strategy | Tres modos: FitRoom, FollowClamped, FollowBounded. `targetOffset` desplaza punto objetivo. Prioridad bounds: SteppedCameraBounds > CameraBoundsZone > boundsMin/Max manual. |
| `CameraBoundsZone.cs` | Value Object | BoxCollider2D Trigger. Gizmo verde en Scene View. Menú: `Eldoria/Add Camera Bounds`. |
| `PlayerCombat.cs` | State Machine | Combos melee tecla X. 3 estados. Hitbox `AttackHitbox` auto-creado. Damage via `IDamageable`. |
| `IDamageable.cs` | Strategy | Interfaz `TakeDamage(int)` |
| `PlayerSpawnManager.cs` | Singleton + Observer | DontDestroyOnLoad. `_sceneLoadHandled` flag evita doble `PlacePlayer()` (OnSceneLoaded dispara ANTES que Start). |
| `SceneBoundary.cs` | Command | Trigger en borde → `PlayerSpawnManager.NextSpawnId = spawnId` → carga escena vecina. |
| `SpawnPoint.cs` | Value Object | `spawnId` string. PSM busca todos y selecciona el coincidente. |
| `ScreenColorEffect.cs` | Strategy | Adjunto a Main Camera. Brillo/contraste/saturación vía `OnRenderImage`. Shader: `Eldoria/ScreenColorEffect`. |
| `LocalizationManager.cs` | Singleton + Observer + Flyweight | DontDestroyOnLoad. Tabla ES/EN estilo medieval. `Get(key)`, `SetLanguage(0/1)`. Bootstrap via `[RuntimeInitializeOnLoadMethod]`. PlayerPrefs: "LangCode". |
| `LocalizedText.cs` | Observer | `[RequireComponent(TMP_Text)]`. Key auto-inicializada desde texto en Awake. |
| `ScreenEffectsManager.cs` | Singleton + Facade | DontDestroyOnLoad. Adjunta ScreenColorEffect a Main Camera en cada OnSceneLoaded. `Apply(b,c,s)` y `EnsureExists()`. |
| `PauseMenuManager.cs` | Singleton + Observer + State | DontDestroyOnLoad desde MainMenu. Tecla Escape → pausa/reanuda solo si hay PlayerController en escena. Time.timeScale=0 al pausar. UI creada en Awake() con: PauseContainer.png (panel central), PauseTitle.png (banner "PAUSA"), 3 botones (normalButton): CONTINUAR → Resume(), AJUSTES → guarda escena actual en `ReturnScene` + carga Settings, SALIR → muestra ConfirmGroup inline. ConfirmGroup: CONFIRMAR (rojo, va a MainMenu), CANCELAR (vuelve a ButtonsGroup). Canvas sortOrder=200. Menú `Eldoria/Add Pause Menu`. |
| `CrystalRespawnManager.cs` | Singleton + Observer + State | Singleton local de escena. `Update` rastrea `IsGrounded` → `_lastSafePos`. `TriggerHazard()` → FadeOut → teleport → -1 vida → FadeIn → blink 2s. Dos flags: `_isRespawning` (bloquea cristales+enemigos, solo durante fade) y `_isBlinking` (bloquea solo enemigos durante parpadeo — cristales SIGUEN matando). `IsBlinking` public para enemigos. 5 vidas default. 0 vidas → SlotsScreen. |
| `CrystalHazard.cs` | Command | **PolygonCollider2D** editable en Scene View (igual que techo). Auto-genera glow radial ámbar sutil (gradiente cúbico, centro opaco→bordes transparente, alpha 4–18%). OnTriggerEnter2D(Player) → `TriggerHazard()`. Ignora `_isBlinking` (siempre activo). Inicializa polígono vacío con rectángulo 5×2u. |

## Sistema de efectos visuales de pantalla
- **Shader:** `Assets/Shaders/ScreenColorEffect.shader` — nombre Unity: `"Eldoria/ScreenColorEffect"`
- **Componente de cámara:** `ScreenColorEffect.cs` — ranges: brightness/contrast ±0.5f, saturation ±1f
- **Gestor global:** `ScreenEffectsManager.cs` — mapeo slider→shader: `brightness_shader = sliderValue - 0.5f`, `saturation_shader = (sliderValue - 0.5f) * 2f`
- **Integración con Settings:** `SettingsManager.SetupGraficos()` llama `ScreenEffectsManager.EnsureExists()` y `ApplyVisualEffects()` al iniciar y al mover cualquier slider visual.

## Assets en el proyecto Unity (`Assets/UI/Sprites/`)
| Carpeta | Archivos |
|---------|----------|
| `Buttons/` | normalButton, hoverButton, pressButton, settingsButtons (Normal/Hover/Press) |
| `Sliders/` | slider0% (fondo vacío), slider100%2 (fill/relleno), sliderButton (handle) |
| `Toggle/` | toggleOn, toggleOff |
| `Containers/` | Container1.png |
| `Logo/` | logo2.png |

**Fuentes:** Minecraft.ttf · Perfect DOS VGA 437.ttf (TMP Asset: `Perfect DOS VGA 437 Win SDF.asset`)
**Audio:** Celestial Kingdom, Enchanted Ruins, Ethereal Waters, Lost Temples, Mystical Forest, Peaceful Village (.ogg)
**Video:** BgEldoriaStartScreen.mp4

## Opciones de Gráficos — valores fijos
| Control | Opciones | Notas |
|---------|----------|-------|
| Resolución | 1920×1080 / 1600×900 / 1200×675 | Lista fija en `FixedResolutions` |
| Modo pantalla | Pantalla completa / Sin bordes / Ventana | `ExclusiveFullScreen / FullScreenWindow / Windowed` |
| Límite FPS | 120 / 144 / Sin límite | `FpsValues = {120, 144, -1}` · default Sin límite |
| VSync | Toggle on/off | `QualitySettings.vSyncCount` |

## Configuración de sliders en Settings — Sección Visual
Sliders de Brillo, Saturación y Contraste: Background=`slider0%`, Fill=`slider100%2`, Handle=`sliderButton`.
Componente `SliderFillReveal` en cada uno: desconecta `fillRect`, `Image.Type=Filled/Horizontal/Left`, actualiza `fillAmount` vía `onValueChanged`.

Jerarquía:
```
Canvas/RightPanel/GraficosPanel/Scroll View/Viewport/Content/
  Row_Brillo/Slider     ← SliderFillReveal + Slider
  Row_Saturacion/Slider ← SliderFillReveal + Slider
  Row_Contraste/Slider  ← SliderFillReveal + Slider
```

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
| `Resolution` | int | Índice de resolución |
| `LangCode` | int (0/1) | Idioma (0=ES, 1=EN) |
| `Key_<actionId>` | string | Teclas reasignadas (ej: Key_Jump) |

## Configuración de la escena Settings
```
[Canvas] (Screen Space - Camera, con MainCamera ortográfica cullingMask=0)
  ├─ Background (RawImage con VideoRenderTexture)
  ├─ LeftColumn
  │    ├─ BackButton (arriba-izquierda, fuera del TabGroup)
  │    ├─ [Espacio flexible]
  │    ├─ TabGroup (Vertical Layout Group)
  │    │    ├─ GraficosTabButton
  │    │    ├─ SonidoTabButton
  │    │    ├─ ControlesTabButton
  │    │    └─ AjustesTabButton
  │    └─ [Espacio flexible]
  └─ RightPanel (Container1.png)
       ├─ GraficosPanel  ← Scroll View con rows
       ├─ SonidoPanel    ← Scroll View con rows
       ├─ ControlesPanel ← KeyRebindUI
       └─ AjustesPanel   ← LanguageSelector

[AudioManager]  ← DontDestroyOnLoad desde MainMenu
[SceneFader]    ← DontDestroyOnLoad desde MainMenu
```
- Tab Active Sprite = `settingsButtons.png` | Tab Normal = `settingsButtonsNormal.png`
- Video: gestionado por `BackgroundVideoManager` — solo se necesita RawImage con VideoRenderTexture.

## SlotsScreen — arquitectura
```
Slot1 (dentro de Canvas/SlotsRow)
  ├─ CardButton  ← Button (SpriteSwap) + Image — ES la tarjeta visible
  ├─ EmptyState  ← QuestionMark TMP
  └─ OccupiedState ← LevelText, ZoneText, TimeText (TMP)
```
- Slot vacío: Normal=`SlotEmptyNormal` · Hover=`SlotEmptyHover` · Press=`SlotEmptyPress`
- Slot lleno: Normal=`SlotFilledNormal` · Hover=`SlotFilledHover` · Press=`SlotFilledPress`
- `Eldoria/Setup Slots Scene` — reconstruye todo | `Eldoria/Wire All Slots References` — solo recablea

## Reglas de diseño globales
**Parallax cielo (TODOS los cielos de TODAS las escenas):**
- Solo se mueven en eje X, nunca en Y. Sin bordes negros.
- Cámara móvil (FollowBounded/FollowClamped): `parallaxFactor=0.12, parallaxFactorY=1, trackPlayer=false`
- Cámara fija (FitRoom): `parallaxFactor=0.88, parallaxFactorY=1, trackPlayer=true`

## PENDIENTES
### Settings (ejecutar en Unity con escena Settings abierta, en orden):
1. `Eldoria/Add Settings Content` — reconstruye SonidoPanel + AjustesPanel, fija padding, cablea paneles al SettingsManager
2. `Eldoria/Add Localization Labels` — añade `LocalizedText` a todos los labels estáticos (TMP con nombre "Lbl" y tabs)
3. `Eldoria/Fix Settings Camera` — añade MainCamera ortográfica y cambia Canvas a Screen Space - Camera
4. Ctrl+S para guardar la escena

### Ajustes en Scene View:
- HV02: mover las 4 puertas (Door_HV04/HV05/HV06/HV02Interior) para alinear con sprite de fondo
- HV02: ajustar CameraBounds (Edit Collider → arrastrar al suelo/techo/paredes reales)
- HV01_Exterior: `Eldoria/Add Camera Bounds` → ajustar rectángulo verde
- HV02: mover y reposicionar Ramp_LeftToRight y Ramp_RightToLeft para alinear con geometría

### Intro:
- Crear asset `SubtitleData` (Assets→Create→Eldoria→Subtitle Data), rellenar entradas {startTime, endTime, text} con el guión, asignarlo al campo `subtitleData` del IntroVideoManager en la escena Intro.
