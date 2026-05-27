# REPASO COMPLETO — ELDORIA
> Documento de estudio para exposición universitaria — Diseño de Interfaces
> Repasa bloque por bloque. Cada sección es independiente.

---

## BLOQUE 1 — ¿Qué es Eldoria?

Eldoria es un videojuego 2D estilo **Metroidvania** desarrollado como proyecto universitario para la asignatura Diseño de Interfaces. El enfoque principal del proyecto es la **capa de interfaz de usuario**: menús, sistemas de guardado, navegación entre pantallas y experiencia del jugador.

El género Metroidvania se caracteriza por un mundo interconectado que el jugador explora progresivamente, desbloqueando habilidades que permiten acceder a zonas antes inaccesibles. Ejemplos conocidos del género: Hollow Knight, Castlevania, Metroid.

**En números:**
- 19+ zonas jugables conectadas entre sí
- 1 boss con 6 fases y 6 tipos de ataques
- 4 ranuras de guardado con autosave
- 10 Singletons persistentes entre escenas
- 6 patrones de diseño aplicados
- 55+ scripts de C#

---

## BLOQUE 2 — Tecnologías Utilizadas

### Unity 2022.3 LTS
El motor del juego. Se encarga de renderizar los gráficos, simular la física, reproducir audio, gestionar las escenas y ejecutar los scripts. LTS significa Long Term Support — versión con soporte garantizado, ideal para proyectos universitarios porque no se rompe con actualizaciones inesperadas.

### C# (C Sharp)
El lenguaje de programación de todos los scripts. Es orientado a objetos, lo que permite aplicar los patrones de diseño de forma natural. Cada archivo `.cs` es una clase que define el comportamiento de un sistema del juego.

### TextMeshPro
Librería de Unity para texto de alta calidad. Permite usar fuentes pixel art (como `Perfect DOS VGA 437`) con renderizado nítido a cualquier resolución, sin el pixelado o bordes borrosos del texto estándar de Unity.

### Unity VideoPlayer + RenderTexture
El `VideoPlayer` reproduce archivos `.mp4` dentro del juego. La `RenderTexture` es una textura virtual — el video se renderiza sobre ella en lugar de directamente en pantalla, lo que permite mostrarlo como fondo en cualquier escena sin que se reinicie al cambiar de pantalla.

### Unity Animator
Sistema visual de animaciones. Funciona como una máquina de estados: cada estado tiene un clip de animación (ej: idle, correr, saltar) y las transiciones entre estados se configuran con condiciones (ej: "si Speed > 0.1 y IsGrounded, pasar a Walk"). `PlayerAnimator.cs` alimenta estos parámetros cada frame.

### Physics2D
El motor de física 2D de Unity. Gestiona Rigidbody2D (cuerpos físicos), BoxCollider2D (cajas de colisión), y detecta contactos entre objetos. `PlayerController` lo usa para detectar si Kael está en el suelo leyendo los ContactPoint2D reales en lugar de hacer raycasts.

### PlayerPrefs
Sistema de almacenamiento clave-valor que Unity guarda en el registro de Windows. Se usa para configuración del juego (volumen, resolución, brillo) y para el mapa de zonas visitadas. Persiste entre sesiones sin necesidad de manejar archivos manualmente.

### JsonUtility
Utilidad de Unity para serializar y deserializar objetos C# a formato JSON. Se usa para guardar y cargar las partidas. Un objeto `SaveData` se convierte a texto JSON y se escribe en un archivo `.json` en el disco del jugador.

### HLSL / ShaderLab
Lenguajes para escribir shaders — programas que corren en la tarjeta gráfica y modifican los píxeles en tiempo real. El shader `Eldoria/ScreenColorEffect` aplica brillo, contraste y saturación a cada fotograma antes de mostrarlo en pantalla.

### Aseprite
Programa externo a Unity para crear sprites pixel art. Se usó para crear los personajes, fondos y elementos visuales del juego. Los archivos exportados como PNG se importan a Unity con PPU (Pixels Per Unit) configurado para mantener la escala correcta.

### Git
Sistema de control de versiones. Permite guardar el historial de cambios del proyecto y revertir a versiones anteriores si algo se rompe.

---

## BLOQUE 3 — Flujo completo del juego

Entender el flujo es fundamental para la exposición porque muestra cómo todos los sistemas se conectan.

```
Jugador abre el juego
        ↓
[MainMenu] — video de fondo en loop + música + 3 botones
        ↓ clic "Jugar"
[SlotsScreen] — 4 ranuras de guardado
    ├── Slot vacío → clic → [Intro] video cinemático → [HV01_Interior]
    └── Slot lleno → clic "Continuar" → carga la escena guardada directamente
        ↓ clic "Opciones"
[Settings] — 4 pestañas: Gráficos / Sonido / Controles / Ajustes
        ↓ durante el juego, presionar Escape
[PauseMenu] — Continuar / Ajustes / Salir al menú
```

