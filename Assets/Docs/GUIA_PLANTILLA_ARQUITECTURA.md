---
destinatario: agente de IA (o dev) que va a construir la plantilla desde cero
origen: Docs/AUDITORIA_ARQUITECTURA.md — HamsterBall, 2026-09-08
estado: guía de construcción, no documento de diseño
---

# Guía para Agente — Plantilla de Arquitectura Unity

Vas a construir una **plantilla reutilizable de proyecto Unity** en una carpeta nueva y vacía.
La arquitectura proviene de un proyecto real (`HamsterBall`) que fue auditado completo. Esa
auditoría encontró que **el diseño de fondo era correcto** (bootstrap en escena propia, datos en
ScriptableObjects, EventBus tipado con structs, state machine por interfaz) pero que tenía 6 bugs
críticos y 8 problemas estructurales, casi todos causados por **decisiones que se postergaron**:
sin Assembly Definitions, sin namespaces, sin tests, sistemas escritos pero nunca conectados.

**Tu trabajo no es copiar el proyecto de referencia. Es construirlo como si las correcciones de la
auditoría hubieran sido decisiones del día 1.** Cada regla de la sección 3 existe porque en el
proyecto real costó caro no tenerla. No las relajes para ir más rápido.

Lectura obligatoria antes de empezar, en este orden:

1. Este documento, entero.
2. `Docs/AUDITORIA_ARQUITECTURA.md` — sección 2 ("Lo que está bien hecho"), sección 4 (críticos),
   sección 5 (arquitectura) y sección 8 (estructura de carpetas). Ahí está el *porqué* de cada regla.
   Este documento es el *qué*; la auditoría es el *porqué*. Si algo de aquí te parece arbitrario,
   la justificación con código real está allí.
3. El código fuente de referencia en `HamsterBall/Assets/_Game/Systems/` — solo cuando este
   documento te remita a un archivo concreto.

---

## 0. Acceso a los dos proyectos a la vez

Vas a trabajar en la carpeta del proyecto nuevo pero necesitas leer el de referencia. Tres opciones
(todas verificadas contra Qwen Code; la 3ª es la推荐ada para una plantilla porque queda en git):

**A. Por sesión, en caliente:**

```
/directory add ~/Projects/Unity/HamsterBall/HamsterBall/Assets/_Game
/directory add ~/Projects/Unity/HamsterBall/Docs
/dir show
```

**B. Al arrancar:**

```bash
qwen --add-dir ~/Projects/Unity/HamsterBall/HamsterBall/Assets/_Game,~/Projects/Unity/HamsterBall/Docs
```

**C. Permanente, en el proyecto nuevo** (requiere reiniciar Qwen Code):

```jsonc
// <NUEVO_PROYECTO>/.qwen/settings.json
{
  "context": {
    "includeDirectories": [
      "~/Projects/Unity/HamsterBall/HamsterBall/Assets/_Game",
      "~/Projects/Unity/HamsterBall/Docs"
    ],
    "loadFromIncludeDirectories": true
  }
}
```

> **No incluyas la raíz del repo de referencia.** Apunta a `Assets/_Game` y a `Docs` por separado.
> Incluir la raíz mete `Library/`, `Temp/`, `Logs/` y `obj/` en el rastreo: es lentísimo y no aporta
> nada. Por el mismo motivo, crea un `.qwenignore` en el proyecto nuevo (paso 8).

---

## 1. Qué es y qué no es esta plantilla

**Es:** un proyecto Unity que arranca, ejecuta una secuencia de bootstrap verificable, tiene un
ciclo de estado de juego funcional, guarda y carga partida, agrupa objetos por pool, y demuestra
cada sistema con al menos un test. Sirve para abrir una copia y empezar un juego nuevo encima.

**No es:**

- ❌ Un juego. **Nada de gameplay específico.** El proyecto de referencia es una bola-hámster con
  taxis, pasajeros y carreras. Nada de eso entra aquí. Donde el reference tiene `BallController`,
  la plantilla tiene un `PlayerMover` mínimo o directamente nada.
- ❌ Un framework. No abstraigas lo que aún no tiene dos implementaciones. Una interfaz por sistema
  solo donde la auditoría la justifica (`IGameState`, `ISaveStorage`).
- ❌ Un escaparate de assets. Cero paquetes del Asset Store, cero assets de terceros, cero UI
  reconocible. Solo paquetes Unity oficiales.
- ❌ Código de ejemplo sin ejercitar. **Este es el punto más importante.** En el proyecto de
  referencia, `EventBus` tenía 3 `Publish` y **0 `Subscribe`**; `SaveSystem.Save()` tenía
  **0 call sites**; `ObjectPoolManager.Get()` **0 call sites**. Código que parece terminado, nunca
  se ejecutó, y tenía bugs dentro. En la plantilla, **todo lo que escribas se llama desde algún
  sitio y tiene un test que lo ejecuta**. Si un sistema no va a tener consumidor todavía, no lo
  escribas.

---

## 2. Parámetros que debes fijar antes de escribir nada

Sustituye estos placeholders de forma consistente en todo el proyecto. Anótalos en el
`README.md` de la plantilla para que quien la clone sepa qué cambiar.

| Placeholder | Significado | Ejemplo |
|---|---|---|
| `{Project}` | Nombre PascalCase del proyecto, sin espacios ni guiones | `HamsterBall`, `NeonDrift` |
| `{ROOT_NS}` | Namespace raíz = `{Project}` | `HamsterBall` |
| `{ASM}` | Prefijo de assemblies = `{Project}` | `HamsterBall.Core` |

**Entorno objetivo** (el del proyecto de referencia; usa el mismo si no te indican otro):

| Componente | Versión |
|---|---|
| Unity Editor | `6000.4.6f1` (Unity 6.4) |
| URP | `com.unity.render-pipelines.universal` 17.4.0 |
| Input System | `com.unity.inputsystem` 1.19.0 |
| Cinemachine | `com.unity.cinemachine` 3.1.6 |
| Test Framework | `com.unity.test-framework` 1.6.0 |
| UI | `com.unity.ugui` 2.0.0 (+ TextMeshPro, incluido en UGUI 2.x) |

Paquetes que el proyecto de referencia tiene y la plantilla **no necesita**: `ai.navigation`,
`visualscripting`, `timeline`, `multiplayer.center`, `collab-proxy`. No los añadas "por si acaso".

---

## 3. Las 14 reglas no negociables

