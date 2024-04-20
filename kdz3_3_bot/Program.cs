using BotProcessing;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
internal class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Созадем экземпляр бота и передаем туда токен
            Bot bot = new  Bot("6577036865:AAGKqQuDEPkW91ZKLiTJleSfkO6gSzWB-fM");
            await bot.StartBotAsync();
            await Task.Delay(-1);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

