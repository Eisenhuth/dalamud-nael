using Dalamud.Configuration;
using Dalamud.Plugin;

namespace nael
{
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; }
        public bool Enabled { get; set; } = true;
        
        public string Dynamo { get; set; } = "IN";
        public string Chariot { get; set; } = "OUT";
        public string Beam { get; set; } = "STACK";
        public string Dive { get; set; } = "DIVE";
        public string MeteorStream { get; set; } = "SPREAD";
        public string Separator { get; set; } = ">";
        

        private IDalamudPluginInterface pluginInterface;

        public void Initialize(IDalamudPluginInterface pInterface)
        {
            pluginInterface = pInterface;
        }

        public void Save()
        {
            pluginInterface.SavePluginConfig(this);
        }
    }
}
