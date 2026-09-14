# QWEN.md — DecoupledTemplate

Plantilla reutilizable de arquitectura Unity para futuros juegos. Se construye a partir de
`Assets/Docs/GUIA_PLANTILLA_ARQUITECTURA.md`, que es la fuente autoritativa de las 14 reglas no
negociables, la estructura de assemblies y el orden de construcción en 8 pasos.

**Lee la guía entera antes de escribir código.** Este archivo no la duplica: solo registra lo que
la guía no puede saber — las decisiones tomadas sobre este repo concreto y el entorno real
verificado, que difiere del que la guía asume.

## Alcance acordado con Fernando (2026-09-08)

El objetivo de la plantilla es **el EventBus tipado + el bootstrap desacoplado que arranca los
sistemas en orden verificable**. La cámara **no** es prioridad: el Paso 5 de la guía queda reducido
al mínimo imprescindible. Si algún día se añade cámara, R11 sigue vigente (un solo dueño del
suavizado, nunca Cinemachine y script en cascada).

**Materializado en el Paso 1 (decisión de Fernando, 2026-09-08): la plantilla tiene 7 assemblies, no
las 8 de la guía.** No existen `Assets/_Game/Runtime/CameraRig/` ni `DecoupledTemplate.Camera.asmdef`.
Toda verificación que en la guía diga `→ 8` (el Paso 1 y el *Definition of Done*) se lee `→ 7` aquí.
Si se añade cámara, entra como hoja nueva del grafo (referencia a `Core`+`Data`, y solo `Debug` y
`Tests` la referencian) y hay que actualizar estas cuentas.

## Placeholders fijados (sección 2 de la guía)

`{Project}` = `{ROOT_NS}` = `{ASM}` = **`DecoupledTemplate`**, coherente con el nombre del repo. Las
7 assemblies son `DecoupledTemplate.{Core,Data,Player,Save,Debug,Tests.EditMode,Tests.PlayMode}` y el
`rootNamespace` de cada una es `DecoupledTemplate.<Módulo>` (`DecoupledTemplate.Tests` en las dos de
tests). Decisión del 2026-09-08 tomada sabiendo que renombrar obliga a regenerar los proyectos y a
tocar todas las referencias: no cambiar a la ligera. La guía pide anotarlos en el `README.md`, que
todavía no existe (Paso 8); hasta entonces viven aquí.

## Proyecto de referencia

HamsterBall — de donde salen los patrones y la auditoría que motiva cada regla. Vive fuera de este
repo y se lee por **ruta absoluta**; no hay directorios incluidos en el contexto ni hace falta.

- Código: `/Users/ningunfernando/Projects/Unity/HamsterBall/HamsterBall/Assets/_Game`
  Relevante para el alcance acordado: `Systems/Core/EventBus.cs`, `Systems/Core/GameEvents.cs`,
  `Systems/Core/Bootstrapper.cs`, `Systems/Core/GameManager.cs`,
  `Systems/Core/StateMachines/`, `Systems/Core/Pool/`, `Systems/Save/`.
- Auditoría (el *porqué*, con el código real de cada bug):
  `/Users/ningunfernando/Projects/Unity/HamsterBall/Docs/AUDITORIA_ARQUITECTURA.md`
- Copia original de la guía:
  `/Users/ningunfernando/Projects/Unity/HamsterBall/Docs/GUIA_PLANTILLA_ARQUITECTURA.md`

**Nunca crear un symlink de HamsterBall dentro de `Assets/`.** Unity sigue symlinks: importaría los
23 scripts del otro proyecto y duplicaría tipos, asmdefs y GUIDs dentro de la plantilla, además de
meter el gameplay específico (bola-hámster, taxis, pasajeros) que la sección 1 de la guía prohíbe.

## Entorno real de este repo (verificado el 2026-09-08)

| Componente | Este repo | La guía asume |
|---|---|---|
| Unity Editor | `6000.6.0f1` | `6000.4.6f1` |
| URP | 17.6.0 | 17.4.0 |
| Input System | 1.20.0 | 1.19.0 |
| Test Framework | 1.8.0 | 1.6.0 |
| UGUI | 2.6.0 | 2.0.0 |
| Cinemachine | **no instalado** | 3.1.6 |

**Decisión: respetar las versiones instaladas.** No downgradear paquetes de Unity para coincidir
con la guía — no aporta nada a la arquitectura y rompe la Library.

