# DecoupledTemplate

Plantilla de arquitectura para juegos en Unity 6: un `EventBus` tipado, un bootstrap que arranca los
sistemas en un orden verificable, guardado versionado, un jugador 3D con `Rigidbody` y un HUD de
desarrollo, todo cubierto por tests. Nace de la auditoría de un proyecto anterior (HamsterBall), y
cada regla evita un fallo que ocurrió allí.

| Documento | Para qué sirve |
|---|---|
| [`Assets/Docs/ARCHITECTURE.md`](Assets/Docs/ARCHITECTURE.md) | Cómo está hecha: assemblies, flujo de arranque, eventos y módulos |
| [`Assets/Docs/GUIA_PLANTILLA_ARQUITECTURA.md`](Assets/Docs/GUIA_PLANTILLA_ARQUITECTURA.md) | La especificación: 14 reglas, orden de construcción y *Definition of Done* |
| [`QWEN.md`](QWEN.md) | Decisiones tomadas sobre este repo, estado y pendientes. También lo leen los agentes |

## Requisitos

- Unity `6000.6.0f1`.
- Paquetes que usa la plantilla, ya declarados en `Packages/manifest.json`: URP 17.6.0, Input System
  1.20.0 y Test Framework 1.8.0.
- Opcional: `jp.shiranui-isuzu.unity-mcp` 4.3.3, también en el manifest, para manejar el Editor desde
  agentes o desde la terminal. Es solo de Editor y no llega a ningún build.

## Abrir y jugar

1. En Unity Hub, *Add project from disk* y elegir la raíz del repo: la raíz es el proyecto de Unity.
2. Abrir `Assets/_Game/Scenes/Scene_Bootstrap.unity` y pulsar Play. **Siempre desde
   `Scene_Bootstrap`**: entrar desde `Scene_Game` no inicializa nada, y la consola lo avisa con un error.
3. Mover al jugador con WASD, las flechas o el stick izquierdo del gamepad.
4. Arriba a la izquierda, el HUD de desarrollo muestra si terminó el bootstrap y el estado del juego.
   Solo existe en el Editor y en los development builds.

Con el símbolo `DECOUPLEDTEMPLATE_VERBOSE` definido (lo está en *Player Settings* para Standalone), la
consola muestra la secuencia de arranque paso a paso.

## Estructura

```
Assets/
├── _Game/
│   ├── Runtime/
│   │   ├── Core/       DecoupledTemplate.Core     bootstrap, EventBus, estados, pool, Log
│   │   ├── Data/       DecoupledTemplate.Data     ScriptableObjects de configuración
│   │   ├── Player/     DecoupledTemplate.Player   input y movimiento
│   │   └── Save/       DecoupledTemplate.Save     guardado en tres capas
│   ├── Debug/          DecoupledTemplate.Debug    HUD, solo Editor y development builds
│   ├── Tests/
│   │   ├── EditMode/   DecoupledTemplate.Tests.EditMode
│   │   └── PlayMode/   DecoupledTemplate.Tests.PlayMode
│   ├── Prefabs/        GameManager, ObjectPoolManager, SaveSystem
│   └── Scenes/         Scene_Bootstrap (índice 0), Scene_Game (índice 1)
├── Docs/               ARCHITECTURE.md y la guía
└── InputSystem_Actions.inputactions
```

## Tests

Desde el Editor: `Window > General > Test Runner`, pestaña *EditMode* o *PlayMode*, y *Run All*.

Desde la terminal, con el Editor abierto y el CLI de Unity MCP instalado:

```bash
isuzu-unity-cli verify --test --test-mode edit
```

```bash
isuzu-unity-cli verify --test --test-mode play
```

| Suite | Tests | Duración en el Test Runner |
|---|---|---|
| EditMode | 59 | 2.5 s |
| PlayMode | 9 | 2.0 s |

Medido el 2026-09-14 en un Mac. Si la suite de EditMode pasa a tardar bastante más, lo más probable es
que un test esté usando Play Mode donde no hace falta.

**Errores esperados en la consola.** Tras la suite de EditMode la consola muestra 6 errores, y tras la
de PlayMode, 4. Son intencionados: cada test que prueba un caso de error declara el mensaje con
`LogAssert.Expect` y falla si no aparece. `isuzu-unity-cli verify` los cuenta igualmente porque solo
lee la consola, no sabe cuáles espera cada test.

**Una corrida de PlayMode con 0 tests no es verde.** El proyecto tiene activadas las *Enter Play Mode
Options* (sin recarga de dominio), y después de entrar en Play con `isuzu-unity-cli call
play_mode_play` las corridas de PlayMode no encuentran ningún test hasta la siguiente recarga de
dominio. Basta con recompilar o con forzar la recarga:

```bash
isuzu-unity-cli call execute_code --json '{"code":"EditorUtility.RequestScriptReload(); return \"ok\";"}'
```

## Crear un juego nuevo a partir de la plantilla

Los tres placeholders de la guía tienen el mismo valor en esta plantilla:

| Placeholder | Significado | Valor aquí |
|---|---|---|
| `{Project}` | Nombre del proyecto en PascalCase | `DecoupledTemplate` |
| `{ROOT_NS}` | Namespace raíz | `DecoupledTemplate` |
| `{ASM}` | Prefijo de las assemblies | `DecoupledTemplate` |

Unity enlaza cada componente de escenas y prefabs por el GUID del script y por `assembly::tipo`.
Cambiar a la vez el nombre de la assembly y el namespace puede romper ese enlace (sección 8 de la
guía), así que el orden importa:

1. **Con el Editor abierto**, renombrar las 7 assemblies: los campos `name` y `rootNamespace` de cada
   `.asmdef` y las `references` entre ellas (32 apariciones en total). Abrir las dos escenas y los tres
   prefabs de sistema, comprobar que ningún componente sale como *Missing Script*, guardar y hacer
   commit.
2. Cambiar los namespaces **módulo a módulo** (`Data`, `Core`, `Save`, `Player`, `Debug` y los tests),
   guardando escenas y prefabs y haciendo un commit después de cada módulo.
3. Cambiar el resto de apariciones del nombre:
   - `Log.VERBOSE` (`"DECOUPLEDTEMPLATE_VERBOSE"`) y el mismo símbolo en *Player Settings > Scripting
     Define Symbols*.
   - El `menuName` del `[CreateAssetMenu]` de `GameConfigSO`.
   - *Product Name* y *Company Name* en *Player Settings*. Cambian `Application.persistentDataPath`,
     así que un save que ya exista deja de encontrarse.
   - El prefijo de la carpeta temporal de `JsonSaveStorageTests` (solo cosmético).
   - Este README, `ARCHITECTURE.md` y `QWEN.md`.
4. Correr las dos suites y entrar en Play desde `Scene_Bootstrap`: la consola no debe mostrar errores ni
   warnings.

Los movimientos y renombrados de archivos `.cs` se hacen siempre desde el Editor, nunca desde el
sistema de archivos: Unity conserva así el GUID del `.meta` y las escenas no pierden sus componentes.

## Unity MCP (opcional)

El paquete `jp.shiranui-isuzu.unity-mcp` hace que el Editor sirva MCP en
`http://127.0.0.1:<puerto>/mcp` y responda al CLI `isuzu-unity-cli` (compilar, correr tests, leer la
consola, entrar en Play, editar escenas). El puerto se deriva de la ruta del proyecto. Para registrarlo
en Claude Code:

```bash
isuzu-unity-cli setup --agent claude-code --mcp
```

Las tools del MCP aparecen en la siguiente sesión del agente. La configuración queda en la carpeta del
usuario, no en el repo.
