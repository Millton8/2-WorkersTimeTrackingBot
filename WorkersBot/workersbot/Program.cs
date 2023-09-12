
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data;
using System.Configuration;
using Google.Apis.Sheets.v4;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;





namespace kpworkersbot
{

    class Program
    {
       
        private static List<string> projects = new List<string>();
        private static Dictionary<string,UserInfo> timeofWork = new Dictionary<string, UserInfo>();
        private static Dictionary<string, int> pricePerHour = new Dictionary<string, int>();
        private static List<string> userIdentify = new List<string>();
        //==========================SHEETS=============================

        private static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        private static readonly string SpreadsheetId = ConfigurationManager.AppSettings.Get("sheetsID");
        private const string GoogleCredentialsFileName = "jsconfig1.json";
        
        private static SpreadsheetsResource.ValuesResource serviceValues = GetSheetsService().Spreadsheets.Values;

        static ITelegramBotClient _botClient = new TelegramBotClient(ConfigurationManager.AppSettings.Get("ReleaseKey"));
        public static CancellationTokenSource cts = new CancellationTokenSource();


    public static async Task ListenForMessagesAsync()
        {
            

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>() 
            };
            

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await _botClient.GetMeAsync();

            Console.WriteLine($"Start listening for @{me.Username}");
            Console.ReadLine();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {

            var keyboardEnd =
                new KeyboardButton[][]
 {

                new KeyboardButton[]
            {
                new KeyboardButton("Закончить"),

            },

 };
            var keyboardBegin =
                new KeyboardButton[][]
{
            new KeyboardButton[]
            {
                new KeyboardButton("Начать"),

            },

};

            var rmEnd = new ReplyKeyboardMarkup(keyboardEnd);
            var rmBegin = new ReplyKeyboardMarkup(keyboardBegin);

            var rmtest = new ReplyKeyboardMarkup(keyboardBegin);

           
                if (update.Type == UpdateType.CallbackQuery)
                {
 
                var workerProject = update.CallbackQuery.Data;
                var callbackQuery = update.CallbackQuery;
                var messagecb = callbackQuery.Message;
                timeofWork[messagecb.Chat.Id.ToString()] = new UserInfo(update.CallbackQuery.Data.ToString());
 
                await botClient.SendTextMessageAsync(messagecb.Chat, $"Хорошей работы\n" +
                       $"Время начала: \n {DateTime.Now}", replyMarkup: rmEnd);

                }

            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;
                var messageText = message.Text;
                if (string.IsNullOrWhiteSpace(messageText)) 
                {
                    return;
                }


                    if (messageText == "/start")
                    {
                    try
                    {
                        
                        string[] userInfo = { message.Chat.Id.ToString(), message.Chat.FirstName.ToString(), "222" };
                        await WriteUserAsync(serviceValues, userInfo);
                        await botClient.SendTextMessageAsync(message.Chat, $"Привет, {message.Chat.FirstName}. По прибытию на объект нажмите кнопку Начать.\n" +
                            "Как будете уходить с работы жмите Закончить\n" +
                            "Также доступно меню слева от окна ввода", replyMarkup: rmBegin);
                    }
                    catch(Exception ex) 
                    {
                        await Console.Out.WriteLineAsync("Ошипка\n" + ex);
                    }
                }
           

                if (messageText.ToLower() == "быстро")
                {
                    try
                    {
                        var stringsOfSalary = await ReadSalaryAsync(serviceValues);
                        var generalList = await MakeGeneralReportAsync(stringsOfSalary);
                        string report = null;
                        var dataForFastReport = DateTime.Today.AddDays(-4).ToString("dd.MM.yy");

                        var listRez = await MakeDataReportAsync(generalList, dataForFastReport);
                        foreach (var item in listRez)
                        {
                            report += item.Id + "\n" + item.name + "\n" + $"{item.salary:0}" + " р." + "\n\n";
                        }
                        await botClient.SendTextMessageAsync(message.Chat, $"{report}", replyMarkup: null);
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync("Ошипка\n"+ex);
                    }

                }

                if (userIdentify.Contains(message.Chat.Id.ToString()))
                {
                    try
                    {
                        var dataForReport = messageText.ToLower();
                    bool check=await CheckStringDateAsync(dataForReport);
                    if (check)
                    {
                        var stringsOfSalary = await ReadSalaryAsync(serviceValues);
                        var generalList = await MakeGeneralReportAsync(stringsOfSalary);
                        string report = null;

                        var listRez = await MakeDataReportAsync(generalList, dataForReport);
                        
                        foreach (var item in listRez)
                        {
                            report += item.Id.ToString() + "\n" + item.name.ToString() + "\n" + item.salary.ToString() + " р." + "\n\n";
                        }
                        await botClient.SendTextMessageAsync(message.Chat, $"Отчет по дате {dataForReport}\n\n" +
                            $"{report}", replyMarkup: null);
                        userIdentify.Remove(message.Chat.Id.ToString());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Вы ввели {dataForReport}\n" +
                            $"Дата в неправильном формате. " +
                            $"Начните заново с ввода кодового слова", replyMarkup: null);
                        userIdentify.Remove(message.Chat.Id.ToString());
                    }
                    }
                    catch (Exception ex)
                    {
                        await Console.Out.WriteLineAsync("Ошипка\n" + ex);
                    }


                }
                if (messageText.ToLower() == "отчет")
                {
                    userIdentify.Add(message.Chat.Id.ToString());
                    
                    await botClient.SendTextMessageAsync(message.Chat, $"Введите дату в формате\n" 
                        + $"дд.мм.гг\n" + $"Например 01.01.23\n\n" + 
                        $"Если необходимо сделать поиск в диапозоне дат. Пишите две даты через пробел\n" + 
                        $"Например\n" + 
                        $"01.09.23 05.09.23", replyMarkup: null);

                }

