# Ancestrales AR

Seis leyendas de Colombia en realidad aumentada: el Silbón, la Patasola, el Mohán, la Madremonte, el Hojarasquín y el Hombre Caimán.

**Abrir la app:** https://andrewl213.github.io/Ancestrales-AR/ (desde el celular)

## App web (esta carpeta raíz)
- Funciona en el navegador, sin instalar nada.
- **Android (Chrome con ARCore):** modo AR propio con WebXR, en `js/ar.js`.
  - Aparece en tamaño real y se ajusta sola: más pequeña si la superficie es una mesa u objeto alto, o si no cabría en la vista a esa distancia.
  - **Un toque** sobre la criatura la hace reaccionar. Un toque en el piso la mueve allá.
  - **Doble toque** la acerca a ti; otro doble toque la devuelve a su sitio.
- **iPhone (Safari):** se abre en AR Quick Look, en tamaño real.
- En la vista 3D:
  - un toque hace que reaccione: **saluda**, **grita** o **asusta**;
  - doble toque acerca la cámara a la cara;
  - también hay botones para cada reacción.
- Hecha con [model-viewer](https://modelviewer.dev) 4.1.0 y [three.js](https://threejs.org) r170, que van incluidos en `js/`. Tipografías UnifrakturMaguntia y EB Garamond (licencia OFL).

| Carpeta | Contenido |
|---|---|
| `modelos/` | Las criaturas en `.glb`: 1 m de alto, origen en los pies, en escala de grises (monocromáticas), con esqueleto y 4 animaciones (Reposo, Saludar, Gritar, Asustar). |
| `img/` | Retratos para el bestiario y el código QR. |

## Módulo para Unity (`unity/Assets/Ancestrales/AR`)
Es la versión completa estilo Pokémon GO, con AR Foundation:
- detección de suelo y mesa;
- modo palma;
- criaturas que respiran y te miran;
- captura lanzando un amuleto, con colección guardada.

Para usarlo, copia `unity/Assets/Ancestrales` en tu proyecto y sigue `LEEME_AR.md`.

## Paleta
| Nombre | Hex |
|---|---|
| Monte | `#0f1d16` |
| Musgo | `#22301f` |
| Pergamino | `#efe3c8` |
| Achiote | `#9b2226` |
| Ocre | `#b08a4a` |
