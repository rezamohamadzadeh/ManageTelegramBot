using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DAL.Models;
using ManageTelegramBot.AppConst;
using ManageTelegramBot.AppEnum;
using ManageTelegramBot.Models;
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
    /// Connection with Telegram bot and manage it(get affiliate reports)
    /// </summary>

    public class ManageBotService : IManageBotService
    {
        private readonly IBotService _botService;
        private readonly IUnitOfWork _uow;
        private readonly IHttpClientFactory _client;
        private readonly IConfiguration _configuration;

        public ManageBotService(IBotService botService, IUnitOfWork uow, IConfiguration Configuration, IHttpClientFactory client)
        {
            _botService = botService;
            _uow = uow;
            _configuration = Configuration;
            _client = client;
        }

        public async Task EchoAsync(Update up)
        {
            try
            {
                #region Check Callback Query
                // if query has call back with inline keyboard
                if (up.CallbackQuery != null)
                {
                    await _botService.Client.SendChatActionAsync(up.CallbackQuery.Message.Chat.Id, ChatAction.Typing);


                    //var userInfo = _uow.UserInfoRepo.Get(d => d.ChatId == up.CallbackQuery.Message.Chat.Id && d.CreateDateTime > DateTime.Now.AddMinutes(-10), d => d.OrderByDescending(m => m.CreateDateTime)).FirstOrDefault();
                    var userInfo = _uow.UserInfoRepo.Get(d => d.ChatId == up.CallbackQuery.Message.Chat.Id, d => d.OrderByDescending(m => m.CreateDateTime)).FirstOrDefault();

                    //if user is null or login state is false
                    if (userInfo == null || !userInfo.LoginState)
                    {
                        var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
                        markupKeyboad.ResizeKeyboard = true;
                        markupKeyboad.OneTimeKeyboard = true;

                        string message = "You are logged out!, Please select your user type:";

                        await _botService.Client.SendTextMessageAsync(up.CallbackQuery.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                        return;
                    }
                    // if userType != admin 
                    if (!userInfo.UserType.Contains(BotConst.adminText))
                    {
                        var markupKeyboad = GenerateButton(0, BotConst.logOut);
                        markupKeyboad.ResizeKeyboard = true;
                        markupKeyboad.OneTimeKeyboard = true;

                        string message = "You do not have access to this section";

                        await _botService.Client.SendTextMessageAsync(up.CallbackQuery.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                        return;
                    }
                    try
                    {
                        //get files from api after click on inline keyabordbtn

                        var affiliateSell = await GetAffiliateSellsApi(up.CallbackQuery.Data);
                        var inputOnlineFile = new InputOnlineFile(affiliateSell, up.CallbackQuery.Data + ".pdf");

                        var message = await _botService.Client.SendTextMessageAsync(up.CallbackQuery.Message.Chat.Id, "Sending file...");
                        await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, "Report " + up.CallbackQuery.Data + " AffiliateCode"));
                        await _uow.SaveAsync();
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
                #endregion

                if (up.Message.Type == MessageType.Text)
                {
                    var userInfo = GetUserByChatId(up.Message.Chat.Id);
                    var lastState = _uow.UserActivitiesRepo.Get(d => d.Tb_UserInfo.ChatId == up.Message.Chat.Id && d.CreateDateTime > DateTime.Now.AddMinutes(-10), d => d.OrderByDescending(m => m.CreateDateTime)).FirstOrDefault();
                    await _botService.Client.SendChatActionAsync(up.Message.Chat.Id, ChatAction.Typing);

                    #region Start
                    if (up.Message.Text == "/start")
                    {
                        var user = InsertUserInfo(up.Message.Chat.Id);
                        await InsertUser(user, up);
                        await StartBot(up, "Hello {0} ✋, Please select your user type on system :");

                        return;
                    }
                    #endregion

                    #region Select UserType
                    if (up.Message.Text == BotConst.adminText || up.Message.Text == BotConst.affiliateText)
                    {
                        await SelectUserType(up);

                        return;
                    }
                    #endregion

                    #region CheckUserExpire in ActivityUser Table
                    if (lastState == null)
                    {
                        await LogOutUser(up);

                        return;

                    }
                    #endregion

                    #region CheckUserInfo
                    if (userInfo == null)
                    {
                        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, BotConst.unknownMessage);

                        return;
                    }
                    #endregion

                    #region Select All FilterDate SellReport
                    if (up.Message.Text.Contains(FilterDate.All.ToString()))
                    {
                        if (!userInfo.LoginState)
                        {
                            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You are logged out!, Please select your user type:";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else if (!userInfo.UserType.Contains(BotConst.adminText))
                        {
                            var markupKeyboad = GenerateButton(0, BotConst.logOut);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You do not have access to this section";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else
                        {
                            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                            await _uow.SaveAsync();
                            await GetData(up, FilterDate.All);

                            return;
                        }

                    }
                    #endregion

                    #region Select Monthly FilterDate SellReport
                    if (up.Message.Text.Contains(FilterDate.Monthly.ToString()))
                    {
                        if (!userInfo.LoginState)
                        {
                            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You are logged out!, Please select your user type:";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else if (!userInfo.UserType.Contains(BotConst.adminText))
                        {
                            var markupKeyboad = GenerateButton(0, BotConst.logOut);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You do not have access to this section";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else
                        {
                            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                            await _uow.SaveAsync();
                            await GetData(up, FilterDate.Monthly);

                            return;
                        }

                    }
                    #endregion

                    #region Select Weekly FilterDate SellReport
                    if (up.Message.Text.Contains(FilterDate.Weekly.ToString()))
                    {
                        if (!userInfo.LoginState)
                        {
                            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You are logged out!, Please select your user type:";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else if (!userInfo.UserType.Contains(BotConst.adminText))
                        {
                            var markupKeyboad = GenerateButton(0, BotConst.logOut);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You do not have access to this section";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else
                        {
                            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                            await _uow.SaveAsync();
                            await GetData(up, FilterDate.Weekly);

                            return;
                        }

                    }
                    #endregion

                    #region Back Btn Clickled
                    if (up.Message.Text == BotConst.back)
                    {

                        if (lastState.Message.Contains(BotConst.adminText) || lastState.Message == BotConst.affiliateText)
                            await StartBot(up, "Dear {0}, Please select your user type:");

                        else if (lastState.Message.Contains(BotConst.enterUserName))
                        {
                            var user = GetUserByChatId(up.Message.Chat.Id);
                            up.Message.Text = user.UserType;
                            await SelectUserType(up);
                        }

                        return;
                    }
                    #endregion

                    #region Get Affiliate List with inlineKeyBtn
                    if (up.Message.Text == BotConst.selectAffiliates)
                    {
                        if (!userInfo.LoginState)
                        {
                            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You are logged out!, Please select your user type:";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else if (!userInfo.UserType.Contains(BotConst.adminText))
                        {
                            var markupKeyboad = GenerateButton(0, BotConst.logOut);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You do not have access to this section";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else
                        {
                            List<InlineKeyboardButton> btnList = new List<InlineKeyboardButton>();

                            var affiliates = await GetAffiliatesApi();
                            foreach (var affiliate in affiliates)
                            {
                                btnList.Add(new InlineKeyboardButton() { Text = affiliate.FirstName, CallbackData = affiliate.Code });

                            }

                            var inlineBtns = GenerateReplyKeyboardMarkup(btnList);
                            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                            await _uow.SaveAsync();
                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Affiliates List", ParseMode.Default, false, false, 0, inlineBtns);

                            var markupKeyboard = GenerateButton(3, BotConst.all, BotConst.monthly, BotConst.weekly, BotConst.selectAffiliates, BotConst.TopAffiliatesSell, BotConst.logOut);

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please select affiliate user 👆", replyMarkup: markupKeyboard);

                            return;
                        }

                    }
                    #endregion

                    #region Get Top Affiliates Sell 
                    if (up.Message.Text == BotConst.TopAffiliatesSell)
                    {
                        if (!userInfo.LoginState)
                        {
                            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You are logged out!, Please select your user type:";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else if (!userInfo.UserType.Contains(BotConst.adminText))
                        {
                            var markupKeyboad = GenerateButton(0, BotConst.logOut);
                            markupKeyboad.ResizeKeyboard = true;
                            markupKeyboad.OneTimeKeyboard = true;

                            string message = "You do not have access to this section";

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);

                            return;
                        }
                        else
                        {
                            await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(userInfo.Id, up.Message.Text));
                            await _uow.SaveAsync();
                            var topSells = await GetTopAffiliatesSellApi();

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, topSells);

                            var markupKeyboard = GenerateButton(3, BotConst.all, BotConst.monthly, BotConst.weekly, BotConst.selectAffiliates, BotConst.TopAffiliatesSell, BotConst.logOut);

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Report of top affiliates sell 👆", replyMarkup: markupKeyboard);

                            return;
                        }

                    }
                    #endregion
                    #region Logout
                    if (up.Message.Text == BotConst.logOut)
                    {
                        await LogOutUser(up);

                        return;
                    }
                    #endregion

                    #region Login

                    if (lastState.Message == BotConst.adminText || lastState.Message == BotConst.affiliateText)
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

                        if (!await CheckUserLoginApi(user))
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
                            var makupKeyboard = GenerateButton(3, BotConst.all, BotConst.monthly, BotConst.weekly, BotConst.selectAffiliates, BotConst.TopAffiliatesSell, BotConst.logOut);

                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, BotConst.selectFilterOption, replyMarkup: makupKeyboard);

                            return;
                        }
                        var markupKeyboard = GenerateButton(0, BotConst.logOut);

                        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Download Your Document", replyMarkup: markupKeyboard);

                        await GetData(up, FilterDate.All);

                        return;
                    }
                    else
                        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, BotConst.unknownMessage);

                    #endregion

                }
            }
            catch (Exception)
            {
                await LogOutUser(up);
            }

        }

        /// <summary>
        ///  Mapping Inser user properties
        /// </summary>
        /// <param name="user"></param>
        /// <param name="up"></param>
        /// <returns></returns>
        private async Task InsertUser(Tb_UserInfo user, Update up)
        {

            await _uow.UserInfoRepo.InsertAsync(user);
            if (up.CallbackQuery != null)
                await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(user.Id, up.CallbackQuery.Message.Text));
            else
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
            if (up.CallbackQuery != null)
                await _botService.Client.SendTextMessageAsync(up.CallbackQuery.Message.Chat.Id, message);
            else
                await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message);
        }


        /// <summary>
        /// Get Affiliate report sell after successfully authenticated
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
            catch (Exception ex)
            {
                await InvalidLogin(up, "❌ Invalid Login. " + ex.Message);
                await LogOutUser(up);
            }

        }
        /// <summary>
        /// Get top affiliates sell report from api
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetTopAffiliatesSellApi()
        {
            var url = _configuration["BaseApiUrl"];
            url += string.Format("/api/Affiliate/GetTopAffiliateSells");
            var api = _client.CreateClient();
            try
            {
                HttpResponseMessage messages = await api.GetAsync(url);

                if (messages.IsSuccessStatusCode)
                {
                    var contentResult = await messages.Content.ReadAsAsync<JsonResultContent<List<TopAffiliatesSellDto>>>();
                    String result = "";
                    int count = 1;
                    foreach (var item in contentResult.Data)
                    {
                        var sells = item.Count <= 1 ? " sell " : " sells ";
                        result += count + ". " + item.Name + " with " + item.Count + sells + "\n\n";
                        count++;
                    }
                    return result;
                }
                else
                    return null;

            }
            catch (Exception)
            {
                return null;
            }

        }

        /// <summary>
        /// Generate AffiliateSellReport Pdf File From Api
        /// </summary>
        /// <param name="filterDate"></param>
        /// <param name="userType"></param>
        /// <param name="email"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Log out user
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
        private async Task LogOutUser(Update up)
        {
            Tb_UserInfo user;
            if (up.CallbackQuery != null)
                user = InsertUserInfo(up.CallbackQuery.Message.Chat.Id);
            else
                user = InsertUserInfo(up.Message.Chat.Id);

            await InsertUser(user, up);
            await StartBot(up, "Dear {0} You are logged out!, Please select your user type:");
        }

        /// <summary>
        /// Check User Login with Api
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        private async Task<bool> CheckUserLoginApi(Tb_UserInfo userInfo)
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

        /// <summary>
        /// Get Affiliate Pdf SellReport After select inlineKeyBtn 
        /// </summary>
        /// <param name="affiliateCode"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get All Affiliates user Api
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Run enter user command after select usertype
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Run start bot command
        /// </summary>
        /// <param name="up"></param>
        /// <param name="welcomeMsg"></param>
        /// <returns></returns>
        private async Task StartBot(Update up, string welcomeMsg)
        {
            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
            markupKeyboad.ResizeKeyboard = true;
            markupKeyboad.OneTimeKeyboard = true;

            string message = up.CallbackQuery != null ? "You are logged out!, Please select your user type:" : string.Format(welcomeMsg, up.Message.From.FirstName);

            if (up.CallbackQuery != null)
                await _botService.Client.SendTextMessageAsync(up.CallbackQuery.Message.Chat.Id, message, replyMarkup: markupKeyboad);
            else
                await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);
        }

        /// <summary>
        /// Run Select user command after start bot
        /// </summary>
        /// <param name="up"></param>
        /// <returns></returns>
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


        /// <summary>
        /// Generate dynamic keyboard btn
        /// </summary>
        /// <param name="ButtonsPerRow"></param>
        /// <param name="Buttons"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Generate dynamic back btn
        /// </summary>
        /// <returns></returns>
        private ReplyKeyboardMarkup GenerateBackButton()
        {
            var markupKeyboad = GenerateButton(1, BotConst.back);
            markupKeyboad.ResizeKeyboard = true;
            markupKeyboad.OneTimeKeyboard = true;
            return markupKeyboad;
        }

        /// <summary>
        /// Mapping useractivity properties
        /// </summary>
        /// <param name="userInfoId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
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

        /// <summary>
        /// After mapping, this method call for insert new value in db
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        private Tb_UserInfo InsertUserInfo(long chatId)
        {
            var userInfo = new Tb_UserInfo()
            {
                ChatId = chatId,
                Tb_UserActivities = null
            };
            return userInfo;
        }

        /// <summary>
        /// Get user by chatId
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        private Tb_UserInfo GetUserByChatId(long chatId)
        {
            return _uow.UserInfoRepo.Get(d => d.ChatId == chatId, d => d.OrderByDescending(m => m.CreateDateTime)).FirstOrDefault();
        }

        /// <summary>
        /// Generate dynamic InlineBtns for show affiliate users
        /// </summary>
        /// <param name="buttons"></param>
        /// <returns></returns>
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
