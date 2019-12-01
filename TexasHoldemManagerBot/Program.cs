using System;
using Telegram.Bot;

namespace TexasHoldemManagerBot
{
    internal class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient("1069876513:AAHCGIcCg0-SV8pqD18GX9yJLi7YRjC39JU");

        private static void Main(string[] args)
        {
            var me = Bot.GetMeAsync().Result;
            Console.WriteLine(
                $"Hello, World! I am user {me.Id} and my name is {me.FirstName}."
            );
        }
    }
}
