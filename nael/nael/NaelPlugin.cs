using Dalamud.Game.Text;

namespace nael
{
    using Dalamud.Game.Command;
    using Dalamud.Game.Gui;
    using Dalamud.Game.Text.SeStringHandling;
    using Dalamud.Game.Text.SeStringHandling.Payloads;
    using Dalamud.IoC;
    using Dalamud.Plugin;

    public class NaelPlugin : IDalamudPlugin
    {
        public string Name =>"Nael'd it for UCOB";
        private const string commandName = "/nael";
        
        private Configuration configuration;
        private ChatGui chatGui;

        
        public NaelPlugin([RequiredVersion("1.0")] DalamudPluginInterface dalamudPluginInterface, [RequiredVersion("1.0")] ChatGui chatGui, [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.chatGui = chatGui;

            configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
            configuration.Initialize(dalamudPluginInterface);
            
            commandManager.AddHandler(commandName, new CommandInfo(NaelCommand)
            {
                HelpMessage = "toggle the plugin",
                ShowInHelp = true
            });

            this.chatGui.ChatMessage += OnChatMessage;
        }

        private void NaelCommand(string command, string args)
        {
            configuration.Enabled = !configuration.Enabled;
            configuration.Save();

            var pluginStatus = configuration.Enabled ? "enabled" : "disabled";
            chatGui.Print($"{Name} {pluginStatus}");
        }

        private void OnChatMessage(XivChatType type, uint id, ref SeString sender, ref SeString message, ref bool handled)
        {
            if (!configuration.Enabled) 
                return;

            foreach (var payload in message.Payloads)
                if (payload is TextPayload textPayload)
                    textPayload.Text = NaelIt(textPayload.Text);
        }
        
        /// <summary>
        /// checks the chat message for any Nael quote and replaces it with the mechanics
        /// </summary>
        /// <param name="input">chat message</param>
        /// <returns>the names of the mechanics</returns>
        private static string NaelIt(string input)
        {
            return input switch
            {
                //Phase 2
                "O hallowed moon, take fire and scorch my foes!" => "Dynamo > Beam",
                "O hallowed moon, shine you the iron path!" => "Dynamo > Chariot",
                "Take fire, O hallowed moon!" => "Beam > Dynamo",
                "Blazing path, lead me to iron rule!" => "Beam > Chariot",
                "From on high I descend, the iron path to call!" or "From on high I descend, the iron path to walk!" => "Dive > Chariot",
                "From on high I descend, the hallowed moon to call!" => "Dive > Dynamo",
                "Fleeting light! 'Neath the red moon, scorch your earth!" => "Dive > Beam",
                "Fleeting light! Amid a rain of stars, exalt you the red moon!" => "Meteor Stream > Dive",
                //Phase 3
                "From on high I descend, the moon and stars to bring!" => "Dive > Dynamo > Meteor Stream",
                "From hallowed moon I descend, a rain of stars to bring!" => "Dynamo > Dive > Meteor Stream",
                //Phase 4
                "From hallowed moon I descend, upon burning earth to tread!" => "Dynamo > Dive > Beam",
                "From hallowed moon I bare iron, in my descent to wield!" => "Dynamo > Chariot > Dive",
                "Unbending iron, take fire and descend! " => "Chariot > Beam > Dive",
                "Unbending iron, descend with fiery edge!" => "Chariot > Dive > Beam",
                _ => input
            };
        }

        public void Dispose()
        {
            chatGui.ChatMessage -= OnChatMessage;
        }
    }
}