**Flujo del mundo jugable:**
```
HV01_Interior (Casa de Kael)
        ↕ puerta
HV01_Exterior
        ↕ borde derecho
HV02_PlazaCentral
    ├── HV04 (Zona A)
    ├── HV05 (Zona B)
    ├── HV06 (Zona C)
    └── HV07 (Corredor) → MTN01 → MTN02 → ... → MTN09 → MTN10 (Boss)
```

---

## BLOQUE 4 — Los 10 Singletons

### ¿Qué es un Singleton?
Un sistema que existe **una sola vez** en toda la aplicación y es accesible desde cualquier parte. Persiste entre escenas con `DontDestroyOnLoad`. Si Unity intenta crear un segundo, el duplicado se destruye a sí mismo.

**Código base de todos los Singletons:**
```csharp
public static AudioManager Instance { get; private set; }

void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

---

### 1. AudioManager
**¿Qué hace?** Controla toda la música y efectos de sonido del juego.

**Ejemplos de uso:**
- Boss despierta → `AudioManager.Instance.PlayMusic(bossMusic)` → suena Mountain Storm
- Boss muere → `AudioManager.Instance.FadeOutMusic(2f)` → música baja en 2 segundos
- Jugador pausa → `AudioManager.Instance.StopMusic()` → silencio

**¿Por qué Singleton?** Si hubiera dos, dos músicas sonarían al mismo tiempo.

---

### 2. SaveManager
**¿Qué hace?** Lee y escribe los archivos `.json` de las 4 partidas en el disco.

**Ejemplos de uso:**
- Nueva partida → `SaveManager.Instance.Save(0, data)` → crea `save_slot_0.json`
- Continuar → `SaveManager.Instance.Load(0)` → lee el archivo y devuelve los datos
- Borrar → `SaveManager.Instance.Delete(0)` → elimina el archivo del disco

**¿Por qué Singleton?** Dos instancias podrían escribir el mismo archivo simultáneamente y corromperse.

---

### 3. SceneFader
**¿Qué hace?** Gestiona el fade negro entre escenas — pantalla se vuelve negra, carga la escena, pantalla vuelve a verse.

**Ejemplos de uso:**
- Puerta → `SceneFader.Instance.LoadScene("HV01_Exterior")`
- Boss derrota → `SceneFader.Instance.FastFadeOutAsync(1.5f)` → negro lento dramático
- Al morir → fade out → carga santuario → `SceneFader.Instance.FadeInAsync()`

**¿Por qué Singleton?** Dos faders activos dejarían la pantalla atascada en negro.

---

### 4. BackgroundVideoManager
**¿Qué hace?** Mantiene el VideoPlayer corriendo sin interrupciones entre pantallas. Renderiza a una RenderTexture que cualquier escena puede mostrar.

**Ejemplos de uso:**
- MainMenu → SlotsScreen → el video sigue en el minuto exacto donde estaba
- `BackgroundVideoManager.Instance.SwitchClip(slotsBgClip)` → cambia al video de Slots

**¿Por qué Singleton?** Sin él cada escena crearía su propio VideoPlayer y el video empezaría desde el principio cada vez.

---

### 5. PlayerSpawnManager
**¿Qué hace?** Decide en qué punto exacto aparece Kael cuando carga una escena nueva. Busca el SpawnPoint con el ID correcto y teletransporta al jugador ahí.

**Ejemplos de uso:**
- Salir de la casa → `PlayerSpawnManager.NextSpawnId = "door"` → en HV01_Exterior aparece junto a la puerta
- Cruzar borde a MTN01 → `NextSpawnId = "left"` → aparece en el lado izquierdo de MTN01
- Morir → `NextSpawnId = "default"` → aparece en el SpawnPoint por defecto de la escena

**¿Por qué Singleton?** Persiste entre escenas para que el ID de spawn no se pierda durante la carga.

---

### 6. PauseMenuManager
**¿Qué hace?** Gestiona el menú de pausa (Escape). Pausa el tiempo del juego, muestra el overlay y maneja el cursor.

**Ejemplos de uso:**
- Escape durante MTN05 → juego se congela, aparece menú
- Alt+Tab → `OnApplicationFocus(false)` → pausa automática
- "Ajustes" desde pausa → guarda `ReturnScene = "MTN05"` → va a Settings → vuelve a MTN05

**¿Por qué Singleton?** El menú de pausa debe existir en todas las escenas de juego sin recrearse.

---

### 7. WorldMapController
**¿Qué hace?** Gestiona el mapa interactivo (tecla M). Recuerda zonas visitadas, las muestra en el mapa y hace parpadear la zona actual.

**Ejemplos de uso:**
- Entrar a MTN05 por primera vez → `MarkVisited("MTN05")` → se guarda en PlayerPrefs
- Abrir mapa (M) → solo se ven las zonas ya visitadas, MTN05 parpadea suavemente
- Tab → alterna entre vista Hub y vista Montañas

**¿Por qué Singleton?** Necesita sobrevivir entre escenas para recordar las zonas visitadas.

---

### 8. ZoneMusicController
**¿Qué hace?** Cambia la música automáticamente según la zona y reacciona a los eventos del boss.

**Flujo de música:**
```
HV* (Hub)          → Celestial Kingdom
MTN01-MTN09        → Enchanted Ruins
PreMTN10           → Cave Ambience (silencio + cueva)
MTN10 (sala boss)  → Silencio total
Boss Phase1        → Mountain Storm
Boss muerto        → Silencio
```

**¿Por qué Singleton?** Debe estar activo en todas las escenas y escuchar eventos del boss que ocurren en MTN10.

---

### 9. ScreenEffectsManager
**¿Qué hace?** Aplica y mantiene el brillo, contraste y saturación configurados en opciones. En cada nueva escena se adjunta automáticamente a la cámara principal.

**Ejemplos de uso:**
- Mover slider de brillo → `ScreenEffectsManager.Apply(0.7f, 0.5f, 0.5f)` → efecto inmediato
- Cargar MTN06 → se adjunta al Main Camera de MTN06 y aplica los valores guardados en PlayerPrefs

**¿Por qué Singleton?** Sin él cada escena se vería con los valores por defecto aunque el jugador haya configurado algo diferente.

---

### 10. GameSaveController
**¿Qué hace?** Autosave cada 30 segundos y al cambiar de zona. Cuenta el tiempo de juego acumulado.

**Ejemplos de uso:**
- Llevar 30 segundos en MTN03 → guarda automáticamente el tiempo actualizado
- Entrar a MTN09 → actualiza `zoneName = "Antesala del Boss"` y guarda inmediatamente
- Al volver al juego → el slot muestra "Antesala del Boss — 01:04:23"

**¿Por qué Singleton?** Debe contar el tiempo continuamente sin reiniciarse al cambiar de escena.

---

## BLOQUE 5 — Patrón Observer

### ¿Qué es?
Un objeto publica que algo ocurrió. Todos los objetos suscritos a esa notificación reaccionan automáticamente. El publicador no sabe quiénes son sus oyentes.

**Analogía:** Canal de YouTube. El canal sube un video y todos los suscriptores reciben notificación. El canal no sabe quiénes son, solo publica.

**¿Por qué se usa?** Sin Observer los sistemas tendrían que conocerse directamente (acoplamiento fuerte). Agregar un sistema nuevo requeriría modificar el existente. Con Observer simplemente te suscribes y listo.

---

### Caso 1 — PlayerAnimator observa a PlayerController

`PlayerController` maneja la física. `PlayerAnimator` maneja las animaciones. Ninguno sabe del otro directamente — `PlayerAnimator` simplemente lee las propiedades públicas de `PlayerController` cada frame y actualiza el Animator.

```csharp
void Update()
{
    anim.SetFloat(PSpeed,      ctrl.SpeedX);       // velocidad horizontal
    anim.SetBool (PIsGrounded, ctrl.IsGrounded);   // ¿en el suelo?
    anim.SetBool (PIsJumping,  ctrl.IsJumping);    // ¿saltando?
    anim.SetBool (PIsFalling,  ctrl.IsFalling);    // ¿cayendo?
    anim.SetBool (PIsDashing,  ctrl.IsDashing);    // ¿en dash?
    anim.SetBool (PIsRunning,  ctrl.IsRunning);    // ¿corriendo?
}
```

**Resultado:** cuando Kael salta, el sprite cambia a la animación de salto automáticamente. Cuando cae, cambia a la de caída. Sin este sistema habría que poner código de animaciones dentro del controlador de física.

---

### Caso 2 — Boss notifica a la música y a la barra de vida

`BossObsesion` declara eventos públicos:
```csharp
public static event System.Action<int, int>  OnHealthChanged;  // nueva vida, vida máxima
public static event System.Action<BossPhase> OnPhaseChanged;   // nueva fase
public static event System.Action            OnBossDead;       // murió
```

`BossHealthBar` se suscribe a `OnHealthChanged` y actualiza la barra de vida visualmente.
`ZoneMusicController` se suscribe a `OnPhaseChanged` y cuando el boss entra a Phase1, cambia la música a Mountain Storm. Se suscribe a `OnBossDead` y para la música cuando el boss muere.

El boss nunca llama directamente a `BossHealthBar` ni a `ZoneMusicController`. Solo dispara el evento y quien esté suscrito reacciona.

---

### Caso 3 — GameSaveController y WorldMapController escuchan SceneManager

Ambos se suscriben al evento de Unity que avisa cuando una escena termina de cargar:

```csharp
// En Awake():
SceneManager.sceneLoaded += OnSceneLoaded;

