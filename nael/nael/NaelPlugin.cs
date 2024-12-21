namespace nael
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using Dalamud.Game;
    using Dalamud.Game.Command;
    using Dalamud.Game.Text;
    using Dalamud.Game.Text.SeStringHandling;
    using Dalamud.Game.Text.SeStringHandling.Payloads;
    using Dalamud.IoC;
    using Dalamud.Plugin;
    using Dalamud.Plugin.Services;
    using FFXIVClientStructs.FFXIV.Client.UI;
    using FuzzySharp;
    using ImGuiNET;

    public class NaelPlugin : IDalamudPlugin
    {
        public string Name =>"Nael'd it for UCOB";
        private const string commandName = "/nael";
        private static bool drawConfiguration;
        
        private Configuration configuration;
        private IChatGui chatGui;
        [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;
        [PluginService] private static ICommandManager CommandManager { get; set; } = null!;
        [PluginService] private static IClientState ClientState { get; set; } = null!;

        private string configDynamo;
        private string configChariot;
        private string configBeam;
        private string configDive;
        private string configMeteorStream;
        private string configSeparator;
        
        private bool gimmickText;
        private int gimmickDuration;

        private readonly NaelQuotes naelQuotes;
        private Dictionary<string, string> naelQuotesDictionary;

        public NaelPlugin(IDalamudPluginInterface dalamudPluginInterface, IChatGui chatGui, ICommandManager commandManager)
        {
            this.chatGui = chatGui;

            configuration = (Configuration) dalamudPluginInterface.GetPluginConfig() ?? new Configuration();
            configuration.Initialize(dalamudPluginInterface);

            LoadConfiguration();
            
            dalamudPluginInterface.UiBuilder.Draw += DrawConfiguration;
            dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
            dalamudPluginInterface.UiBuilder.OpenMainUi += OpenConfig;
            
            commandManager.AddHandler(commandName, new CommandInfo(NaelCommand)
            {
                HelpMessage = "toggle the translation\n/nael test → print test quotes\n/nael random → print random quote\n/nael cfg → open the configuration window",
                ShowInHelp = true
            });
            
            this.chatGui.ChatMessage += OnChatMessage;
            
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("nael.NaelQuotes.json");
            using var streamReader = new StreamReader(stream);
            var json = streamReader.ReadToEnd();
            naelQuotes = JsonSerializer.Deserialize<NaelQuotes>(json);
            
            LoadQuotesDictionary();
        }

        private void NaelCommand(string command, string args)
        {
            switch (args.ToLower())
            {
                case "cfg":
                    OpenConfig();
                    break;
                case "test":
                    PrintAllQuotes();
                    break;
                case "random":
                    PrintRandomQuote();
                    break;
                default:
                {
                    configuration.Enabled = !configuration.Enabled;
                    configuration.Save();

                    var pluginStatus = configuration.Enabled ? "translation enabled" : "translation disabled";
                    chatGui.Print($"{Name} {pluginStatus}");
                    break;
                }
            }
        }

        private void PrintAllQuotes()
        {
            foreach (var quote in naelQuotes.Quotes) 
                chatGui.Print(NaelMessage($"{GetQuote(quote.ID)}"));
        }

        private void PrintRandomQuote()
        {
            var number = new Random().Next(0, naelQuotes.Quotes.Count);
            var quote = naelQuotes.Quotes[number].ID;
            chatGui.Print(NaelMessage($"{GetQuote(quote)}"));
        }
        
        private static bool CheckForNael(string name)
        {
            var names = new string[]
            {
                "Nael deus Darnus", //EN/DE/FR
                "ネール・デウス・ダーナス", //JA
                "奈尔·神·达纳斯", //CN
            };

            return names.Contains(name);
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

        private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool handled)
        {
            if (type != XivChatType.NPCDialogueAnnouncements)
                return;

            foreach (var payload in message.Payloads)
                if (payload is TextPayload { Text: not null } textPayload)
                { 
                    if (configuration.Enabled) 
                        textPayload.Text = NaelIt(textPayload.Text);
                    
                    if (gimmickText && CheckForNael(sender.TextValue))
                        ShowTextGimmick(textPayload.Text, gimmickDuration);
                }
        }
        
        private static unsafe void ShowTextGimmick(string message, int durationInSeconds)
        {
            RaptureAtkModule.Instance()->ShowTextGimmickHint(message, RaptureAtkModule.TextGimmickHintStyle.Warning, durationInSeconds * 10);
        }
        
        /// <summary>
        /// checks the chat message for any Nael quote and replaces it with the mechanics
        /// </summary>
        /// <param name="input">chat message</param>
        /// <returns>the names of the mechanics or the chat message if no quotes are found</returns>
        private string NaelIt(string input)
        {
            var match = Process.ExtractOne(input, naelQuotesDictionary.Keys, (s) => s);
            
            return match is { Score: >= 85 } ? naelQuotesDictionary[match.Value] : input;
        }

        /// <summary>
        /// uses the API quote ID to get the quote matching the client's language from the embedded NaelQuotes.json
        /// quotes taken from https://xivapi.com/NpcYell/[id] and https://cafemaker.wakingsands.com/NpcYell/[id]
        /// </summary>
        /// <param name="id">the quote ID</param>
        /// <returns>the quote based on the ID and the client language</returns>
        private string GetQuote(int id)
        {
            var quote = naelQuotes.Quotes.Find(q => q.ID == id);
            var quoteText =  ClientState.ClientLanguage switch
            {
                ClientLanguage.English => quote.Text.Text_en,
                ClientLanguage.French => quote.Text.Text_fr,
                ClientLanguage.German => quote.Text.Text_de,
                ClientLanguage.Japanese => quote.Text.Text_ja,
                _ => quote.Text.Text_chs
            };
            quoteText = quoteText.Replace("\n\n", "\n");
            
            return quoteText;
        }

        /// <summary>
        /// loads all quotes from the embedded .json into a dictionary
        /// </summary>
        private void LoadQuotesDictionary()
        {
            naelQuotesDictionary = new Dictionary<string, string>
            {
                {GetQuote(6492), $"{configDynamo} {configSeparator} {configChariot}"}, //Phase 2 - Nael
                {GetQuote(6493), $"{configDynamo} {configSeparator} {configBeam}"},
                {GetQuote(6494), $"{configBeam} {configSeparator} {configChariot}"},
                {GetQuote(6495), $"{configBeam} {configSeparator} {configDynamo}"},
                {GetQuote(6496), $"{configDive} {configSeparator} {configChariot}"},
                {GetQuote(6497), $"{configDive} {configSeparator} {configDynamo}"},
                {GetQuote(6500), $"{configMeteorStream} {configSeparator} {configDive}"},
                {GetQuote(6501), $"{configDive} {configSeparator} {configBeam}"},
                {GetQuote(6502), $"{configDive} {configSeparator} {configDynamo} {configSeparator} {configMeteorStream}"}, //Phase 3 - Bahamut Prime
                {GetQuote(6503), $"{configDynamo} {configSeparator} {configDive} {configSeparator} {configMeteorStream}"},
                {GetQuote(6504), $"{configChariot} {configSeparator} {configBeam} {configSeparator} {configDive}"}, //Phase 4 - Adds
                {GetQuote(6505), $"{configChariot} {configSeparator} {configDive} {configSeparator} {configBeam}"},
                {GetQuote(6506), $"{configDynamo} {configSeparator} {configDive} {configSeparator} {configBeam}"},
                {GetQuote(6507), $"{configDynamo} {configSeparator} {configChariot} {configSeparator} {configDive}"}
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

            ImGui.Checkbox("Show Dawntrail Mechanic Text", ref gimmickText);
            ImGui.SliderInt("Display Duration", ref gimmickDuration, 1, 10);
            
            ImGui.Separator();       
            
            if (ImGui.Button("Test random quote"))
                PrintRandomQuote();
            
            ImGui.SameLine();

            if (ImGui.Button("Test all quotes"))
                PrintAllQuotes();
            
            
            ImGui.Separator();
            
            if (ImGui.Button("Save and Close"))
            {
                SaveConfiguration();
                LoadQuotesDictionary();
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
            gimmickText = configuration.GimmickText;
            gimmickDuration = configuration.GimmickDuration;
        }

        private void SaveConfiguration()
        {
            configuration.Dynamo = configDynamo;
            configuration.Chariot = configChariot;
            configuration.Beam = configBeam;
            configuration.Dive = configDive;
            configuration.MeteorStream = configMeteorStream;
            configuration.Separator = configSeparator;
            configuration.GimmickText = gimmickText;
            configuration.GimmickDuration = gimmickDuration;
            
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