using Dalamud.Configuration;
using Dalamud.Plugin;

namespace nael
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool Enabled { get; set; } = true;
        
        public static string Dynamo { get; set; } = "IN";
        public static string Chariot { get; set; } = "OUT";
        public static string Beam { get; set; } = "STACK";
        public static string Dive { get; set; } = "DIVE";
        public static string MeteorStream { get; set; } = "SPREAD";
        public static string Separator { get; set; } = ">";
        

        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pInterface)
        {
            pluginInterface = pInterface;
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }
}
