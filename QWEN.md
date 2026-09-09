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
El símbolo que usa, en cambio, sí es problemático: ver pendiente 5.

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

### Pendientes

1. **La guía existe dos veces** (`Assets/Docs/` aquí y `HamsterBall/Docs/`). La autoritativa es la de
   este repo; si se edita, la otra diverge en silencio. (El pendiente de rastrear `QWEN.md`,
   `.qwenignore` y `Assets/Docs/` quedó resuelto por Fernando en el commit `2e4ed28`.)
2. **`Core` no puede referenciar `Save` (R3), pero §6.4 y §6.5 lo dan por supuesto.**
   `GameManager.RegisterManagers(SaveSystem, ObjectPoolManager)` y el campo `_saveSystemPrefab` del
   `Bootstrapper` son imposibles tal como están escritos: en HamsterBall todo vivía en
   `Assembly-CSharp`, aquí `Core` solo referencia `Data`. El Paso 2 se escribió sin ninguna
   dependencia de `Save` (`RegisterManagers(ObjectPoolManager)` y un `TODO(Fase-4)` marcando el hueco
   en la secuencia). **Decisión para el Paso 4**, con tres salidas: (a) una interfaz en `Core` que
   `SaveSystem` implemente, inversión de dependencias clásica, pero §1 prohíbe interfaces que no
   tengan dos implementaciones; (b) que el save se dispare solo por `EventBus` (R4) y `GameManager`
   no llegue a conocer `SaveSystem`; (c) mover el cableado del bootstrap a una assembly por encima
   de `Save`, que rompe la ubicación de §4.
3. **`OnBootstrapComplete` no tiene suscriptores**, y `Publish` avisa cuando no los hay (§6.2). Eso
   choca con la verificación del Paso 6 ("sin errores ni warnings"). La salida limpia es que
   `DebugHud` se suscriba a él en el Paso 5; la otra es aceptar el warning.
4. **`Log.Info` todavía no tiene ningún call site.** Es la única pieza escrita hasta ahora sin
   consumidor. Se mantiene porque §6.1 especifica los cuatro niveles del wrapper; inventarle una
   llamada para cumplir la regla sería justo el código decorativo que la guía critica.
5. **El `defineConstraints` del asmdef de `Debug` usa `DEVELOPMENT_BUILD`**, el símbolo deprecado que
   dispara `UAC0009`. Como esa assembly aún no tiene scripts no se compila y no avisa, pero en cuanto
   exista `DebugHud.cs` puede pasar algo peor que un warning: que el constraint no se cumpla en un
   development build y el HUD quede fuera en silencio. Verificar con un development build real (el
   último punto del *Definition of Done* ya lo pide) antes de cambiarlo a ciegas.

### Anotado para el Paso 5 (no antes: sería adelantar trabajo)

- **Faltan las referencias a paquetes.** Se respetó al pie de la letra la plantilla 4.1, que solo
  referencia assemblies del proyecto. En cuanto exista `PlayerInputReader`, `Player` necesita
  `Unity.InputSystem`; en cuanto exista `DebugHud`, `Debug` necesita `Unity.TextMeshPro` y
  `UnityEngine.UI`. Sin ellas el fallo es un `CS0246` despistado, porque el `autoReferenced` de un
  paquete solo afecta a las assemblies predefinidas de Unity (`Assembly-CSharp`), no a las nuestras.
  Nombres verificados en `Library/PackageCache`.
- **`namespace DecoupledTemplate.Debug` sombrea `UnityEngine.Debug`.** Dentro de ese namespace,
  `Debug.Log(...)` resuelve al namespace y da `CS0118`. R13 (todo logging por `Log.cs`) lo hace
  improbable, pero es una razón más para que `DebugHud` no llame a `Debug.*` directamente.
