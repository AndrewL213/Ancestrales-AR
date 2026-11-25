using UnityEngine;

namespace Ancestrales.AR
{
    /// <summary>Lee un dedo (o el mouse en el editor) con el Input System nuevo o el antiguo.</summary>
    public static class EntradaTactil
    {
        public struct Estado
        {
            public bool empezo, termino, presionado;
            public Vector2 posicion;
        }

        static int frameCache = -1;
        static Estado cache;

        public static Estado Leer()
        {
            if (frameCache == Time.frameCount) return cache;
            frameCache = Time.frameCount;
            cache = LeerAhora();
            return cache;
        }

        static Estado LeerAhora()
        {
            var e = new Estado();
#if ENABLE_INPUT_SYSTEM
            var pantalla = UnityEngine.InputSystem.Touchscreen.current;
            if (pantalla != null)
            {
                var t = pantalla.primaryTouch;
                if (t.press.isPressed || t.press.wasReleasedThisFrame)
                {
                    e.posicion = t.position.ReadValue();
                    e.empezo = t.press.wasPressedThisFrame;
                    e.termino = t.press.wasReleasedThisFrame;
                    e.presionado = t.press.isPressed;
                    return e;
                }
            }
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                e.posicion = mouse.position.ReadValue();
                e.empezo = mouse.leftButton.wasPressedThisFrame;
                e.termino = mouse.leftButton.wasReleasedThisFrame;
                e.presionado = mouse.leftButton.isPressed;
            }
            return e;
#else
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                e.posicion = t.position;
                e.empezo = t.phase == TouchPhase.Began;
                e.termino = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                e.presionado = !e.termino;
                return e;
            }
            e.posicion = Input.mousePosition;
            e.empezo = Input.GetMouseButtonDown(0);
            e.termino = Input.GetMouseButtonUp(0);
            e.presionado = Input.GetMouseButton(0);
            return e;
#endif
        }
    }
}