                if (messageText.ToLower() == "начать" || messageText == "/begin")
                {
                    var value = new UserInfo();
                    if (timeofWork.TryGetValue(message.Chat.Id.ToString(), out value))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вы уже работаете на объекте. Сперва закончите работу", replyMarkup: rmEnd);
                        return;

                    }
                    await ReadProjectsAsync(serviceValues);
                   
                    var list = new List<List<InlineKeyboardButton>>();

                    for (int i = 0; i < projects.Count; i++)
                        list.Add(new List<InlineKeyboardButton>(projects.Skip(i).Take(1).Select(s => InlineKeyboardButton.WithCallbackData(s))));
                    var inline = new InlineKeyboardMarkup(list);

                    await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите объект", replyMarkup: new ReplyKeyboardRemove());
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Доступные вам :", replyMarkup: inline);

                }

                if (messageText.ToLower() == "закончить" || messageText=="/thend")
                {
                    await ReadPricePerHourAsync(serviceValues);
                    var value=new UserInfo();
                    if (timeofWork.TryGetValue(message.Chat.Id.ToString(), out value))
                    {
                        var tEnd = DateTime.Now;
                        var tBegin = value.tBegin;
                        var tRezult=tEnd - tBegin;

                        await botClient.SendTextMessageAsync(message.Chat, $"Молодец, {message.Chat.FirstName}. Время завершения работы\n {tEnd}\n\n" +
                                $"Всего вы отработали:\n {tRezult.Hours}:{tRezult.Minutes}:{tRezult.Seconds}\n" +
                                $"Зарплата:\n {tRezult.TotalHours * pricePerHour[message.Chat.Id.ToString()]:f} ₽" +
                                $"\n\nЕсли есть вопросы пишите директору", replyMarkup: rmBegin);
                        await WriteAsync(serviceValues, new WorkRezult(message.Chat.Id.ToString(), message.Chat.FirstName.ToString(), timeofWork[message.Chat.Id.ToString()], pricePerHour[message.Chat.Id.ToString()]));
                        await RemoveObject(message.Chat.Id.ToString());
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, $"Вы еще не начали работать. Нажмите кнопку Начать чтобы приступить к работе", replyMarkup: rmBegin);
                    }

                }

                

            }
        }

        private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        public static async Task Main()
        {       
            await ListenForMessagesAsync();
            Console.ReadLine();
        }

        private static SheetsService GetSheetsService()
        {
            using (var stream =
                new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read))
            {
                var serviceInitializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential.FromStream(stream).CreateScoped(Scopes)
                };
                return new SheetsService(serviceInitializer);
            }
        }

        private static async Task ReadProjectsAsync(SpreadsheetsResource.ValuesResource valuesResource)
        {
           
            var response = await valuesResource.Get(SpreadsheetId, "Проекты!A:A").ExecuteAsync();
            var values = response.Values;

            if (values == null || !values.Any())
            {
                Console.WriteLine("No data found.");
                projects.Add("Проект без названия");
                return;
            }
            projects.Clear();
            foreach (var row in values.Skip(1))
            {
                var res = string.Join(" ", row.Select(r => r.ToString()));

                projects.Add(res);
            }
        }
        private static async Task<string[]> ReadSalaryAsync(SpreadsheetsResource.ValuesResource valuesResource)
        {
            var response = await valuesResource.Get(SpreadsheetId, "A:H").ExecuteAsync();
            var values = response.Values;
            var listOfRows= new List<WorkerSalary>();
            var stringsForSalary=new string[response.Values.Count() + 1];
            int i=0;

            if (values == null || !values.Any())
            {
                Console.WriteLine("No data found.");
               
                return null;
            }
            
            foreach (var row in values.Skip(1))
            {
                stringsForSalary[i] = row[0].ToString() +" "+ row[1].ToString() + " " + row[3].ToString() + " " + row[7].ToString();
                i++;
            }

            return stringsForSalary;
            
        }
        private static async Task ReadPricePerHourAsync(SpreadsheetsResource.ValuesResource valuesResource)
        {
            var response = await valuesResource.Get(SpreadsheetId, "Зарплата!A:C").ExecuteAsync();
            var values = response.Values;
            int price;

            if (values == null || !values.Any())
            {
                Console.WriteLine("No data found.");
                projects.Add("Проект без названия");
                return;
            }
           
            foreach (var row in values.Skip(1))
            {
                if (int.TryParse(row[2].ToString(), out price))
                    
                    pricePerHour[row[0].ToString()] = price;
                else
                {
                    pricePerHour[row[0].ToString()] = 222;
                    Console.WriteLine("Не удалось преобразовать цену");
                }

            }
            
        }

        private static async Task WriteAsync(SpreadsheetsResource.ValuesResource valuesResource, WorkRezult workRez)
        {
            var response = await valuesResource.Get(SpreadsheetId, "A:H").ExecuteAsync();
            if (response.Values == null || !response.Values.Any())
            {
                Console.WriteLine("No data found.");
                return;
            }

            int wr = response.Values.Count()+1;
            var tBegin = workRez.beginAndProject.tBegin;
            var tEnd = workRez.tEnd;
            var timeofWork = tEnd - tBegin;


            var valueRange = new ValueRange { Values = new List<IList<object>> { new List<object> { workRez.ID, workRez.name, workRez.beginAndProject.project, $"{tBegin:dd.MM.yy HH:mm}", $"{tEnd:dd.MM.yy HH:mm}", timeofWork.TotalHours,workRez.pricePerHour,$"{workRez.salary:f1}" } } };
            

            var update = valuesResource.Update(valueRange, SpreadsheetId, $"A{wr}:H{wr}");
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            await update.ExecuteAsync();

        }

        private static async Task WriteUserAsync(SpreadsheetsResource.ValuesResource valuesResource, string[] userInfo)
        {
            var response = await valuesResource.Get(SpreadsheetId, "Зарплата!A:F").ExecuteAsync();
            if (response.Values == null || !response.Values.Any())
            {
                Console.WriteLine("No data found.");
                return;
            }

            int wr = response.Values.Count() + 1;

            var valueRange = new ValueRange { Values = new List<IList<object>> { new List<object> { userInfo[0], userInfo[1], userInfo[2] } } };

            var update = valuesResource.Update(valueRange, SpreadsheetId, $"Зарплата!A{wr}:F{wr}");
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            await update.ExecuteAsync();

        }

        private static async Task RemoveObject(string userID)
        {
            timeofWork.Remove(userID);
        }
        private static async Task<List<WorkerSalary>> MakeGeneralReportAsync(string[] stringsOfSalary)
        {

           var generalList=new List<WorkerSalary>();

            foreach (var rows in stringsOfSalary)
            {
                if (rows == null)
                    continue;
                
                string[] info = rows.Split(" ");
                generalList.Add(new WorkerSalary { Id = info[0], name = info[1], date = Convert.ToDateTime(info[2]), salary = Convert.ToSingle(info[4])});
            
            }

            return generalList;

        }


        private static async Task<List<WorkerSalary>> MakeDataReportAsync(List<WorkerSalary> listSalary,string dateString)
        {
            var listID = new List<string>();
            string[] twoDatesString = new string[2];
            bool isTwoDate = false;
            
            DateTime[] date =new DateTime[2];

            foreach (var c in dateString)
            {
                if (c == ' ')
                    isTwoDate = true;
            }
            if (isTwoDate)
            {
                twoDatesString = dateString.Split(' ');
                date[0] = Convert.ToDateTime(twoDatesString[0]);
                date[1] = Convert.ToDateTime(twoDatesString[1]);
                var filteredList = listSalary.Where(x => (x.date >= date[0] && x.date < date[1])).ToList();
                listSalary = filteredList;
            }
            else
            {
                date[0] = Convert.ToDateTime(dateString);
                var filteredList = listSalary.Where(x => x.date >= date[0]).ToList();
                listSalary = filteredList;

            }


            listID.Add(listSalary[0].Id);
            for (int i = 1; i < listSalary.Count; i++)
            {
                if (listID.Contains(listSalary[i].Id))
                {
                    listSalary[listSalary.FindIndex(a => a.Id == listSalary[i].Id)].salary += listSalary[i].salary;
                    listSalary.RemoveAt(i);
                    i--;
                }

                listID.Add(listSalary[i].Id);

            }
            return listSalary;

        }

        private static async Task<bool> CheckStringDateAsync(string dateString)
        {
            
            string[] twoDatesString = new string[2];
            bool isTwoDate = false;
            

            foreach (var c in dateString)
            {
                if (c == ' ')
                    isTwoDate = true;
            }
            if (isTwoDate)
            {
                twoDatesString = dateString.Split(' ');
                try
                {
                    Convert.ToDateTime(twoDatesString[0]);
                    Convert.ToDateTime(twoDatesString[1]);

                    
                    return true;
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync("Ошибка формата даты\n"+ ex);
                    return false;
                }
            }
            else
            {
                try
                {
                    Convert.ToDateTime(dateString);
                    return true;
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync("Ошибка формата даты\n" + ex);
                    return false;
                }
                              

            }


        }


    }
}






