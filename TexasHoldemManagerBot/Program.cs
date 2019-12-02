using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TexasHoldemManagerBot
{
    internal class Program
    {
        private static readonly TelegramBotClient Bot =
            new TelegramBotClient("1069876513:AAHCGIcCg0-SV8pqD18GX9yJLi7YRjC39JU");

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
            try
            {
                var message = messageEventArgs.Message;
                if (message == null || message.Type != MessageType.Text) return;
                var messageText = message.Text.Split(' ');
                for (var i = 1; i < messageText.Length; i++)
                {
                    messageText[i] = messageText[i].ToLower();
                }
                var player = Game.Players.FirstOrDefault(p => p.Name == messageText[0]);
                if (player != null)
                {
                    if (messageText.Length == 1)
                    {
                        await Bot.SendTextMessageAsync(message.Chat.Id, $"{player.Name}'s commands",
                            replyMarkup: GetPlayerReplyKeyboardMarkup(player.Name),
                            replyToMessageId: message.MessageId);
                    }
                    else
                    {
                        switch (messageText[1])
                        {
                            case "fold":
                                player.State = TexasHoldemGame.Player.PlayerState.Fold;
                                player.Fold();
                                await Bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} folds");
                                break;

                            case "call":
                                var maxBet = Game.Players.Select(p => p.TotalBet).Max();
                                var moneyToCall = maxBet - player.TotalBet;
                                player.Call(moneyToCall);
                                Game.Pot += moneyToCall;

                                await Bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} calls {moneyToCall}");
                                break;

                            case "bet":
                                if (messageText.Length < 3) break;
                                switch (messageText[2])
                                {
                                    case "Half":
                                        var moneyToBetHalf = Game.Pot / 2;
                                        player.Bet(moneyToBetHalf);
                                        Game.Pot += moneyToBetHalf;

                                        await Bot.SendTextMessageAsync(message.Chat.Id,
                                            $"{player.Name} bets {moneyToBetHalf}");
                                        break;
                                    case "Pot":
                                        var moneyToBetPot = Game.Pot;
                                        player.Bet(moneyToBetPot);
                                        Game.Pot += moneyToBetPot;

                                        await Bot.SendTextMessageAsync(message.Chat.Id,
                                            $"{player.Name} bets {moneyToBetPot}");
                                        break;
                                }
                                if (!int.TryParse(messageText[2], out var moneyToBet)) break;
                                if ((int)Game.Pot + moneyToBet >= 0 && player.Bankroll - moneyToBet >= 0)
                                {
                                    if ((int)player.TotalBet + moneyToBet >= 0)
                                    {
                                        player.Bet((uint)moneyToBet);
                                        Game.Pot += (uint)moneyToBet;
                                    }
                                    else
                                        player.TotalBet = 0;

                                    await Bot.SendTextMessageAsync(message.Chat.Id,
                                        $"{player.Name} bets {moneyToBet}");
                                }
                                break;

                            case "win":
                                uint moneyToWin;
                                if (messageText.Length > 2)
                                {
                                    if (!uint.TryParse(messageText[2], out moneyToWin) ||
                                        (int) Game.Pot - moneyToWin < 0)
                                        break;
                                    player.Bankroll += moneyToWin;
                                    Game.Pot -= moneyToWin;
                                }
                                else
                                {
                                    player.Bankroll += Game.Pot;
                                    moneyToWin = Game.Pot;
                                    Game.Pot = 0;
                                }
                                foreach (var p in Game.Players)
                                {
                                    p.Ready();
                                }
                                await Bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} wins {moneyToWin}");
                                break;

                            case "all-in":
                                var moneyToAllIn = player.Bankroll; 
                                player.Bet(moneyToAllIn);
                                Game.Pot += moneyToAllIn;

                                await Bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} all-in {moneyToAllIn}");
                                break;

                            case "add":
                                if (messageText.Length < 4 || messageText[2] != "money") break;
                                if (!int.TryParse(messageText[3], out var moneyToAdd)) break;
                                if ((int)player.Bankroll + moneyToAdd >= 0)
                                    player.Bankroll += (uint)moneyToAdd;
                                else
                                    player.Bankroll = 0;
                                await Bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} gets {moneyToAdd}");
                                break;
                        }
                    }
                }
                else
                {
                    messageText[0] = messageText[0].ToLower();
                    switch (messageText[0])
                    {
                        case "info":
                            await SendInfo(message);
                            break;
                        case "add":
                            if (messageText.Length < 2) break;
                            switch (messageText[1])
                            {
                                case "player":
                                    if (messageText.Length < 3) break;
                                    var name = messageText[2].Length > 12
                                            ? messageText[2].Substring(0, 12)
                                            : messageText[2];
                                    if (Game.Players.FirstOrDefault(p => p.Name == name) != null)
                                    {
                                        await Bot.SendTextMessageAsync(message.Chat.Id, $"{name} already exist");
                                        break;
                                    }
                                    Game.Players.Add(new TexasHoldemGame.Player(name));
                                    await Bot.SendTextMessageAsync(message.Chat.Id, $"Player {name} added");
                                    break;

                                case "money":
                                    if (messageText.Length < 3 && messageText[1] != "money") break;
                                    if (!int.TryParse(messageText[2], out var moneyToAdd)) break;
                                    foreach (var p in Game.Players)
                                    {
                                        if ((int)p.Bankroll + moneyToAdd >= 0)
                                            p.Bankroll += (uint)moneyToAdd;
                                        else
                                            p.Bankroll = 0;
                                    }
                                    await Bot.SendTextMessageAsync(message.Chat.Id,
                                        $"Each player gets {moneyToAdd}");
                                    break;
                            }
                            break;

                        case "remove":
                            if (messageText.Length < 2) break;
                            switch (messageText[1])
                            {
                                case "player":
                                    if (messageText.Length < 3) break;
                                    Game.Players.Remove(Game.Players.FirstOrDefault(p => p.Name == messageText[2]));
                                    await Bot.SendTextMessageAsync(message.Chat.Id, $"Player {messageText[2]} removed");
                                    break;
                            }
                            break;

                        case "set":
                            if (messageText.Length < 3 && messageText[1] != "blinds") break;
                            if (!uint.TryParse(messageText[2], out var blindToSet)) break;
                            Game.SetBlinds(blindToSet);
                            break;

                        case "restart":
                            Game.Restart();
                            await Bot.SendTextMessageAsync(message.Chat.Id, "New game started");
                            break;

                        case "/help":
                            const string helpText = @"
List of commands:
Info
Restart
Add player [player]
Remove player [player]
Add money [amount]
Set blinds [amount]
[player] add money [amount]
[player]
[player] call
[player] bet [amount]
[player] bet Half
[player] bet Pot
[player] all-in
[player] fold
[player] win
[player] win [amount]";
                            await Bot.SendTextMessageAsync(message.Chat.Id, helpText);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task SendInfo(Message message)
        {
            var infoText = $"Total Pot: {Game.Pot}\n";
            foreach (var p in Game.Players)
            {
                infoText += $"{p.Name}: {p.Bankroll} - {p.State}";
                switch (p.State)
                {
                    case TexasHoldemGame.Player.PlayerState.Call:
                        infoText += $": {p.TotalBet}\n";
                        break;
                    case TexasHoldemGame.Player.PlayerState.Bet:
                        infoText += $": {p.TotalBet}\n";
                        break;
                    case TexasHoldemGame.Player.PlayerState.Fold:
                        infoText += $": {p.TotalBet}\n";
                        break;
                    default:
                        infoText += "\n";
                        break;
                }
            }
            await Bot.SendTextMessageAsync(message.Chat.Id, Game.Players.Count > 0 ? infoText : "No players");
        }

        private static ReplyKeyboardMarkup GetPlayerReplyKeyboardMarkup(string name)
        {
            var rkm = new ReplyKeyboardMarkup()
            {
                Keyboard = new[]
                {
                    new[]
                    {
                        new KeyboardButton($"{name} bet {Game.BigBlind}"),
                        new KeyboardButton($"{name} bet {Game.SmallBlind}")
                    },
                    new[] {new KeyboardButton($"{name} bet Pot"), new KeyboardButton($"{name} bet Half")},
                    new[] {new KeyboardButton($"{name} win"), new KeyboardButton($"{name} call")},
                    new[] {new KeyboardButton("Info"), new KeyboardButton($"{name} fold")}
                },
                ResizeKeyboard = true,
                Selective = true
            };
            rkm.ResizeKeyboard = true;
            return rkm;
        }
    }
}