**Decisión: no instalar Cinemachine y no tocar `Packages/manifest.json`.** Consecuencia: si llega a
existir un rig de cámara, `CameraRigTarget` es el dueño del suavizado, no Cinemachine.

**Excepción (2026-09-13): `jp.shiranui-isuzu.unity-mcp` sí entra en `manifest.json`**, fijado por git
URL a `#v4.3.3`. Es tooling para que los agentes (Claude Code, Qwen) manejen el Editor por MCP, no
una dependencia del juego: todo vive en `Editor/` y no llega a ningún build. La regla de arriba sigue
valiendo para paquetes de runtime. Subir de versión se hace cambiando el tag en el manifest (o con
`isuzu-unity-cli update`), no a mano en `Library/`.

Ya vienen instalados y la guía preferiría que no (`ai.navigation`, `visualscripting`, `timeline`,
`collab-proxy`, más `ai.assistant` y `ai.inference` en pre-release). **Quedan como están**: quitarlos
es una decisión pendiente, no un paso de la plantilla.

## Estructura: desviaciones aceptadas respecto a la sección 4 de la guía

- **Repo plano.** La raíz del repo ES el proyecto Unity. No se crea el wrapper `{Project}/` que
  dibuja la guía. La ruta de trabajo es `<REPO>/Assets/_Game/…`. El wrapper solo servía para alojar
  contenido no-Unity, y `Docs/` ya cumple eso sin necesidad de mover el proyecto.
- **`Docs/` se queda dentro de `Assets/`** (`Assets/Docs/`). Decisión consciente: Unity genera
  `Assets/Docs.meta` y un `.meta` por cada `.md`, y aparecerán en los diffs de git. No moverla más
  adelante sin decidir antes qué hacer con esos `.meta` (ya habrán asignado GUID).
- **Qwen Code se arranca desde la raíz del repo**, no desde `Assets/`, para que este archivo,
  `.qwenignore` y `.qwen/` vivan donde tocan y la memoria de proyecto tenga una clave estable.
- **Sin `Runtime/CameraRig/` ni `{ASM}.Camera`**: 7 assemblies. Ver "Alcance acordado" arriba.
- **Sin `Art/`, `Audio/`, `Shading/` ni `_Game/Settings/`.** La sección 4 de la guía los dibuja, pero
  ningún paso los llena nunca, y `Assets/Settings/` ya existe con los assets de URP: un segundo
  `Settings/` vacío solo invita a dudar de cuál manda. Una carpeta vacía es la versión-carpeta de los
  stubs que critica M11. Crearlas cuando haya contenido cuesta cero.
- **Sin `_Game/Docs/`.** La guía pone ahí el `ARCHITECTURE.md` del Paso 8; aquí va en `Assets/Docs/`,
  que es donde ya vive la guía.

## Reglas que aplican siempre

- Las 14 reglas de la sección 3 de la guía. Las que más fácil se relajan: **R1** (los 7 asmdef de
  este repo existen antes que cualquier `.cs`), **R3** (grafo acíclico: `Data` no referencia nada,
  `Core` solo a `Data`, los módulos de gameplay nunca entre sí), **R4** (gameplay se comunica solo
  por `EventBus`), **R5** (dominio en C# puro, el `MonoBehaviour` es un adapter fino), **R6** (nada
  de `FindAnyObjectByType` para cablear; quien crea un objeto conserva la referencia y la inyecta),
  **R10** (suscripciones en `OnEnable`/`OnDisable`, nunca en `Awake`), **R12** (cero stubs
  silenciosos), **R13** (ningún `Debug.Log` directo fuera de `Log.cs`).
- **Los movimientos y renombrados de `.cs` se hacen con el Editor de Unity abierto**, nunca desde el
  filesystem. Unity preserva los GUID de los `.meta` al mover dentro del Editor; por terminal se
  rompen las referencias de escenas y prefabs y aparece "Missing Script".
- **Un `.cs` nuevo escrito desde fuera del Editor puede quedarse sin compilar y sin ningún aviso.**
  Pasó el 2026-09-14 con `BootstrapperTests.cs`, creado mientras Unity recargaba el dominio: se importó
  como `MonoScript` de la assembly correcta, pero Unity no lo metió en su lista de fuentes (ni
  reimportarlo ni un build limpio lo arreglaron), así que no hubo error y la suite siguió en verde con
  los tests viejos. Síntoma: la cuenta de tests no sube. **Después de añadir tests, comprobar siempre la
  cuenta.** Arreglo que funcionó: renombrarlo y devolverle el nombre desde el Editor
  (`AssetDatabase.MoveAsset` ida y vuelta), que conserva el GUID y fuerza el alta en la lista.
