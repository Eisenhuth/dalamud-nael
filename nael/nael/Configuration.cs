using Dalamud.Configuration;
using Dalamud.Plugin;

namespace nael
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool Enabled { get; set; } = true;
        
        public static string Dynamo { get; set; } = "Dynamo";
        public static string Chariot { get; set; } = "Chariot";
        public static string Beam { get; set; } = "Beam";
        public static string Dive { get; set; } = "Dive";
        public static string MeteorStream { get; set; } = "Meteor Stream";
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
