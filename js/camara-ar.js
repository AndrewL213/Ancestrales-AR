// Realidad aumentada con la cámara del celular, como el modo AR clásico de Pokémon GO.
// Funciona en cualquier navegador móvil (iPhone y Android): video de la cámara de fondo,
// la criatura en 3D encima y el giroscopio para que se quede en su sitio al mover el celular.
//
//  - El PRIMER toque la coloca en el piso, donde tocaste.
//  - La cámara ajusta sola la distancia y el tamaño (grande, pero que quepa en la vista).
//  - Luego: 1 toque = reacciona (Saludar → Gritar → Asustar), 2 toques = acercar / alejar.
import * as THREE from './three/three.module.min.js';
import { GLTFLoader } from './three/addons/loaders/GLTFLoader.js';

const REACCIONES = ['Saludar', 'Gritar', 'Asustar'];
const ESPERA_TOQUES = 350;      // ms para decidir si fueron 1 o 2 toques
const ALTURA_CELULAR = 1.4;     // metros sobre el piso (se asume)
const FOV = 62;                 // campo de visión vertical aproximado de la cámara del celular
// Cuánto de la altura de la pantalla ocupa la criatura (como en Pokémon GO, nunca gigante).
const PANTALLA_NORMAL = 0.42;
const PANTALLA_CERCA = 0.62;

/** Pide los permisos. Llamar directamente desde el toque del usuario (iPhone lo exige). */
export function pedirPermisos() {
  const orientacion = (typeof DeviceOrientationEvent !== 'undefined' && DeviceOrientationEvent.requestPermission)
    ? DeviceOrientationEvent.requestPermission().catch(() => 'denied')
    : Promise.resolve('granted');
  const camara = navigator.mediaDevices?.getUserMedia
    ? navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' } }, audio: false })
    : Promise.reject(new Error('Este navegador no permite usar la cámara.'));
  return { orientacion, camara };
}

/**
 * @param {object} o
 * @param {HTMLElement} o.contenedor  elemento a pantalla completa donde va todo
 * @param {{orientacion:Promise, camara:Promise}} o.permisos  lo que devolvió pedirPermisos()
 * @param {string} o.url
 * @param {number} o.altura
 * @param {(t:string)=>void} o.mensaje
 * @param {()=>void} [o.alColocar]
 */
