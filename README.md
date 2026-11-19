# RECOLEC(ECO): Mareas de Esperanza

*Navega por una bahía peruana, recoge residuos, recíclalos y mejora tu chalana mientras ayudas a devolverle la vida al mar.*

Proyecto final académico 3D en Unity. Aventura ecológica en la que manejas una **chalana pesquera** por la bahía
del Puerto de Pisco, recoges residuos flotantes con la red de proa, los llevas al muelle, los clasificas con
Doña Rosa, ganas EcoCréditos y mejoras tu misma chalana en el taller de Don Mateo. ODS 12, 13 y 14.

## Versión exacta de Unity

**Unity 6000.3.23f1 (Unity 6.3 LTS)**, Universal Render Pipeline 17.3.0, Input System 1.20.0, C#.
Abrir el proyecto siempre con esta versión.

## Contenido de la entrega

| Carpeta / archivo | Qué es |
|---|---|
| `RECOLECECO_Unity/` | Proyecto Unity editable (escenas, scripts C#, prefabs, materiales, mallas, shaders) |
| `Build/RECOLECECO_Windows/` | Build de Windows x64 (`RECOLECECO.exe` + `RECOLECECO_Data/` y DLLs) |
| `Build/RECOLECECO_Windows.zip` | La misma build comprimida para entregar |
| `README.md` | Este archivo |
| `CREDITS.md` | Origen y licencia de cada recurso (todo es original o integrado en Unity) |
| `GDD.md` | Documento de diseño actualizado con lo realmente implementado |
| `PRUEBAS.md` | Lista de pruebas mínimas (§12) con resultados y lo pendiente |
| `Pruebas/` | Informes de las pruebas automatizadas ejecutadas en la build y capturas |

## Cómo jugar (build de Windows)

1. Descomprimir `RECOLECECO_Windows.zip` (mantener juntos el `.exe`, la carpeta `RECOLECECO_Data`, `UnityPlayer.dll`, `MonoBleedingEdge` y `D3D12`).
2. Ejecutar `RECOLECECO.exe`.
3. En el menú: **Jugar** (o **Continuar** si hay una partida guardada), **Controles** o **Salir**.

Requisitos: Windows 10/11 de 64 bits, GPU compatible con DirectX 11/12.

## Controles

| Tecla | Acción |
|---|---|
| W / S (o ↑ / ↓) | Avanzar / frenar y retroceder |
| A / D (o ← / →) | Girar |
| Mouse (clic derecho + mover) | Orientar la cámara · rueda: zoom |
| E | Recoger residuo / atracar en el muelle / continuar diálogo |
| I | Inventario (bodega) |
| Esc | Pausa (continuar, controles, reiniciar, menú) |
| 1–5 | Contenedores en el centro de reciclaje · 1/2/3 en el panel del muelle |

## Ciclo de juego

Salir del puerto → navegar → recoger residuos con la red (E) → volver y atracar (E) → clasificar con Doña Rosa →
ganar EcoCréditos → comprar mejoras con Don Mateo → limpiar los sectores B y C → final.

Cinco misiones encadenadas; una flecha amarilla y una columna de luz marcan siempre el siguiente destino.
El progreso se guarda automáticamente al atracar y al completar cada misión
(`%USERPROFILE%\AppData\LocalLow\RECOLECECO\RECOLEC(ECO) Mareas de Esperanza\recolececo_partida.json`).

## Abrir y modificar el proyecto

1. Unity Hub → *Add project from disk* → `RECOLECECO_Unity` (con Unity 6000.3.23f1).
2. Escenas: `Assets/RECOLECECO/Scenes/MainMenu.unity` (inicio) y `BahiaPuerto.unity` (juego).
3. Valores del juego (velocidades, alcance, capacidad, costos, valores de reciclaje, distribución de residuos,
   metas de misiones): `Assets/RECOLECECO/Resources/GameConfig.asset` (ScriptableObject).
4. Todo el contenido 3D se regenera de forma reproducible con el menú **RECOLECECO → Construir todo**
   (o en línea de comandos con `-executeMethod RecolecEco.EditorTools.RecolecBuilder.BatchBuildAll`, que además genera la build).
   **Atención:** «Construir todo» reescribe las dos escenas; si se editan a mano, no volver a ejecutarlo.
5. Build manual: **RECOLECECO → Build Windows (x64)** o *File → Build Profiles → Windows*.

## Arquitectura (resumen)

```text
Assets/RECOLECECO/
  Scenes/      MainMenu.unity, BahiaPuerto.unity
  Scripts/
    Core/      GameManager, GameConfig (ScriptableObject), SaveManager (JSON), GameEvents (+Waves)
    Boat/      BoatController, BoatCamera, BoatVisuals, CollectorArm
    Waste/     CollectibleWaste, CollectionSystem
    Systems/   InventorySystem, RecyclingSystem, UpgradeSystem, QuestManager, DialogueSystem, EnvironmentProgress
    World/     DockZone, SectorZone, FaunaGroup, NPCAnimator, ObjectiveBeacon, fauna y decoración
    UI/        UIManager (toda la interfaz), MainMenuController, UIFactory, IconFactory
    Audio/     AudioManager, Synth (audio procedural original)
    Procedural/MeshGen (mallas low-poly)
    Editor/    RecolecBuilder (genera contenido, escenas y build)
    Test/      AutoTestRunner (prueba automatizada dentro de la build)
  Prefabs/     Boat, Waste, Characters, Environment
  Shaders/     RecolecWater, RecolecVertexLit
  Materials/ Models/ Textures/ Resources/ Data/
```

La lógica no conoce la interfaz: los sistemas publican eventos (`GameEvents`) y `UIManager` solo lee estado y llama a los sistemas.

## Prueba automatizada (opcional)

La build incluye un modo de prueba que juega la partida completa con un piloto automático (sustituye solo a las teclas):

```
RECOLECECO.exe -screen-fullscreen 0 -autotest full -autotestOut informe.md -autotestShots capturas
RECOLECECO.exe -screen-fullscreen 0 -autotest save   (juega, guarda y cierra)
RECOLECECO.exe -screen-fullscreen 0 -autotest load   (reabre, continúa y comprueba el guardado)
```

Sin estos argumentos el juego funciona normalmente.
