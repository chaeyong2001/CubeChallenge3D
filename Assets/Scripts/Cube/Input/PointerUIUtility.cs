using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CubeChallenge3D.Cube.Input
{
    public static class PointerUIUtility
    {
        public static bool IsPointerOverUi(int pointerId = -1)
        {
            return EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject(pointerId);
        }

        public static bool IsScreenPositionOverUi(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            return results.Count > 0;
        }

        public static bool HasMultipleActiveTouches()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return false;
            }

            int activeTouches = 0;
            foreach (var touch in touchscreen.touches)
            {
                if (touch.press.isPressed && ++activeTouches > 1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
