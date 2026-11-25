# Ancestrales AR

Seis leyendas de Colombia en realidad aumentada: el Silbón, la Patasola, el Mohán, la Madremonte, el Hojarasquín y el Hombre Caimán.

**Abrir la app:** https://andrewl213.github.io/Ancestrales-AR/ (desde el celular)

## App web (esta carpeta raíz)
- Funciona en el navegador, sin instalar nada.
- Android (Chrome, con ARCore): la criatura se pone sobre el suelo o una mesa con WebXR o Scene Viewer.
- iPhone (Safari): se abre en AR Quick Look.
- Tamaños: suelo (tamaño real), mesa (30 cm) y palma (12 cm).
- Al tocar la criatura reacciona: **saluda**, **grita** o **asusta**. También hay botones para cada reacción.
- Hecha con [model-viewer](https://modelviewer.dev) 4.1.0, que va incluido en `js/`. Tipografías UnifrakturMaguntia y EB Garamond (licencia OFL).

| Carpeta | Contenido |
|---|---|
| `modelos/` | Las criaturas en `.glb`: 1 m de alto, origen en los pies, colores unificados, con esqueleto y 4 animaciones (Reposo, Saludar, Gritar, Asustar). |
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
