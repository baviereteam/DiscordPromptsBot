using Discord;
using Discord.WebSocket;

namespace DiscordPromptsBot
{
    public class Worker : BackgroundService
    {
        private readonly IHostApplicationLifetime Application;
        private readonly IConfiguration Configuration;
        private readonly DiscordSocketClient Client;
        private readonly string LastPromptSentFile = Path.Combine(Environment.CurrentDirectory, "last_sent_prompt_number.txt");
        private readonly string[] Prompts;

        public Worker(IConfiguration configuration, IHostApplicationLifetime application)
        {
            Application = application;
            Configuration = configuration;
            Prompts = Configuration.GetSection("Prompts").GetChildren().Select(item => item.Value).ToArray();
            Client = new DiscordSocketClient();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Verify that we have the right configuration
            try
            {
                ValidateConfiguration();

                Client.Log += Log;

                await Client.LoginAsync(TokenType.Bot, Configuration.GetValue<string>("BotToken"));
                await Client.StartAsync();

                Client.Ready += async () =>
                {
                    await SendMessage();
                    await Client.LogoutAsync();
                    Application.StopApplication();
                };
            }

            catch (ConfigurationException e) 
            {
                Console.WriteLine($"Configuration is invalid: {e.Message}");
                Application.StopApplication();
            }
        }

        private void ValidateConfiguration()
        {
            var token = Configuration.GetValue<string>("BotToken");
            if (token == null)
            {
                throw new ConfigurationException("Missing BotToken parameter.");
            }

            var thread = Configuration.GetValue<ulong>("ThreadId");
            if (thread == 0)
            {
                throw new ConfigurationException("Missing ThreadId parameter.");
            }

            var role = Configuration.GetValue<string>("RoleToPingId");
            if (role == null)
            {
                throw new ConfigurationException("Missing RoleToPingId parameter.");
            }

            if (Prompts.Length == 0)
            {
                throw new ConfigurationException("No prompts found.");
            }
        }

        private async Task SendMessage()
        {
            // No error checking in here for now, because crashing is probably the best thing to do if something goes wrong anyway.
            var channel = Client.GetChannel(Configuration.GetValue<ulong>("ThreadId"));
            var channelTypesAllowed = new List<ChannelType?>
            {
                ChannelType.PrivateThread,
                ChannelType.PublicThread
            };

            if (!channelTypesAllowed.Contains(channel.GetChannelType()))
            {
                // The most probable cause of this crash, if the thread actually exists, is that the bot is not present in it.
                // The easiest fix is to ping the bot from inside the thread.

                throw new Exception("The channel matching the ThreadId is not a thread!");
            }

            var thread = channel as IThreadChannel;

            // once we're here, we just need to find a prompt
            var numberOfTheLastPromptSent = FindNumberOfLastPromptSent();

            int promptToSendToday;
            if (numberOfTheLastPromptSent == null)
            {
                // if we can't find which was the last prompt we sent, we'll set it to 0, that will send the first one
                promptToSendToday = 0;
            }
            else
            {
                promptToSendToday = numberOfTheLastPromptSent.Value + 1;
            }

            var promptText = Prompts[promptToSendToday];

            // if there's an @role in the prompt, we put the real role instead
            var roleId = Configuration.GetValue<string>("RoleToPingId");
            var finalPrompt = promptText.Replace("@role", $"<@&{roleId}>");

            await thread.SendMessageAsync(finalPrompt);

            // remember which prompt we sent today, for next time.
            SaveNumberOfPromptSentToday(promptToSendToday);
        }

        private void SaveNumberOfPromptSentToday(int promptNumber)
        {
            try
            {
                File.WriteAllText(LastPromptSentFile, promptNumber.ToString());
            }
            catch (Exception e)
            {
                // If an error happens here, we write it on the screen, but we don't crash because we're in a "catch".
                Console.WriteLine("Could not save the number of the last prompt that was sent. Details follow.");
                Console.WriteLine(e.ToString());
            }
        }

        private int? FindNumberOfLastPromptSent()
        {
            try
            {
                string lastPromptNumber = File.ReadAllText(LastPromptSentFile);
                return int.Parse(lastPromptNumber);
            } 
            catch
            {
                // something went wrong (the file does not exist because it's our first time, or what's in the file isn't a number, or whatever else)
                return null;
            }
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine($"[Discord.Net log] {message.ToString()}");
            return Task.CompletedTask;
        }
    }
}
