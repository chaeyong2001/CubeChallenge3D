using UnityEngine;

namespace CubeChallenge3D.UI.Style
{
    public sealed class MediumActionButton : MonoBehaviour
    {
        [SerializeField] private string iconKey;
        [SerializeField] private CasualUIColor theme;

        public void Configure(string value, CasualUIColor colorTheme)
        {
            iconKey = value;
            theme = colorTheme;
        }
    }
}