// Se ejecuta automáticamente al cargar cualquier escena:
void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    // GameSaveController: guarda la zona nueva
    // WorldMapController: marca la zona como visitada
}

// Al destruirse, se desuscribe para no dejar referencias colgantes:
void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;
```

**Resultado:** no importa cómo ni desde dónde se cargue una escena, el guardado y el mapa siempre se actualizan.

---

### Caso 4 — CrystalRespawnManager notifica al HUD

Cuando el jugador pierde una vida al tocar un cristal o recibir daño del boss:
```csharp
public static event System.Action<int>      OnLivesChanged;   // vidas actuales
public static event System.Action<int, int> OnDamageTaken;    // nuevas vidas, vidas anteriores
public static event System.Action<int>      OnLivesRestored;  // al descansar en santuario
```

`PlayerLivesHUD` está suscrito y actualiza los iconos de Ara (vidas) en pantalla cuando recibe cualquiera de estas notificaciones.

---

### Caso 5 — SettingsManager notifica cambios de volumen

```csharp
public static event System.Action<float> OnMusicVolumeChanged;
public static event System.Action<float> OnSFXVolumeChanged;

// Al mover el slider:
OnMusicVolumeChanged?.Invoke(nuevoValor);
```

`AudioManager` está suscrito y ajusta el volumen. `SettingsManager` no sabe que `AudioManager` existe.

---

## BLOQUE 6 — Patrón State Machine

### ¿Qué es?
El objeto tiene un estado activo en cada momento y su comportamiento cambia completamente según ese estado. Solo puede estar en un estado a la vez.

**Analogía:** Un semáforo. Solo puede estar en rojo, amarillo o verde. Cada color significa un comportamiento distinto para los autos.

---

### Caso 1 — Estados de Kael (PlayerController)

Kael tiene propiedades de estado que toda la aplicación puede leer:
```
IsGrounded, IsRunning, IsJumping, IsFalling, IsDashing, IsWallSliding
```

**Transiciones posibles:**
```
Idle ──────────────────→ Walking (Speed > 0.1)
Walking ───────────────→ Running (Shift toggle + Speed > 0.1)
Idle/Walking/Running ──→ Jumping (presionar Z, coyoteCounter > 0)
Jumping ───────────────→ Falling (velocidad Y < 0)
Falling ───────────────→ Idle (aterriza)
Cualquiera ────────────→ Dashing (Shift, si hasDash = true)
En aire + pared ───────→ WallSliding (si hasWallClimb = true)
```

Cada estado cambia qué puede hacer Kael: en dash no se aplica gravedad, en WallSlide la velocidad de caída es limitada, en Idle no se mueve.

---

### Caso 2 — Fases del Boss (BossObsesion)

```
Dormant → Waking → Phase1 → Phase2 → Phase3 → Defeated → Dead
```

| Fase | Condición | Cambios |
|------|-----------|---------|
| Dormant | Estado inicial | Duerme, no ataca |
| Waking | Jugador entra en rango | Animación de despertar + música |
| Phase1 | Al despertar | Ataca con delay moderado |
| Phase2 | HP ≤ 50% | Velocidad +25%, repite ataques hasta 3 veces |
| Phase3 | HP ≤ 25% | Velocidad +50%, casi sin pausa entre ataques |
| Defeated | HP = 0 | Se congela, espera que el jugador extraiga el fragmento |
| Dead | Extracción completada | Desaparece, se desbloquea el dash |

---

### Caso 3 — Ciclo día/noche (DayCycleController)

```
Night → Dawn → Day → Dusk → Night (bucle infinito)
```

Cada estado tiene una imagen del cielo. Al cambiar de estado, el sistema hace un crossfade suave entre la imagen actual y la siguiente. El estado se determina por el tiempo total de juego acumulado — si llevas 300 segundos jugando estás en Amanecer, si llevas 600 en Día, etc.

---

### Caso 4 — Pestañas de Settings (SettingsManager)

```
GraficosPanel activo → SonidoPanel activo → ControlesPanel activo → AjustesPanel activo
```

Al hacer clic en una pestaña, `ShowPanel()` desactiva todos los paneles y activa solo el seleccionado. El sprite del botón también cambia entre activo y normal.

---

### Caso 5 — Sistema de vidas (CrystalRespawnManager)

```
Normal → Respawning (durante fade + teleport) → Blinking (2s post-respawn) → Normal
```

- En **Respawning**: ni cristales ni boss pueden dañar al jugador.
- En **Blinking**: los cristales siguen siendo letales pero los ataques del boss no dañan (ventana de invulnerabilidad post-respawn).
- En **Normal**: todo daña normalmente.

---

## BLOQUE 7 — Patrón Strategy

### ¿Qué es?
Define comportamientos intercambiables. El sistema que los usa no sabe cuál está usando — solo hace una pregunta y recibe una respuesta.

**Analogía:** Un repartidor que puede usar bicicleta, moto o carro. La pizzería siempre le da el pedido de la misma forma — el cómo llega es decisión del repartidor.

---

### Caso 1 — OneWayPlatform y OneWayRamp

Ambas responden la misma pregunta: `ShouldIgnore()` — ¿debo dejar pasar al jugador?

`OneWayPlatform` compara posición Y:
```
Si jugador está debajo → ignorar colisión
Si jugador está de lado → ignorar colisión
Si jugador sube → ignorar colisión
En cualquier otro caso → colisionar
```

`OneWayRamp` hace lo mismo pero proyectando sobre `transform.up` (la normal de la rampa inclinada) en lugar de comparar Y puro.

`PlayerController` llama `TriggerDropThrough()` en ambas sin saber cuál es cuál.

---

### Caso 2 — CameraFollow (4 modos)

| Modo | Escenas que lo usan | Comportamiento |
|------|--------------------|-|
| FitRoom | HV01_Interior | Ajusta el tamaño al fondo y no se mueve |
| FollowClamped | Escenas con fondo fijo | Sigue al jugador sin salirse del fondo |
| FollowBounded | HV01_Exterior, HV02, MTN* | Sigue dentro de un rectángulo configurable |
| FreeFollow | Debug / salas sin límites | Sigue al jugador sin restricciones |

---

### Caso 3 — ChooseAttack() del Boss

El boss elige entre 6 estrategias de ataque dependiendo de la distancia al jugador y la fase actual:

| Ataque | Distancia | Descripción |
|--------|-----------|-------------|
| melee | Cerca | Golpe cuerpo a cuerpo |
| range | Lejos | Disparo directo |
| boomerang | Lejos | Brazos que salen y vuelven |
| spincharge | Cualquiera | Dash con hitbox activo |
| super | Cualquiera | Golpe de área fuerte |
| armsweep | Cualquiera | Brazos rasantes al nivel del suelo |

Además tiene la **mecánica de Obsesión**: repite el mismo ataque 2-4 veces antes de cambiar (más repeticiones en fases avanzadas).

---

### Caso 4 — ScreenColorEffect (Shader)

El shader se adjunta a cualquier cámara y aplica el post-proceso sin que la cámara necesite saber cómo funciona el shader internamente.

```csharp
void OnRenderImage(RenderTexture src, RenderTexture dst)
{
    _mat.SetFloat("_Brightness", brightness);
    _mat.SetFloat("_Contrast",   contrast);
    _mat.SetFloat("_Saturation", saturation);
    Graphics.Blit(src, dst, _mat);  // aplica el shader a cada frame
}
```

---

## BLOQUE 8 — Patrón Command

### ¿Qué es?
Encapsula una acción como un objeto independiente. Quien pide la acción no sabe cómo se ejecuta, y quien la ejecuta no sabe quién la pidió.

**Analogía:** La orden de un restaurante en un papel. El cliente escribe el pedido, el mesero lo lleva, la cocina lo ejecuta. Ninguno necesita conocer a los demás.

---

### Caso 1 — DoorExit
Cada puerta tiene configurado un `targetScene` y un `labelText`. Cuando el jugador presiona E, ejecuta la carga de escena sin saber cómo funciona SceneFader.

```
Kael presiona E → DoorExit.OnInteract() → SceneFader.Instance.LoadScene("HV01_Exterior")
```

---

### Caso 2 — SceneBoundary
Al cruzar el borde de una zona, el boundary escribe el ID de spawn y carga la escena vecina.

```
Kael cruza borde izquierdo de HV02 → PlayerSpawnManager.NextSpawnId = "right" → LoadScene("HV07")
```

---

### Caso 3 — ArenaBarrier
Al despertar el boss, las barreras se activan bloqueando todas las salidas de la arena. Al morir el boss, se desactivan. El boss solo ejecuta la orden, no gestiona las barreras directamente.

---

### Caso 4 — PauseMenu → Settings
`PauseMenuManager` guarda `ReturnScene = "MTN05"` antes de ir a Settings. Cuando el jugador sale de Settings, el botón Back lee `ReturnScene` y sabe exactamente a dónde volver.

---

## BLOQUE 9 — Patrón Facade

### ¿Qué es?
Oculta un sistema complejo detrás de una interfaz simple. El resto del juego solo ve y usa esa interfaz simple.

**Analogía:** El botón de encendido de un auto. Por dentro hay cientos de procesos. Tú solo presionas el botón.

---

### Caso — ScreenEffectsManager

Lo que hay por dentro: buscar la cámara principal, verificar si tiene el componente `ScreenColorEffect`, adjuntarlo si no existe, convertir los valores del slider (0-1) al rango del shader (-0.5 a 0.5), aplicarlos al material.

Lo que ve `SettingsManager`:
```csharp
ScreenEffectsManager.Apply(0.7f, 0.5f, 0.6f);  // una sola línea
```

---

## BLOQUE 10 — Estructuras de Datos

### ¿Qué es una estructura de datos?
La forma en que el programa organiza y almacena la información en memoria. Elegir la correcta hace el código más rápido y más claro.

---

### Array fijo — `SaveData[4]`

Los 4 slots de guardado son un array de tamaño fijo.

```csharp
private readonly SaveData[] _saves = new SaveData[4];
```

**¿Por qué array y no lista?** El número de slots nunca cambia. El acceso por índice es instantáneo: `_saves[2]` llega directo al tercer slot sin recorrer nada. Una lista permitiría agregar y eliminar elementos, lo cual no se necesita aquí.

---

### Dictionary — mapeo de escenas a zonas

```csharp
private static readonly Dictionary<string, string> ZoneNames = new()
{
    { "HV01_Interior",     "Casa de Kael"  },
    { "MTN05",             "Galería de Cristal" },
    // ...
};
```

**¿Por qué Dictionary?** La búsqueda por clave es O(1) — instantánea sin importar cuántas zonas existan. `ZoneNames["MTN05"]` devuelve `"Galería de Cristal"` directamente.

---

### Buffer reutilizable — `ContactPoint2D[8]`

```csharp
private readonly ContactPoint2D[] _contacts = new ContactPoint2D[8];

