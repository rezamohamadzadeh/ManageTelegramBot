using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DAL.Models;
using ManageTelegramBot.AppConst;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Repository.InterFace;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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


        public ManageBotService(IBotService botService,
            IWebHostEnvironment hostingEnvironment,
            IUnitOfWork uow,
            IConfiguration Configuration,
            IHttpClientFactory client)
        {
            _botService = botService;
            _hostingEnvironment = hostingEnvironment;
            _uow = uow;
            _configuration = Configuration;
            _client = client;
        }

        public async Task EchoAsync(Update up)
        {


            if (up.Message.From.IsBot)
            {
                return;
            }

            if (up.Message.Type == MessageType.Text)
            {
                if (up.Message.Text == "/start" || up.Message.Text == BotConst.logOut)
                {
                    await _uow.UserInfoRepo.InsertAsync(InsertUserInfo(up.Message.Chat.Id));
                    await StartBot(up);
                }
                else if (up.Message.Text == BotConst.back)
                {
                    await StartBot(up);

                }
                else if (up.Message.Text == BotConst.adminText || up.Message.Text == BotConst.affiliateText)
                {
                    await SelectUserType(up);
                }
                //else if (up.Message.Text == FilterByDate.All.ToString() || up.Message.Text == FilterByDate.Monthly.ToString() || up.Message.Text == FilterByDate.Weekly.ToString())
                //{
                //    if (_credentials.TryGetValue(up.Message.Chat.Id, out Tb_UserActivities userAuth))
                //    {
                //        userAuth.FilterByDate = (FilterByDate)Enum.Parse(typeof(FilterByDate), up.Message.Text);
                //        await GetData(userAuth, up);
                //    }

                //}
                else if (up.Message.ReplyToMessage != null && up.Message.ReplyToMessage.Text == "Please enter your username:")
                {
                    var user = GetUserByChatId(up.Message.Chat.Id);
                    user.UserName = up.Message.Text;
                    _uow.UserInfoRepo.Update(user);
                    await _uow.UserActivitiesRepo.InsertAsync(InsertUserActivity(user.Id, up.Message.ReplyToMessage.Text));
                    await _uow.SaveAsync();
                    var markupKeyboad = GenerateBackButton();
                    await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your password:", replyToMessageId: up.Message.MessageId, replyMarkup: markupKeyboad);                    
                }
                else if (up.Message.ReplyToMessage != null && up.Message.ReplyToMessage.Text == "Please enter your password:")
                {

                    //// TODO: This has been changed.
                    //if (_credentials.TryGetValue(up.Message.Chat.Id, out Tb_UserActivities userAuth))
                    //{
                    //    userAuth.Password = up.Message.Text;

                    //    // TODO: Check form Database

                    //    var result = await _signInManager.PasswordSignInAsync(userAuth.Username, userAuth.Password, false, lockoutOnFailure: false);
                    //    if (result.Succeeded)
                    //    {
                    //        var user = _uowApp.UserRepo.GetUserByName(userAuth.Username);

                    //        var roles = await _userManager.GetRolesAsync(user);
                    //        bool hasAccess = false;
                    //        foreach (var role in roles)
                    //        {
                    //            if (userAuth.UserType.Contains(role))
                    //            {
                    //                hasAccess = true;
                    //                break;
                    //            }
                    //        }
                    //        if (!hasAccess)
                    //        {
                    //            await InvalidLogin(up, "❌ Invalid Login.");
                    //            return;
                    //        }
                    //        if (!user.IsActive)
                    //        {
                    //            await InvalidLogin(up, "❌ User is not active.");
                    //            return;
                    //        }
                    //        // if login has successfully 
                    //        userAuth.Email = user.Email;

                    //        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "✅ Logged in successfully.");
                    //        if (userAuth.UserType.Contains(adminText))
                    //        {
                    //            var markupKeyboad = GenerateButton(3, all, monthly, weekly, logOut);

                    //            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please select your option :", replyMarkup: markupKeyboad);

                    //            return;
                    //        }
                    //        await GetData(userAuth, up);
                    //    }
                    //    else
                    //    {
                    //        await InvalidLogin(up, "❌ Invalid Login.");
                    //        return;
                    //    }
                    //}
                }
                else if (up.Message.ReplyToMessage != null && up.Message.ReplyToMessage.Text == "Please select your option :")
                {

                    //if (_credentials.TryGetValue(up.Message.Chat.Id, out Tb_UserActivities userAuth))
                    //{
                    //    var message = up.Message.Text;
                    //    if (message.Contains(FilterByDate.All.ToString()))
                    //        userAuth.FilterByDate = FilterByDate.All;
                    //    else if (message.Contains(FilterByDate.Monthly.ToString()))
                    //        userAuth.FilterByDate = FilterByDate.Monthly;
                    //    else
                    //        userAuth.FilterByDate = FilterByDate.Weekly;

                    //}
                    //await GetData(userAuth, up);
                }

                else
                {
                    LogOutUser(up);
                    await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "🔍 Your message is unknown. Please enter /start and select your user type!");
                }
            }
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
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your username:", replyMarkup: new ForceReplyMarkup());
            LogOutUser(up);
        }


        /// <summary>
        /// Get Affiliate report sell after authenticated
        /// </summary>
        /// <param name="userAuth"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task GetData(Tb_UserActivities userAuth, Update up)
        {
            //List<AffiliateReportDto> list = new List<AffiliateReportDto>();
            ////if sender pm is admin? can get all affiliate sell report else affiliate only get their report
            //IEnumerable<Tb_Sell> sells = null;
            //var affiliates = userAuth.UserType.Contains(adminText) ? await _uowApp.AffiliateRepo.GetAsync() : await _uowApp.AffiliateRepo.GetAsync(d => d.Email == userAuth.Email);

            //foreach (var affiliate in affiliates)
            //{
            //    if (userAuth.UserType.Contains(adminText))
            //    {
            //        switch (userAuth.FilterByDate)
            //        {
            //            case FilterByDate.All:
            //                sells = _uowApp.SellRepo.Get(d => d.AffiliateCode == affiliate.Code);
            //                break;

            //            case FilterByDate.Monthly:
            //                sells = _uowApp.SellRepo.Get(d => d.AffiliateCode == affiliate.Code && d.CreateAt > DateTime.Now.AddMonths(-1));
            //                break;

            //            case FilterByDate.Weekly:
            //                sells = _uowApp.SellRepo.Get(d => d.AffiliateCode == affiliate.Code && d.CreateAt > DateTime.Now.AddDays(-7));
            //                break;
            //            default:
            //                break;
            //        }
            //    }
            //    else
            //        sells = _uowApp.SellRepo.Get(d => d.AffiliateCode == affiliate.Code && d.CreateAt > DateTime.Now.AddDays(-7));


            //    var registeredCount = sells.Count() == 0 ? 0 : sells.Count(d => d.PayStatus == PayStatus.Registered);
            //    var sell = sells.Count() == 0 ? 0 : sells.Count(d => d.PayStatus == PayStatus.Registered);
            //    var sumSell = sells.Count() == 0 ? 0 : sells.Where(m => m.PayStatus != PayStatus.Registered).Sum(d => d.Price);
            //    var report = new AffiliateReportDto()
            //    {
            //        AffiliateCode = affiliate.Code,
            //        RegisteredCount = registeredCount,
            //        SumSell = sumSell,
            //        AffiliateEmail = affiliate.Email
            //    };
            //    list.Add(report);
            //}
            //try
            //{
            //    // generate pdf file
            //    var pdfFile = CreatePdf.createReport(_hostingEnvironment.WebRootPath, list).GenerateAsByteArray();
            //    Stream stream = new MemoryStream(pdfFile);
            //    var inputOnlineFile = new InputOnlineFile(stream, "AffiliateSellReport.pdf");

            //    var message = await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Sending file...");
            //    await _botService.Client.SendDocumentAsync(up.Message.Chat.Id, inputOnlineFile);
            //    await _botService.Client.DeleteMessageAsync(up.Message.Chat.Id, message.MessageId);

            //    //_credentials.TryRemove(up.Message.Chat.Id, out _);
            //}
            //catch (Exception ex)
            //{
            //    await InvalidLogin(up, "❌ Invalid Login.");
            //    LogOutUser(up);
            //}

        }
        public void LogOutUser(Update up)
        {
            //_credentials.TryRemove(up.Message.Chat.Id, out _);
        }


        public async Task StartBot(Update up)
        {
            var markupKeyboad = GenerateButton(2, BotConst.adminText, BotConst.affiliateText);
            markupKeyboad.ResizeKeyboard = true;
            markupKeyboad.OneTimeKeyboard = true;
            var message = string.Format("Hello {0} ✋, Please select your user type on system :", up.Message.From.FirstName);
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);
        }
        public async Task SelectUserType(Update up)
        {
            var markupKeyboad = GenerateBackButton();
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your username:", replyToMessageId: up.Message.MessageId, replyMarkup: markupKeyboad);

        }
        public async Task BackToStart(Update up)
        {
            var markupKeyboad = GenerateBackButton();
            var message = string.Format("Hello {0} ✋, Please select your user type on system :", up.Message.From.FirstName);
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);
        }

        public ReplyKeyboardMarkup GenerateButton(int ButtonsPerRow, params string[] Buttons)
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
        public ReplyKeyboardMarkup GenerateBackButton()
        {
            var markupKeyboad = GenerateButton(0, BotConst.back);
            markupKeyboad.ResizeKeyboard = true;
            markupKeyboad.OneTimeKeyboard = true;
            return markupKeyboad;
        }
        public Tb_UserInfo GetUserLastActivity(long chatId)
        {
            return _uow.UserInfoRepo
                .Get(d => d.ChatId == chatId, null, "Tb_UserActivities")
                .FirstOrDefault();
        }
        public Tb_UserActivities InsertUserActivity(Guid userInfoId, string message)
        {
            var userActivity = new Tb_UserActivities()
            {
                UserInfoId = userInfoId,
                Message = message

            };
            return userActivity;
        }
        public Tb_UserInfo InsertUserInfo(long chatId)
        {
            var userInfo = new Tb_UserInfo()
            {
                ChatId = chatId
            };
            return userInfo;
        }
        public Tb_UserInfo GetUserByChatId(long chatId)
        {
            return _uow.UserInfoRepo.Get(d => d.ChatId == chatId).FirstOrDefault();
        }
    }
}
