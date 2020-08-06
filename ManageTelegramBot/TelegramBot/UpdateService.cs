using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Accounting.Models;
using Accounting.Utility.GeneratePdfFile;
using DAL.Models;
using ManageTelegramBot.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Repository.InterFace;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace ManageTelegramBot.TelegramBot
{
    /// <summary>
    /// Connection with Telegram bot and manage it(get affiliate sell report)
    /// </summary>

    public class UpdateService : IUpdateService
    {
        private readonly IBotService _botService;
        private static ConcurrentDictionary<long, BotUserAuth> _credentials = new ConcurrentDictionary<long, BotUserAuth>();
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IUnitOfWork _uow;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser>  _userManager;

        public UpdateService(IBotService botService,
            IWebHostEnvironment hostingEnvironment,
            IUnitOfWork uow,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _botService = botService;
            _hostingEnvironment = hostingEnvironment;
            _uow = uow;
            _signInManager = signInManager;
            _userManager = userManager;
        }       

        public async Task EchoAsync(Update up)
        {
            string adminText = "🔥 Admin";
            string affiliateText = "😅 Affiliate";

            if (up.Message.From.IsBot)
            {
                return;
            }

            if (up.Message.Type == MessageType.Text)
            {
                if (up.Message.Text == "/start")
                {
                    var markupKeyboad = new ReplyKeyboardMarkup();
                    markupKeyboad.Keyboard = new KeyboardButton[][]
                    {
                                new KeyboardButton[]
                                {
                                    new KeyboardButton(adminText),
                                    new KeyboardButton(affiliateText)
                                },
                    };

                    markupKeyboad.ResizeKeyboard = true;
                    markupKeyboad.OneTimeKeyboard = true;

                    var message = string.Format("Hello {0} ✋, Please select your user type on system :", up.Message.From.FirstName);
                    await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message, replyMarkup: markupKeyboad);
                }
                else if (up.Message.Text == adminText || up.Message.Text == affiliateText)
                {
                    var userAuth = new BotUserAuth()
                    {
                        // Set Admin/Affiliate.
                        UserType = up.Message.Text,
                        CreateDate = DateTimeOffset.Now,
                    };
                    _credentials.TryRemove(up.Message.Chat.Id, out _);
                    if (_credentials.TryAdd(up.Message.Chat.Id, userAuth))
                    {
                        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your username:", replyToMessageId: up.Message.MessageId, replyMarkup: new ForceReplyMarkup());
                    }
                }
                else if (up.Message.ReplyToMessage != null && up.Message.ReplyToMessage.Text == "Please enter your username:")
                {

                    if (_credentials.TryGetValue(up.Message.Chat.Id, out BotUserAuth userAuth))
                    {
                        userAuth.Username = up.Message.Text;
                        await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your password:", replyToMessageId: up.Message.MessageId, replyMarkup: new ForceReplyMarkup());
                    }
                }
                else if (up.Message.ReplyToMessage != null && up.Message.ReplyToMessage.Text == "Please enter your password:")
                {
                    // TODO: This has been changed.
                    if (_credentials.TryGetValue(up.Message.Chat.Id, out BotUserAuth userAuth))
                    {
                        userAuth.Password = up.Message.Text;

                        // TODO: Check form Database

                        var result = await _signInManager.PasswordSignInAsync(userAuth.Username, userAuth.Password, false, lockoutOnFailure: false);
                        if (result.Succeeded)
                        {
                            var user = _uow.UserRepo.GetUserByName(userAuth.Username);

                            var roles = await _userManager.GetRolesAsync(user);
                            bool hasAccess = false;
                            foreach (var role in roles)
                            {
                                if (userAuth.UserType.Contains(role))
                                {
                                    hasAccess = true;
                                    break;
                                }
                            }
                            if(!hasAccess)
                            {
                                await InvalidLogin(up,"❌ Invalid Login.");
                                return;
                            }
                            if (!user.IsActive)
                            {
                                await InvalidLogin(up,"❌ User is not active.");
                                return;
                            }
                            // if login has successfully 
                            
                            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "✅ Logged in successfully.");

                            List<AffiliateReportDto> list = new List<AffiliateReportDto>();

                            //if sender pm is admin? can get all affiliate sell report else affiliate only get their report
                            var affiliates = userAuth.UserType == adminText ? _uow.AffiliateRepo.Get() : _uow.AffiliateRepo.Get(d => d.Email == user.Email);
                            foreach (var affiliate in affiliates)
                            {
                                var sells = _uow.SellRepo.Get(d => d.AffiliateCode == affiliate.Code && d.CreateAt > DateTime.Now.AddDays(-7));
                                var registeredCount = sells.Count() == 0 ? 0 : sells.Count(d => d.PayStatus == PayStatus.Registered);
                                var sell = sells.Count() == 0 ? 0 : sells.Count(d => d.PayStatus == PayStatus.Registered);
                                var sumSell = sells.Count() == 0 ? 0 : sells.Where(m => m.PayStatus != PayStatus.Registered).Sum(d => d.Price);
                                var report = new AffiliateReportDto()
                                {
                                    AffiliateCode = affiliate.Code,
                                    RegisteredCount = registeredCount,
                                    SumSell = sumSell,
                                    AffiliateEmail = affiliate.Email
                                };
                                list.Add(report);
                            }
                            try
                            {
                                // generate pdf file
                                var pdfFile = CreatePdf.createReport(_hostingEnvironment.WebRootPath, list).GenerateAsByteArray();
                                Stream stream = new MemoryStream(pdfFile);
                                var inputOnlineFile = new InputOnlineFile(stream, "AffiliateSellReport.pdf");

                                var message = await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Sending file...");
                                await _botService.Client.SendDocumentAsync(up.Message.Chat.Id, inputOnlineFile);
                                await _botService.Client.DeleteMessageAsync(up.Message.Chat.Id, message.MessageId);

                                _credentials.TryRemove(up.Message.Chat.Id, out _);
                            }
                            catch (Exception ex)
                            {
                                await InvalidLogin(up,"❌ Invalid Login.");
                                return;
                            }

                        }
                        else
                        {
                            await InvalidLogin(up,"❌ Invalid Login.");
                            return;
                        }
                    }
                }
                else
                {
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
        private async Task InvalidLogin(Update up,string message)
        {
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, message);
            // TODO: This has been added.
            await _botService.Client.SendTextMessageAsync(up.Message.Chat.Id, "Please enter your username:", replyMarkup: new ForceReplyMarkup());
        }
    }
}