export async function iniciarCamaraAR({ contenedor, permisos, url, altura, mensaje, alColocar }) {
  mensaje('Abriendo la cámara…');
  const flujo = await permisos.camara;
  const giroscopioPermitido = (await permisos.orientacion) === 'granted';

  // Video de la cámara
  const video = document.createElement('video');
  video.className = 'ar-video';
  video.setAttribute('playsinline', ''); video.muted = true; video.autoplay = true;
  video.srcObject = flujo;
  contenedor.prepend(video);
  await video.play().catch(() => {});

  // Escena 3D transparente encima del video
  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer.setPixelRatio(Math.min(2, window.devicePixelRatio));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.domElement.className = 'ar-lienzo';
  contenedor.insertBefore(renderer.domElement, video.nextSibling);

  const escena = new THREE.Scene();
  const camara = new THREE.PerspectiveCamera(FOV, 1, 0.05, 80);
  camara.position.set(0, ALTURA_CELULAR, 0);
  escena.add(new THREE.HemisphereLight(0xffffff, 0x666666, 1.7));
  const sol = new THREE.DirectionalLight(0xffffff, 1.6);
  sol.position.set(1, 3, 2);
  escena.add(sol);

  function ajustarTamano() {
    const w = contenedor.clientWidth, h = contenedor.clientHeight;
    renderer.setSize(w, h, false);
    camara.aspect = w / h;
    camara.updateProjectionMatrix();
  }
  ajustarTamano();
  window.addEventListener('resize', ajustarTamano);

  // ---------- Orientación del celular (giroscopio) ----------
  const objetivo = new THREE.Quaternion();
  const euler = new THREE.Euler();
  const q1 = new THREE.Quaternion(-Math.sqrt(0.5), 0, 0, Math.sqrt(0.5)); // de "mirando arriba" a "mirando al frente"
  const zee = new THREE.Vector3(0, 0, 1);
  const q0 = new THREE.Quaternion();
  let hayGiroscopio = false;
  function alOrientar(e) {
    if (e.alpha == null && e.beta == null) return;
    hayGiroscopio = true;
    const a = THREE.MathUtils.degToRad(e.alpha || 0);
    const b = THREE.MathUtils.degToRad(e.beta || 0);
    const g = THREE.MathUtils.degToRad(e.gamma || 0);
    const pantalla = THREE.MathUtils.degToRad(screen.orientation?.angle ?? window.orientation ?? 0);
    euler.set(b, a, -g, 'YXZ');
    objetivo.setFromEuler(euler).multiply(q1).multiply(q0.setFromAxisAngle(zee, -pantalla));
  }
  if (giroscopioPermitido) window.addEventListener('deviceorientation', alOrientar);
  // Sin giroscopio: mirando al frente y un poco hacia abajo.
  objetivo.setFromEuler(new THREE.Euler(THREE.MathUtils.degToRad(-18), 0, 0, 'YXZ'));
  camara.quaternion.copy(objetivo);

  // ---------- Criatura ----------
  mensaje('Invocando…');
  const gltf = await new GLTFLoader().loadAsync(url);
  const modelo = gltf.scene;
  modelo.traverse(o => { if (o.isMesh) o.frustumCulled = false; });
  const raiz = new THREE.Group();
  raiz.add(modelo);
  raiz.add(crearSombra());
  raiz.visible = false;
  escena.add(raiz);

  const mezclador = new THREE.AnimationMixer(modelo);
  const accion = n => { const c = gltf.animations.find(x => x.name === n); return c ? mezclador.clipAction(c) : null; };
  const reposo = accion('Reposo');
  reposo?.play();
  let actual = reposo, siguiente = 0;
  mezclador.addEventListener('finished', () => {
    if (!reposo || actual === reposo) return;
    reposo.reset().fadeIn(0.25).play(); actual.fadeOut(0.25); actual = reposo;
  });
  function reaccionar() {
    const n = REACCIONES[siguiente++ % REACCIONES.length];
    const a = accion(n);
    if (!a) return;
    a.reset().setLoop(THREE.LoopOnce, 1);
    a.clampWhenFinished = true;
    a.fadeIn(0.15).play(); actual?.fadeOut(0.15); actual = a;
    navigator.vibrate?.(n === 'Asustar' ? [40, 30, 60] : 25);
  }

  // ---------- Colocar y ajustar ----------
  let colocada = false, cerca = false;
  const sitio = new THREE.Vector3();
  let movimiento = null;

  /**
   * Tamaño automático según la distancia: la criatura ocupa una parte fija de la pantalla
   * (más si está "cerca"), y nunca pasa de su tamaño real.
   */
  function tamanoPara(pos, fraccion) {
    let h = altura;
    for (let i = 0; i < 3; i++) {                     // la distancia depende de la altura: se afina
      const centro = pos.clone().setY(pos.y + h * 0.5);
      const d = camara.position.distanceTo(centro);
      const visible = 2 * d * Math.tan(THREE.MathUtils.degToRad(FOV) / 2);
      h = Math.min(altura, visible * fraccion);
    }
    return Math.max(0.1, h);
  }

  function ponerEn(pos, animado) {
    const h = tamanoPara(pos, cerca ? PANTALLA_CERCA : PANTALLA_NORMAL);
    const giro = Math.atan2(camara.position.x - pos.x, camara.position.z - pos.z);
    if (animado && raiz.visible) {
      movimiento = { desde: raiz.position.clone(), hasta: pos.clone(), e0: raiz.scale.x, e1: h, t: 0 };
    } else {
      raiz.position.copy(pos); raiz.scale.setScalar(h);
      raiz.rotation.y = giro;
      aparecer();
    }
    raiz.visible = true;
  }

  let aparicion = 1;
  function aparecer() { aparicion = 0; }

  const rayo = new THREE.Raycaster();
  const piso = new THREE.Plane(new THREE.Vector3(0, 1, 0), 0);
  function puntoEnPiso(x, y) {
    const r = contenedor.getBoundingClientRect();
    const ndc = new THREE.Vector2(((x - r.left) / r.width) * 2 - 1, -((y - r.top) / r.height) * 2 + 1);
    rayo.setFromCamera(ndc, camara);
    const p = new THREE.Vector3();
    const dir = rayo.ray.direction.clone();
    if (!rayo.ray.intersectPlane(piso, p) || dir.y > -0.05) {
      // Apuntando al horizonte o arriba: va al piso a 2,5 m en esa dirección.
      dir.y = 0; dir.normalize();
      p.copy(camara.position).addScaledVector(dir, 2.5).setY(0);
    }
    // Ni encima de uno, ni demasiado lejos.
    const plano = new THREE.Vector3(p.x - camara.position.x, 0, p.z - camara.position.z);
    const dist = THREE.MathUtils.clamp(plano.length(), 1.5, 6);
    plano.normalize().multiplyScalar(dist);
    return new THREE.Vector3(camara.position.x + plano.x, 0, camara.position.z + plano.z);
  }

  function colocar(x, y) {
    sitio.copy(puntoEnPiso(x, y));
    cerca = false;
    ponerEn(sitio, false);
    colocada = true;
    mensaje('');
    alColocar?.();
  }

  function alternarCercania() {
    if (!colocada) return;
    cerca = !cerca;
    if (cerca) {
      const dir = new THREE.Vector3(sitio.x - camara.position.x, 0, sitio.z - camara.position.z);
      const nueva = Math.max(1.0, dir.length() * 0.6);
      dir.normalize().multiplyScalar(Math.min(nueva, dir.length()));
      ponerEn(new THREE.Vector3(camara.position.x + dir.x, 0, camara.position.z + dir.z), true);
    } else {
      ponerEn(sitio, true);
    }
  }

  // ---------- Toques: el primero coloca; luego 1 = reacción, 2 = acercar/alejar ----------
  // Se usa la hora del propio toque (no la del reloj del navegador), así un cuadro lento
  // no convierte un doble toque en dos toques sueltos.
  let pendiente = null, tPrevio = -1e9;
  function alTocar(e) {
    if (e.target.closest('button')) return;
    e.preventDefault();
    if (!colocada) { colocar(e.clientX, e.clientY); return; }
    const t = e.timeStamp;
    if (pendiente && t - tPrevio < ESPERA_TOQUES) {
      clearTimeout(pendiente); pendiente = null; tPrevio = -1e9;
      alternarCercania();
      return;
    }
    tPrevio = t;
    const decidir = () => {
      // Si el navegador estuvo ocupado, deja pasar los toques que quedaron en cola.
      if (performance.now() - tPrevio < ESPERA_TOQUES) { pendiente = setTimeout(decidir, 40); return; }
      pendiente = null;
      reaccionar();
    };
    pendiente = setTimeout(decidir, ESPERA_TOQUES);
  }
  contenedor.addEventListener('pointerup', alTocar);

  if (!hayGiroscopio && !giroscopioPermitido)
    mensaje('Toca para colocarla. (Sin permiso de movimiento, la criatura no se quedará fija al girar el celular.)');
  else mensaje('Apunta al piso y toca donde quieras que aparezca.');

  // ---------- Bucle ----------
  const reloj = new THREE.Clock();
  let activo = true;
  renderer.setAnimationLoop(() => {
    if (!activo) return;
    const dt = Math.min(0.1, reloj.getDelta());
    mezclador.update(dt);
    camara.quaternion.slerp(objetivo, hayGiroscopio ? 0.35 : 1);

    if (movimiento) {
      movimiento.t = Math.min(1, movimiento.t + dt / 0.6);
      const e = movimiento.t * movimiento.t * (3 - 2 * movimiento.t);
      raiz.position.lerpVectors(movimiento.desde, movimiento.hasta, e);
      raiz.scale.setScalar(THREE.MathUtils.lerp(movimiento.e0, movimiento.e1, e));
      if (movimiento.t >= 1) movimiento = null;
    }
    if (aparicion < 1) {
      aparicion = Math.min(1, aparicion + dt / 0.6);
      const k = aparicion, s = k === 1 ? 1 : 1 - Math.pow(2, -10 * k) * Math.cos(k * 10);
      modelo.scale.setScalar(Math.max(0.001, s));
    }
    if (colocada) {
      // Te sigue con la mirada.
      const obj = Math.atan2(camara.position.x - raiz.position.x, camara.position.z - raiz.position.z);
      let dif = obj - raiz.rotation.y; dif = Math.atan2(Math.sin(dif), Math.cos(dif));
      raiz.rotation.y += dif * Math.min(1, dt * 2);
    }
    renderer.render(escena, camara);
  });

  return {
    recolocar() { colocada = false; raiz.visible = false; mensaje('Toca donde quieras que aparezca.'); },
    terminar() {
      activo = false;
      renderer.setAnimationLoop(null);
      window.removeEventListener('deviceorientation', alOrientar);
      window.removeEventListener('resize', ajustarTamano);
      contenedor.removeEventListener('pointerup', alTocar);
      flujo.getTracks().forEach(t => t.stop());
      video.remove(); renderer.domElement.remove(); renderer.dispose();
    },
  };
}

function crearSombra() {
  const c = document.createElement('canvas'); c.width = c.height = 128;
  const g = c.getContext('2d');
  const grad = g.createRadialGradient(64, 64, 0, 64, 64, 64);
  grad.addColorStop(0, 'rgba(0,0,0,0.55)'); grad.addColorStop(1, 'rgba(0,0,0,0)');
  g.fillStyle = grad; g.fillRect(0, 0, 128, 128);
  const tex = new THREE.CanvasTexture(c);
  const m = new THREE.Mesh(new THREE.PlaneGeometry(0.9, 0.9).rotateX(-Math.PI / 2),
    new THREE.MeshBasicMaterial({ map: tex, transparent: true, depthWrite: false }));
  m.position.y = 0.003;
  return m;
}
