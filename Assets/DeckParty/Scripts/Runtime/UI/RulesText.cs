using System.Text;

namespace DeckParty.UI
{
    /// <summary>How-to-play copy shared by the main menu and the in-game menu (keys "rules.*").</summary>
    public static class RulesText
    {
        static readonly (string key, string color)[] s_Sections =
        {
            ("goal", "#FFC94A"),
            ("night", "#FFC94A"),
            ("risk", "#FF5A5A"),
            ("subcap", "#FF9A3D"),
            ("money", "#6BE38B"),
            ("communities", "#5FD3FF"),
            ("actions", "#A77BFF"),
            ("streamers", "#FF9AD5"),
            ("controls", "#3DDBD9"),
            ("multiplayer", "#FF4F8B"),
        };

        public static string Full
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var (key, color) in s_Sections)
                {
                    if (sb.Length > 0) sb.Append("\n\n");
                    sb.Append("<size=125%><b><color=").Append(color).Append('>').Append(Loc.Get($"rules.{key}.title")).Append("</color></b></size>\n");
                    sb.Append(Loc.Get($"rules.{key}.body"));
                }
                return sb.ToString();
            }
        }

        public static string Quick => Loc.Get("rules.quick");
    }
}
