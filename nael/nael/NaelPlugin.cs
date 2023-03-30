using System.Numerics;
using Dalamud.Game.Text;

namespace nael
{
    using Dalamud.Game.Command;
    using Dalamud.Game.Gui;
    using Dalamud.Game.Text.SeStringHandling;
    using Dalamud.Game.Text.SeStringHandling.Payloads;
    using Dalamud.IoC;
    using Dalamud.Plugin;
    using ImGuiNET;

    public class NaelPlugin : IDalamudPlugin
    {
        public string Name =>"Nael'd it for UCOB";
        private const string commandName = "/nael";
        private static bool drawConfiguration;
        
        private Configuration configuration;
        private ChatGui chatGui;
        [PluginService] private static DalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] private static CommandManager CommandManager { get; set; } = null!;

        private string configDynamo;
        private string configChariot;
        private string configBeam;
        private string configDive;
        private string configMeteorStream;
        private string configSeparator;

        
        public NaelPlugin([RequiredVersion("1.0")] DalamudPluginInterface dalamudPluginInterface, [RequiredVersion("1.0")] ChatGui chatGui, [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.chatGui = chatGui;

            configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
            configuration.Initialize(dalamudPluginInterface);

            LoadConfiguration();
            
            dalamudPluginInterface.UiBuilder.Draw += DrawConfiguration;
            dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            
            commandManager.AddHandler(commandName, new CommandInfo(NaelCommand)
            {
                HelpMessage = "toggle the plugin",
                ShowInHelp = true
            });
            
            this.chatGui.ChatMessage += OnChatMessage;
        }

        private void NaelCommand(string command, string args)
        {
            if (args.ToLower() == "test")
            {
                TestPlugin();
            }
            else
            {
                configuration.Enabled = !configuration.Enabled;
                configuration.Save();

                var pluginStatus = configuration.Enabled ? "enabled" : "disabled";
                chatGui.Print($"{Name} {pluginStatus}");
            }
        }

        private void TestPlugin()
        {
            chatGui.PrintChat(NaelMessage("Blazing path,\nlead me to iron rule!")); //beam > chariot
            chatGui.PrintChat(NaelMessage("From hallowed moon I descend,\na rain of stars to bring!")); //dynamo > dive > meteor stream
        }

        private static XivChatEntry NaelMessage(string message)
        {
            var entry = new XivChatEntry
            {
                Name = "Nael deus Darnus",
                Type = XivChatType.NPCDialogueAnnouncements,
                Message = message
            };

            return entry;
        }

        private void OnChatMessage(XivChatType type, uint id, ref SeString sender, ref SeString message, ref bool handled)
        {
            if (!configuration.Enabled) 
                return;

            if (type != XivChatType.NPCDialogueAnnouncements)
                return;

            foreach (var payload in message.Payloads)
                if (payload is TextPayload { Text: { } } textPayload)
                {
                    textPayload.Text = textPayload.Text.Replace("\n", " ");
                    textPayload.Text = NaelIt(textPayload.Text);
                }
        }
        
        /// <summary>
        /// checks the chat message for any Nael quote and replaces it with the mechanics
        /// </summary>
        /// <param name="input">chat message</param>
        /// <returns>the names of the mechanics</returns>
        private string NaelIt(string input)
        {
            return input switch
            {
                //Phase 2
                "O hallowed moon, take fire and scorch my foes!" => $"{configDynamo} {configSeparator} {configBeam}",
                "O hallowed moon, shine you the iron path!" => $"{configDynamo} {configSeparator} {configChariot}",
                "Take fire, O hallowed moon!" => $"{configBeam} {configSeparator} {configDynamo}",
                "Blazing path, lead me to iron rule!" => $"{configBeam} {configSeparator} {configChariot}",
                "From on high I descend, the iron path to call!" or "From on high I descend, the iron path to walk!" => $"{configDive} {configSeparator} {configChariot}",
                "From on high I descend, the hallowed moon to call!" => $"{configDive} {configSeparator} {configDynamo}",
                "Fleeting light! 'Neath the red moon, scorch you the earth!" => $"{configDive} {configSeparator} {configBeam}",
                "Fleeting light! Amid a rain of stars, exalt you the red moon!" => $"{configMeteorStream} {configSeparator} {configDive}",
                //Phase 3
                "From on high I descend, the moon and stars to bring!" => $"{configDive} {configSeparator} {configDynamo} {configSeparator} {configMeteorStream}",
                "From hallowed moon I descend, a rain of stars to bring!" => $"{configDynamo} {configSeparator} {configDive} {configSeparator} {configMeteorStream}",
                //Phase 4
                "From hallowed moon I descend, upon burning earth to tread!" => $"{configDynamo} {configSeparator} {configDive} {configSeparator} {configBeam}",
                "From hallowed moon I bare iron, in my descent to wield!" => $"{configDynamo} {configSeparator} {configChariot} {configSeparator} {configDive}",
                "Unbending iron, take fire and descend! " => $"{configChariot} {configSeparator} {configBeam} {configSeparator} {configDive}",
                "Unbending iron, descend with fiery edge!" => $"{configChariot} {configSeparator} {configDive} {configSeparator} {configBeam}",
                _ => input
            };
        }

        private void DrawConfiguration()
        {
            if (!drawConfiguration)
                return;
            
            ImGui.Begin($"{Name} Configuration", ref drawConfiguration);
            
            ImGui.InputText("Dynamo", ref configDynamo, 32);
            ImGui.InputText("Chariot", ref configChariot, 32);
            ImGui.InputText("Beam", ref configBeam, 32);
            ImGui.InputText("Dive", ref configDive, 32);
            ImGui.InputText("Meteor Stream", ref configMeteorStream, 32);
            ImGui.InputText("Separator", ref configSeparator, 8);
            
            ImGui.Separator();
            
            ImGui.Text($"Nael deus Darnus: {configBeam} {configSeparator} {configChariot}");
            ImGui.Text($"Nael deus Darnus: {configDynamo} {configSeparator} {configDive} {configSeparator} {configMeteorStream}");
            
            ImGui.Separator();        

            if (ImGui.Button("Test"))
            {
                TestPlugin();
            }
            
            if (ImGui.Button("Save and Close"))
            {
                SaveConfiguration();

                drawConfiguration = false;
            }
            
            ImGui.End();
        }

        private static void OpenConfig()
        {
            drawConfiguration = true;
        }

        private void LoadConfiguration()
        {
            configDynamo = configuration.Dynamo;
            configChariot = configuration.Chariot;
            configBeam = configuration.Beam;
            configDive = configuration.Dive;
            configMeteorStream = configuration.MeteorStream;
            configSeparator = configuration.Separator;
        }

        private void SaveConfiguration()
        {
            configuration.Dynamo = configDynamo;
            configuration.Chariot = configChariot;
            configuration.Beam = configBeam;
            configuration.Dive = configDive;
            configuration.MeteorStream = configMeteorStream;
            configuration.Separator = configSeparator;
            
            PluginInterface.SavePluginConfig(configuration);
        }

        public void Dispose()
        {
            chatGui.ChatMessage -= OnChatMessage;
            PluginInterface.UiBuilder.Draw -= DrawConfiguration;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

            CommandManager.RemoveHandler(commandName);
        }
    }
}