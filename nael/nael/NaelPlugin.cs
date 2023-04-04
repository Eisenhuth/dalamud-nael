namespace nael
{
    using System.IO;
    using System.Reflection;
    using System.Text.Json;
    using Dalamud;
    using Dalamud.Game.ClientState;
    using Dalamud.Game.Text;
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
        [PluginService] private static ClientState ClientState { get; set; } = null!;

        private string configDynamo;
        private string configChariot;
        private string configBeam;
        private string configDive;
        private string configMeteorStream;
        private string configSeparator;

        private NaelQuotes _naelQuotes;

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
                HelpMessage = "toggle the plugin\n/nael test → print test quotes\n/nael cfg → open the configuration window",
                ShowInHelp = true
            });
            
            this.chatGui.ChatMessage += OnChatMessage;
            
            //load the quotes
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("nael.NaelQuotes.json");
            using var streamReader = new StreamReader(stream);
            var json = streamReader.ReadToEnd();
            _naelQuotes = JsonSerializer.Deserialize<NaelQuotes>(json);
        }

        private void NaelCommand(string command, string args)
        {
            switch (args.ToLower())
            {
                case "cfg":
                    OpenConfig();
                    break;
                case "test":
                    TestPlugin();
                    break;
                default:
                {
                    configuration.Enabled = !configuration.Enabled;
                    configuration.Save();

                    var pluginStatus = configuration.Enabled ? "enabled" : "disabled";
                    chatGui.Print($"{Name} {pluginStatus}");
                    break;
                }
            }
        }

        private void TestPlugin()
        {
            foreach (var quote in _naelQuotes.Quotes) 
                chatGui.PrintChat(NaelMessage($"{GetQuote(quote.ID)}"));
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
                    textPayload.Text = NaelIt(textPayload.Text);
                }
        }
        
        /// <summary>
        /// checks the chat message for any Nael quote and replaces it with the mechanics
        /// </summary>
        /// <param name="input">chat message</param>
        /// <returns>the names of the mechanics or the chat message if no quotes are found</returns>
        private string NaelIt(string input)
        {
            //Phase 2
            if (input == GetQuote(6492))
                return $"{configDynamo} {configSeparator} {configChariot}";
            if (input == GetQuote(6493))
                return $"{configDynamo} {configSeparator} {configBeam}";
            if (input == GetQuote(6494))
                return $"{configBeam} {configSeparator} {configChariot}";
            if (input == GetQuote(6495))
                return $"{configBeam} {configSeparator} {configDynamo}";
            if (input == GetQuote(6496))
                return $"{configDive} {configSeparator} {configChariot}";
            if (input == GetQuote(6497))
                return $"{configDive} {configSeparator} {configDynamo}";
            if (input == GetQuote(6500))
                return $"{configMeteorStream} {configSeparator} {configDive}";
            if (input == GetQuote(6501))
                return $"{configDive} {configSeparator} {configBeam}";
            //Phase 3
            if (input == GetQuote(6502))
                return $"{configDive} {configSeparator} {configDynamo} {configSeparator} {configMeteorStream}";
            if (input == GetQuote(6503))
                return $"{configDynamo} {configSeparator} {configDive} {configSeparator} {configMeteorStream}";
            //Phase 4
            if (input == GetQuote(6504))
                return $"{configChariot} {configSeparator} {configBeam} {configSeparator} {configDive}";
            if (input == GetQuote(6505))
                return $"{configChariot} {configSeparator} {configDive} {configSeparator} {configBeam}";
            if (input == GetQuote(6506))
                return $"{configDynamo} {configSeparator} {configDive} {configSeparator} {configBeam}";
            if (input == GetQuote(6507))
                return $"{configDynamo} {configSeparator} {configChariot} {configSeparator} {configDive}";
            
            return input;
        }

        /// <summary>
        /// uses the API quote ID to get the quote matching the client's language from the embedded NaelQuotes.json
        /// quotes taken from https://xivapi.com/NpcYell/[id]
        /// </summary>
        /// <param name="id">the quote ID</param>
        /// <returns>the quote based on the ID and the client language</returns>
        private string GetQuote(int id)
        {
            var quote = _naelQuotes.Quotes.Find(q => q.ID == id);
            var quoteText =  ClientState.ClientLanguage switch
            {
                ClientLanguage.English => quote.Text.Text_en,
                ClientLanguage.French => quote.Text.Text_fr,
                ClientLanguage.German => quote.Text.Text_de,
                ClientLanguage.Japanese => quote.Text.Text_ja,
                _ => quote.Text.Text_en
            };
            quoteText = quoteText.Replace("\n\n", "\n");
            
            return quoteText;
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
            
            if (ImGui.Button("Save and Close"))
            {
                SaveConfiguration();

                drawConfiguration = false;
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Test all quotes"))
            {
                TestPlugin();
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