- **No desuscribir `SceneManager.sceneLoaded` en el `OnDestroy` del `Bootstrapper`.** Cargar la escena
  de juego descarga `Scene_Bootstrap`, y eso destruye el `Bootstrapper` antes de que Unity dispare el
  evento: un `OnDestroy` que desuscribe deja la secuencia muda, la escena cambia pero `StartGame()`
  nunca corre y no hay ni un error en la consola que lo delate. El handler se desuscribe a sí mismo
  como primera línea y con eso basta para no dejar el evento estático colgado. Bug real encontrado al
  probar en Play Mode el 2026-09-08.
- **Todo lo que se escriba tiene un call site y un test que lo ejecuta.** El defecto central de
  HamsterBall fue código con apariencia de terminado que nunca se ejecutó: `EventBus` con 3
  `Publish` y 0 `Subscribe`, `SaveSystem.Save()` con 0 call sites, `ObjectPoolManager.Get()` con 0.
  Si un sistema todavía no va a tener consumidor, no se escribe.
- No commitear ni revertir cambios preexistentes del worktree: son de Fernando.

## Convenciones de escritura (obligatorias para cualquier agente)

Fijadas por Fernando el 2026-09-08. Aplican a todo lo que se escriba en este repo: código,
comentarios, mensajes de commit y documentación.

- **Cero emojis.** Ni en comentarios, ni en código, ni en strings de log, ni en documentación.
- **Cero em-dash (`—`).** En su lugar: coma, punto, dos puntos o paréntesis. Aplica también a la
  documentación en español. Los separadores de sección que la sección 7 de la guía dibuja con
  caracteres de caja (`─`, U+2500) no son em-dash y se mantienen.
- **Código y comentarios en inglés.** Nombres de tipos, métodos y campos, strings de log, XML doc y
  comentarios de código. La sección 7 de la guía ya exige un solo idioma en los logs: inglés.
- **Documentación en español.** `QWEN.md`, `Assets/Docs/*.md`, y en su momento `README.md` y
  `ARCHITECTURE.md`.

El texto preexistente de este archivo y de la guía contiene em-dash escritos antes de esta decisión.
No hacer reescrituras masivas: corregirlos solo en los párrafos que se toquen.

## Estado de la construcción

**Paso 1 completado el 2026-09-08** — commit `27caaa8` (19 carpetas + 7 `.asmdef` + sus `.meta`,
34 archivos). La higiene de git previa va en su propio commit, `36fe949`. Cero `.cs`, como exige R1.

Verificado por script y contra `Logs/Editor.log`, no de memoria: grafo acíclico y sin referencias
colgando · `Data` sin referencias · `Core` → solo `Data` · `Player` y `Save` → `Core`+`Data` y nunca
entre sí · `Debug` y `Tests` hojas · Unity importó los 7 asmdefs (`AssemblyDefinitionImporter` en los
7 `.meta`) y no reescribió el contenido de ninguno · cero `error CS` en el log. Unity informa
*"will not be compiled, because it has no scripts associated with it"* para los 7: es el estado
correcto hasta el Paso 2, no un fallo.

Lo único que sigue sin comprobar es el grafo **visual** en el Editor (`Window → Analysis → Assembly
Dependencies`, o abrir cada `.asmdef` y mirar *References*). La sintaxis con `||` del constraint de
`Debug` sí está resuelta: esa forma la usan paquetes de Unity instalados en esta misma versión
(`Unity.AI.Assistant.Runtime`, `Unity.AppUI`), así que el fallback de la sección 4.1 no hace falta.
El símbolo que usa, en cambio, sí es problemático: ver pendiente 4.

**Paso 2 completado el 2026-09-08** — commit `ac95464`, 13 `.cs` (858 líneas) en
`DecoupledTemplate.Core`. Unity los compiló a `Library/ScriptAssemblies/DecoupledTemplate.Core.dll`
con **cero errores y cero warnings**, comprobado sobre el trozo nuevo de `Logs/Editor.log` y no de
memoria.
Cero `Debug.Log` fuera de `Log.cs` (R13) · namespace en los 13 (R2) · cero `Find` (R6) · dos
`TODO(Fase-4)` con el formato de R12 · cero em-dash y cero emojis.

Dos defectos de la guía, corregidos al escribir el código:

- **§6.1 no compila.** `[Conditional("UNITY_EDITOR", "DEVELOPMENT_BUILD")]` es ilegal:
  `ConditionalAttribute` toma un solo string. La forma válida serían dos atributos apilados, que el
  compilador lee como OR.
