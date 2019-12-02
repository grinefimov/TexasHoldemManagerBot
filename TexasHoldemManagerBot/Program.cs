using System;
using System.Linq;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TexasHoldemManagerBot
{
    internal class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient("1069876513:AAHCGIcCg0-SV8pqD18GX9yJLi7YRjC39JU");

        private static TexasHoldemGame Game { get; set; } = new TexasHoldemGame();

        private static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Console.WriteLine(
                $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );

            Bot.OnMessage += BotOnMessageReceived;
            Bot.StartReceiving();
            Thread.Sleep(int.MaxValue);
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.Text) return;

            var messageText = message.Text.Split(' ');
            switch (messageText[0])
            {
                case "Info":
                    var infoText = "";

                    foreach (var player in Game.Players)
                    {
                        infoText += $"{player.Name}: {player.Bankroll}\n";
                    }

                    await Bot.SendTextMessageAsync(
                        message.Chat.Id,
                        infoText != "" ? infoText : "No players");
                    break;
                case "Add":
                    if (messageText.Length <= 1) break;
                    switch (messageText[1])
                    {
                        case "player":
                            if (messageText.Length <= 2) break;

                            var name = messageText[2].Length > 12
                                    ? messageText[2].Substring(0, 12)
                                    : messageText[2];
                            if (Game.Players.FirstOrDefault(p => p.Name == name) != null)
                            {
                                await Bot.SendTextMessageAsync(
                                    message.Chat.Id,
                                    $"{name} already exist");
                                break;
                            }

                            Game.Players.Add(new TexasHoldemGame.Player(name));

                            await Bot.SendTextMessageAsync(
                                message.Chat.Id,
                                $"Player {name} added");
                            break;
                        case "money":
                            if (messageText.Length <= 3) break;

                            var tryAMoney = int.TryParse(messageText[3], out var moneyToAdd);
                            if (!tryAMoney) break;

                            switch (messageText[2])
                            {
                                case "all":
                                    foreach (var player in Game.Players)
                                    {
                                        if ((int)player.Bankroll + moneyToAdd > 0)
                                        {
                                            player.Bankroll += (uint)moneyToAdd;
                                        }
                                        else
                                        {
                                            player.Bankroll = 0;
                                        }
                                    }

                                    await Bot.SendTextMessageAsync(
                                        message.Chat.Id,
                                        $"Each player gets {moneyToAdd}");
                                    break;
                            }
                            break;
                    }
                    break;
                case "Remove":
                    if (messageText.Length <= 1) break;
                    switch (messageText[1])
                    {
                        case "player":
                            if (messageText.Length <= 1) break;

                            Game.Players.Remove(Game.Players.FirstOrDefault(p => p.Name == messageText[2]));

                            await Bot.SendTextMessageAsync(
                                message.Chat.Id,
                                $"Player {messageText[2]} removed");
                            break;
                    }
                    break;
                case "ThmbStart":
                    Game = new TexasHoldemGame();

                    var thmbStartRk = new ReplyKeyboardMarkup()
                    {
                        Keyboard = new[]
                        {
                            new[] { new KeyboardButton("Info") },
                            new[] { new KeyboardButton("Start") }
                        },
                        ResizeKeyboard = true
                    };

                    thmbStartRk.ResizeKeyboard = true;

                    await Bot.SendTextMessageAsync(
                        message.Chat.Id,
                        "New game created",
                        replyMarkup: thmbStartRk);
                    break;
                case "Help":
                    const string helpText = @"
Commands:
Info
Add player [name]
Remove player [name]
Add money [""all"" or name] [amount]";

                    await Bot.SendTextMessageAsync(
                        message.Chat.Id,
                        helpText);
                    break;
            }
        }
    }
}
