// Modo de realidad aumentada propio (WebXR, Android con Chrome).
// - Tamaño: el grande (tamaño real). Se reduce solo si la superficie es una mesa
//   u otro objeto alto, o si la criatura no cabría en la vista a esa distancia.
// - Un toque sobre la criatura: reacciona (Saludar → Gritar → Asustar).
// - Un toque en otra parte de la superficie: la mueve allí.
// - Doble toque: la acerca a ti; otro doble toque la devuelve a su sitio.
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

const REACCIONES = ['Saludar', 'Gritar', 'Asustar'];
const DOBLE_TOQUE_MS = 300;

export async function arDisponible() {
  try { return !!(navigator.xr && await navigator.xr.isSessionSupported('immersive-ar')); }
  catch { return false; }
}

/**
 * @param {object} o
 * @param {string} o.url        .glb de la criatura (1 m de alto, origen en los pies)
 * @param {number} o.altura     altura grande (tamaño real) en metros
 * @param {HTMLElement} o.capa  capa HTML que se muestra encima de la cámara
 * @param {(t:string)=>void} o.mensaje
 * @param {(n:string)=>void} [o.alReaccionar]
 * @param {()=>void} [o.alColocar]
 * @param {()=>void} [o.alTerminar]
 */
export async function iniciarAR({ url, altura, capa, mensaje, alReaccionar, alColocar, alTerminar }) {
  // La sesión se pide primero: el navegador solo la permite justo después del toque del usuario.
  const sesion = await navigator.xr.requestSession('immersive-ar', {
    requiredFeatures: ['hit-test'],
    optionalFeatures: ['dom-overlay'],
    domOverlay: { root: capa },
  });
  try {
    return await prepararEscena(sesion, { url, altura, capa, mensaje, alReaccionar, alColocar, alTerminar });
  } catch (err) {
    sesion.end().catch(() => {});
    throw err;
  }
}