- **`DEVELOPMENT_BUILD` está deprecado como directiva de compilación en Unity 6** y genera el warning
  `UAC0009` en cada compilado. `Log.Info` lleva `[Conditional("DEBUG")]`, el símbolo variant-aware
  que el propio aviso recomienda y que cubre la misma intención (Editor + development build).
  Verificado contra `Library/Bee/artifacts/*.dag/DecoupledTemplate.Core.rsp`: `DEBUG` sí está
  definido en el Editor, `DEVELOPMENT_BUILD` no.

**Paso 3 completado el 2026-09-08** — `GameConfigSO.cs` en `DecoupledTemplate.Data` y su cableado en
el `Bootstrapper`, que pasa de un `const GAME_SCENE_NAME` a leer `_gameConfig.GameSceneName`. Con eso
la arista `Core → Data` deja de estar muerta: ya hay un tipo de `Core` usando uno de `Data`. Unity
compila `DecoupledTemplate.Data.dll` y `DecoupledTemplate.Core.dll` con cero errores y cero warnings.
El asset `GameConfig_Default.asset` lo crea Fernando en el Editor, que es justo lo que ejercita el
`[CreateAssetMenu]`: si está mal escrito, no se descubre hasta ese momento.

Consecuencia estructural que conviene no olvidar: **`Data` no referencia nada (R3), así que ningún SO
puede usar tipos de `Core`.** `GameState` vive en `Core.State` y `PoolConfig` en `Core.Pool`, por lo
que un `GameConfigSO` no puede tener un campo de estado inicial ni una lista de pools; cualquier enum
que necesite tiene que declararlo dentro de `Data`. Si algún día se quiere configuración dirigida por
datos, hay dos salidas: mover el enum `GameState` a `Data` (la arista ya existe, y es más barato
antes del Paso 6, cuando aún no hay prefabs ni escenas que referencien `GameManager`), o declarar en
`Data` un enum propio que `Core` traduzca.

`GameConfigSO` lleva un solo campo a propósito: es lo único de `Core` configurable hoy sin romper R3,
y añadir campos sin consumidor para que el archivo parezca más completo sería el M11 que la guía
prohíbe. Tampoco lleva `OnValidate`, porque se dispara en cada pulsación del Inspector y cualquier
reescritura o aviso ahí pelea con quien está editando. La validación vive en el consumidor:
`Bootstrapper.ValidateConfiguration()` comprueba el SO y el nombre de escena **antes** de instanciar
nada y lanza excepción, en vez de fallar al final de la secuencia con un `LoadScene` incomprensible.

**Verificado en Play Mode el 2026-09-08.** Fernando montó adelantando parte del Paso 6
(`Scene_Bootstrap`, `Scene_Game`, `GameManager.prefab`, `ObjectPoolManager.prefab` y el asset
`GameConfig_Default`), y la secuencia sale completa en la consola y termina en `Scene_Game` con
`PlayState` activo: las once líneas de la secuencia más `Scene_Game loaded`, `Menu -> Play`,
`MenuState Exit`, `PlayState Enter` y `Game started`. Del Paso 6 queda el índice 0 de
`Scene_Bootstrap` en Build Settings, meter el `DebugHud` en `Scene_Game`, y comprobar que entrar en
Play desde `Scene_Game` directamente degrada con un error claro en vez de con una NRE.

**Paso 4 completado el 2026-09-08** — las tres capas de `DecoupledTemplate.Save`
(`ISaveStorage` + `JsonSaveStorage` de infraestructura, `ProgressService` de dominio, `SaveSystem`
de adapter) más `SaveData` versionado desde el día 1 y `SaveMigrations` con su cadena. La
verificación de la guía sale limpia: `MonoBehaviour` solo aparece en `SaveSystem.cs`. Compila con
cero errores y cero warnings. El save se escribe en
`~/Library/Application Support/DefaultCompany/DecoupledTemplate/save.json`.

El antiguo pendiente de cómo llegar al save sin romper R3 quedó resuelto con la salida (a): **`Core`
declara `ISaveLifecycle` (`Load`/`Save`) y el `Bootstrapper` instancia el prefab de `SaveSystem` como
`GameObject` sin tipo**, resolviendo el contrato con `GetComponent`, que lanza si el prefab no lo
lleva. `GameManager` no llega a conocer el save: la referencia la conserva el `Bootstrapper`, que es
quien la crea (R6). Es la única abstracción nueva del proyecto y no es especulativa: es la frontera
que R3 obliga a tener entre las dos assemblies.

