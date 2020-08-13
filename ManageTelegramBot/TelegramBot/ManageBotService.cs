using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using DAL.Models;
using ManageTelegramBot.AppConst;
using ManageTelegramBot.AppEnum;
using ManageTelegramBot.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Repository.InterFace;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using Telegram.Bot.Types.InputFiles;

namespace ManageTelegramBot.TelegramBot
{
    /// <summary>
    /// Connection with Telegram bot and manage it(get affiliate sell report)
    /// </summary>

    public class ManageBotService : IManageBotService
    {
        private readonly IBotService _botService;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _client;
        private readonly IConfiguration _configuration;

        private readonly IHttpContextAccessor _httpContext;

        public ManageBotService(IBotService botService,
            IWebHostEnvironment hostingEnvironment,
            IUnitOfWork uow,
            IConfiguration Configuration,
            IHttpClientFactory client,
            IHttpContextAccessor httpContext)
        {
            _botService = botService;
            _hostingEnvironment = hostingEnvironment;
            _uow = uow;
            _configuration = Configuration;
            _client = client;
            _httpContext = httpContext;
        }

        public async Task EchoAsync(Update up)
        {

            try
            {

                if (up.CallbackQuery != null)
                {
                    var userInfo = GetUserByChatId(up.CallbackQuery.Message.Chat.Id);

                    if (userInfo == null)
                    {
                        await LogOutUser(up);
                        return;
                    }

                    try
                    {
                        var affiliateSell = await GetAffiliateSellsApi(up.CallbackQuery.Data);
                        var inputOnlineFile = new InputOnlineFile(affiliateSell, up.CallbackQuery.Data + ".pdf");

                        var message = await _botService.Client.SendTextMessageAsync(up.CallbackQuery.Message.Chat.Id, "Sending file...");
                        await _botService.Client.SendDocumentAsync(up.CallbackQuery.Message.Chat.Id, inputOnlineFile);
                        await _botService.Client.DeleteMessageAsync(up.CallbackQuery.Message.Chat.Id, message.MessageId);

                    }
                    catch (Exception)
                    {
                        await InvalidLogin(up, "❌ Invalid Login.");
                        await LogOutUser(up);
                    }

                    return;
                }
                if (up.Message.Type == MessageType.Text)
                {
                    var userInfo = GetUserByChatId(up.Message.Chat.Id);
                    var lastState = _uow.UserActivitiesRepo.Get(d => d.Tb_UserInfo.ChatId == up.Message.Chat.Id && d.CreateDateTime > DateTime.Now.AddMinutes(-10), d => d.OrderByDescending(m => m.CreateDateTime)).FirstOrDefault();

                    if (up.Message.Text == "/start")
                    {
                        var user = InsertUserInfo(up.Message.Chat.Id);
                        await InsertUser(user, up);

                        await StartBot(up, "Hello {0} ✋, Please select your user type on system :");
                    }
                    else if (userInfo == null)
                    {
                        await LogOutUser(up);
                    }
                    else if (up.Message.Text == BotConst.adminText || up.Message.Text == BotConst.affiliateText)
                    {
                        await SelectUserType(up);
                    }
                    else if (up.Message.Text.Contains(FilterDate.All.ToString()))
                    {

                        await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                        await GetData(up, FilterDate.All);

                    }
                    else if (up.Message.Text.Contains(FilterDate.Monthly.ToString()))
                    {
                        await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                        await GetData(up, FilterDate.Monthly);
                    }
                    else if (up.Message.Text.Contains(FilterDate.Weekly.ToString()))
                    {
                        await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                        await GetData(up, FilterDate.Weekly);
                    }

                    else if (up.Message.Text == BotConst.back)
                    {
                        if (lastState.Message.Contains(BotConst.adminText) || lastState.Message == BotConst.affiliateText)
                            await StartBot(up, "Dear {0}, Please select your user type:");

                        else if (lastState.Message.Contains(BotConst.enterUserName))
                        {
                            var user = GetUserByChatId(up.Message.Chat.Id);
                            up.Message.Text = user.UserType;
                            await SelectUserType(up);
                        }
                    }
                    else if (up.Message.Text == BotConst.selectAffiliates)
                    {
                        List<InlineKeyboardButton> btnList = new List<InlineKeyboardButton>();

                        var affiliates = await GetAffiliatesApi();
                        foreach (var affiliate in affiliates)
                        {
                            btnList.Add(new InlineKeyboardButton() { Text = affiliate.FirstName, CallbackData = affiliate.Code });

                        }

                        var inlineBtns = GenerateReplyKeyboardMarkup(btnList);
                        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Affiliates List", ParseMode.Default, false, false, 0, inlineBtns);

                        var markupKeyboard = GenerateButton(3, BotConst.all, BotConst.monthly, BotConst.weekly, BotConst.selectAffiliates, BotConst.logOut);

                        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please select affiliate user 👆", replyMarkup: markupKeyboard);
                    }
                    else if (up.Message.Text == BotConst.logOut)
                    {
                        await LogOutUser(up);
                    }
                    else
                    {
                        if (lastState == null)
                        {
                            await LogOutUser(up);

                        }
                        else if (lastState.Message == BotConst.adminText || lastState.Message == BotConst.affiliateText)
                        {
                            await EnterUserName(up);
                        }
                        else if (lastState.Message == BotConst.enterUserName)
                        {

                            var user = GetUserByChatId(up.Message.Chat.Id);

                            user.Password = up.Message.Text;
                            _uow.UserInfoRepo.Update(user);
                            //await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(user.Id, BotConst.enterPassword));
                            await _uow.SaveAsync();
                            var markupKeyboad = GenerateBackButton();

                            if (!await CheckUserLogin(user))
                            {
                                await InvalidLogin(up, "❌ Invalid Login.");
                                up.Message.Text = user.UserType;
                                await SelectUserType(up);
                                return;
                            }

                            user.LoginState = true;
                            _uow.UserInfoRepo.Update(user);
                            await _uow.SaveAsync();

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "✅ Logged in successfully.");
                            if (user.UserType.Contains(BotConst.adminText))
                            {
                                var markupKeyboard = GenerateButton(3, BotConst.all, BotConst.monthly, BotConst.weekly, BotConst.selectAffiliates, BotConst.logOut);

                                await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, BotConst.selectFilterOption, replyMarkup: markupKeyboard);

                                return;
                            }
                            await GetData(up, FilterDate.None);
                        }
                        else
                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "🔍 Your message is unknown. Please enter /start and select your user type!");


                    }
                }
            }
            catch (Exception)
            {

            }

        }

        private async Task InsertUser(Tb_UserInfo user, Update up)
        {
            await _uow.UserInfoRepo.InsertAsync(user);
            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(user.Id, up.Message.Text));
            await _uow.SaveAsync();
        }

        /// <summary>
        /// Invalid login method
        /// </summary>
        /// <param name="up"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task InvalidLogin(Update up, string message)
        {
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message);
            // TODO: This has been added.
            //await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your username:", replyMarkup: new ForceReplyMarkup());
        }


        /// <summary>
        /// Get Affiliate report sell after authenticated
        /// </summary>
        /// <param name="userAuth"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task GetData(Update up, FilterDate filterDate)
        {
            List<AffiliateReportDto> list = new List<AffiliateReportDto>();
            //if sender pm is admin? can get all affiliate sell report else affiliate only get their report

            var userAuth = GetUserByChatId(up.Message.Chat.Id);
            if (userAuth == null)
            {
                await LogOutUser(up);
                return;
            }

            try
            {
                var affiliateReportFile = await GetAffiliateReportApi(filterDate.ToString(), userAuth.UserType, userAuth.UserName);
                var inputOnlineFile = new InputOnlineFile(affiliateReportFile, "AffiliateSellReport.pdf");

                var message = await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Sending file...");
                await _botService.Client.SendDocumentAsync(up.Message.Chat.Id, inputOnlineFile);
                await _botService.Client.DeleteMessageAsync(up.Message.Chat.Id, message.MessageId);

            }
            catch (Exception)
            {
                await InvalidLogin(up, "❌ Invalid Login.");
                await LogOutUser(up);
            }

        }

        private async Task<Stream> GetAffiliateReportApi(string filterDate, string userType, string email)
        {
            var url = _configuration["BaseApiUrl"];
            url += string.Format("/api/Affiliate/GetAffiliateUsers?filterDate={0}&userType={1}&email={2}", filterDate, userType, email);
            var api = _client.CreateClient();
            try
            {
                HttpResponseMessage messages = await api.GetAsync(url);

                if (messages.IsSuccessStatusCode)
                {
                    var contentResult = await messages.Content.ReadAsAsync<JsonResultContent<byte[]>>();
                    Stream stream = new MemoryStream(contentResult.Data);
                    return stream;
                }
                else
                    return null;

            }
            catch (Exception)
            {
                return null;
            }

        }

        private async Task LogOutUser(Update up)
        {
            var user = InsertUserInfo(up.Message.Chat.Id);
            await InsertUser(user, up);
            await StartBot(up, "Dear {0} You are logged out!, Please select your user type:");
        }
        private async Task<bool> CheckUserLogin(Tb_UserInfo userInfo)
        {
            var url = _configuration["BaseApiUrl"];
            url += string.Format("/api/User/UserLoginForBotManage?UserName={0}&UserType={1}&Password={2}", userInfo.UserName, userInfo.UserType, userInfo.Password);
            var api = _client.CreateClient();

            try
            {
                HttpResponseMessage messages = await api.GetAsync(url);

                if (messages.IsSuccessStatusCode)
                    return true;
                else
                    return false;

            }
            catch (Exception)
            {
                return false;
            }


        }
        private async Task<Stream> GetAffiliateSellsApi(string affiliateCode)
        {
            var url = _configuration["BaseApiUrl"];
            url += $"/api/Affiliate/GetAffiliateSells?affiliateCode={affiliateCode}";
            var api = _client.CreateClient();
            try
            {
                HttpResponseMessage messages = await api.GetAsync(url);

                if (messages.IsSuccessStatusCode)
                {
                    var result = await messages.Content.ReadAsAsync<JsonResultContent<byte[]>>();
                    Stream stream = new MemoryStream(result.Data);
                    return stream;
                }
                else
                    return null;

            }
            catch (Exception)
            {
                return null;
            }
        }
        private async Task<IEnumerable<AffiliateDto>> GetAffiliatesApi()
        {
            var url = _configuration["BaseApiUrl"];
            url += "/api/Affiliate/GetAffiliates";
            var api = _client.CreateClient();
            try
            {
                HttpResponseMessage messages = await api.GetAsync(url);

                if (messages.IsSuccessStatusCode)
                {
                    var result = await messages.Content.ReadAsAsync<JsonResultContent<IEnumerable<AffiliateDto>>>();
                    return result.Data;
                }
                else
                    return null;

            }
            catch (Exception)
            {
                return null;
            }
        }
        private async Task EnterUserName(Update up)
        {
            var user = GetUserByChatId(up.Message.Chat.Id);
            user.UserName = up.Message.Text;
            _uow.UserInfoRepo.Update(user);
            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(user.Id, BotConst.enterUserName));
            await _uow.SaveAsync();
            var markupKeyboad = GenerateBackButton();
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, BotConst.enterPassword, replyMarkup: markupKeyboad);

        }
        private async Task StartBot(Update up, string welcomeMsg)
        {
            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
            markupKeyboad.ResizeKeyboard = true;
            markupKeyboad.OneTimeKeyboard = true;
            var message = string.Format(welcomeMsg, up.Message.From.FirstName);
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);
        }
        private async Task SelectUserType(Update up)
        {
            var userInfo = GetUserByChatId(up.Message.Chat.Id);
            if (userInfo == null)
                await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, BotConst.logOut, replyToMessageId: up.Message.MessageId, replyMarkup: null);

            userInfo.UserType = up.Message.Text;
            _uow.UserInfoRepo.Update(userInfo);
            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
            await _uow.SaveAsync();

            var markupKeyboad = GenerateBackButton();
            //await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your username:", replyToMessageId: up.Message.MessageId, replyMarkup: new ForceReplyMarkup());
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your username:", replyToMessageId: up.Message.MessageId, replyMarkup: markupKeyboad);

        }


        private ReplyKeyboardMarkup GenerateButton(int ButtonsPerRow, params string[] Buttons)
        {
            List<List<KeyboardButton>> buttonsmap = new List<List<KeyboardButton>>()
            {
                new List<KeyboardButton>()
            };
            int buttonsaddtorow = 0;
            int rowindex = 0;
            foreach (var item in Buttons)
            {
                buttonsmap[rowindex].Add(new KeyboardButton(item));
                buttonsaddtorow++;
                if (buttonsaddtorow == ButtonsPerRow)
                {
                    rowindex++;
                    buttonsaddtorow = 0;
                    buttonsmap.Add(new List<KeyboardButton>());
                }
            }
            return new ReplyKeyboardMarkup
            {
                Keyboard = buttonsmap,
                ResizeKeyboard = true,
            };
        }
        private ReplyKeyboardMarkup GenerateBackButton()
        {
            var markupKeyboad = GenerateButton(1, BotConst.back);
            markupKeyboad.ResizeKeyboard = true;
            markupKeyboad.OneTimeKeyboard = true;
            return markupKeyboad;
        }
        private Tb_UserInfo GetUserLastActivity(long chatId)
        {
            return _uow.UserInfoRepo
                .Get(d => d.ChatId == chatId, null, "Tb_UserActivities")
                .FirstOrDefault();
        }
        private Tb_UserActivities InsertUserActivity(Guid userInfoId, string message)
        {
            var userActivity = new Tb_UserActivities()
            {
                UserInfoId = userInfoId,
                Message = message,
                Tb_UserInfo = null

            };
            return userActivity;
        }
        private Tb_UserInfo InsertUserInfo(long chatId)
        {
            var userInfo = new Tb_UserInfo()
            {
                ChatId = chatId,
                Tb_UserActivities = null
            };
            return userInfo;
        }
        private Tb_UserInfo GetUserByChatId(long chatId)
        {
            return _uow.UserInfoRepo.Get(d => d.ChatId == chatId && d.CreateDateTime > DateTime.Now.AddMinutes(-10), d => d.OrderByDescending(m => m.CreateDateTime)).FirstOrDefault();
        }
        private InlineKeyboardMarkup GenerateReplyKeyboardMarkup(List<InlineKeyboardButton> buttons)
        {
            int buttonperrow = 3;
            List<List<InlineKeyboardButton>> buttonsmap = new List<List<InlineKeyboardButton>>()
            {
                new List<InlineKeyboardButton>()
            };
            int buttonsaddtorow = 0;
            int rowindex = 0;
            foreach (var item in buttons)
            {
                buttonsmap[rowindex].Add(item);
                buttonsaddtorow++;
                if (buttonsaddtorow == buttonperrow)
                {
                    rowindex++;
                    buttonsaddtorow = 0;
                    buttonsmap.Add(new List<InlineKeyboardButton>());
                }
            }
            return new InlineKeyboardMarkup(buttonsmap);
        }
    }
}
