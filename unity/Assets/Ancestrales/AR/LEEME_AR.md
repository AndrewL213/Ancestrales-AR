# Ancestrales AR — criaturas en tu espacio, estilo Pokémon GO

Este módulo pone a las seis leyendas sobre el **suelo**, sobre una **mesa** o sobre la **palma de la mano**, ancladas al mundo real. Se capturan lanzando un amuleto.

## Qué trae

| Carpeta | Contenido |
|---|---|
| `Modelos/` | silbon, patasola, mohan, madremonte, hojarasquin y hombre_caiman en `.glb`. Miden 1 m, tienen el origen en los pies y texturas de 1–2K para celular. |
| `Runtime/` | Lógica del encuentro: detección de superficies, invocación, criatura "viva", amuleto, aro de captura, sombra, luz real y la interfaz. |
| `Shaders/` | 4 shaders sencillos que funcionan en Built-in y en URP. |
| `Resources/HudAR.uss` | Estilos de la interfaz AR, con la paleta de Ancestrales. |
| `Editor/` | Menú **Ancestrales > AR**, que instala los paquetes y arma todo. |

## Instalación (unos 10 minutos)

1. **Importa** `Ancestrales_AR.unitypackage`, o copia la carpeta `Ancestrales/AR` dentro de `Assets/Ancestrales/`. Funciona en Unity 2021.3 o más reciente (se recomienda 2022.3 LTS o Unity 6).
2. **Ancestrales > AR > 0. Instalar paquetes**. Instala AR Foundation, ARCore, ARKit y glTFast (este último es el que lee los `.glb`).
   - Si Unity pregunta por el *nuevo Input System*, acepta. Después ve a **Player > Other Settings > Active Input Handling** y elige **Both** para que no se rompan los controles del runner.
3. Entra a **Edit > Project Settings > XR Plug-in Management**:
   - en la pestaña Android, marca **ARCore**;
   - en la pestaña iOS, marca **ARKit**.
4. **Ancestrales > AR > Configurar todo**. Este menú hace lo siguiente:
   - crea `Datos/` con una ficha por criatura y el catálogo;
   - crea los materiales y el prefab de los planos;
   - crea `Escenas/EncuentroAR.unity` y la agrega al Build;
   - ajusta Android (API 24+, IL2CPP, ARM64, OpenGLES3) y el permiso de cámara de iOS;
   - si usas URP, agrega el *AR Background Renderer Feature*. Sin ese paso, el video de la cámara se ve negro.
5. Ve a **File > Build Settings > Android**, conecta el celular con la depuración USB activada y presiona **Build And Run**.

> Para probar sin celular: en XR Plug-in Management, pestaña PC, marca **XR Simulation**. Así puedes jugar en el editor dentro de un cuarto simulado.

## Cómo se juega

1. Mueve el celular despacio. Aparecen puntitos sobre las superficies que se van detectando.
2. Un **aro** marca dónde va a aparecer la criatura. Es **verde en el suelo** y **ámbar en una mesa**.
3. Toca la pantalla y la criatura aparece. Respira, se balancea, te sigue con la mirada y a veces da un saltito.
4. **Desliza el amuleto hacia arriba** para lanzarlo. El aro de color se encoge una y otra vez: si aciertas cuando está pequeño, tienes más probabilidad de captura (¡Bien!, ¡Genial!, ¡Excelente!).
5. El amuleto se sacude tres veces. La criatura puede escaparse o incluso huir.
6. Las capturas se guardan en PlayerPrefs (`ColeccionAR`).

### Superficies

| Modo | Qué hace | Tamaño por defecto |
|---|---|---|
| **Auto** | Distingue suelo y mesa por la altura del plano. | — |
| **Suelo** | Solo acepta el suelo. | 1,5–1,9 m |
| **Mesa** | Solo acepta superficies altas (mesas, escritorios). | 26–30 cm |
| **Palma** | La criatura aparece pequeña a ~33 cm de la cámara, anclada al espacio. Pones la palma debajo y queda "parada" en tu mano. | 12–13 cm |

**Sobre la palma:** ARCore y ARKit no detectan manos como superficie. Pokémon GO resuelve las fotos en la mano de la misma manera: la criatura queda en un punto fijo y la persona acomoda la mano debajo. En este modo se apaga la oclusión para que la mano no tape los pies. Si más adelante quieres que la criatura siga a la mano, se puede conectar un plugin de seguimiento de manos (por ejemplo MediaPipe Unity Plugin) y mover el ancla al punto de la palma.

## Ajustar cada criatura

Abre `Datos/Criatura_<id>.asset`:

- **alturaSuelo / alturaMesa / alturaPalma**: el tamaño final en metros.
- **giroY**: si la criatura aparece de espaldas, pon `180`.
- **facilidadCaptura**: entre 0,05 (muy difícil) y 1 (muy fácil). También define el color del aro.
- **probabilidadHuir** y **colorAura**.
- **modelo**: lo puedes cambiar por un prefab con animaciones (ver abajo).

## Animaciones

Los modelos vienen en pose T o A y sin animaciones, porque los rigs de los `.blend` no tenían acciones exportables. Para que se vean vivos, `CriaturaViva` agrega respiración, balanceo, giro hacia la cámara, saltitos y las animaciones de aparecer, ser absorbida y huir.

Para usar animaciones reales:

1. Sube el `.glb` o un `.fbx` de cada criatura a **Mixamo**, que hace el auto-rig.
2. Descarga una animación de *Idle* en FBX y otras que quieras (Taunt, Roar…).
3. En Unity, crea un Animator Controller con *Idle* en bucle como estado por defecto y arma un prefab con ese Animator.
4. Pon ese prefab en el campo **modelo** de la criatura. `CriaturaViva` detecta el Animator y deja de hacer la respiración procedural.

## Abrir el AR desde el juego

```csharp
Ancestrales.AR.GestorEncuentroAR.CriaturaInicialId = "silbon"; // opcional
UnityEngine.SceneManagement.SceneManager.LoadScene("EncuentroAR");

// Para enterarse de una captura (por ejemplo, para desbloquear algo en la Colección):
Ancestrales.AR.ColeccionAR.AlCapturar += id => Debug.Log("Capturó " + id);
```

En el componente **HudAR** (objeto *Encuentro AR*), escribe en `escenaMenu` el nombre de tu escena de menú. Así aparece el botón ✕ para volver.

## Problemas comunes

| Síntoma | Solución |
|---|---|
| Pantalla negra | Revisa que ARCore o ARKit estén marcados en XR Plug-in Management. En URP, revisa que el *AR Background Renderer Feature* esté en el Renderer. |
| Las criaturas no aparecen en las fichas | glTFast no estaba instalado cuando se crearon los datos. Instálalo y vuelve a usar **1. Crear datos**. |
| Criatura de espaldas | Pon `giroY = 180` en su ficha. |
| No detecta la mesa | Necesita textura y luz: una mesa blanca y lisa cuesta más. Prueba con el modo **Mesa**, o sube `distanciaMaximaMesa` en el Gestor. |
| El Mohán se ve de colores planos | Su `.blend` apuntaba a texturas que no venían dentro del archivo: `texturas_mohan/`, `Textures/` (dreadlocks), `falda_texturas/` y `barba_texturas/`. Por ahora tiene colores base. Con esas carpetas se puede volver a exportar con texturas. |