Cada una viene de un hallazgo concreto de la auditoría. La columna "Ref" apunta a la sección de
`AUDITORIA_ARQUITECTURA.md` donde está el bug real que la motiva.

| # | Regla | Ref |
|---|---|---|
| **R1** | **Assembly Definitions desde el primer archivo.** No se escribe ni un `.cs` antes de que existan los asmdefs y el grafo de dependencias de la sección 4. | A7 |
| **R2** | **Namespaces en el 100% de los tipos.** `{ROOT_NS}.Core`, `{ROOT_NS}.Data`, etc. Ningún tipo en el scope global. | A8 |
| **R3** | **Grafo acíclico y obligatorio:** `Data` no referencia nada del proyecto · `Core` referencia solo `Data` · los módulos de gameplay referencian `Core`+`Data` y **nunca entre sí** · `Debug` y `Tests` son hojas. | A7, §8 |
| **R4** | **Los módulos de gameplay se comunican solo por `EventBus`.** Es lo que hace cumplir R3 en la práctica y lo que convierte el bus en columna vertebral en vez de código decorativo. | A1, A7 |
| **R5** | **Lógica de dominio en C# puro.** Sin `MonoBehaviour`, sin `UnityEngine.Object`. El `MonoBehaviour` es un adapter fino. Objetivo: el 80% del código testeable en EditMode en milisegundos. | M6, M8 |
| **R6** | **Nada de `FindAnyObjectByType` para cablear sistemas.** El que crea un objeto conserva la referencia y la inyecta explícitamente. `Find` queda reservado para código de debug. | A3 |
| **R7** | **Una sola fuente de verdad por concepto.** El estado del juego lo posee la state machine; el enum se deriva de ella (`IGameState.Id`). Nunca dos campos que puedan divergir. | C2, A2 |
| **R8** | **`[Range]`, `[Min]` y `[Header]` son ayudas de edición, no invariantes de runtime.** Todo campo serializado cuyo valor incorrecto produzca comportamiento no-obvio se valida explícitamente en `OnValidate` (editor) y en `Awake` (build). | C3 |
| **R9** | **`Debug.LogError` + `return` NO es manejo de errores** — es dejar el objeto vivo en estado inválido. O se deshabilita el componente, o se lanza excepción, o se usa un fallback válido. Nunca degradar a cero silencioso. | A5, A3 |
| **R10** | **Suscripciones siempre en `OnEnable`/`OnDisable`.** Nunca en `Awake`, `Start` o constructor. Así un objeto revive suscripto y `ClearAllSubscriptions()` deja de ser peligroso. | A1 |
| **R11** | **Un solo dueño por responsabilidad.** Un sistema de cámara, un sistema de input, un lugar donde se muta el dinero. Dos dueños sobre el mismo eje = tunear a ciegas. | M1, M6 |
| **R12** | **Cero stubs silenciosos.** Lo no implementado lanza `NotImplementedException`; lo muerto se borra o se marca `[Obsolete]`; los pendientes usan `// TODO(Fase-N): …` uniforme y grep-able. | M11 |
| **R13** | **Ningún `Debug.Log` directo fuera de `Log.cs`.** Todo logging pasa por el wrapper con `[Conditional]`, que se compila fuera en release. | M10 |
| **R14** | **El save se migra, nunca se borra.** Versionado + cadena de migraciones + backup previo + escritura transaccional (`.tmp` → move). | M7, C6 |

---

## 4. Estructura de carpetas y assemblies

```
<NUEVO_PROYECTO>/                        ← raíz git
├── {Project}/                           ← proyecto Unity (Assets, Packages, ProjectSettings)
│   ├── .qwenignore                      ← paso 8
│   └── Assets/
│       └── _Game/
│           ├── Runtime/
│           │   ├── Core/                       → {ASM}.Core.asmdef
│           │   │   ├── Bootstrap/
│           │   │   │   ├── Bootstrapper.cs           {ROOT_NS}.Core
│           │   │   │   └── GameManager.cs            {ROOT_NS}.Core
│           │   │   ├── Events/
│           │   │   │   ├── EventBus.cs               {ROOT_NS}.Core
│           │   │   │   └── GameEvents.cs             {ROOT_NS}.Core
│           │   │   ├── StateMachines/
│           │   │   │   ├── IGameState.cs             {ROOT_NS}.Core.State
│           │   │   │   │   ├── GameStateMachine.cs   {ROOT_NS}.Core.State
│           │   │   │   └── States/
│           │   │   │       ├── MenuState.cs          {ROOT_NS}.Core.State
│           │   │   │       ├── PlayState.cs          {ROOT_NS}.Core.State
│           │   │   │       └── PausedState.cs        {ROOT_NS}.Core.State
│           │   │   ├── Pool/
│           │   │   │   ├── ObjectPoolManager.cs      {ROOT_NS}.Core.Pool
│           │   │   │   ├── PoolConfig.cs             {ROOT_NS}.Core.Pool
│           │   │   │   └── IPoolable.cs              {ROOT_NS}.Core.Pool
│           │   │   └── Log.cs                        {ROOT_NS}.Core
│           │   │
│           │   ├── Data/                       → {ASM}.Data.asmdef
│           │   │   ├── ScriptableObjects/
│           │   │   │   └── GameConfigSO.cs           {ROOT_NS}.Data
│           │   │   └── SO/                           ← assets .asset, no código
│           │   │       └── GameConfig_Default.asset
│           │   │
│           │   ├── Player/                     → {ASM}.Player.asmdef
│           │   │   ├── PlayerMover.cs                {ROOT_NS}.Player
│           │   │   └── Input/
│           │   │       └── PlayerInputReader.cs      {ROOT_NS}.Player
│           │   │
│           │   ├── CameraRig/                  → {ASM}.Camera.asmdef
│           │   │   └── CameraRigTarget.cs            {ROOT_NS}.Camera
│           │   │
│           │   └── Save/                       → {ASM}.Save.asmdef
│           │       ├── ISaveStorage.cs               {ROOT_NS}.Save
│           │       ├── JsonSaveStorage.cs            {ROOT_NS}.Save
│           │       ├── SaveData.cs                   {ROOT_NS}.Save
│           │       ├── SaveMigrations.cs             {ROOT_NS}.Save
│           │       ├── ProgressService.cs            {ROOT_NS}.Save   ← dominio, C# puro
│           │       └── SaveSystem.cs                 {ROOT_NS}.Save   ← adapter MonoBehaviour
│           │
│           ├── Debug/                          → {ASM}.Debug.asmdef   (solo development)
│           │   └── DebugHud.cs                       {ROOT_NS}.Debug
│           │
│           ├── Tests/
│           │   ├── EditMode/                   → {ASM}.Tests.EditMode.asmdef
│           │   └── PlayMode/                   → {ASM}.Tests.PlayMode.asmdef
│           │
│           ├── Prefabs/                        ← sin asmdef
│           │   ├── GameManager.prefab
│           │   ├── SaveSystem.prefab
│           │   └── ObjectPoolManager.prefab
│           ├── Scenes/
│           │   ├── Scene_Bootstrap.unity
│           │   └── Scene_Game.unity
│           ├── Art/ · Audio/ · Shading/ · Settings/
│           └── Docs/                           ← ARCHITECTURE.md de la plantilla (paso 8)
└── Docs/                                       ← fuera de Assets/, ver nota
```