async function prepararEscena(sesion, { url, altura, capa, mensaje, alReaccionar, alColocar, alTerminar }) {
  const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
  renderer.setPixelRatio(window.devicePixelRatio);
  renderer.setSize(window.innerWidth, window.innerHeight);
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.xr.enabled = true;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;
  renderer.domElement.className = 'lienzo-ar';

  const escena = new THREE.Scene();
  const camara = new THREE.PerspectiveCamera(60, window.innerWidth / window.innerHeight, 0.01, 60);
  escena.add(new THREE.HemisphereLight(0xffffff, 0x555555, 1.6));
  const sol = new THREE.DirectionalLight(0xffffff, 1.8);
  sol.castShadow = true;
  sol.shadow.mapSize.set(1024, 1024);
  escena.add(sol, sol.target);

  // Aro que marca la superficie
  const aro = new THREE.Mesh(
    new THREE.RingGeometry(0.09, 0.11, 40).rotateX(-Math.PI / 2),
    new THREE.MeshBasicMaterial({ color: 0xefe3c8, transparent: true, opacity: 0.9 }));
  aro.matrixAutoUpdate = false;
  aro.visible = false;
  escena.add(aro);

  renderer.xr.setReferenceSpaceType('local');
  await renderer.xr.setSession(sesion);
  const espacioVista = await sesion.requestReferenceSpace('viewer');
  const fuenteImpactos = await sesion.requestHitTestSource({ space: espacioVista });
  // Los botones de la capa no deben contar como toques en la escena.
  capa.querySelectorAll('button').forEach(b => b.addEventListener('beforexrselect', e => e.preventDefault()));
  sesion.addEventListener('end', () => {
    renderer.setAnimationLoop(null);
    fuenteImpactos.cancel?.();
    renderer.domElement.remove();
    renderer.dispose();
    alTerminar?.();
  });
  document.body.appendChild(renderer.domElement);

  // Criatura (se carga ya dentro de la sesión, con la cámara encendida)
  mensaje('Invocando…');
  const gltf = await new GLTFLoader().loadAsync(url);
  const modelo = gltf.scene;
  modelo.traverse(o => { if (o.isMesh) { o.castShadow = true; o.frustumCulled = false; } });
  const raiz = new THREE.Group();
  raiz.add(modelo);
  const sombra = new THREE.Mesh(new THREE.PlaneGeometry(1.4, 1.4).rotateX(-Math.PI / 2),
    new THREE.ShadowMaterial({ opacity: 0.35 }));
  sombra.receiveShadow = true;
  sombra.position.y = 0.002;
  raiz.add(sombra);
  raiz.visible = false;
  escena.add(raiz);

  const mezclador = new THREE.AnimationMixer(modelo);
  const accion = n => {
    const clip = gltf.animations.find(c => c.name === n);
    return clip ? mezclador.clipAction(clip) : null;
  };
  const reposo = accion('Reposo');
  reposo?.play();
  let actual = reposo, siguiente = 0;
  mezclador.addEventListener('finished', () => {
    if (!reposo || actual === reposo) return;
    reposo.reset().fadeIn(0.25).play();
    actual.fadeOut(0.25);
    actual = reposo;
  });
  function reaccionar() {
    const n = REACCIONES[siguiente++ % REACCIONES.length];
    const a = accion(n);
    if (!a) return;
    a.reset().setLoop(THREE.LoopOnce, 1);
    a.clampWhenFinished = true;
    a.fadeIn(0.15).play();
    actual?.fadeOut(0.15);
    actual = a;
    alReaccionar?.(n);
    navigator.vibrate?.(n === 'Asustar' ? [40, 30, 60] : 25);
  }

  const control = renderer.xr.getController(0);
  escena.add(control);

  let colocada = false, sitio = new THREE.Vector3(), cerca = false;
  let movimiento = null;           // {desde, hasta, t}
  const camPos = new THREE.Vector3();

  function posCamara() { return camPos.setFromMatrixPosition(renderer.xr.getCamera().matrixWorld); }

  /** Tamaño automático: grande, salvo mesa/objeto alto o si no cabe en la vista. */
  function tamano(pos) {
    const c = posCamara();
    const caida = c.y - pos.y;                                 // cuánto más abajo que el celular
    const dist = Math.hypot(c.x - pos.x, c.z - pos.z);
    let h = altura;
    if (caida < 0.85) h = Math.min(h, 0.35);                   // mesa, mueble o algo alto
    const xrCam = renderer.xr.getCamera();
    const fov = 2 * Math.atan(1 / xrCam.projectionMatrix.elements[5]);
    const cabe = 2 * Math.hypot(dist, caida) * Math.tan(fov / 2) * 0.85;
    return Math.max(0.12, Math.min(h, cabe));
  }

  function mirarCamara() {
    const c = posCamara();
    raiz.rotation.y = Math.atan2(c.x - raiz.position.x, c.z - raiz.position.z);
  }

  function ponerEn(pos, animado) {
    const h = tamano(pos);
    if (animado && colocada) {
      movimiento = { desde: raiz.position.clone(), hasta: pos.clone(), e0: raiz.scale.x, e1: h, t: 0 };
    } else {
      raiz.position.copy(pos);
      raiz.scale.setScalar(h);
    }
    raiz.visible = true;
    mirarCamara();
  }

  function colocar() {
    if (!aro.visible) return;
    sitio.setFromMatrixPosition(aro.matrix);
    ponerEn(sitio, colocada);
    cerca = false;
    if (!colocada) { colocada = true; alColocar?.(); }
    mensaje('');
  }

  function alternarCercania() {
    if (!colocada) return;
    const c = posCamara();
    if (!cerca) {
      const dir = new THREE.Vector3(raiz.position.x - c.x, 0, raiz.position.z - c.z);
      const d = dir.length();
      const nueva = Math.max(0.7, d * 0.45);
      if (nueva >= d) return;
      dir.normalize().multiplyScalar(nueva);
      ponerEn(new THREE.Vector3(c.x + dir.x, raiz.position.y, c.z + dir.z), true);
      cerca = true;
    } else {
      ponerEn(sitio, true);
      cerca = false;
    }
  }

  const rayo = new THREE.Raycaster();
  const caja = new THREE.Box3();
  function tocoCriatura() {
    if (!colocada) return false;
    control.updateMatrixWorld();
    rayo.ray.origin.setFromMatrixPosition(control.matrixWorld);
    rayo.ray.direction.set(0, 0, -1).transformDirection(control.matrixWorld);
    caja.setFromObject(modelo).expandByScalar(raiz.scale.x * 0.08);
    return rayo.ray.intersectsBox(caja);
  }

  // Un toque o doble toque (el simple espera un instante por si llega el segundo).
  let ultimo = 0, pendiente = null;
  sesion.addEventListener('select', () => {
    const ahora = performance.now();
    const sobreCriatura = tocoCriatura();
    if (ahora - ultimo < DOBLE_TOQUE_MS) {
      clearTimeout(pendiente); pendiente = null; ultimo = 0;
      alternarCercania();
      return;
    }
    ultimo = ahora;
    pendiente = setTimeout(() => {
      pendiente = null;
      if (!colocada) colocar();
      else if (sobreCriatura) reaccionar();
      else colocar();               // toque en el piso: se mueve allá
    }, DOBLE_TOQUE_MS);
  });

  let ultimoT = 0;
  renderer.setAnimationLoop((t, frame) => {
    const dt = ultimoT ? Math.min(0.1, (t - ultimoT) / 1000) : 0;
    ultimoT = t;
    mezclador.update(dt);

    if (frame) {
      const ref = renderer.xr.getReferenceSpace();
      const imp = frame.getHitTestResults(fuenteImpactos);
      if (imp.length) {
        const pose = imp[0].getPose(ref);
        aro.matrix.fromArray(pose.transform.matrix);
        aro.visible = true;
        if (!colocada) mensaje('Toca para invocarla aquí.');
      } else {
        aro.visible = false;
        if (!colocada) mensaje('Mueve el celular despacio apuntando al suelo o a una mesa…');
      }
      // Una vez colocada, el aro solo se ve si apunta lejos de la criatura (para moverla).
      if (colocada && aro.visible) {
        const p = new THREE.Vector3().setFromMatrixPosition(aro.matrix);
        aro.visible = p.distanceTo(raiz.position) > raiz.scale.x * 0.4;
      }
    }

    if (movimiento) {
      movimiento.t = Math.min(1, movimiento.t + dt / 0.6);
      const e = movimiento.t * movimiento.t * (3 - 2 * movimiento.t);
      raiz.position.lerpVectors(movimiento.desde, movimiento.hasta, e);
      raiz.scale.setScalar(THREE.MathUtils.lerp(movimiento.e0, movimiento.e1, e));
      if (movimiento.t >= 1) movimiento = null;
    }
    if (colocada) {
      // Gira despacio para mirarte.
      const c = posCamara();
      const objetivo = Math.atan2(c.x - raiz.position.x, c.z - raiz.position.z);
      let dif = objetivo - raiz.rotation.y;
      dif = Math.atan2(Math.sin(dif), Math.cos(dif));
      raiz.rotation.y += dif * Math.min(1, dt * 2);
      // La sombra sigue a la criatura.
      const h = raiz.scale.x;
      sol.position.set(raiz.position.x + h * 0.6, raiz.position.y + h * 2.2, raiz.position.z + h * 0.6);
      sol.target.position.copy(raiz.position);
      const sc = sol.shadow.camera;
      sc.left = sc.bottom = -h; sc.right = sc.top = h; sc.near = 0.01; sc.far = h * 5;
      sc.updateProjectionMatrix();
    }
    renderer.render(escena, camara);
  });

  return { terminar: () => sesion.end() };
}
