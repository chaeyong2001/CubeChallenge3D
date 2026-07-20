using UnityEngine;

namespace CubeChallenge3D.UI.Style
{
    public sealed class LargeMenuCardButton : MonoBehaviour
    {
        [SerializeField] private string iconKey;
        [SerializeField] private CasualUIColor theme;

        public string IconKey => iconKey;
        public CasualUIColor Theme => theme;

        public void Configure(string value, CasualUIColor colorTheme)
        {
            iconKey = value;
            theme = colorTheme;
        }
    }
}
