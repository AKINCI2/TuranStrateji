using UnityEngine;
using LegacyTouchPhase = UnityEngine.TouchPhase;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

public static class TuranTouchInput
{
    public static bool TryGetPrimaryTapDown(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemTapDown(out screenPosition))
            return true;
#endif

        if (TryGetTouchTapDown(out screenPosition))
            return true;

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        return false;
    }

    public static bool TryGetPrimaryPointerPosition(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemPointerPosition(out screenPosition))
            return true;
#endif

        if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
            return true;
        }

        screenPosition = Input.mousePosition;
        return true;
    }

    public static bool TryGetOneFingerPanDelta(out Vector2 delta)
    {
        delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemOneFingerPanDelta(out delta))
            return true;
#endif

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == LegacyTouchPhase.Moved)
            {
                delta = touch.deltaPosition;
                return true;
            }

            return false;
        }

        if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 28f;
            return delta.sqrMagnitude > 0.0001f;
        }

        return false;
    }

    public static bool TryGetPinchOrScrollDelta(out float delta)
    {
        delta = 0f;

#if ENABLE_INPUT_SYSTEM
        if (TryGetInputSystemPinchOrScrollDelta(out delta))
            return true;
#endif

        if (Input.touchCount >= 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 prevZero = touchZero.position - touchZero.deltaPosition;
            Vector2 prevOne = touchOne.position - touchOne.deltaPosition;

            float prevDistance = Vector2.Distance(prevZero, prevOne);
            float currentDistance = Vector2.Distance(touchZero.position, touchOne.position);
            delta = currentDistance - prevDistance;
            return Mathf.Abs(delta) > 0.001f;
        }

        delta = Input.mouseScrollDelta.y * 20f;
        return Mathf.Abs(delta) > 0.001f;
    }

    public static int TouchCount()
    {
#if ENABLE_INPUT_SYSTEM
        int inputSystemCount = GetInputSystemTouchCount();
        if (inputSystemCount >= 0)
            return inputSystemCount;
#endif

        return Input.touchCount;
    }

    private static bool TryGetTouchTapDown(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;
        if (Input.touchCount <= 0)
            return false;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != LegacyTouchPhase.Began)
            return false;

        screenPosition = touch.position;
        return true;
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetInputSystemTapDown(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        if (touchscreen.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        return false;
    }

    private static bool TryGetInputSystemPointerPosition(out Vector2 screenPosition)
    {
        screenPosition = Vector2.zero;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            screenPosition = touchscreen.primaryTouch.position.ReadValue();
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            screenPosition = mouse.position.ReadValue();
            return true;
        }

        return false;
    }

    private static bool TryGetInputSystemOneFingerPanDelta(out Vector2 delta)
    {
        delta = Vector2.zero;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return false;

        if (!touchscreen.primaryTouch.press.isPressed)
            return false;

        delta = touchscreen.primaryTouch.delta.ReadValue();
        return delta.sqrMagnitude > 0.0001f;
    }

    private static bool TryGetInputSystemPinchOrScrollDelta(out float delta)
    {
        delta = 0f;

        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var activeTouches = new System.Collections.Generic.List<TouchControl>();
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (touch != null && touch.press.isPressed)
                    activeTouches.Add(touch);
            }

            if (activeTouches.Count >= 2)
            {
                TouchControl touchZero = activeTouches[0];
                TouchControl touchOne = activeTouches[1];

                Vector2 currZero = touchZero.position.ReadValue();
                Vector2 currOne = touchOne.position.ReadValue();
                Vector2 prevZero = currZero - touchZero.delta.ReadValue();
                Vector2 prevOne = currOne - touchOne.delta.ReadValue();

                float prevDistance = Vector2.Distance(prevZero, prevOne);
                float currentDistance = Vector2.Distance(currZero, currOne);
                delta = currentDistance - prevDistance;
                return Mathf.Abs(delta) > 0.001f;
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            delta = mouse.scroll.ReadValue().y;
            return Mathf.Abs(delta) > 0.001f;
        }

        return false;
    }

    private static int GetInputSystemTouchCount()
    {
        Touchscreen touchscreen = Touchscreen.current;
        if (touchscreen == null)
            return -1;

        int count = 0;
        foreach (TouchControl touch in touchscreen.touches)
        {
            if (touch != null && touch.press.isPressed)
                count++;
        }

        return count;
    }
#endif
}
