using UnityEngine;

namespace CubeChallenge3D.UI.Style
{
    public sealed class InfoChip : MonoBehaviour
    {
        [SerializeField] private string role;

        public string Role => role;

        public void Configure(string value)
        {
            role = value;
        }
    }
}
