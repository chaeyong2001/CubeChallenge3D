namespace CubeChallenge3D.UI.Style
{
    public static class UIIconRegistry
    {
        public static string Get(string key)
        {
            switch (key)
            {
                case "heart": return "5";
                case "coin": return "C";
                case "gem": return "G";
                case "shop": return "$";
                case "stages": return "3D";
                case "ranking": return "#1";
                case "solver": return "?";
                case "rewards": return "+";
                case "records": return "R";
                case "settings": return "*";
                case "hint": return "?";
                case "undo": return "<";
                case "scramble": return "X";
                case "start": return ">";
                case "menu": return "H";
                case "list": return "=";
                default: return string.Empty;
            }
        }
    }
}