// En Update(), 60 veces por segundo:
int count = rb.GetContacts(_contacts);
```

**¿Por qué no crear un array nuevo cada frame?** Crear y destruir memoria 60 veces por segundo activa el recolector de basura de C# que hace pausas ocasionales en el juego (micro-freezes). El buffer se crea una vez en `Awake()` y se reutiliza siempre.

---

### JSON en disco — archivos de partida

```json
{
  "isEmpty": false,
  "slotName": "Partida 1",
  "zoneName": "Galería de Cristal",
  "sceneName": "MTN05",
  "playTimeSeconds": 3847.5,
  "level": 1,
  "health": 4
}
```

**¿Por qué JSON?** Es legible por humanos (puedes abrirlo con Notepad), Unity lo serializa y deserializa con una sola línea (`JsonUtility.ToJson` / `FromJson`), y persiste completamente entre sesiones sin depender de la memoria RAM.

---

### PlayerPrefs — configuración y mapa visitado

```
"MusicVolume"        → 0.8       (float)
"Brightness"         → 0.65      (float)
"VSync"              → 1         (int: 0 o 1)
"Resolution"         → 0         (int: índice)
"MapVisited_MTN05"   → 1         (int: 0 o 1)
"MapVisited_MTN10"   → 0         (int: no visitado)
"SanctuaryScene"     → "MTN03"   (string)
"SanctuaryX"         → -12.5     (float)
```

**¿Por qué PlayerPrefs?** Para datos pequeños y frecuentemente leídos es ideal — acceso inmediato, gestionado por Unity en el registro de Windows, no requiere manejo de archivos.

---

### ScriptableObject — ZoneMusicConfig

Contiene los AudioClips de cada bioma configurados desde el editor de Unity sin tocar el código.

```csharp
public class ZoneMusicConfig : ScriptableObject
{
    public AudioClip hvMusic;       // Celestial Kingdom
    public AudioClip mtnMusic;      // Enchanted Ruins
    public AudioClip caveAmbience;  // Cave sound
    public AudioClip bossMusic;     // Mountain Storm
}
```

**¿Por qué ScriptableObject?** Separa los datos del código. Para cambiar qué canción suena en el Hub solo arrastras otro clip en el editor, sin modificar ningún script.

---

## BLOQUE 11 — Sistemas especiales

### Sistema de vidas — CrystalRespawnManager

El jugador tiene 5 vidas representadas por iconos de Ara en pantalla. Al tocar un cristal de daño pierde 1 vida y reaparece en el último punto seguro pisado (el suelo). Al llegar a 0 vidas, la animación de muerte completa se reproduce y el jugador vuelve al último santuario activado.

**Flujo de daño normal:**
```
Kael toca cristal → TriggerHazard() → animación Hurt (0.58s) →
fade negro → teletransporte a lastSafePos → fade de vuelta → parpadeo 2s
```

**Flujo de muerte (0 vidas):**
```
Kael toca cristal con 1 vida → animación Death (1.92s) →
fade negro → carga escena del último santuario visitado
```

**Punto seguro:** cada frame que Kael está en el suelo (`IsGrounded = true`), su posición se guarda en `_lastSafePos`. Así el respawn siempre lleva al último suelo pisado.

---

### Sistema de santuarios — SanctuaryFlame

Al interactuar con un santuario de Ara (hoguera), el juego guarda la posición del santuario en PlayerPrefs. Si el jugador muere, respawnea en ese punto con las vidas completas. Es el equivalente a las hogueras de Dark Souls o los bancos de Hollow Knight.

```
"SanctuaryScene" → "MTN03"
"SanctuaryX"     → -12.5
"SanctuaryY"     → -8.0
```

---

### Sistema de spawn — PlayerSpawnManager

Cada escena tiene objetos `SpawnPoint` con un ID string. Antes de cargar una escena, cualquier sistema puede escribir `PlayerSpawnManager.NextSpawnId = "left"`. Al terminar de cargar, `PlayerSpawnManager` busca el SpawnPoint con ese ID y teletransporta a Kael ahí.

**Bug histórico resuelto:** Unity dispara `sceneLoaded` antes que `Start()`. Sin el flag `_sceneLoadHandled`, el jugador era teletransportado dos veces — una al punto correcto (por `OnSceneLoaded`) y otra de vuelta al punto por defecto (por `Start()`). El flag evita que `Start()` ejecute `PlacePlayer()` si `OnSceneLoaded` ya lo hizo.

---

### Sistema de video de fondo

El `BackgroundVideoManager` (Singleton) crea una `RenderTexture` en `Awake()` si no existe, configura el `VideoPlayer` para renderizar sobre ella, y reproduce el video. Cada escena tiene un `RawImage` que muestra esa textura. Como el Singleton persiste, el video nunca se reinicia al cambiar de escena.

Si se abre una escena directamente (sin pasar por MainMenu), `SlotsScreenManager.SetupBackground()` detecta que no hay `BackgroundVideoManager` y crea un VideoPlayer local temporal para esa escena.

---

### Sistema de efectos de pantalla

**Shader** (`ScreenColorEffect.shader`): corre en la tarjeta gráfica. Recibe el fotograma renderizado, aplica la fórmula matemática de brillo/contraste/saturación píxel por píxel, y lo entrega a pantalla.

**Componente** (`ScreenColorEffect.cs`): se adjunta a la Main Camera. En `OnRenderImage` intercepta el fotograma antes de mostrarlo y aplica el shader.

**Gestor** (`ScreenEffectsManager.cs`): Singleton que en cada `sceneLoaded` adjunta `ScreenColorEffect` a la cámara nueva y aplica los valores guardados en PlayerPrefs.

**Conversión de valores:**
```
Slider 0.5 (centro) → shader 0.0 (neutro, sin efecto)
Slider 0.0          → shader -0.5 (mínimo brillo/contraste)
Slider 1.0          → shader +0.5 (máximo brillo/contraste)
Saturación: (slider - 0.5) × 2 → rango más amplio ±1
```

---

### Sistema de parallax

`ParallaxBackground` mueve los fondos a velocidades distintas para simular profundidad. La fórmula es:

```
posición_fondo = posición_referencia × (1 - factor) + posición_origen × factor
```

- `factor = 0` → el fondo sigue exactamente a la cámara (sin efecto parallax)
- `factor = 1` → el fondo es completamente estático
- `factor = 0.12` → el fondo se mueve al 88% de la velocidad de la cámara (parallax sutil)

El cielo usa `parallaxFactorY = 1.0` para que no suba ni baje con la cámara — solo se mueve horizontalmente.

---

## BLOQUE 12 — El mundo jugable completo

### Mapa de zonas

| ID | Nombre | Escena Unity |
|----|--------|-------------|
| HUB01 | Casa de Kael | HV01_Interior / HV01_Exterior |
| HUB02 | Plaza Central | HV02_PlazaCentral |
| HUB04 | Zona A | HV04 |
| HUB05 | Zona B | HV05 |
| HUB06 | Zona C | HV06 |
| HUB07 | Camino a las Montañas | HV07 |
| MTN01 | Afueras de las Montañas | MTN01_Exterior / MTN01_Interior |
| MTN02 | Ruinas de las Laderas | MTN02 |
| MTN03 | La Bifurcación | MTN03 |
| MTN04 | Boca de las Cuevas | MTN04 |
| MTN05 | Galería de Cristal | MTN05 |
| MTN06 | Laboratorio en Ruinas | MTN06 |
| MTN07 | Zona Bloqueada | MTN07 |
| MTN08 | Cruce de Vetas | MTN08 |
| MTN09 | Antesala del Boss | MTN09 |
| MTN10 | Sala del Boss | MTN10 |

---

## BLOQUE 13 — Posibles preguntas en la exposición

**¿Por qué Unity y no otro motor?**
Unity es el estándar de la industria para juegos 2D indie, tiene documentación extensa, una comunidad enorme, y C# es un lenguaje con buen soporte de patrones orientados a objetos. Además permite construir tanto la UI como el gameplay en el mismo entorno.

**¿Por qué usaste patrones de diseño?**
Porque sin ellos el código crece desordenado y es difícil de mantener. Los patrones son soluciones probadas a problemas recurrentes. Por ejemplo, sin Singleton el AudioManager podría duplicarse al cambiar de escena y reproducir dos músicas simultáneamente. Los patrones no son reglas arbitrarias — cada uno resuelve un problema concreto.

**¿Cuál fue el problema más difícil que resolviste?**
El bug del sistema de spawn doble. Unity dispara `sceneLoaded` antes que `Start()`, lo que causaba que el jugador fuera teletransportado dos veces — una al punto correcto y otra de vuelta al por defecto. La solución fue un flag `_sceneLoadHandled` que evita que `Start()` ejecute el spawn si `OnSceneLoaded` ya lo hizo.

**¿Cómo manejas que el juego no pierda datos si se cierra inesperadamente?**
El autosave cada 30 segundos y el guardado inmediato al cambiar de zona minimizan la pérdida máxima a 30 segundos. Además el sistema de santuarios guarda un punto de respawn adicional para no perder progreso de exploración.

**¿Qué diferencia hay entre PlayerPrefs y los archivos JSON?**
PlayerPrefs guarda pequeñas configuraciones (volumen, resolución, zonas visitadas) en el registro de Windows — acceso inmediato pero sin estructura compleja. Los archivos JSON guardan los datos completos de partida (zona, tiempo, vidas, escena) en el disco como archivos independientes — más organizados y legibles.

**¿Cómo funciona el ciclo día/noche?**
Hay 4 imágenes de cielo. `DayCycleController` calcula en qué fase debe estar dividiendo el tiempo total de juego entre la duración de cada fase. Si una fase dura 300 segundos y llevas 750 segundos jugando, estás en la tercera fase (Día). Al cambiar de fase hace un crossfade suave reduciendo el alpha de la imagen actual y subiendo el de la nueva.

**¿Qué es un Metroidvania?**
Un género de videojuego caracterizado por exploración no lineal de un mundo interconectado. El jugador desbloquea habilidades que le permiten acceder a zonas antes inaccesibles. El nombre viene de la combinación de Metroid y Castlevania, los juegos que popularizaron el género.

---

## RESUMEN FINAL — Todo el proyecto en una página

| Categoría | Contenido |
|-----------|-----------|
| **Motor** | Unity 2022.3 LTS |
| **Lenguaje** | C# |
| **Zonas** | 19+ interconectadas |
| **Singletons** | 10 sistemas persistentes |
| **Patrones** | Singleton, Observer, State Machine, Strategy, Command, Facade |
| **Estructuras de datos** | Array fijo, Dictionary, Buffer reutilizable, JSON, PlayerPrefs, ScriptableObject |
| **Boss** | 6 fases, 6 ataques, mecánica de Obsesión |
| **Guardado** | JSON en disco, autosave 30s, guardado al cambiar zona |
| **Mapa** | Descubrimiento por zonas visitadas, guardado en PlayerPrefs |
| **Música** | Automática por zona, reactiva al boss |
| **Vidas** | 5 vidas (Aras), respawn en último suelo, santuarios |
| **Efectos** | Brillo/contraste/saturación via shader en tiempo real |
| **Video** | Persiste entre escenas sin reinicio via RenderTexture |
| **Personaje** | Walk/Run/Jump/Dash/WallSlide/Float, detección de suelo via ContactPoint2D |