**Por qué `Docs/` vive fuera de `Assets/`:** Unity importa todo lo que cae bajo `Assets/` y genera
un `.meta` por archivo, incluidos los `.md`. Eso ensucia el diff de git con metadatos de documentos
que no son del juego. Los `.md` van en la raíz del repo.

**Grafo de dependencias** — las flechas solo apuntan hacia abajo, ningún ciclo:

```
        {ASM}.Debug     {ASM}.Tests.EditMode     {ASM}.Tests.PlayMode
              │                  │                        │
     ┌────────┴──────┬───────────┴─────────┬──────────────┘
     ↓               ↓                     ↓
  Player          Camera                 Save
     │               │                     │
     └───────────────┴─────────────────────┘
                     ↓
               {ASM}.Core
                     ↓
               {ASM}.Data
```

Verificación del grafo: si `Player.asmdef` llega a listar `Save` en `references`, es un defecto.
`Player` publica un evento; `Save` lo escucha. Nunca al revés, nunca en directo.

### 4.1 Plantillas de `.asmdef`

Runtime normal:

```jsonc
// {ASM}.Player.asmdef
{
    "name": "{ASM}.Player",
    "rootNamespace": "{ROOT_NS}.Player",
    "references": [ "{ASM}.Core", "{ASM}.Data" ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Core` → `"references": [ "{ASM}.Data" ]`. `Data` → `"references": []`.

Módulo de debug, fuera del build de release:

```jsonc
// {ASM}.Debug.asmdef
{
    "name": "{ASM}.Debug",
    "rootNamespace": "{ROOT_NS}.Debug",
    "references": [ "{ASM}.Core", "{ASM}.Data", "{ASM}.Player" ],
    "autoReferenced": false,
    "defineConstraints": [ "UNITY_EDITOR || DEVELOPMENT_BUILD" ]
}
```

> Al crearlo, comprueba en el Inspector que Unity acepta la expresión de `Define Constraints`.
> Si tu versión la rechaza, el fallback es `["DEVELOPMENT_BUILD"]` y asumir que el HUD de debug no
> existe en el Editor (peor): en ese caso, deja el constraint vacío y excluye el prefab del build
> manualmente. **Verifícalo, no lo asumas.**

Tests EditMode:

```jsonc
// {ASM}.Tests.EditMode.asmdef
{
    "name": "{ASM}.Tests.EditMode",
    "rootNamespace": "{ROOT_NS}.Tests",
    "references": [
        "{ASM}.Core", "{ASM}.Data", "{ASM}.Save", "{ASM}.Player",
        "UnityEngine.TestRunner", "UnityEditor.TestRunner"
    ],
    "includePlatforms": [ "Editor" ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [ "nunit.framework.dll" ],
    "autoReferenced": false,
    "defineConstraints": [ "UNITY_INCLUDE_TESTS" ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Tests PlayMode: igual pero `"includePlatforms": []`, sin `UnityEditor.TestRunner`.

---

## 5. Orden de construcción

Ocho pasos. **No adelantes trabajo de un paso posterior**: el orden existe porque en Unity hay
cambios que son triviales antes de escribir código y carísimos después.

> ⚠️ **Regla transversal de Unity:** todos los movimientos y renombrados de archivos `.cs` se hacen
> **con el Editor abierto** (o vía Editor script), nunca desde el filesystem. Unity mantiene los
> GUID de los `.meta` al mover dentro del Editor; si mueves por terminal, las referencias en escenas
> y prefabs se rompen y aparece "Missing Script". (A8 documenta el caso real.)

### Paso 1 — Esqueleto y assemblies (antes de cualquier `.cs`)

1. Crear la jerarquía de carpetas de la sección 4 bajo `Assets/_Game/`.
2. Crear los 8 `.asmdef` con el contenido de 4.1.
3. Verificar en `Window → Analysis → Assembly Dependencies` (o abriendo cada asmdef) que el grafo
   es el de la sección 4 y que no hay ciclos.

**Verificación:** `find Assets -name "*.asmdef" | wc -l` → 8. Cero archivos `.cs` todavía.

### Paso 2 — `Core`: logging, eventos, estados

Orden interno: `Log.cs` → `EventBus.cs` → `GameEvents.cs` → `IGameState.cs` →
`GameStateMachine.cs` → estados → `GameManager.cs` → `Bootstrapper.cs` → `Pool/`.

Contratos y código en la sección 6.1 a 6.5.

**Verificación:** compila sin warnings; `grep -rn "Debug.Log" Assets/_Game/Runtime --include="*.cs"`
devuelve resultados **solo** dentro de `Log.cs`.

### Paso 3 — `Data`: ScriptableObjects

Un único `GameConfigSO` de ejemplo que demuestre la convención (`[CreateAssetMenu]`, `[Header]`,
`[Tooltip]` en cada campo, valores por defecto sanos) + el asset `GameConfig_Default.asset`.

No crees SOs vacíos "para el futuro" (era el caso de `ZoneSO`/`PassengerSO`/`BuffEffectSO` en el
reference: clases vacías indistinguibles de clases olvidadas — M11).

**Verificación:** el asset existe y al seleccionarlo el Inspector muestra los campos con tooltips.

### Paso 4 — `Save`: almacenamiento, dominio, adapter

Tres capas obligatorias (R5, R14, M6):

| Capa | Tipo | Responsabilidad |
|---|---|---|
| Infraestructura | `ISaveStorage` + `JsonSaveStorage` | Leer/escribir disco. Cero lógica de juego. C# puro. |
| Dominio | `ProgressService` | Mutar el progreso. Valida invariantes. Publica eventos. C# puro. |
| Adapter | `SaveSystem : MonoBehaviour` | Ciclo de vida de Unity, `persistentDataPath`, `OnApplicationPause`. Fino. |

`SaveData` incluye `saveVersion` desde el día 1 y `SaveMigrations` implementa la cadena (6.7).

**Verificación:** `ProgressService` y `JsonSaveStorage` no heredan de nada de `UnityEngine`.
Compruébalo con `grep -n "MonoBehaviour\|ScriptableObject" Assets/_Game/Runtime/Save/*.cs` →
solo debe aparecer en `SaveSystem.cs`.

### Paso 5 — `Player`, `CameraRig`, `Debug`

- `PlayerInputReader`: input por `InputActionReference` serializado (6.8). **Nunca** `PlayerInput`
  en modo *Send Messages*: resuelve handlers por reflexión sobre el nombre del método, así que
  renombrar `OnMove` rompe el input sin error de compilación ni warning (M9).
- `PlayerMover`: consume el reader, mueve un transform o un Rigidbody. Suavizado
  framerate-independent (6.9).
- `CameraRigTarget`: **un solo dueño del suavizado** (R11). O Cinemachine hace el damping, o lo hace
  el script — jamás los dos en cascada (M1). Recomendado: Cinemachine como dueño (regala blending
  entre estados de juego), y `CameraRigTarget` como proveedor pasivo de posición/orientación.
- `DebugHud`: Canvas + TextMeshPro, **no `OnGUI`** (IMGUI aloca un `GUIStyle` por repaint y
  `OnGUI` corre 4–8 veces por frame — M5). Se suscribe a eventos del bus, no hace `Find` (C5, R10).

**Verificación:** `grep -rn "OnGUI\|OnMove(InputValue" Assets/_Game` → 0 resultados.

### Paso 6 — Escenas, prefabs y build settings

1. `Prefabs/`: `GameManager.prefab`, `SaveSystem.prefab`, `ObjectPoolManager.prefab`.
2. `Scene_Bootstrap`: un GameObject con `Bootstrapper`, con los tres prefabs asignados en el
   Inspector. Nada más en la escena.
3. `Scene_Game`: cámara (Cinemachine + `CinemachineBrain`), luz, suelo de prueba, `Player`,
   `DebugHud`.
4. `File → Build Settings`: `Scene_Bootstrap` en índice **0**, `Scene_Game` en índice 1.
5. Cableado por **inyección explícita**, no por `Find` (R6). El `Bootstrapper` conserva lo que
   instancia y lo pasa a `GameManager.RegisterManagers(save, pool)`.

**Verificación:** entrar en Play Mode desde `Scene_Bootstrap` (no desde `Scene_Game`) y comprobar en
la consola la secuencia completa de pasos numerados, sin errores ni warnings. Este es el primer
momento en que el sistema se ejercita de verdad — si algo estaba mal, aparece aquí.

### Paso 7 — Tests

**Los tests son parte de la plantilla, no un extra.** Son lo que demuestra que los sistemas están
conectados y lo que impide que se repitan los 6 críticos de la auditoría. Cinco de esos seis eran
testeables en EditMode (M8).

Mínimo obligatorio — cada uno cubre un bug real del proyecto de referencia:

| Test | Cubre | Ref auditoría |
|---|---|---|
| `EventBusTests.Publish_WithSubscriber_InvokesCallback` | el bus funciona | A1 |
| `EventBusTests.Publish_WithNoSubscriber_DoesNotThrow` | publicar sin oyentes no rompe | A1 |
| `EventBusTests.Unsubscribe_LastSubscriber_RemovesEventType` | no se filtran claves vacías | §2 |
| `GameStateMachineTests.TransitionTo_CallsExitOnPrevious_AndEnterOnNew` | `Exit()` sí se ejecuta | C2 |
| `GameStateMachineTests.TransitionTo_Null_KeepsCurrentState` | transición inválida no corrompe | C2 |
| `GameManagerTests.ChangeState_UnimplementedState_DoesNotMutateCurrentState` | una sola fuente de verdad | C2, A2 |
| `ObjectPoolManagerTests.Get_WhenPoolEmptyAndAutoExpand_ReturnsActiveObject` | **el objeto vuelve activo** | **C1** |
| `ObjectPoolManagerTests.Return_SameObjectTwice_SecondIsIgnored` | doble retorno | M3 |
| `SaveMigrationsTests.Migrate_OldVersion_UpgradesWithoutDataLoss` | migrar ≠ borrar | M7 |
| `ProgressServiceTests.Earn_NegativeAmount_Throws` | acumulado monotónico | M6 |

**Verificación:** `Window → General → Test Runner → EditMode → Run All` → todo en verde.
Anota el número de tests y el tiempo en el `README.md`; si alguien añade un test que tarda segundos,
es señal de que metió Play Mode donde no tocaba.

### Paso 8 — Documentación e higiene del repo

En el proyecto nuevo:

- `Docs/ARCHITECTURE.md` — el grafo de assemblies, las 14 reglas resumidas y el flujo de bootstrap.
  **Fuera de `Assets/`.**
- `QWEN.md` en la raíz del repo nuevo, con las 14 reglas, para que cualquier agente futuro las
  herede sin tener que leer la auditoría.
- `.gitignore` estándar de Unity (`Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`,
  `UserSettings/`, `MemoryCaptures/`). El de referencia está en `HamsterBall/.gitignore`.
- `.qwenignore` excluyendo lo mismo: sin él, el rastreo de archivos del agente se come `Library/`.
- `README.md` — cómo abrir la plantilla, qué placeholders sustituir, cómo correr los tests.

---

## 6. Contratos y código crítico

No son sugerencias de estilo: son las implementaciones corregidas. En cada caso se indica qué bug
del proyecto de referencia neutralizan. **Copia la lógica, cambia los nombres al placeholder.**

### 6.1 `Log` — wrapper con `[Conditional]` (R13, M10)

`Debug.Log` no se strippea en release y la interpolación `$""` se evalúa siempre, antes de decidir
si se loguea. `[Conditional]` hace que el compilador elimine la llamada **y la evaluación de los
argumentos**.

```csharp
namespace {ROOT_NS}.Core
{
    public static class Log
    {
        public const string VERBOSE = "{ROOT_NS_UPPER}_VERBOSE";

        [System.Diagnostics.Conditional(VERBOSE)]
        public static void Trace(string msg) => Debug.Log(msg);

        [System.Diagnostics.Conditional("UNITY_EDITOR", "DEVELOPMENT_BUILD")]
        public static void Info(string msg) => Debug.Log(msg);

        public static void Warn(string msg) => Debug.LogWarning(msg);
        public static void Error(string msg) => Debug.LogError(msg);
    }
}
```

Uso: `Trace` para flujo de inicialización y transiciones · `Info` para hitos de gameplay ·
`Warn`/`Error` siempre visibles. Define el símbolo en `Project Settings → Player → Scripting
Define Symbols` para builds de desarrollo.

Nunca `Log.Info("stats: " + someMonoBehaviour)` — `ToString()` sobre un `MonoBehaviour` imprime
`{ROOT_NS}.Player.PlayerMover (PlayerMover)`, no los datos (M10).

### 6.2 `EventBus` — la implementación del reference es correcta, con un añadido (A1)

`Dictionary<Type, Delegate>` + `Delegate.Combine`/`Remove` + **structs** como payload (evita boxing
y una clase genérica por evento) + borrar la clave cuando se va el último suscriptor. Copia
`Systems/Core/EventBus.cs` del reference tal cual y añade el diagnóstico:

```csharp
public static void Publish<T>(T eventData) where T : struct
{
    if (!_subscribers.TryGetValue(typeof(eventData), out Delegate d))
    {
        Log.Warn($"[EventBus] Published {typeof(T).Name} with no subscribers.");
        return;
    }
    (d as Action<T>)?.Invoke(eventData);
}
```

Un bus silencioso es indistinguible de un bus roto. En el reference había 3 `Publish` y nadie
escuchaba, y no había forma de darse cuenta.

`TryGetValue` además elimina el doble lookup `ContainsKey` + indexador que tiene el original.

**Eventos mínimos** (`GameEvents.cs`, todos `struct`): `OnBootstrapComplete` (ojo: el reference lo
escribió `OnBootstrappComplete`, con doble p — M12), `OnGameStateChanged`, `OnProgressChanged`.
No definas eventos que nadie emite todavía.

### 6.3 `IGameState` / `GameStateMachine` — una fuente de verdad (R7, C2, A2)

El bug real: `GameManager.CurrentState` (enum) y `GameStateMachine.CurrentState` (`IGameState`) eran
dos campos independientes escritos en momentos distintos. Al cambiar a un estado no implementado, el
enum mutaba, la máquina no, `Exit()` nunca corría, y el HUD mostraba un estado mientras el juego
corría otro.

```csharp
public enum GameState { Bootstrap, Menu, Play, Paused, GameOver }

public interface IGameState
{
    GameState Id { get; }          // ← la identidad la declara el estado, no un switch externo
    void Enter();
    void Tick();
    void Exit();
}
```

`GameStateMachine.TransitionTo(null)` debe **no** mutar nada y loguear un error. `Initialize(IGameState)`
existe y **se llama** (en el reference era código muerto con 0 call sites — A2, M11).

### 6.4 `GameManager` — resolver antes de mutar (C2, R9)

```csharp
public GameState CurrentState =>
    _stateMachine?.CurrentState?.Id ?? GameState.Bootstrap;   // derivado: no puede divergir

public void ChangeState(GameState newState)
{
    if (CurrentState == newState) return;

    IGameState target = ResolveState(newState);
    if (target == null)
    {
        Log.Error($"[GameManager] State {newState} has no implementation.");
        return;                                    // ← nada se mutó
    }

    GameState previous = CurrentState;
    _stateMachine.TransitionTo(target);
    EventBus.Publish(new OnGameStateChanged { previousState = previous, newState = newState });
}

private IGameState ResolveState(GameState state) => state switch
{
    GameState.Menu     => _menuState,
    GameState.Play     => _playState,
    GameState.Paused   => _pausedState,
    _ => null
};
```

Con `Id` en la interfaz, añadir un estado es **añadir una clase**, no editar dos sitios que pueden
desincronizarse. Y `CurrentState` como propiedad derivada elimina estructuralmente la divergencia.

`GameManager` expone `RegisterManagers(SaveSystem, ObjectPoolManager)` con **parámetros** (R6) y
conserva el singleton `Instance` + `DontDestroyOnLoad` como hace el reference.

### 6.5 `Bootstrapper` — inyección explícita y fallo fuerte (R6, A3)

El patrón del reference era correcto en la forma (escena propia, pasos nombrados, `yield return null`
entre pasos para dejar que `Awake`/`Start` se asienten) y defectuoso en el cableado: descartaba el
retorno de `Instantiate` y luego buscaba los managers por toda la escena con `FindAnyObjectByType`.

```csharp
private GameManager       _gameManager;
private SaveSystem        _saveSystem;
private ObjectPoolManager _poolManager;

private IEnumerator InitializeSequence()
{
    Log.Trace("[Bootstrapper] ─────── SEQUENCE STARTED ───────");

    InstantiateManagers();   yield return null;  Log.Trace("[Bootstrapper] Step 1 complete: Managers");
    LoadSaveData();          yield return null;  Log.Trace("[Bootstrapper] Step 2 complete: Save Data");
    InitializeObjectPools(); yield return null;  Log.Trace("[Bootstrapper] Step 3 complete: Object Pools");

    Log.Trace("[Bootstrapper] ─────── SEQUENCE COMPLETED ───────");
    LoadGameScene();
}

private void InstantiateManagers()
{
    EventBus.ClearAllSubscriptions();      // correcto AQUÍ: corre antes de instanciar nada

    _gameManager = InstantiateRequired(_gameManagerPrefab, "GameManager");
    _saveSystem  = InstantiateRequired(_saveSystemPrefab,  "SaveSystem");
    _poolManager = InstantiateRequired(_poolManagerPrefab, "ObjectPoolManager");
}

private T InstantiateRequired<T>(T prefab, string label) where T : Component
{
    if (prefab == null)
        throw new InvalidOperationException($"[Bootstrapper] {label} prefab not assigned.");
    return Instantiate(prefab);
}

private void LoadSaveData()
{
    _gameManager.RegisterManagers(_saveSystem, _poolManager);   // ← referencias directas
    _saveSystem.Load();
}
```

Lanzar excepción es deliberado: **un bootstrap incompleto debe fallar fuerte y temprano**, no
degradarse a un juego que arranca sin save system. En el reference, un prefab sin asignar producía
un `LogError` enterrado seguido de una `NullReferenceException` genérica que cortaba la coroutine a
mitad: pantalla negra en la escena de bootstrap y ningún diagnóstico útil (A3).

`ClearAllSubscriptions()` va **solo** aquí. El reference lo llamaba también en `RestartScene()`, y
como los managers viven en `DontDestroyOnLoad` y el `Bootstrapper` no se re-ejecuta, recargar la
escena de juego dejaba a los suscriptores persistentes **sordos de forma permanente** (A1). Si la
plantilla necesita reiniciar, que recargue `Scene_Bootstrap`:

```csharp
private void RestartFlow() => SceneManager.LoadScene("Scene_Bootstrap");
```

### 6.6 `ObjectPoolManager` — una sola salida activa + doble retorno (C1, M3, M4)

Tres bugs reales en el mismo archivo:

```csharp
private readonly Dictionary<string, Queue<GameObject>>   _pools     = new();
private readonly Dictionary<string, HashSet<GameObject>> _active    = new();
private readonly Dictionary<string, Transform>           _containers = new();

public GameObject Get(string poolId)
{
    if (!_pools.TryGetValue(poolId, out Queue<GameObject> pool))
    {
        Log.Error($"[ObjectPoolManager] Pool '{poolId}' does not exist.");
        return null;
    }

    GameObject obj;
    if (pool.Count > 0)
    {
        obj = pool.Dequeue();
    }
    else if (_configs[poolId].autoExpand)
    {
        Log.Warn($"[ObjectPoolManager] Pool '{poolId}' empty. Expanding.");
        obj = CreateInstance(_configs[poolId]);
    }
    else
    {
        Log.Warn($"[ObjectPoolManager] Pool '{poolId}' empty, no autoExpand.");
        return null;
    }

    obj.SetActive(true);          // ← ÚNICA salida activa. C1: el path de autoExpand
    _active[poolId].Add(obj);     //    devolvía el objeto todavía INACTIVO.
    obj.GetComponent<IPoolable>()?.OnSpawn();
    return obj;
}

public void Return(string poolId, GameObject obj)
{
    if (!_active.TryGetValue(poolId, out HashSet<GameObject> active)) { Destroy(obj); return; }

    if (!active.Remove(obj))      // ← false = no estaba activo = doble retorno (M3)
    {
        Log.Warn($"[ObjectPoolManager] '{poolId}' double-return ignored: {obj.name}");
        return;
    }

    obj.GetComponent<IPoolable>()?.OnDespawn();
    obj.SetActive(false);
    obj.transform.SetParent(_containers[poolId]);
    _pools[poolId].Enqueue(obj);
}

public void ReturnAll(string poolId)
{
    if (!_containers.TryGetValue(poolId, out Transform container)) return;

    for (int i = container.childCount - 1; i >= 0; i--)   // ← hacia atrás: SetParent reordena
    {                                                     //    hermanos y un foreach sobre la
        GameObject child = container.GetChild(i).gameObject;   // jerarquía mientras se muta es
        if (child.activeSelf) Return(poolId, child);           //    comportamiento indefinido (M4)
    }
}
```

`CreateInstance` conserva su `SetActive(false)` (su trabajo es poblar la cola); la compensación vive
ahora en un solo lugar, así los dos paths no pueden volver a divergir.

`IPoolable { void OnSpawn(); void OnDespawn(); }` — los objetos reusados salen con el estado del uso
anterior (partículas, timers, vida). Sin reseteo explícito, el pooling introduce bugs aleatorios.

### 6.7 `Save` — migración en cadena y escritura transaccional (R14, M7, C6)

El reference versionaba el save desde el día 1 (buena intuición) pero ante una versión distinta
ejecutaba `CurrentData = CreateNewSave()`: **borraba la partida del jugador** en vez de migrarla.

```csharp
private static readonly Dictionary<int, Func<SaveData, SaveData>> Migrations = new()
{
    // [1] = data => { data.newField = defaultValue; data.saveVersion = 2; return data; },
};

public SaveData Migrate(SaveData data)
{
    while (data.saveVersion < SaveData.CURRENT_VERSION)
    {
        if (!Migrations.TryGetValue(data.saveVersion, out Func<SaveData, SaveData> step))
        {
            Log.Error($"[SaveMigrations] No migration path from v{data.saveVersion}.");
            return null;                     // ← el caller decide; nunca borrar en silencio
        }
        data = step(data);
    }
    return data.saveVersion == SaveData.CURRENT_VERSION ? data : null;
}
```

Notas que debes respetar:

- **Backup antes de migrar** (`{name}.v{n}.bak`). Si la migración tiene un bug, el jugador no pierde
  la partida y en desarrollo puedes reproducir el caso.
- **Escritura transaccional:** escribir a `.tmp` y luego `File.Move(tmp, final, overwrite: true)`.
  `File.WriteAllText` directo puede dejar el JSON truncado si la app se corta a mitad — en móvil es
  un escenario real, no teórico (C6).
- **`JsonUtility` deja los campos ausentes en su default de C#.** Añadir un campo no rompe la
  lectura; **renombrar o cambiar el tipo de uno existente sí**, y sin avisar. Por eso las
  migraciones son explícitas.
- **Cuándo guardar:** `OnApplicationQuit()` **no** se dispara de forma consistente en Android/iOS;
  `OnApplicationPause(true)` sí. Implementa ambos. Mejor aún: suscribirse a los eventos de dominio
  y marcar `_dirty`, con guardado diferido (C6).
- **`ProgressService` valida invariantes.** En el reference, `AddCurrency(-50)` era legal y
  decrementaba `totalEarned`, que es un acumulado histórico monotónico. Aquí:

```csharp
public void Earn(int amount)
{
    if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
    _data.currency += amount;
    _data.totalEarned += amount;
    PublishProgress();
}

public bool TrySpend(int amount)
{
    if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
    if (_data.currency < amount) return false;
    _data.currency -= amount;               // totalEarned intacto
    PublishProgress();
    return true;
}
```

`SaveData.CurrentData` **no** se expone público y mutable: `SaveSystem.CurrentData.currency = 999999`
compilaba en el reference y no dejaba rastro (M6).

### 6.8 Input — `InputActionReference`, no *Send Messages* (M9)

```csharp
[Header("Input")]
[SerializeField] private InputActionReference _moveAction;

private void OnEnable()
{
    _moveAction.action.Enable();
    _moveAction.action.performed += OnMove;
    _moveAction.action.canceled  += OnMove;   // ← sin esto, el input conserva el último valor al soltar
}

private void OnDisable()
{
    _moveAction.action.performed -= OnMove;
    _moveAction.action.canceled  -= OnMove;
    _moveAction.action.Disable();
}

private void OnMove(InputAction.CallbackContext ctx) => _moveInput = ctx.ReadValue<Vector2>();
```

Ventajas: el enlace es una referencia serializada que el Editor valida (si se borra la action, el
campo queda `None` y **se ve**), el compilador verifica las firmas, y el rebind/gamepad son posibles.

Si en algún momento necesitas que un bot, un replay o un test de PlayMode conduzcan al jugador,
interpón `IInputProvider { Vector2 Move { get; } }`. No lo añadas antes de tener el segundo
consumidor (R: no abstraer sin dos implementaciones).

### 6.9 Suavizado framerate-independent (M2)

```csharp
// ❌ Lerp(a, b, speed * dt) NO es suavizado exponencial: es una fracción lineal del tramo
//    restante, así que el feel cambia entre 30 y 144 fps con el mismo valor de speed.
transform.position = Vector3.Lerp(transform.position, target, _speed * Time.deltaTime);

// ✅ Opción preferida: parámetro con significado físico ("segundos hasta ~63% del objetivo")
transform.position = Vector3.SmoothDamp(transform.position, target, ref _velocity, _smoothTime);

// ✅ O equivalente exponencial (ojo: destinos invertidos con Exp)
float t = Mathf.Exp(-_speed * Time.deltaTime);
transform.position = Vector3.Lerp(target, transform.position, t);
```

Y recuerda R11: si usa Cinemachine, el damping de Cinemachine es **el único**. El reference tenía
`CameraAnchor` haciendo `Lerp` propio **y** `CinemachineFollow` con damping encima, más un `LookAt`
instantáneo contra un `RotationDamping` que lo amortiguaba: latencia compuesta, rotaciones en
conflicto y ningún parámetro que arreglara nada (M1).

---

## 7. Convenciones de estilo y naming

Son las del proyecto de referencia (sección 2 de la auditoría las valida explícitamente). Mantenerlas
hace que los dos proyectos se lean igual, que es el punto de tener plantilla.

**Código**

- `_camelCase` para privados serializados y no serializados · `PascalCase` para públicos ·
  `CONSTANT_CASE` para `const`.
- `#region` agrupando por rol, precedidas de banner de sección:
  ```csharp
  // ────────────────────────────────
  // INSPECTOR
  // ────────────────────────────────
  #region Inspector
  ...
  #endregion
  ```
  Regiones típicas: `Inspector` · `Lifecycle` · `Initialization` · `Public API` · `Event Handlers` ·
  `Private`.
- Campos serializados alineados en columna cuando son varios del mismo grupo (como hace el
  `Bootstrapper` del reference).
- `[Header("...")]` agrupando campos en el Inspector · `[Tooltip("...")]` en **todo** campo
  expuesto al diseño · `[RequireComponent(typeof(X))]` cuando el componente necesita otro para no
  estar en un estado inválido.
- `[CreateAssetMenu(fileName = "New...", menuName = "{Project}/...")]` en todo `ScriptableObject`.
- XML doc `///<summary>` solo donde la intención no es obvia. **Cero comentarios que narren lo que
  el código ya dice.**

**Logs**

- Prefijo `[NombreDeClase]` en cada mensaje — el filtro de la consola de Unity es por subcadena, así
  que la consistencia del prefijo es funcional, no estética. En el reference, 3 de 7 logs del
  `Bootstrapper` decían `[Bootsrapper]` (sin la `t`), y filtrar por `Bootstrapper` **ocultaba justo
  el inicio y el fin de la secuencia** (M12).
- **Un solo idioma: inglés.** El reference mezclaba dos líneas en español y el filtrado se rompía.
- Revisa los typos en strings de log: en el reference había `whitout`, `Errased`, `ordenated`, una
  frase duplicada y un `"current data is nullhas Load() been called?"` sin separador (M12).

**Archivos y assets**

| Tipo | Convención | Ejemplo |
|---|---|---|
| Escenas | `Scene_<Nombre>` | `Scene_Bootstrap`, `Scene_Game` |
| Clases ScriptableObject | `<Nombre>SO` | `GameConfigSO` |
| Assets ScriptableObject | `<Nombre>_<Variante>.asset` | `GameConfig_Default.asset` |
| Prefabs de sistema | `<NombreDelManager>.prefab` | `GameManager.prefab` |
| Tests | `<Clase>Tests.cs` | `EventBusTests.cs` |
| TODOs | `// TODO(Fase-N): descripción` | grep-able por fase |

---

## 8. Trampas de Unity que la plantilla neutraliza por diseño

Lista de verificación para que no las redescubras. Todas ocurrieron en el proyecto de referencia.

| Trampa | Por qué muerde | Defensa en la plantilla |
|---|---|---|
| `[Range]` **no clampea al deserializar** | Solo constriñe el slider del Inspector. Un valor fuera de rango ya escrito en la escena se carga tal cual, sin aviso. En el reference había un umbral de `-0.65` guardado como `0.5` en `Scene_Game.unity`, y el drift se auto-cancelaba. | R8: `OnValidate` clampea; `Awake` valida en build. |
| **Orden de `Awake` entre scripts indeterminado** | Unity respeta en la práctica el orden de `m_Component`, pero no lo garantiza. Reordenar componentes en el Inspector, un prefab variant o un cambio de versión lo rompen sin aviso. | R: inicialización explícita (`_stats.Initialize()` llamado por el consumidor), no magia de orden. `Initialize()` + `IsReady`. |
| **Mover `.cs` desde el filesystem** | Rompe el GUID del `.meta` → "Missing Script" en escenas y prefabs. | Mover siempre con el Editor abierto (paso 5, aviso). |
| **Cambiar namespace y assembly a la vez** | Unity resuelve componentes por GUID + `m_EditorClassIdentifier` (`Assembly-CSharp::BallController`). Cambiar las dos cosas de golpe puede perder el enlace. | Primero asmdefs y mover carpetas; verificar escenas; **después** namespaces módulo a módulo, guardando cada escena tras cada módulo, un commit por módulo. |
| **`FindAnyObjectByType` no es determinista** | Con más de un candidato devuelve cualquiera, en un orden interno que no es estable entre ejecuciones → bug intermitente. | R6. El reference capturaba `FindAnyObjectByType<Rigidbody>()` para leer la velocidad de la bola: en cuanto hubiera un segundo Rigidbody, el HUD mostraría los datos de otro objeto (C5). |
| **`Keyboard.current` es `null` sin teclado** | Build en móvil/consola → `NullReferenceException` **cada frame**, consola inundada y degradación de rendimiento. | Null-check en todo `*.current` del Input System. El HUD de debug ni siquiera llega al build (asmdef con constraint). |
| **`OnApplicationQuit` no es fiable en móvil** | Android/iOS matan el proceso sin avisar. | `OnApplicationPause(true)` como disparador principal de guardado (C6). |
| **`OnGUI` corre varias veces por frame** | Un `new GUIStyle(...)` dentro son 4–8 allocs por frame → GC y micro-stutter. | Canvas + TextMeshPro. Cero `OnGUI` (M5). |
| **`SetParent` reordena hermanos** | Un `foreach` sobre la jerarquía que hace `SetParent` dentro muta la colección que enumera. | Iterar por índice hacia atrás (M4). |
| **`DontDestroyOnLoad` + `ClearAllSubscriptions()`** | Recargar la escena de juego no re-ejecuta el bootstrap: los suscriptores persistentes quedan sordos para siempre. | R10 + `ClearAllSubscriptions` solo en el `Bootstrapper` (A1). |
| **`0 / 0` = `NaN` y se propaga** | Un `MaxSpeed` en 0 produce `normalizedSpeed = NaN`, que llega a `Image.fillAmount` (lanza) y a `AudioSource.pitch` (lanza). La causa y el síntoma quedan en sistemas distintos. | R9 + guarda en el punto de la división: `max > 0.001f ? Clamp01(v / max) : 0f` (A5). |

---

## 9. Definition of Done

Todo verificable con comandos. **No des la plantilla por terminada con ningún punto en rojo.**

```bash
cd <NUEVO_PROYECTO>/{Project}/Assets/_Game

# 1. Assemblies: 8 asmdefs, sin ciclos
find . -name "*.asmdef" | wc -l                                    # → 8

# 2. Namespaces en todos los archivos (R2)
total=$(find . -name "*.cs" | wc -l); ns=$(grep -rl "^namespace" --include="*.cs" . | wc -l)
echo "$ns/$total"                                                  # → ambos iguales

# 3. R3: Data no referencia nada; Core solo Data
grep -A3 '"references"' Runtime/Data/*.asmdef                      # → []
grep -A3 '"references"' Runtime/Core/*.asmdef                      # → solo {ASM}.Data

# 4. R4/R6: gameplay no se cablea con Find
grep -rn "FindAnyObjectByType" Runtime/                            # → 0 resultados

# 5. R13: ningún Debug.Log fuera del wrapper
grep -rn "Debug\.Log" Runtime/ --include="*.cs" | grep -v "Core/Log.cs"   # → 0 resultados

# 6. R10: suscripciones solo en OnEnable/OnDisable
grep -rn "EventBus.Subscribe" Runtime/                             # revisar: cada una dentro de OnEnable

# 7. R12: cero stubs silenciosos
grep -rn "TODO" Runtime/                                           # → solo formato TODO(Fase-N)

# 8. M9/M5: ni Send Messages ni IMGUI
grep -rn "OnGUI\|InputValue" Runtime/ Debug/                       # → 0 resultados

# 9. R5: el dominio no toca MonoBehaviour
grep -rn "MonoBehaviour" Runtime/Save/                             # → solo SaveSystem.cs

# 10. Typos en prefijos de log (M12): cada prefijo coincide con su clase
grep -rhn "\[Bootstrapper\]\|\[GameManager\]\|\[EventBus\]" Runtime/
```

En el Editor:

- [ ] `Test Runner → EditMode → Run All` → **todo en verde**, y los 10 tests mínimos de 5.7 existen.
- [ ] `Test Runner → PlayMode` → verde (aunque sea un solo test de humo).
- [ ] Play Mode desde `Scene_Bootstrap` → secuencia de 3 pasos numerados en la consola, sin errores
      ni warnings, y `Scene_Game` cargada.
- [ ] Play Mode desde `Scene_Game` directamente → no explota (debe degradar con un error claro, no
      con una NRE en cadena).
- [ ] `Window → Analysis → Assembly Dependencies` → el grafo es el de la sección 4.
- [ ] Ningún aviso de "Missing Script" en ninguna escena ni prefab.
- [ ] `File → Build Settings` → `Scene_Bootstrap` en índice 0.
- [ ] Build de desarrollo compilada al menos una vez (verifica los constraints del asmdef de Debug).

---

## 10. Referencias

| Documento | Ruta | Para qué |
|---|---|---|
| Auditoría completa | `~/Projects/Unity/HamsterBall/Docs/AUDITORIA_ARQUITECTURA.md` | El *porqué* de cada regla, con el código defectuoso real |
| Código de referencia | `~/Projects/Unity/HamsterBall/HamsterBall/Assets/_Game/Systems/` | Implementaciones a copiar/adaptar (18 `.cs`, ~1.400 líneas) |
| Manifest de referencia | `~/Projects/Unity/HamsterBall/HamsterBall/Packages/manifest.json` | Versiones exactas de paquetes |
| `.gitignore` Unity | `~/Projects/Unity/HamsterBall/HamsterBall/.gitignore` | Copiar al proyecto nuevo |

**Índice de hallazgos de la auditoría** (para buscar el detalle de una regla concreta):

- **Críticos:** C1 pool devuelve inactivos · C2 enum desincronizado de la máquina · C3 `[Range]` no
  clampea al deserializar · C4 `Keyboard.current` null · C5 `Find<Rigidbody>` equivocado ·
  C6 se carga pero nunca se guarda.
- **Arquitectura:** A1 EventBus sin suscriptores + borrado global · A2 dos fuentes de verdad ·
  A3 service locator por `Find` · A4 orden de `Awake` implícito · A5 degrada a ceros y produce
  `NaN` · A6 upgrades que no llegan al Rigidbody · A7 sin asmdefs · A8 sin namespaces.
- **Medios:** M1 dos cámaras compitiendo · M2 suavizado dependiente del framerate · M3 doble retorno
  al pool · M4 `ReturnAll` muta mientras itera · M5 `GUIStyle` por repaint · M6 `SaveSystem` mezcla
  tres responsabilidades · M7 versionado que borra · M8 cero tests · M9 input por Send Messages ·
  M10 53 `Debug.Log` sin niveles · M11 stubs indistinguibles · M12 typos en nombres públicos y
  prefijos de log.
