using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BotProcessing
{
    public class Bot
    {
        private TelegramBotClient _botClient;

        private ILogger<Bot> _logger;
        // Создаем словарь для отслеживания состояния пользователя
        private Dictionary<long, UserStates.UserState> _userState = new Dictionary<long, UserStates.UserState>();
        // Конструктор принимающий токен для доступа к API Telegram
        public Bot(string accessToken)
        {
            _botClient = new TelegramBotClient(accessToken);
            // Созадем файл логов, и выводим все логи в консоль
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddConsole().AddFile(options =>
                {
                    options.InternalLogFile = Path.Combine("bin", "logs", "bot.log"); // Путь к файлу логов
                }); 
            }).CreateLogger<Bot>();
        }

        private List<GasStation> data = new List<GasStation>();
        // Асинхронный метод для запуска бота.
        public async Task StartBotAsync(CancellationToken cancellationToken = default)
        {
            ReceiverOptions receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };

            _botClient.StartReceiving(updateHandler: HandleUpdateAsync, pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions, cancellationToken: cancellationToken);
            // Записывем в логи время старта бота
            var me = await _botClient.GetMeAsync(cancellationToken: cancellationToken);
            _logger.LogInformation("Бот запущен", DateTime.Now);
        }
        
        // Асинхронный метод для обработки ошибок при работе с запросами в Telegram
        public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };
                _logger.LogError($"{ErrorMessage}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}", DateTime.Now);
            }
        }

        // Асинхронный метод для обработки обновление от Telegram
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            try
            {
                var message = update.Message;
                _logger.LogInformation($"Получено сообщение {message}", DateTime.Now);
                var chatId = message.Chat.Id;
                Console.ForegroundColor = ConsoleColor.Cyan;
                if (!_userState.ContainsKey(chatId))
                {
                    _userState[chatId] = UserStates.UserState.Default;
                }
                // Если пользователь выбирал что хочет отправить CSV файл проверяем его ли он отправил
                if (message.Type == MessageType.Document && Path.GetExtension(message.Document.FileName) == ".csv")
                {
                    if (_userState[chatId] == UserStates.UserState.WaitForJson)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Передайте JSON файл!",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    else
                    {
                        _logger.LogInformation("Получен JSON файл", DateTime.Now);
                        await HandleCsvAsync(botClient, message, cancellationToken);
                        return;
                    }
                }
                // Если пользователь выбирал что хочет отправить CSV файл проверяем его ли он отправил
                if (message.Type == MessageType.Document && Path.GetExtension(message.Document.FileName) == ".json")
                {
                    if (_userState[chatId] == UserStates.UserState.WaitForCsv)
                    {
                        await botClient.SendTextMessageAsync(chatId, "Передайте CSV файл!",
                            cancellationToken: cancellationToken);
                        return;
                    }
                    else
                    {
                        _logger.LogInformation("Получен JSON файл", DateTime.Now);
                        await HandleJsonAsync(botClient, message, cancellationToken);
                        return;
                    }
                }
                // Если пользователь отправил файл некорректного разрешения информируем его об этом
                if (message.Type == MessageType.Document)
                {
                    _logger.LogInformation("Получен файл неверного формата", DateTime.Now);
                    await botClient.SendTextMessageAsync(chatId, "Вы передали файл неверного формата!",
                        cancellationToken: cancellationToken);
                    return;
                }
                // Обрабатываем все выборки. Если в качестве состояния пользователя установлена выборка то все сообщения считаем как поля для выборок
                switch (_userState[chatId])
                {
                    case UserStates.UserState.FilterByDistrict:
                        if (message.Text != "/start" && message.Text != "/help" && message.Text != "/menu")
                        {
                            var filtredDataByDistrict = data.Where(v => v.District.Contains(message.Text)).ToList();
                            if (filtredDataByDistrict.Count != 0)
                            {
                                data = filtredDataByDistrict;
                                _logger.LogInformation("Произведена фильтрация данных по полю District");
                                await botClient.SendTextMessageAsync(chatId,
                                    "Данные были отфильтрованы! Введите /menu для продолжения",
                                    cancellationToken: cancellationToken);
                                _userState[chatId] = UserStates.UserState.GotFile;
                                return;
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId,
                                    "Совпадений не нашлось. Введите новое значение",
                                    cancellationToken: cancellationToken);
                                return;
                            }
                        }

                        break;
                    case UserStates.UserState.FilterByOwner:
                        if (message.Text != "/start" && message.Text != "/help" && message.Text != "/menu")
                        {
                            var filtredDataByOwner = data.Where(v => v.Owner.Contains(message.Text)).ToList();
                            if (filtredDataByOwner.Count != 0)
                            {
                                data = filtredDataByOwner;
                                _logger.LogInformation("Произведена фильтрация данных по полю Owner");
                                await botClient.SendTextMessageAsync(chatId,
                                    "Данные были отфильтрованы! Введите /menu для продолжения",
                                    cancellationToken: cancellationToken);
                                _userState[chatId] = UserStates.UserState.GotFile;
                                return;
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId,
                                    "Совпадений не нашлось. Введите новое значение",
                                    cancellationToken: cancellationToken);
                                return;
                            }
                        }

                        break;
                    case UserStates.UserState.FilterByAdmAreaAndOwner:
                        if (message.Text != "/start" && message.Text != "/help" && message.Text != "/menu")
                        {
                            string[] arr = message.Text.Split(' ');
                            if (arr.Length == 2)
                            {
                                var filtredByAdmAreaAndOwner =
                                    data.Where(v => v.AdmArea.Contains(arr[0]) && v.Owner.Contains(arr[1])).ToList();
                                if (filtredByAdmAreaAndOwner.Count != 0)
                                {
                                    data = filtredByAdmAreaAndOwner;
                                    _logger.LogInformation("Произведена фильтрация данных по AdmArea и Owner");
                                    await botClient.SendTextMessageAsync(chatId,
                                        "Данные были отфильтрованы! Введите /menu для продолжения",
                                        cancellationToken: cancellationToken);
                                    _userState[chatId] = UserStates.UserState.GotFile;
                                    return;
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(chatId,
                                        "Совпадений не нашлось. Введите два значения для выборки через пробел. Первое для AdmArea второе для Owner.",
                                        cancellationToken: cancellationToken);
                                    return;
                                }
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(chatId,
                                    "Совпадений не нашлось. Введите два значения для выборки через пробел. Первое для AdmArea второе для Owner.",
                                    cancellationToken: cancellationToken);
                                return;
                            }
                        }

                        break;
                }
                // Обрабатываем все возможные сообщения пользователя. Там где это необходимо проверяем состояние чтобы пользователь вызывал только нужные команды в нужное время
                switch (message.Text)
                {
                    case "/start":
                        _logger.LogInformation("Получена команда /start");
                        await botClient.SendTextMessageAsync(chatId, $"Привет, {message.From.FirstName}!",
                            cancellationToken: cancellationToken);
                        _userState[chatId] = UserStates.UserState.Default;
                        await SelectSendingFileType(botClient, chatId, cancellationToken);
                        return;
                    case "Загрузить файл формата CSV":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        await AskForCsvFile(botClient,chatId, cancellationToken);
                        return;
                    case "Загрузить файл формата JSON":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        await AskForJsonFile(botClient,chatId, cancellationToken);
                        return;
                    case "Произвести фильтрацию (выборку) по полю District":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] == UserStates.UserState.FilterSelection)
                        {
                            await FilteringByDistrict(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Я вас не понимаю. Нажмите /menu и выберете что вы хотите сделать.",
                                cancellationToken: cancellationToken);
                        }
                        return;
                    case "Произвести фильтрацию (выборку) по полю Owner":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] == UserStates.UserState.FilterSelection)
                        {
                            await FilteringByOwner(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Я вас не понимаю. Нажмите /menu и выберете что вы хотите сделать.",
                                cancellationToken: cancellationToken);
                        }
                        return;
                    case "Произвести выборку по одному из полей":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] == UserStates.UserState.GotFile)
                        {
                            await FilteringSelection(botClient,chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не предоставили файл для работы",
                                cancellationToken: cancellationToken);
                            await SelectSendingFileType(botClient, chatId, cancellationToken);
                        }
                        return;
                    case "Произвести фильтрацию (выборку) по полям AdmArea и Owner":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] == UserStates.UserState.FilterSelection)
                        {
                            await FilteringByAdmAreaAndOwner(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Я вас не понимаю. Нажмите /menu и выберете что вы хотите сделать.",
                                cancellationToken: cancellationToken);
                        }
                        return;
                    case "Скачать обработанный файл в формате JSON":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] != UserStates.UserState.Default)
                        {
                            await ReturnJsonFileToUser(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не предоставили файл для работы",
                                cancellationToken: cancellationToken);
                            await SelectSendingFileType(botClient, chatId, cancellationToken);
                        }
                        return;
                    case "Скачать обработанный файл в формате CSV":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] != UserStates.UserState.Default)
                        {
                            await ReturnCsvFileToUser(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не предоставили файл для работы",
                                cancellationToken: cancellationToken);
                            await SelectSendingFileType(botClient, chatId, cancellationToken);
                        }
                        return;
                    case "Скачать обработанный файл в формате CSV или JSON":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] != UserStates.UserState.Default)
                        {
                            await AskForSavingParametr(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не предоставили файл для работы",
                                cancellationToken: cancellationToken);
                            await SelectSendingFileType(botClient, chatId, cancellationToken);
                        }
                        return;
                    case "Отсортировать по одному из полей":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (_userState[chatId] == UserStates.UserState.GotFile)
                        {
                            await AscForSortingField(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не предоставили файл для работы",
                                cancellationToken: cancellationToken);
                            await SelectSendingFileType(botClient, chatId, cancellationToken);
                        }

                        return;
                    case "Сортировка по полю TestDate по возрастанию даты":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (data.Count!=0 && (_userState[chatId] == UserStates.UserState.SortingSelection))
                        {
                            await SortingTestDateAscending(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не выбрали режим фильтарции в меню. Нажмите /menu и сделайте это если хотите произвести сортировку",
                                cancellationToken: cancellationToken);
                        }
                        
                        return;
                    case "Сортировка по полю TestDate по убыванию даты":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        if (data.Count!=0 && (_userState[chatId] == UserStates.UserState.SortingSelection))
                        {
                            await SortingTestDateDescending(botClient, chatId, cancellationToken);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(chatId, "Вы не выбрали режим фильтарции в меню. Нажмите /menu и сделайте это если хотите произвести сортировку",
                                cancellationToken: cancellationToken);
                        }
                        return;
                    case "/help":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        await botClient.SendTextMessageAsync(chatId, "Нажмите _/start_ для начала работы. Нажмите _/menu_"
                                                                     +" чтобы увидеть все функции \n***Важно!*** Для фильтрации и сортировок сначала нужно передать файл.",
                            parseMode:ParseMode.Markdown,cancellationToken: cancellationToken);
                        return;
                    case "/menu":
                        _logger.LogInformation($"Получена команда {message.Text}");
                        await SendStartKeyboard(botClient, chatId, cancellationToken);
                        return;
                    default:
                        _logger.LogInformation($"Получена команда которую не удалось обработать");
                        await botClient.SendTextMessageAsync(chatId, "Извините, я не понимаю ваш запрос. Нажмите /help чтобы узнать, что я могу.",
                            cancellationToken: cancellationToken);
                        return;
                }
            }
            catch (Exception e)
            {
                // Ошибки записываем в логи
                _logger.LogError($"{e.Message}", DateTime.Now);
            }
        }
        // Метод предлагает пользователю выбрать формат файла для скачивания
        public async Task AskForSavingParametr(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Скачать обработанный файл в формате JSON" },
                new KeyboardButton[] { "Скачать обработанный файл в формате CSV" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            _logger.LogInformation($"Пользователю отправлено меню выбора параметра скачивания файла");
            _userState[chatId] = UserStates.UserState.SortingSelection;
            await botClient.SendTextMessageAsync(chatId, "Выберите режим сохранения:", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);

        }
        // Метод запрашивает поле для фильтрации
        public async Task FilteringByDistrict(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(chatId, "Введите слово по которому будет производиться фильтрация:", cancellationToken: cancellationToken);
            _userState[chatId] = UserStates.UserState.FilterByDistrict;

        }
        // Метод запрашивает поле для фильтрации
        public async Task FilteringByOwner(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(chatId, "Введите слово по которому будет производиться фильтрация:", cancellationToken: cancellationToken);
            _userState[chatId] = UserStates.UserState.FilterByOwner;

        }
        // Метод запрашивает два поля для выборки
        public async Task FilteringByAdmAreaAndOwner(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(chatId, "Введите два значения для выборки через пробел. Первое для AdmArea второе для Owner:", cancellationToken: cancellationToken);
            _userState[chatId] = UserStates.UserState.FilterByAdmAreaAndOwner;

        }
        // Метод предоставляет выбор поля фильтрации
        public async Task FilteringSelection(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Произвести фильтрацию (выборку) по полю District" },
                new KeyboardButton[] { "Произвести фильтрацию (выборку) по полю Owner" },
                new KeyboardButton[] { "Произвести фильтрацию (выборку) по полям AdmArea и Owner" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            _logger.LogInformation($"Пользователю отправлено меню выбора параметра фильтрации");
            _userState[chatId] = UserStates.UserState.FilterSelection;
            await botClient.SendTextMessageAsync(chatId, "Выберите режим фильтрации:", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);

        }
        // Метод для сортировки по убыванию даты
        public async Task SortingTestDateDescending(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            List<GasStation> sortedData = data.OrderByDescending(date =>
            {
                // Парсинг в формат даты. Некорректные данные останутся внизу файла
                DateTime i;
                return DateTime.TryParse(date.TestDate, out i) ? i : DateTime.MinValue;
            }).ToList();
            data = sortedData;
            _logger.LogInformation($"Произведена сортировка по убыванию по TestDate");
            await botClient.SendTextMessageAsync(chatId,"Данные были успешно отсортированы! Нажмите /menu если хотите сделать что-то еще.", cancellationToken: cancellationToken);

        }
        // Метод для сортировки по возрастанию
        public async Task SortingTestDateAscending(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            List<GasStation> sortedData = data.OrderBy(date =>
            {
                // Парсинг в формат даты. все некорректные данные переместятся вниз файла
                DateTime i;
                return DateTime.TryParse(date.TestDate, out i) ? i : DateTime.MaxValue;
            }).ToList();
            data = sortedData;
            _logger.LogInformation($"Произведена сортировка по djphfcnfyb. по TestDate");
            await botClient.SendTextMessageAsync(chatId,"Данные были успешно отсортированы! Нажмите /menu если хотите сделать что-то еще.", cancellationToken: cancellationToken);

        }
        // Метод предоставляет пользователю выбор формата сортировки
        public async Task AscForSortingField(ITelegramBotClient botClient, long chatId,
            CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Сортировка по полю TestDate по возрастанию даты" },
                new KeyboardButton[] { "Сортировка по полю TestDate по убыванию даты" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            _logger.LogInformation($"Пользователю отправлено меню для выбора режима сортировки");
            _userState[chatId] = UserStates.UserState.SortingSelection;
            await botClient.SendTextMessageAsync(chatId, "Выберите режим сортировки:", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);

        }
        // Метод просит пользователя отправить файл
        public async Task AskForCsvFile(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(chatId, "Пожалуйста, отправьте мне CSV файл:", cancellationToken: cancellationToken);
            _userState[chatId] = UserStates.UserState.WaitForCsv;
        }
        // Метод отправляет сообщение с просьбой отправить файл
        public async Task AskForJsonFile(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            await botClient.SendTextMessageAsync(chatId, "Пожалуйста, отправьте мне JSON файл:", cancellationToken: cancellationToken);
            _userState[chatId] = UserStates.UserState.WaitForJson;
        }
        // Асинхронный метод предоставляет пользователю выбор формата файла для обработки
        public async Task SelectSendingFileType(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Загрузить файл формата CSV" },
                new KeyboardButton[] { "Загрузить файл формата JSON" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            _logger.LogInformation($"Пользователю отправлено меню для выбора формата файла для загрузки");
            await botClient.SendTextMessageAsync(chatId, "Выберите формат файла для обработки:", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
        }
        // Метод для считывания CSV файла
        public async Task HandleCsvAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            try
            {
                var chatId = message.Chat.Id;
                var file = await botClient.GetFileAsync(message.Document.FileId, cancellationToken);
                // Используем для считывания MemoryStream наследник Stream
                using (var stream = new MemoryStream())
                {
                    await botClient.DownloadFileAsync(file.FilePath, stream, cancellationToken);

                    stream.Position = 0; // Установить позицию потока в начало

                    // Теперь можно передать поток напрямую в методы обработки CSV файла
                    data = CSVProcessing.Read(stream);
                    if (data == null)
                    {
                        botClient.SendTextMessageAsync(chatId,"Этот файл не соответсвует нужному формату или вы передали пустой файл! Попробуйте другой файл", cancellationToken: cancellationToken);
                        // Информируем пользователя о неверном формате
                    }
                }
                // Если файл обработан успешно, можно установить соответствующее состояние пользователя и отправить клавиатуру для дальнейших действий
                if (data != null && data.Count != 0)
                {
                    _logger.LogInformation($"Файл от пользователя успешно загружен");
                    await botClient.SendTextMessageAsync(chatId, $"Файл {Path.GetFileName(message.Text)} успешно загружен.", cancellationToken: cancellationToken);
                    await SendStartKeyboard(botClient, chatId, cancellationToken);
                    _userState[chatId] = UserStates.UserState.GotFile;
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Некорректный формат файла. Попробуйте отправить еще один файл", cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обработке CSV-файла: {ex.Message}", DateTime.Now);
            }
        }
        // Метод для считывания JSON файла
        public async Task HandleJsonAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            try
            {
                var chatId = message.Chat.Id;
                var file = await botClient.GetFileAsync(message.Document.FileId, cancellationToken);
                
                using (var stream = new MemoryStream())
                {
                    await botClient.DownloadFileAsync(file.FilePath, stream, cancellationToken);

                    stream.Position = 0; // Установить позицию потока в начало

                    // Теперь можно передать поток напрямую в методы обработки JSON файла
                    data = JSONProcessing.Read(stream, botClient, chatId, cancellationToken: cancellationToken);
                }
                // Если файл обработан успешно, можно установить соответствующее состояние пользователя и отправить клавиатуру для дальнейших действий
                if (data != null && data.Count != 0)
                {
                    await botClient.SendTextMessageAsync(chatId, $"Файл {Path.GetFileName(message.Text)} успешно загружен.", cancellationToken: cancellationToken);
                    await SendStartKeyboard(botClient, chatId, cancellationToken);
                    _userState[chatId] = UserStates.UserState.GotFile;
                    _logger.LogInformation($"Файл от пользователя успешно загружен");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Некорректный формат файла. Попробуйте отправить еще один файл", cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при обработке JSON-файла: {ex.Message}", DateTime.Now);
            }
        }
        // Метод предоставляет выбор действий из меню
        public async Task SendStartKeyboard(ITelegramBotClient botClient,  long chatId,
            CancellationToken cancellationToken)
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton[] { "Загрузить файл формата CSV" , "Загрузить файл формата JSON" },
                new KeyboardButton[] { "Произвести выборку по одному из полей" ,"Отсортировать по одному из полей" },
                new KeyboardButton[] { "Скачать обработанный файл в формате CSV или JSON" }
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            _logger.LogInformation($"Пользователю отправлено меню для выбора формата файла для загрузки");
            _userState[chatId] = UserStates.UserState.GotFile;
            await botClient.SendTextMessageAsync(chatId, "Что вы хотите сделать?", replyMarkup: replyKeyboardMarkup, cancellationToken: cancellationToken);
        }
        // Метод отправляет пользователю обработанный файл в формате JSON
        public async Task ReturnJsonFileToUser(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            try
            {
                if (data.Count == 0)
                {
                    await botClient.SendTextMessageAsync(chatId, "Список объектов пуст. Нет данных для сохранения.", cancellationToken: cancellationToken);
                    return;
                }
                var stream = JSONProcessing.Write(data);
                string fileName = "processed_data.json";
                _logger.LogInformation($"Пользователю отправлен файл для скачивания");
                await botClient.SendDocumentAsync(chatId: chatId, document: InputFile.FromStream(stream: stream, fileName),
                    caption: "Обработанный файл", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при отправке JSON-файла: {ex.Message}", DateTime.Now);
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при отправке JSON-файла.", cancellationToken: cancellationToken);
            }
        }
        // Метод отправляет пользователю обработанный файл в формате CSV
        public async Task ReturnCsvFileToUser(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
        {
            try
            {
                if (data.Count == 0)
                {
                    await botClient.SendTextMessageAsync(chatId, "Список объектов пуст. Нет данных для сохранения.", cancellationToken: cancellationToken);
                    return;
                }
                var stream = CSVProcessing.Write(data);
                string fileName = "processed_data.csv";
                await botClient.SendDocumentAsync(chatId: chatId, document: InputFile.FromStream(stream: stream, fileName),
                    caption: "Обработанный файл", cancellationToken: cancellationToken);
                _logger.LogInformation($"Пользователю отправлен файл для скачивания");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка при отправке CSV-файла: {ex.Message}", DateTime.Now);
                await botClient.SendTextMessageAsync(chatId, "Произошла ошибка при скачивании CSV-файла.", cancellationToken: cancellationToken);
            }
        }
    }
}