Detalle de plataforma que la guía da por supuesto y este entorno no cumple: `File.Move(origen,
destino, overwrite)` **no existe** en el nivel de compatibilidad de API del proyecto (CS1501). La
escritura transaccional de `JsonSaveStorage` borra el destino antes de mover: el archivo final nunca
queda truncado, y si el proceso muere entre el borrado y el move, el `.tmp` conserva el payload.

**Verificado en Play Mode el 2026-09-08.** Con `SaveSystem.prefab` creado y asignado al campo nuevo
del `Bootstrapper`, la secuencia sale con sus tres pasos y `[SaveSystem] Loaded save v1.` entre el
paso 1 y el 2. Todavía no se escribe `save.json` en disco: nada muta el progreso, `_dirty` nunca se
activa, y eso es lo correcto. El round-trip de escritura y migración lo prueban los tests del Paso 7
contra una carpeta temporal.

**Paso 5 empezado el 2026-09-13: `DebugHud` hecho, falta `Player`.** En `Assets/_Game/Debug/`:
`DebugHudModel` (C# puro que arma el texto, testeable en EditMode sin panel ni Play Mode, R5),
`DebugHud` (adapter: se suscribe en `OnEnable` a `OnBootstrapComplete` y `OnGameStateChanged` y
actualiza un `Label`), y `PanelSettings_DebugHud.asset` con su tema `UnityDefaultRuntimeTheme.tss`.
En `Scene_Game` hay un GameObject `DebugHud` con `UIDocument` y `DebugHud`. Tests: 4 nuevos en
`DebugHudModelTests` y uno de PlayMode que arranca desde `Scene_Bootstrap` y lee el `Label`. Total:
46 EditMode y 2 PlayMode, todo en verde (tras el pendiente 5, el 2026-09-14: 50 y 4).

**Decisión de Fernando (2026-09-13): el HUD usa UI Toolkit, no Canvas + TextMeshPro** como dice el
Paso 5 de la guía. La intención de la guía (no usar `OnGUI`, M5) se cumple igual, porque UI Toolkit
es de modo retenido y no redibuja por frame. A cambio: `Debug` no necesita referencias a paquetes (es
un módulo del motor), no hay que importar TMP Essentials, y el `Label` lo crea el script, así que en
un build sin la assembly `Debug` el `UIDocument` queda vacío y no se ve nada. `OnGUI` sigue prohibido.

Detalle que sostiene el diseño: `UIDocument` reconstruye su árbol en su propio `OnEnable` y descarta
lo que se haya añadido antes. `DebugHud` añade el `Label` en su `OnEnable` y funciona porque
`UIDocument` declara `[DefaultExecutionOrder(-100)]`, comprobado por reflexión en `6000.6.0f1`. Si una
versión futura lo cambia, el test de PlayMode del HUD falla.

**Escala del HUD (2026-09-13).** `PanelSettings_DebugHud` usa `ScaleWithScreenSize` con referencia
1080x1920 y `match` 0.5, y el `Label` usa fuente de 32 puntos. Al crear el asset por código había
quedado en `ConstantPhysicalSize` con `referenceDpi` 257, el DPI de la pantalla Retina donde se creó:
medido en 1080x1920, el texto salía a 10.5 px y ocupaba el 1.6% del alto. Medido tras el cambio: 32 px
y 4.4% del alto en vertical 1080x1920; 37.4 px y 9.1% en horizontal 1920x1080. En UI Toolkit `match`
interpola en lineal, no en logarítmico como el `CanvasScaler` de UGUI, así que el mismo tamaño no se ve
igual en las dos orientaciones (escala 1.0 frente a 1.17). Si el juego va a ser horizontal, bajar la
fuente o subir `match` hacia 1 (escala por el alto). `referenceDpi` quedó en 96 para que volver a
`ConstantPhysicalSize` no herede el 257.

**Verificado en Play Mode el 2026-09-13.** Desde `Scene_Bootstrap`: 18 líneas de log, **cero errores
y cero warnings** (desaparecen los dos `Published ... with no subscribers`), y el HUD muestra
`Bootstrap: complete` y `State: Play`. Con eso quedan hechos también dos restos del Paso 6 anotados
arriba: `Scene_Bootstrap` ya está en el índice 0 de Build Settings y `DebugHud` ya está en
`Scene_Game`. Desde `Scene_Game` directamente no explota, y desde el mismo día da un error claro (pendiente 5).

### Pendientes

1. **La guía existe dos veces** (`Assets/Docs/` aquí y `HamsterBall/Docs/`). La autoritativa es la de
   este repo; si se edita, la otra diverge en silencio. (El pendiente de rastrear `QWEN.md`,
   `.qwenignore` y `Assets/Docs/` quedó resuelto por Fernando en el commit `2e4ed28`.)
2. **Resuelto el 2026-09-13.** `OnBootstrapComplete` y `OnGameStateChanged` ya tienen suscriptor
   (`DebugHud`), y Play Mode desde `Scene_Bootstrap` sale sin warnings.
3. **`Log.Info` todavía no tiene ningún call site.** Es la única pieza escrita hasta ahora sin
   consumidor. Se mantiene porque §6.1 especifica los cuatro niveles del wrapper; inventarle una
   llamada para cumplir la regla sería justo el código decorativo que la guía critica.
4. **El `defineConstraints` del asmdef de `Debug` usa `DEVELOPMENT_BUILD`**, el símbolo deprecado de
   `UAC0009`. Desde el 2026-09-13 la assembly tiene scripts y compila en el Editor con cero warnings,
   así que el constraint no dispara `UAC0009` ahí. Sigue sin comprobar el development build real: que
   el constraint se cumpla y el HUD no quede fuera en silencio. En el mismo build conviene mirar el de
   release, donde lo esperado es que el componente `DebugHud` de `Scene_Game` quede como script
   ausente y Unity lo avise en el log del player. El último punto del *Definition of Done* lo pide.
5. **Resuelto el 2026-09-13: Play Mode desde `Scene_Game` da un error claro.** Antes degradaba en
   silencio (consola vacía, HUD en `pending`/`unknown`). Ahora `Bootstrapper.CheckEntryScene`, con
   `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, registra un `Log.Error` si la primera escena
   cargada depende del bootstrap, y dice desde qué escena entrar. La regla vive en
   `DependsOnBootstrap`: índice de build mayor que 0, salvo la escena del Test Runner. Se descartó
   ponerlo en `DebugHud`: detectar el arranque es asunto del bootstrap, y así funciona en cualquier
   escena y sin la assembly `Debug`.
   - **Las escenas fuera del build (índice -1) no avisan a propósito**: son escenas de prueba sueltas.
   - **La escena del Test Runner (`InitTestScene<guid>`) se excluye por nombre.** Durante una corrida
     de PlayMode reporta un índice de build positivo (visto en `6000.6.0f1` con Test Framework 1.8.0).
     La primera versión del guard suponía -1 y cada corrida empezaba con un error falso en la consola.
   - Tests: `BootstrapperTests` (la regla, 4 casos, incluido `InitTestScene` con índice positivo),
     `EntryGuard_FromGameScene_LogsClearError` (espera el error y ninguna excepción en los frames
     siguientes) y `EntryGuard_FromBootstrapScene_LogsNothing`. Los dos de PlayMode comprueban además
     que el atributo sigue puesto, porque invocan el método a mano.
   - Verificado: tras la corrida de PlayMode la consola solo tiene el error que espera el test. En Play
     Mode manual, desde `Scene_Game` sale un solo error y nada más, y desde `Scene_Bootstrap` cero
     errores y cero warnings.

### Anotado para el Paso 5 (no antes: sería adelantar trabajo)

- **Faltan las referencias a paquetes en `Player`.** Se respetó al pie de la letra la plantilla 4.1,
  que solo referencia assemblies del proyecto. En cuanto exista `PlayerInputReader`, `Player` necesita
  `Unity.InputSystem`. Sin ella el fallo es un `CS0246` despistado, porque el `autoReferenced` de un
  paquete solo afecta a las assemblies predefinidas de Unity (`Assembly-CSharp`), no a las nuestras.
  Nombre verificado en `Library/PackageCache`. (Lo de `Debug` con `Unity.TextMeshPro` y
  `UnityEngine.UI` ya no aplica: el HUD usa UI Toolkit.)
- **`namespace DecoupledTemplate.Debug` sombrea `UnityEngine.Debug`.** Dentro de ese namespace,
  `Debug.Log(...)` resuelve al namespace y da `CS0118`. R13 (todo logging por `Log.cs`) lo hace
  improbable. Ojo: los dos asmdef de tests ya referencian `Debug`, así que un `Debug.Log` sin calificar
  dentro de `DecoupledTemplate.Tests` también daría `CS0118`.
