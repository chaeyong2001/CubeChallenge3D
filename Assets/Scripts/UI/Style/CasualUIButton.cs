using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeChallenge3D.UI.Style
{
    public sealed class CasualUIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private CasualUIColor theme = CasualUIColor.Blue;
        private Vector3 restingScale = Vector3.one;

        private void Awake()
        {
            restingScale = transform.localScale;
        }

        public void Apply(CasualUIColor colorTheme)
        {
            theme = colorTheme;
            Button button = GetComponent<Button>();
            if (button != null)
            {
                CasualUIStyle.ApplyButton(button, theme);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            transform.localScale = restingScale * 0.97f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.localScale = restingScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.localScale = restingScale;
        }
    }
}
