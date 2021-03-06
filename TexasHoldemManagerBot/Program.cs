using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NLog;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using LogLevel = NLog.LogLevel;

namespace TexasHoldemManagerBot
{
    internal class Program
    {
        private static IConfigurationRoot _configuration;
        private static Logger _logger;
        private static TelegramBotClient _bot;
        private static TexasHoldemGame Game { get; set; }
        private static bool _active;

        private static void Main()
        {
            var builder = new ConfigurationBuilder();
            builder.AddUserSecrets<Program>();
            _configuration = builder.Build();

            _logger = LogManager.GetCurrentClassLogger();

            _bot = new TelegramBotClient(_configuration["Strings:Token"]);
            _logger.Log(LogLevel.Info, "Start TexasHoldemManagerBot.");
            _bot.OnMessage += BotOnMessageReceived;
            try
            {
                _bot.StartReceiving();
                // Test start
                //_logger.Log(LogLevel.Debug, "Test start.");
                //Game = new TexasHoldemGame();
                //_active = true;
                //Bot.SendTextMessageAsync(468892911, "Game started");
                //Game.AddPlayer("Alex");
                //Game.AddChips(100);
                //Game.SetBlinds(5);
                //_logger.Log(LogLevel.Debug, "Test end.");
                // Test end
                while (true) Thread.Sleep(int.MaxValue);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            try
            {
                var message = messageEventArgs.Message;
                if (message == null || message.Type != MessageType.Text) return;
                if (message.Text.ToLower() == "thmbgstart")
                {
                    _active = true;
                    Game = new TexasHoldemGame();
                    await _bot.SendTextMessageAsync(message.Chat.Id, "Manager started");
                }
                if (!_active) return;
                var messageText = message.Text.Split(' ');
                var player = Game.Players.FirstOrDefault(p => p.Name == messageText[0]);
                if (player != null)
                {
                    if (messageText.Length == 1)
                    {
                        await _bot.SendTextMessageAsync(message.Chat.Id, $"{player.Name}'s commands",
                            replyMarkup: GetPlayerReplyKeyboardMarkup(player.Name),
                            replyToMessageId: message.MessageId);
                    }
                    else
                    {
                        switch (messageText[1].ToLower())
                        {
                            case "fold":
                                player.Fold();
                                await _bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} folds");
                                break;

                            case "call":
                                var chipsToCall = player.Call();
                                await _bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} calls {chipsToCall}, Bet: {player.TotalBet}, Bank: {player.Bankroll}, Pot: {Game.Pot}");
                                break;

                            case "bet":
                                if (messageText.Length < 3) break;
                                switch (messageText[2].ToLower())
                                {
                                    case "half":
                                        messageText[2] = (Game.Pot / 2).ToString();
                                        break;
                                    case "pot":
                                        messageText[2] = Game.Pot.ToString();
                                        break;
                                }

                                if (!int.TryParse(messageText[2], out var chipsToBet)) break;
                                if (player.Bet((uint) chipsToBet))
                                {
                                    var info = $", Bet: {player.TotalBet}, Bank: {player.Bankroll}, Pot: {Game.Pot}";
                                    if (player.Bankroll == 0)
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id,
                                            $"{player.Name} all-in {chipsToBet}{info}");
                                    }
                                    else
                                    {
                                        await _bot.SendTextMessageAsync(message.Chat.Id,
                                            $"{player.Name} bets {chipsToBet}{info}");
                                    }
                                }
                                break;

                            case "win":
                                uint chipsToWin;
                                if (messageText.Length == 2)
                                {
                                    chipsToWin = Game.Pot;
                                    player.Win(chipsToWin);
                                }
                                else
                                {
                                    if (!uint.TryParse(messageText[2], out chipsToWin) || !player.Win(chipsToWin))
                                        break;
                                }
                                await _bot.SendTextMessageAsync(message.Chat.Id, $"{player.Name} wins {chipsToWin}");
                                break;

                            case "all-in":
                                var chipsToAllIn = player.Bankroll;
                                player.Bet(chipsToAllIn);
                                await _bot.SendTextMessageAsync(message.Chat.Id,
                                    $"{player.Name} all-in {chipsToAllIn} ({player.TotalBet}) Total Pot: {Game.Pot}");
                                break;

                            case "add":
                                if (messageText.Length < 4 || messageText[2].ToLower() != "chips" ||
                                    !int.TryParse(messageText[3], out var chipsToAdd) || !player.AddChips(chipsToAdd))
                                    break;
                                await _bot.SendTextMessageAsync(message.Chat.Id, $"{player.Name} gets {chipsToAdd}");
                                break;
                        }
                    }
                }
                else
                {
                    switch (messageText[0].ToLower())
                    {
                        case "info":
                            await SendInfo(message);
                            break;
                        case "add":
                            if (messageText.Length < 2) break;
                            switch (messageText[1].ToLower())
                            {
                                case "player":
                                    if (messageText.Length < 3) break;
                                    var name = messageText[2].Length > 12
                                        ? messageText[2].Substring(0, 12)
                                        : messageText[2];
                                    if (Game.AddPlayer(name))
                                        await _bot.SendTextMessageAsync(message.Chat.Id, $"Player {name} added");
                                    else
                                        await _bot.SendTextMessageAsync(message.Chat.Id, $"{name} already exist");
                                    break;

                                case "chips":
                                    if (messageText.Length < 3) break;
                                    if (!int.TryParse(messageText[2], out var chipsToAdd)) break;
                                    Game.AddChips(chipsToAdd);
                                    await _bot.SendTextMessageAsync(message.Chat.Id, $"Each player gets {chipsToAdd}");
                                    break;
                            }
                            break;

                        case "remove":
                            if (messageText.Length < 2) break;
                            if (messageText[1].ToLower() == "player")
                            {
                                if (messageText.Length < 3) break;
                                if (Game.RemovePlayer(messageText[2]))
                                    await _bot.SendTextMessageAsync(message.Chat.Id, $"Player {messageText[2]} removed");
                            }
                            break;

                        case "set":
                            if (messageText.Length < 3 && messageText[1] != "blinds") break;
                            if (!uint.TryParse(messageText[2], out var blindToSet)) break;
                            Game.SetBlinds(blindToSet);
                            await _bot.SendTextMessageAsync(message.Chat.Id,
                                $"Small Blind: {Game.SmallBlind}, Big Blind: {Game.BigBlind}");
                            break;

                        case "restart":
                            Game.Restart();
                            await _bot.SendTextMessageAsync(message.Chat.Id, "New game started");
                            break;

                        case "thmbgstop":
                            _active = false;
                            await _bot.SendTextMessageAsync(message.Chat.Id, "Manager stopped");
                            break;

                        case "/help":
                            const string helpText = @"
List of commands:
Info
Restart
Add player [player]
Remove player [player]
Add chips [(-)amount]
Set blinds [amount]
[player] add chips [(-)amount]
[player]
[player] call
[player] bet [(-)amount]
[player] bet Half
[player] bet Pot
[player] all-in
[player] fold
[player] win
[player] win [amount]";
                            await _bot.SendTextMessageAsync(message.Chat.Id, helpText);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
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
                        infoText += $": {p.TotalBet} <---\n";
                        break;
                    case TexasHoldemGame.Player.PlayerState.Bet:
                        infoText += $": {p.TotalBet} <---\n";
                        break;
                    case TexasHoldemGame.Player.PlayerState.Fold:
                        infoText += $": {p.TotalBet}\n";
                        break;
                    default:
                        infoText += "\n";
                        break;
                }
            }
            await _bot.SendTextMessageAsync(message.Chat.Id, Game.Players.Count > 0 ? infoText : "No players");
        }

        private static ReplyKeyboardMarkup GetPlayerReplyKeyboardMarkup(string name)
        {
            return new ReplyKeyboardMarkup()
            {
                Keyboard = new[]
                {
                    new[]
                    {
                        new KeyboardButton($"{name} bet Pot"),
                        new KeyboardButton($"{name} bet {Game.BigBlind}")
                    },
                    new[]
                    {
                        new KeyboardButton($"{name} bet Half"),
                        new KeyboardButton($"{name} bet {Game.SmallBlind}")
                    },
                    new[] {new KeyboardButton($"{name} win"), new KeyboardButton($"{name} call")},
                    new[] {new KeyboardButton("Info"), new KeyboardButton($"{name} fold")}
                },
                ResizeKeyboard = true,
                Selective = true
            };
        }
    }
}