using Microsoft.Extensions.Options;
using StockMate.Application.Carts.Models;
using StockMate.Application.Carts.Services;
using StockMate.Application.Customers.Models;
using StockMate.Application.Customers.Services;
using StockMate.Application.DeliveryCompanies.Services;
using StockMate.Application.Orders.Models;
using StockMate.Application.Orders.Services;
using StockMate.Application.Products.Models;
using StockMate.Application.Products.Services;
using StockMate.Application.Shipments.Models;
using StockMate.Application.Shipments.Services;
using StockMate.Application.Staff.Models;
using StockMate.Application.Staff.Services;
using StockMate.Bot.Options;
using StockMate.Bot.Telegram.State;
using StockMate.Domain.Enums;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StockMate.Bot.Telegram.Handlers;

public class TelegramUpdateHandler
{
    private readonly TelegramBotClient _botClient;
    private readonly ICustomerService _customerService;
    private readonly ICustomerAddressService _customerAddressService;
    private readonly IDeliveryCompanyService _deliveryCompanyService;
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly IProductService _productService;
    private readonly ICartService _cartService;
    private readonly TelegramSessionStore _sessionStore;
    private readonly IOrderService _orderService;
    private readonly IStaffService _staffService;
    private readonly IOrderAssignmentService _assignmentService;
    private readonly IShipmentService _shipmentService;
    private readonly TelegramOptions _telegramOptions;

    public TelegramUpdateHandler(
    TelegramBotClient botClient,
    ICustomerService customerService,
    ICustomerAddressService customerAddressService,
    IDeliveryCompanyService deliveryCompanyService,
    IProductService productService,
    ICartService cartService,
    TelegramSessionStore sessionStore,
    IOrderService orderService,
    IStaffService staffService,
    IOrderAssignmentService assignmentService,
    IShipmentService shipmentService,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<TelegramUpdateHandler> logger)
    {
        _botClient = botClient;
        _customerService = customerService;
        _customerAddressService = customerAddressService;
        _deliveryCompanyService = deliveryCompanyService;
        _productService = productService;
        _cartService = cartService;
        _logger = logger;
        _sessionStore = sessionStore;
        _orderService = orderService;
        _staffService = staffService;
        _assignmentService = assignmentService;
        _shipmentService = shipmentService;
        _telegramOptions = telegramOptions.Value;
    }

    public async Task HandleMessageAsync(
        Message message,
        UpdateType updateType)
    {
        if (message.From is null)
            return;

        if (message.Text?.StartsWith("/start") == true ||
                message.Text?.Equals(
        "menyu",
        StringComparison.OrdinalIgnoreCase) == true)
        {
            await HandleStartAsync(message);
            return;
        }
        if (message.Text == "/cancel")
        {
            _sessionStore.Clear(message.From.Id);

            await _botClient.SendMessage(
                message.Chat.Id,
                "Əməliyyat ləğv edildi.");

            if (message.From.Id == _telegramOptions.AdminUserId)
            {
                await SendAdminMainMenuAsync(message.Chat.Id);
            }
            else
            {
                await SendCustomerMainMenuAsync(message.Chat.Id);
            }

            return;
        }
        if (message.Text == "/myid")
        {
            await _botClient.SendMessage(
                message.Chat.Id,
                $"Telegram ID: {message.From.Id}");

            return;
        }

        if (await TryHandleCustomerInputAsync(message))
            return;

        if (await TryHandleAdminInputAsync(message))
            return;

        if (await TryHandleCheckoutMessageAsync(message))
            return;

        await _botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Zəhmət olmasa menyudan seçim edin.");
    }

    public async Task HandleUpdateAsync(Update update)
    {
        if (update.CallbackQuery is not { } query)
            return;

        await _botClient.AnswerCallbackQuery(query.Id);

        if (query.Message is null || query.Data is null)
            return;

        var chatId = query.Message.Chat.Id;
        var telegramUserId = query.From.Id;

        if (query.Data.StartsWith("admin:") &&
            query.From.Id != _telegramOptions.AdminUserId)
        {
            await _botClient.SendMessage(
                chatId,
                "Bu əməliyyat yalnız admin üçündür.");

            return;
        }

        if (query.Data == "menu:products")
        {
            await SendProductsPageAsync(
                telegramUserId,
                chatId,
                pageNumber: 1);

            return;
        }

        if (query.Data == "menu:cart")
        {
            _sessionStore.Clear(telegramUserId);
            await SendCartAsync(telegramUserId, chatId);
            return;
        }

        if (query.Data == "menu:profile")
        {
            await SendProfileAsync(telegramUserId, chatId);
            return;
        }

        if (query.Data == "menu:orders")
        {
            await SendCustomerOrdersAsync(
                telegramUserId,
                chatId);

            return;
        }
        if (query.Data.StartsWith("order:view:"))
        {
            var numberText =
                query.Data["order:view:".Length..];

            if (long.TryParse(numberText, out var orderNumber))
            {
                await SendCustomerOrderDetailAsync(
                    telegramUserId,
                    chatId,
                    orderNumber);
            }

            return;
        }
        if (query.Data.StartsWith("product:view:"))
        {
            var idText = query.Data["product:view:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                await SendProductDetailAsync(
                    chatId,
                    productId,
                    quantity: 1);
            }

            return;
        }
        if (query.Data == "order:confirm-checkout")
        {
            if (!_sessionStore.TryGet(telegramUserId, out var checkoutSession) ||
                checkoutSession?.CustomerAddressId is not Guid customerAddressId ||
                checkoutSession.DeliveryCompanyId is not Guid deliveryCompanyId)
            {
                await _botClient.SendMessage(
                    chatId,
                    "Çatdırılma seçimi tamamlanmayıb. Ünvanı yenidən seçin.");
                await BeginCheckoutAsync(telegramUserId, chatId);
                return;
            }

            var order = await _orderService.CheckoutAsync(
                telegramUserId,
                customerAddressId,
                deliveryCompanyId);

            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                chatId,
                $"✅ Sifarişiniz qəbul edildi.\n\n" +
                $"Sifariş nömrəsi: #{order.OrderNumber}\n" +
                $"Məhsullar: {order.ProductsTotal:0.00} AZN\n" +
                "Çatdırılma haqqı adminlə razılaşdırılacaq.");

            if (_telegramOptions.AdminUserId > 0)
            {
                await SendNewOrderToAdminAsync(order);
            }

            return;
        }

        if (query.Data.StartsWith("product:qty:"))
        {
            var parts = query.Data.Split(':');

            if (parts.Length == 4 &&
                Guid.TryParse(parts[2], out var productId) &&
                int.TryParse(parts[3], out var quantity))
            {
                quantity = Math.Clamp(quantity, 1, 99);

                await UpdateProductDetailAsync(
                    query.Message,
                    productId,
                    quantity);
            }

            return;
        }

        if (query.Data.StartsWith("checkout:address:"))
        {
            var idText = query.Data["checkout:address:".Length..];
            if (Guid.TryParse(idText, out var addressId))
            {
                var addresses = await _customerAddressService.GetActiveAsync(
                    telegramUserId);
                if (addresses.All(address => address.Id != addressId))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Bu ünvan artıq aktiv deyil. Başqa ünvan seçin.");
                    await ShowAddressSelectionAsync(telegramUserId, chatId);
                    return;
                }

                var session = _sessionStore.GetOrCreate(telegramUserId);
                session.CustomerAddressId = addressId;
                session.DeliveryCompanyId = null;
                session.CheckoutStep = CheckoutStep.SelectingDeliveryCompany;
                await ShowDeliveryCompanySelectionAsync(telegramUserId, chatId);
            }

            return;
        }

        if (query.Data == "checkout:new-address")
        {
            await PromptForNewAddressAsync(telegramUserId, chatId);
            return;
        }

        if (query.Data == "checkout:back-address")
        {
            await ShowAddressSelectionAsync(telegramUserId, chatId);
            return;
        }

        if (query.Data.StartsWith("checkout:company:"))
        {
            var idText = query.Data["checkout:company:".Length..];
            if (Guid.TryParse(idText, out var companyId))
            {
                var companies = await _deliveryCompanyService.GetActiveAsync();
                if (companies.All(company => company.Id != companyId))
                {
                    await _botClient.SendMessage(
                        chatId,
                        "Bu çatdırılma şirkəti artıq aktiv deyil. Başqa şirkət seçin.");
                    await ShowDeliveryCompanySelectionAsync(telegramUserId, chatId);
                    return;
                }

                var session = _sessionStore.GetOrCreate(telegramUserId);
                if (session.CustomerAddressId is null)
                {
                    await ShowAddressSelectionAsync(telegramUserId, chatId);
                    return;
                }

                session.DeliveryCompanyId = companyId;
                session.CheckoutStep = CheckoutStep.AwaitingConfirmation;
                await ShowCheckoutConfirmationAsync(telegramUserId, chatId);
            }

            return;
        }

        if (query.Data == "checkout:cancel")
        {
            _sessionStore.Clear(telegramUserId);
            await _botClient.SendMessage(chatId, "Sifarişin rəsmiləşdirilməsi dayandırıldı.");
            await SendCartAsync(telegramUserId, chatId);
            return;
        }
        if (query.Data.StartsWith("product:quantity:"))
        {
            var idText = query.Data["product:quantity:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                var session = _sessionStore.GetOrCreate(telegramUserId);
                session.CustomerInputStep = CustomerInputStep.AwaitingProductQuantity;
                session.CustomerProductId = productId;

                await _botClient.SendMessage(
                    chatId,
                    "Məhsul sayını rəqəmlə yazın (1–99).\n" +
                    "Ləğv etmək üçün /cancel yazın.");
            }

            return;
        }
        if (query.Data.StartsWith("admin:confirm:"))
        {
            if (query.From.Id != _telegramOptions.AdminUserId)
            {
                await _botClient.AnswerCallbackQuery(
                    query.Id,
                    "Bu əməliyyat yalnız admin üçündür.",
                    showAlert: true);

                return;
            }

            var orderNumberText =
                query.Data["admin:confirm:".Length..];

            if (!long.TryParse(
                    orderNumberText,
                    out var orderNumber))
            {
                return;
            }

            await _orderService.ConfirmAsync(orderNumber);

            var order =
                await _orderService.GetByOrderNumberAsync(orderNumber)
                ?? throw new KeyNotFoundException(
                    "Order was not found.");

            var customer =
                await _customerService.GetByIdAsync(order.CustomerId)
                ?? throw new KeyNotFoundException(
                    "Customer was not found.");

            await _botClient.AnswerCallbackQuery(
                query.Id,
                $"Sifariş #{orderNumber} təsdiqləndi.",
                showAlert: true);

            await _botClient.SendMessage(
                query.Message.Chat.Id,
                $"✅ Sifariş #{orderNumber} admin tərəfindən təsdiqləndi.\n" +
                $"Status: {order.Status}");

            await _botClient.SendMessage(
                customer.TelegramUserId,
                $"✅ Sifarişiniz təsdiqləndi.\n\n" +
                $"Sifariş nömrəsi: #{orderNumber}\n" +
                "Hazırda sifarişiniz hazırlanma mərhələsinə keçəcək.");
            await SendAdminOrderActionsAsync(
                chatId,
                orderNumber);

            return;
        }

        if (query.Data.StartsWith("admin:delivery-mode:all:"))
        {
            var numberText = query.Data["admin:delivery-mode:all:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _orderService.SetDeliveryModeAsync(
                orderNumber,
                DeliveryMode.AllTogether);

            await NotifyOrderStatusAsync(
                orderNumber,
                "⏳ Sifarişinizin bütün məhsulları birlikdə çatdırılacaq. Hazır olduqda sizə məlumat veriləcək.");

            await SendAdminOrderActionsAsync(chatId, orderNumber);
            return;
        }

        if (query.Data.StartsWith("admin:delivery-mode:partial:"))
        {
            var numberText = query.Data["admin:delivery-mode:partial:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _orderService.SetDeliveryModeAsync(
                orderNumber,
                DeliveryMode.PartialAllowed);

            await SendAdminOrderActionsAsync(chatId, orderNumber);
            return;
        }

        if (query.Data.StartsWith("admin:deliver-ready:"))
        {
            var numberText = query.Data["admin:deliver-ready:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            var before = await _orderService.GetByOrderNumberAsync(orderNumber)
                ?? throw new KeyNotFoundException("Order was not found.");

            var order = await _orderService.DeliverReadyItemsAsync(orderNumber);
            var deliveredText = new StringBuilder();

            foreach (var item in order.Items)
            {
                var previous = before.Items
                    .First(existing => existing.Id == item.Id)
                    .DeliveredQuantity;
                var deliveredNow = item.DeliveredQuantity - previous;

                if (deliveredNow > 0)
                    deliveredText.AppendLine($"• {item.ProductName}: {deliveredNow} ədəd");
            }

            await NotifyOrderStatusAsync(
                orderNumber,
                $"🚚 Sifarişinizdən aşağıdakı məhsullar göndərildi:\n\n" +
                deliveredText +
                (order.RemainingQuantity > 0
                    ? $"\nQalan məhsul sayı: {order.RemainingQuantity}. Hazır olduqda sizə məlumat veriləcək."
                    : "\nSifarişiniz tam çatdırıldı."));

            await SendAdminOrderActionsAsync(chatId, orderNumber);
            return;
        }

        if (query.Data.StartsWith("admin:cancel-remaining:"))
        {
            var numberText = query.Data["admin:cancel-remaining:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _botClient.SendMessage(
                chatId,
                $"Sifariş #{orderNumber} üzrə qalan hissə ləğv edilsin?",
                replyMarkup: new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "Bəli, qalanı ləğv et",
                                $"admin:cancel-rem-confirm:{orderNumber}"),
                            InlineKeyboardButton.WithCallbackData(
                                "Xeyr",
                                $"admin:actions:{orderNumber}")
                        }
                    }));

            return;
        }

        if (query.Data.StartsWith("admin:cancel-rem-confirm:"))
        {
            var numberText = query.Data["admin:cancel-rem-confirm:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _orderService.CancelRemainingItemsAsync(orderNumber);

            await NotifyOrderStatusAsync(
                orderNumber,
                "Qalan çatdırılmamış məhsullar admin tərəfindən ləğv edildi.");

            await SendAdminOrderActionsAsync(chatId, orderNumber);
            return;
        }
        if (query.Data.StartsWith("admin:preparing:"))
        {
            var numberText =
                query.Data["admin:preparing:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _orderService.MarkAsPreparingAsync(orderNumber);

            await NotifyOrderStatusAsync(
                orderNumber,
                "📦 Sifarişiniz hazırlanır.");

            await SendAdminOrderActionsAsync(
                chatId,
                orderNumber);

            return;
        }

        if (query.Data.StartsWith("admin:delivering:"))
        {
            var numberText =
                query.Data["admin:delivering:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _orderService.MarkAsDeliveringAsync(orderNumber);

            await NotifyOrderStatusAsync(
                orderNumber,
                "🚚 Sifarişiniz çatdırılmağa verildi.");

            await SendAdminOrderActionsAsync(
                chatId,
                orderNumber);

            return;
        }

        if (query.Data.StartsWith("admin:complete:"))
        {
            var numberText =
                query.Data["admin:complete:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _orderService.CompleteAsync(orderNumber);

            await NotifyOrderStatusAsync(
                orderNumber,
                "✅ Sifarişiniz tamamlandı.");

            await _botClient.SendMessage(
                chatId,
                $"✅ Sifariş #{orderNumber} tamamlandı.");

            return;
        }

        if (query.Data.StartsWith("admin:cancel:"))
        {
            var numberText =
                query.Data["admin:cancel:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _botClient.SendMessage(
                chatId,
                $"Sifariş #{orderNumber} ləğv edilsin?",
                replyMarkup: new InlineKeyboardMarkup(
                    new[]
                    {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "Bəli, ləğv et",
                        $"admin:cancel-confirm:{orderNumber}"),

                    InlineKeyboardButton.WithCallbackData(
                        "Xeyr",
                        $"admin:actions:{orderNumber}")
                }
                    }));

            return;
        }

        if (query.Data.StartsWith("admin:cancel-confirm:"))
        {
            var numberText =
                query.Data["admin:cancel-confirm:".Length..];

            if (!long.TryParse(numberText, out var orderNumber))
                return;

            await _orderService.CancelAsync(
                orderNumber,
                "Admin tərəfindən ləğv edildi.");

            await NotifyOrderStatusAsync(
                orderNumber,
                "❌ Sifarişiniz admin tərəfindən ləğv edildi.");

            await _botClient.SendMessage(
                chatId,
                $"Sifariş #{orderNumber} ləğv edildi.");

            return;
        }

        if (query.Data.StartsWith("admin:actions:"))
        {
            var numberText =
                query.Data["admin:actions:".Length..];

            if (long.TryParse(numberText, out var orderNumber))
            {
                await SendAdminOrderActionsAsync(
                    chatId,
                    orderNumber);
            }

            return;
        }

        if (query.Data.StartsWith("admin:customer-orders:"))
        {
            var numberText =
                query.Data["admin:customer-orders:".Length..];

            if (!long.TryParse(numberText, out var currentOrderNumber))
                return;

            var currentOrder =
                await _orderService.GetByOrderNumberAsync(currentOrderNumber)
                ?? throw new KeyNotFoundException("Order was not found.");

            var customer =
                await _customerService.GetByIdAsync(currentOrder.CustomerId)
                ?? throw new KeyNotFoundException("Customer was not found.");

            var orders = (await _orderService.GetCustomerOrdersAsync(
                    customer.TelegramUserId))
                .Where(order => order.OrderNumber != currentOrderNumber)
                .Take(10)
                .ToList();

            var rows = orders
                .Select(order => new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"#{order.OrderNumber} — {GetOrderStatusText(order.Status)}",
                        $"admin:actions:{order.OrderNumber}")
                })
                .ToList();

            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData(
                    $"⬅️ Sifariş #{currentOrderNumber}",
                    $"admin:actions:{currentOrderNumber}")
            ]);

            await _botClient.SendMessage(
                chatId,
                $"👤 {customer.FirstName} {customer.LastName}\n\n" +
                "Müştərinin digər sifarişləri:",
                replyMarkup: new InlineKeyboardMarkup(rows));

            return;
        }

        if (query.Data.StartsWith("cart:add:"))
        {
            var parts = query.Data.Split(':');

            if (parts.Length != 5 ||
                !Guid.TryParse(parts[2], out var cartProductId) ||
                !int.TryParse(parts[3], out var cartQuantity))
            {
                return;
            }

            var mode = CartItemFulfillmentMode.IncludeSpecialOrder;

            await _cartService.AddOrUpdateItemAsync(
                telegramUserId,
                new AddCartItemRequest(
                    cartProductId,
                    cartQuantity,
                    mode));

            await _botClient.SendMessage(
                chatId,
                "✅ Məhsul səbətə əlavə edildi.");

            return;
        }

        if (query.Data.StartsWith("products:page:"))
        {
            var pageText = query.Data["products:page:".Length..];

            if (int.TryParse(pageText, out var pageNumber))
            {
                await SendProductsPageAsync(
                    telegramUserId,
                    chatId,
                    pageNumber);
            }

            return;
        }

        if (query.Data.StartsWith("products:size:"))
        {
            var sizeText = query.Data["products:size:".Length..];

            if (int.TryParse(sizeText, out var pageSize))
            {
                await _customerService.ChangeProductPageSizeAsync(
                    telegramUserId,
                    pageSize);

                await SendProductsPageAsync(
                    telegramUserId,
                    chatId,
                    pageNumber: 1);
            }
        }
        if (query.Data.StartsWith("cart:remove:"))
        {
            var idText = query.Data["cart:remove:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                await _cartService.RemoveItemAsync(
                    telegramUserId,
                    productId);

                await SendCartAsync(
                    telegramUserId,
                    chatId);
            }

            return;
        }

        if (query.Data == "cart:clear")
        {
            await _cartService.ClearAsync(telegramUserId);

            await _botClient.SendMessage(
                chatId,
                "🗑 Səbət təmizləndi.");

            return;
        }

        if (query.Data == "cart:checkout")
        {
            await BeginCheckoutAsync(
                telegramUserId,
                chatId);

            return;
        }
        if (query.Data == "admin:menu")
        {
            await SendAdminMainMenuAsync(chatId);
            return;
        }

        if (query.Data == "admin:customer-menu")
        {
            await SendCustomerMainMenuAsync(chatId);
            return;
        }

        if (query.Data == "admin:staff")
        {
            await SendAdminStaffListAsync(chatId);
            return;
        }

        if (query.Data == "admin:staff:add")
        {
            var session = _sessionStore.GetOrCreate(telegramUserId);
            session.AdminInputStep = AdminInputStep.AwaitingWorkerTelegramId;
            session.WorkerTelegramUserId = null;
            session.WorkerFirstName = null;
            session.WorkerLastName = null;

            await _botClient.SendMessage(
                chatId,
                "İşçinin Telegram ID-sini yazın.\n\n" +
                "İşçi öz botunda /myid yazaraq ID-ni görə bilər.\n" +
                "Ləğv etmək üçün /cancel yazın.");
            return;
        }

        if (query.Data.StartsWith("admin:staff:view:"))
        {
            if (Guid.TryParse(query.Data["admin:staff:view:".Length..], out var staffId))
                await SendAdminStaffDetailAsync(chatId, staffId);
            return;
        }

        if (query.Data.StartsWith("admin:staff:on:"))
        {
            if (Guid.TryParse(query.Data["admin:staff:on:".Length..], out var staffId))
            {
                await _staffService.ActivateAsync(staffId);
                await SendAdminStaffDetailAsync(chatId, staffId);
            }
            return;
        }

        if (query.Data.StartsWith("admin:staff:off:"))
        {
            if (Guid.TryParse(query.Data["admin:staff:off:".Length..], out var staffId))
            {
                await _staffService.DeactivateAsync(staffId);
                await SendAdminStaffDetailAsync(chatId, staffId);
            }
            return;
        }

        if (query.Data.StartsWith("admin:aw:"))
        {
            if (long.TryParse(query.Data["admin:aw:".Length..], out var orderNumber))
                await SendWorkerSelectionAsync(chatId, orderNumber);
            return;
        }

        if (query.Data.StartsWith("admin:a:"))
        {
            var parts = query.Data.Split(':');
            if (parts.Length == 4 &&
                long.TryParse(parts[2], out var orderNumber) &&
                Guid.TryParse(parts[3], out var workerId))
            {
                var admin = await GetRequiredAdminStaffAsync(telegramUserId);
                await _assignmentService.AssignAsync(orderNumber, workerId, admin.Id);
                await _botClient.SendMessage(chatId, "✅ Sifariş işçiyə təyin edildi.");
                await SendAdminOrderActionsAsync(chatId, orderNumber);
            }
            return;
        }

        if (query.Data.StartsWith("admin:unassign:"))
        {
            if (long.TryParse(query.Data["admin:unassign:".Length..], out var orderNumber))
            {
                var admin = await GetRequiredAdminStaffAsync(telegramUserId);
                await _assignmentService.ReleaseAsync(orderNumber, admin.Id);
                await _botClient.SendMessage(chatId, "✅ İşçi təyinatı ləğv edildi.");
                await SendAdminOrderActionsAsync(chatId, orderNumber);
            }
            return;
        }

        if (query.Data.StartsWith("admin:ship:new:"))
        {
            if (long.TryParse(query.Data["admin:ship:new:".Length..], out var orderNumber))
            {
                var admin = await GetRequiredAdminStaffAsync(telegramUserId);
                var assignment = await _assignmentService.GetOrderAssignmentAsync(orderNumber)
                    ?? throw new InvalidOperationException("Sifariş əvvəlcə işçiyə təyin edilməlidir.");

                await _shipmentService.CreateReadyShipmentAsync(
                    orderNumber, assignment.StaffId, admin.Id);
                await _botClient.SendMessage(chatId, "✅ Hazır məhsullar üçün göndəriş yaradıldı.");
                await SendAdminShipmentsAsync(chatId, orderNumber);
            }
            return;
        }

        if (query.Data.StartsWith("admin:ss:"))
        {
            var parts = query.Data.Split(':');
            if (parts.Length == 4 && Guid.TryParse(parts[3], out var shipmentId))
            {
                var status = parts[2] switch
                {
                    "p" => ShipmentStatus.Preparing,
                    "r" => ShipmentStatus.Ready,
                    "o" => ShipmentStatus.OutForDelivery,
                    "d" => ShipmentStatus.Delivered,
                    "c" => ShipmentStatus.Cancelled,
                    _ => throw new InvalidOperationException("Naməlum göndəriş statusu.")
                };
                var admin = await GetRequiredAdminStaffAsync(telegramUserId);
                var shipment = await _shipmentService.ChangeStatusAsync(shipmentId, admin.Id, status);
                await SendAdminShipmentsAsync(chatId, shipment.OrderNumber);
            }
            return;
        }

        if (query.Data.StartsWith("admin:ships:"))
        {
            if (long.TryParse(query.Data["admin:ships:".Length..], out var orderNumber))
                await SendAdminShipmentsAsync(chatId, orderNumber);
            return;
        }

        if (query.Data == "admin:orders:new")
        {
            var orders = await _orderService.GetNewOrdersAsync();

            await SendAdminOrderListAsync(
                chatId,
                "🆕 Yeni sifarişlər",
                orders);

            return;
        }

        if (query.Data == "admin:orders:active")
        {
            var orders = await _orderService.GetActiveOrdersAsync();

            await SendAdminOrderListAsync(
                chatId,
                "📦 Aktiv sifarişlər",
                orders);

            return;
        }

        if (query.Data == "admin:orders:history")
        {
            var orders = await _orderService.GetOrderHistoryAsync();

            await SendAdminOrderListAsync(
                chatId,
                "📜 Sifariş tarixçəsi",
                orders);

            return;
        }

        if (query.Data == "admin:orders:special")
        {
            var newOrders = await _orderService.GetNewOrdersAsync();
            var activeOrders = await _orderService.GetActiveOrdersAsync();

            var specialOrders = newOrders
                .Concat(activeOrders)
                .Where(order => order.ContainsSpecialOrder)
                .OrderByDescending(order => order.CreatedAtUtc)
                .ToList();

            await SendAdminOrderListAsync(
                chatId,
                "🚚 Xüsusi sifarişlər",
                specialOrders);

            return;
        }
        if (query.Data == "admin:products" ||
    query.Data == "admin:stock")
        {
            await SendAdminProductsPageAsync(
                chatId,
                pageNumber: 1);

            return;
        }

        if (query.Data.StartsWith("admin:products:page:"))
        {
            var pageText =
                query.Data["admin:products:page:".Length..];

            if (int.TryParse(pageText, out var pageNumber))
            {
                await SendAdminProductsPageAsync(
                    chatId,
                    pageNumber);
            }

            return;
        }

        if (query.Data.StartsWith("admin:product:view:"))
        {
            var idText =
                query.Data["admin:product:view:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                await SendAdminProductDetailAsync(
                    chatId,
                    productId);
            }

            return;
        }

        if (query.Data.StartsWith("admin:product:deactivate:"))
        {
            var idText =
                query.Data["admin:product:deactivate:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                await _productService.DeactivateAsync(productId);

                await _botClient.SendMessage(
                    chatId,
                    "⛔ Məhsul passiv edildi.");

                await SendAdminProductDetailAsync(
                    chatId,
                    productId);
            }

            return;
        }

        if (query.Data.StartsWith("admin:product:activate:"))
        {
            var idText =
                query.Data["admin:product:activate:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                await _productService.ActivateAsync(productId);

                await _botClient.SendMessage(
                    chatId,
                    "✅ Məhsul aktiv edildi.");

                await SendAdminProductDetailAsync(
                    chatId,
                    productId);
            }

            return;
        }

        if (query.Data.StartsWith("admin:product:history:"))
        {
            var idText =
                query.Data["admin:product:history:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                await SendStockHistoryAsync(
                    chatId,
                    productId);
            }

            return;
        }
        if (query.Data.StartsWith("admin:price:"))
        {
            var idText = query.Data["admin:price:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                var session = _sessionStore.GetOrCreate(
                    telegramUserId);

                session.ProductId = productId;
                session.AdminInputStep =
                    AdminInputStep.AwaitingPrice;

                await _botClient.SendMessage(
                    chatId,
                    "Yeni qiyməti AZN ilə yazın.\nMəsələn: 2.50");
            }

            return;
        }

        if (query.Data.StartsWith("admin:stock-edit:"))
        {
            var idText =
                query.Data["admin:stock-edit:".Length..];

            if (Guid.TryParse(idText, out var productId))
            {
                await _botClient.SendMessage(
                    chatId,
                    "Stok əməliyyatını seçin:",
                    replyMarkup: new InlineKeyboardMarkup(
                        new[]
                        {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "➕ Əlavə et",
                            $"admin:stock-add:{productId}"),

                        InlineKeyboardButton.WithCallbackData(
                            "➖ Stokdan çıx",
                            $"admin:stock-remove:{productId}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "🔢 Dəqiq təyin et",
                            $"admin:stock-set:{productId}")
                    }
                        }));
            }
            

            return;
        }
        if (query.Data.StartsWith("admin:stock-add:") ||
            query.Data.StartsWith("admin:stock-remove:") ||
            query.Data.StartsWith("admin:stock-set:"))
        {
            var lastSeparator =
                query.Data.LastIndexOf(':');

            var stockProductIdText =
                query.Data[(lastSeparator + 1)..];

            if (!Guid.TryParse(
                    stockProductIdText,
                    out var stockProductId))
            {
                return;
            }

            var action = query.Data switch
            {
                var data when data.StartsWith("admin:stock-add:") =>
                    AdminStockAction.Add,

                var data when data.StartsWith("admin:stock-remove:") =>
                    AdminStockAction.Remove,

                _ => AdminStockAction.Set
            };

            var session = _sessionStore.GetOrCreate(
                telegramUserId);

            session.ProductId = stockProductId;
            session.AdminStockAction = action;
            session.AdminInputStep =
                AdminInputStep.AwaitingStockQuantity;

            await _botClient.SendMessage(
                chatId,
                action == AdminStockAction.Set
                    ? "Yeni dəqiq stok sayını yazın:"
                    : "Miqdarı yazın:");

            return;
        }
        if (query.Data == "admin:product:add")
        {
            var session = _sessionStore.GetOrCreate(
                telegramUserId);

            session.AdminInputStep =
                AdminInputStep.AwaitingProductName;

            session.ProductId = null;
            session.ProductName = null;
            session.ProductCode = null;
            session.ProductPrice = null;
            session.InitialStockQuantity = null;
            session.ProductDescription = null;

            await _botClient.SendMessage(
                chatId,
                "Yeni məhsulun adını yazın.\n\n" +
                "Ləğv etmək üçün /cancel yazın.");

            return;
        }
        if (query.Data.StartsWith("admin:edit-name:"))
        {
            var idText =
                query.Data["admin:edit-name:".Length..];

            if (Guid.TryParse(idText, out var editNameProductId))
            {
                var session = _sessionStore.GetOrCreate(
                    telegramUserId);

                session.ProductId = editNameProductId;
                session.AdminInputStep =
                    AdminInputStep.AwaitingProductNameEdit;

                await _botClient.SendMessage(
                    chatId,
                    "Məhsulun yeni adını yazın:");
            }

            return;
        }

        if (query.Data.StartsWith("admin:edit-description:"))
        {
            var idText =
                query.Data["admin:edit-description:".Length..];

            if (Guid.TryParse(
                    idText,
                    out var editDescriptionProductId))
            {
                var session = _sessionStore.GetOrCreate(
                    telegramUserId);

                session.ProductId = editDescriptionProductId;
                session.AdminInputStep =
                    AdminInputStep.AwaitingProductDescriptionEdit;

                await _botClient.SendMessage(
                    chatId,
                    "Məhsulun yeni açıqlamasını yazın:");
            }

            return;
        }

        if (query.Data.StartsWith("admin:edit-image:"))
        {
            var idText =
                query.Data["admin:edit-image:".Length..];

            if (Guid.TryParse(idText, out var editImageProductId))
            {
                var session = _sessionStore.GetOrCreate(
                    telegramUserId);

                session.ProductId = editImageProductId;
                session.AdminInputStep =
                    AdminInputStep.AwaitingProductImageEdit;

                await _botClient.SendMessage(
                    chatId,
                    "Yeni şəkli göndərin.\n" +
                    "Şəkli silmək üçün “Şəkli sil” yazın.");
            }

            return;
        }

        if (query.Data.StartsWith("admin:sp:"))
        {
            var parts = query.Data.Split(':');

            if (parts.Length != 5 ||
                !long.TryParse(parts[3], out var orderNumber) ||
                !Guid.TryParse(parts[4], out var orderItemId))
            {
                return;
            }

            var newStatus = parts[2] switch
            {
                "ordered" =>
                    SpecialOrderStatus.OrderedFromSupplier,

                "arrived" =>
                    SpecialOrderStatus.Arrived,

                "completed" =>
                    SpecialOrderStatus.Completed,

                _ => throw new InvalidOperationException(
                    "Unknown special-order action.")
            };

            await _orderService.ChangeSpecialOrderStatusAsync(
                orderNumber,
                orderItemId,
                newStatus);

            var customerMessage = newStatus switch
            {
                SpecialOrderStatus.OrderedFromSupplier =>
                    "📦 Çatışmayan məhsulunuz təchizatçıdan sifariş edildi.",

                SpecialOrderStatus.Arrived =>
                    "✅ Sifarişlə gətirilən məhsulunuz gəldi.",

                SpecialOrderStatus.Completed =>
                    "✅ Xüsusi məhsul sifarişi tamamlandı.",

                _ => "Xüsusi sifariş statusu dəyişdi."
            };

            await NotifyOrderStatusAsync(
                orderNumber,
                customerMessage);

            await SendAdminOrderActionsAsync(
                chatId,
                orderNumber);

            return;
        }
        if (query.Data == "menu:main")
        {
            _sessionStore.Clear(telegramUserId);
            await SendCustomerMainMenuAsync(chatId);
            return;
        }

        if (query.Data == "profile:edit")
        {
            var session = _sessionStore.GetOrCreate(
                telegramUserId);

            session.CheckoutStep =
                CheckoutStep.AwaitingPhone;

            session.PhoneNumber = null;
            session.DeliveryAddress = null;
            session.ContinueToCheckout = false;

            var contactKeyboard = new ReplyKeyboardMarkup(
                new[]
                {
            new[]
            {
                new KeyboardButton("📱 Nömrəni paylaş")
                {
                    RequestContact = true
                }
            }
                })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await _botClient.SendMessage(
                chatId,
                "Yeni telefon nömrənizi paylaşın və ya mətnlə yazın.",
                replyMarkup: contactKeyboard);

            return;
        }

        if (query.Data == "cart:continue")
        {
            await SendProductsPageAsync(
                telegramUserId,
                chatId,
                pageNumber: 1);

            return;
        }

    }

    private async Task HandleStartAsync(Message message)
    {
        var user = message.From!;

        if (user.Id == _telegramOptions.AdminUserId)
        {
            await _staffService.EnsureAdminAsync(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Username);

            await SendAdminMainMenuAsync(message.Chat.Id);
            return;
        }

        var staff = await _staffService.GetByTelegramUserIdAsync(user.Id);
        if (staff?.Role == StaffRole.Worker)
        {
            if (!staff.IsActive)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "İşçi hesabınız hazırda passivdir. Adminlə əlaqə saxlayın.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_telegramOptions.MiniAppUrl))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "İşçi paneli hələ yayımlanmayıb.");
                return;
            }

            await _botClient.SendMessage(
                message.Chat.Id,
                "Tapşırıqlarınızı Mini App panelindən idarə edin.",
                replyMarkup: BuildMiniAppKeyboard("👷 İşçi panelini aç"));
            return;
        }

        await _customerService.CreateOrUpdateAsync(
            new UpsertCustomerRequest(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Username));

        await SendCustomerMainMenuAsync(message.Chat.Id);
    }
    private async Task SendProductsPageAsync(
    long telegramUserId,
    long chatId,
    int pageNumber)
    {
        var customer =
            await _customerService.GetByTelegramUserIdAsync(
                telegramUserId)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        var result = await _productService.GetActivePageAsync(
            pageNumber,
            customer.ProductPageSize);

        if (result.Items.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "Hazırda aktiv məhsul yoxdur.",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData(
                        "🏠 Ana menyu",
                        "menu:main")));

            return;
        }

        var text = new StringBuilder();

        text.AppendLine(
            $"🛍 Məhsullar — səhifə {result.PageNumber}/{result.TotalPages}");

        text.AppendLine();

        for (var index = 0; index < result.Items.Count; index++)
        {
            var product = result.Items[index];

            text.AppendLine(
                $"{index + 1}. {product.Name}");

            text.AppendLine(
                $"   💰 {product.Price:0.00} AZN");
        }

        var rows = new List<InlineKeyboardButton[]>();

        rows.AddRange(
            result.Items
                .Select((product, index) =>
                    InlineKeyboardButton.WithCallbackData(
                        (index + 1).ToString(),
                        $"product:view:{product.Id}"))
                .Chunk(5)
                .Select(chunk => chunk.ToArray()));

        var navigationRow = new List<InlineKeyboardButton>();

        if (result.PageNumber > 1)
        {
            navigationRow.Add(
                InlineKeyboardButton.WithCallbackData(
                    "⬅️ Əvvəlki",
                    $"products:page:{result.PageNumber - 1}"));
        }

        if (result.PageNumber < result.TotalPages)
        {
            navigationRow.Add(
                InlineKeyboardButton.WithCallbackData(
                    "Növbəti ➡️",
                    $"products:page:{result.PageNumber + 1}"));
        }

        if (navigationRow.Count > 0)
            rows.Add(navigationRow.ToArray());

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "5 göstər",
            "products:size:5"),

        InlineKeyboardButton.WithCallbackData(
            "10 göstər",
            "products:size:10"),

        InlineKeyboardButton.WithCallbackData(
            "20 göstər",
            "products:size:20")
        ]);

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "🏠 Ana menyu",
                "menu:main")
        ]);

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task SendCartAsync(
    long telegramUserId,
    long chatId)
    {
        var cart = await _cartService.GetAsync(telegramUserId);

        if (cart.Items.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "🛒 Səbətiniz boşdur.",
                replyMarkup: new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "🛍 Məhsullara bax",
                                "menu:products")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "🏠 Ana menyu",
                                "menu:main")
                        }
                    }));

            return;
        }

        var text = new StringBuilder();

        text.AppendLine("🛒 Səbətiniz");
        text.AppendLine();

        foreach (var item in cart.Items)
        {
            text.AppendLine(
                $"• {item.ProductName}");

            text.AppendLine(
                $"  {item.RequestedQuantity} × " +
                $"{item.UnitPrice:0.00} AZN = " +
                $"{item.LineTotal:0.00} AZN");

        }

        text.AppendLine();
        text.AppendLine(
            $"💰 Məhsulların cəmi: {cart.ProductsTotal:0.00} AZN");

        var rows = new List<InlineKeyboardButton[]>();

        foreach (var item in cart.Items)
        {
            var buttonName = item.ProductName.Length > 25
                ? item.ProductName[..25] + "..."
                : item.ProductName;

            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData(
                $"❌ Sil: {buttonName}",
                $"cart:remove:{item.ProductId}")
            ]);
        }

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "🛍 Alışa davam et",
            "cart:continue"),

        InlineKeyboardButton.WithCallbackData(
            "🗑 Təmizlə",
            "cart:clear")
        ]);

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "✅ Sifarişi tamamla",
            "cart:checkout")
        ]);

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "🏠 Ana menyu",
                "menu:main")
        ]);

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task SendProfileAsync(
        long telegramUserId,
        long chatId)
    {
        var customer =
            await _customerService.GetByTelegramUserIdAsync(
                telegramUserId)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        var phone = customer.PhoneNumber ?? "Əlavə edilməyib";
        var address = customer.DeliveryAddress ?? "Əlavə edilməyib";

        await _botClient.SendMessage(
    chatId,
    $"📍 Məlumatlarım\n\n" +
    $"Ad: {customer.FirstName} {customer.LastName}\n" +
    $"Telefon: {phone}\n" +
    $"Ünvan: {address}\n" +
    $"Səhifə ölçüsü: {customer.ProductPageSize}",
    replyMarkup: new InlineKeyboardMarkup(
        new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "✏️ Telefon və ünvanı dəyiş",
                    "profile:edit")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 Ana menyu",
                    "menu:main")
            }
        }));
    }
    private async Task SendProductDetailAsync(
    long chatId,
    Guid productId,
    int quantity)
    {
        var product = await _productService.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException(
                "Product was not found.");

        var text = BuildProductDetailText(product, quantity);
        var keyboard = BuildProductKeyboard(product, quantity);

        if (!string.IsNullOrWhiteSpace(product.ImageReference))
        {
            await _botClient.SendPhoto(
                chatId: chatId,
                photo: product.ImageReference,
                caption: text,
                replyMarkup: keyboard);

            return;
        }

        await _botClient.SendMessage(
            chatId,
            text,
            replyMarkup: keyboard);
    }

    private async Task UpdateProductDetailAsync(
        Message message,
        Guid productId,
        int quantity)
    {
        var product = await _productService.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException(
                "Product was not found.");

        var text = BuildProductDetailText(product, quantity);
        var keyboard = BuildProductKeyboard(product, quantity);

        if (message.Photo is { Length: > 0 })
        {
            await _botClient.EditMessageCaption(
                chatId: message.Chat.Id,
                messageId: message.Id,
                caption: text,
                replyMarkup: keyboard);

            return;
        }

        await _botClient.EditMessageText(
            chatId: message.Chat.Id,
            messageId: message.Id,
            text: text,
            replyMarkup: keyboard);
    }

    private static string BuildProductDetailText(
        ProductDto product,
        int quantity)
    {
        var description = product.Description.Length > 600
            ? product.Description[..600] + "..."
            : product.Description;

        var text = new StringBuilder();

        text.AppendLine($"📦 {product.Name}");
        text.AppendLine();
        text.AppendLine($"Kod: {product.Code}");
        text.AppendLine($"Qiymət: {product.Price:0.00} AZN");
        text.AppendLine($"Miqdar: {quantity}");
        text.AppendLine($"Cəmi: {product.Price * quantity:0.00} AZN");

        text.AppendLine();
        text.AppendLine(description);

        return text.ToString();
    }

    private static InlineKeyboardMarkup BuildProductKeyboard(
        ProductDto product,
        int quantity)
    {
        var rows = new List<InlineKeyboardButton[]>
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "➖",
                $"product:qty:{product.Id}:{Math.Max(1, quantity - 1)}"),

            InlineKeyboardButton.WithCallbackData(
                $"✏️ {quantity}",
                $"product:quantity:{product.Id}"),

            InlineKeyboardButton.WithCallbackData(
                "➕",
                $"product:qty:{product.Id}:{Math.Min(99, quantity + 1)}")
        }
    };

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                $"🛒 {quantity} ədəd səbətə əlavə et",
                $"cart:add:{product.Id}:{quantity}:p")
        ]);

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "⬅️ Məhsullara qayıt",
            "menu:products")
        ]);

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "🏠 Ana menyu",
                "menu:main")
        ]);

        return new InlineKeyboardMarkup(rows);
    }

    private async Task BeginCheckoutAsync(
    long telegramUserId,
    long chatId)
    {
        var customer =
            await _customerService.GetByTelegramUserIdAsync(
                telegramUserId)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
        {
            var session = _sessionStore.GetOrCreate(telegramUserId);

            session.CheckoutStep = CheckoutStep.AwaitingPhone;
            session.PhoneNumber = null;
            session.DeliveryAddress = null;
            session.ContinueToCheckout = true;

            var contactKeyboard = new ReplyKeyboardMarkup(
                new[]
                {
            new[]
            {
                new KeyboardButton("📱 Nömrəni paylaş")
                {
                    RequestContact = true
                }
            }
                })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };

            await _botClient.SendMessage(
                chatId,
                "Sifariş üçün telefon nömrənizi paylaşın " +
                "və ya +994501234567 formatında yazın.",
                replyMarkup: contactKeyboard);

            return;
        }

        await ShowAddressSelectionAsync(telegramUserId, chatId);
    }
    private async Task<bool> TryHandleCustomerInputAsync(Message message)
    {
        var telegramUserId = message.From!.Id;

        if (!_sessionStore.TryGet(telegramUserId, out var session) ||
            session is null ||
            session.CustomerInputStep == CustomerInputStep.None)
        {
            return false;
        }

        if (session.CustomerInputStep == CustomerInputStep.AwaitingProductQuantity)
        {
            if (session.CustomerProductId is null)
            {
                _sessionStore.Clear(telegramUserId);
                return false;
            }

            if (!int.TryParse(message.Text?.Trim(), out var quantity) ||
                quantity is < 1 or > 99)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Miqdarı 1–99 aralığında tam rəqəm kimi yazın.");
                return true;
            }

            var productId = session.CustomerProductId.Value;
            _sessionStore.Clear(telegramUserId);
            await SendProductDetailAsync(message.Chat.Id, productId, quantity);
            return true;
        }

        return false;
    }

    private async Task<bool> TryHandleCheckoutMessageAsync(
    Message message)
    {
        var telegramUserId = message.From!.Id;

        if (!_sessionStore.TryGet(
                telegramUserId,
                out var session) ||
            session is null)
        {
            return false;
        }

        if (session.CheckoutStep == CheckoutStep.AwaitingPhone)
        {
            string? phoneNumber = null;

            if (message.Contact is not null)
            {
                if (message.Contact.UserId.HasValue &&
                    message.Contact.UserId.Value != telegramUserId)
                {
                    await _botClient.SendMessage(
                        message.Chat.Id,
                        "Yalnız öz telefon nömrənizi paylaşa bilərsiniz.");

                    return true;
                }

                phoneNumber = message.Contact.PhoneNumber;
            }
            else if (!string.IsNullOrWhiteSpace(message.Text))
            {
                phoneNumber = message.Text.Trim();
            }

            if (string.IsNullOrWhiteSpace(phoneNumber) ||
                phoneNumber.Count(char.IsDigit) < 9)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Telefon nömrəsini düzgün formatda yazın.\n" +
                    "Məsələn: +994501234567");

                return true;
            }

            session.PhoneNumber = phoneNumber;

            if (session.ContinueToCheckout)
            {
                var customer = await _customerService.GetByTelegramUserIdAsync(
                    telegramUserId)
                    ?? throw new KeyNotFoundException("Customer was not found.");
                var addresses = await _customerAddressService.GetActiveAsync(
                    telegramUserId);
                var contactAddress = addresses.FirstOrDefault();

                if (contactAddress is not null)
                {
                    await _customerService.UpdateContactInformationAsync(
                        telegramUserId,
                        new UpdateContactInformationRequest(
                            phoneNumber,
                            contactAddress.AddressLine,
                            contactAddress.Latitude,
                            contactAddress.Longitude));
                    await _botClient.SendMessage(
                        message.Chat.Id,
                        "✅ Telefon nömrəsi saxlanıldı.",
                        replyMarkup: new ReplyKeyboardRemove());
                    await ShowAddressSelectionAsync(
                        telegramUserId,
                        message.Chat.Id);
                    return true;
                }

                session.CheckoutStep = CheckoutStep.AwaitingNewAddress;
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "📍 Çatdırılma ünvanını mətnlə yazın.",
                    replyMarkup: new ReplyKeyboardRemove());
                return true;
            }

            session.CheckoutStep = CheckoutStep.AwaitingAddress;

            await _botClient.SendMessage(
                message.Chat.Id,
                "📍 Çatdırılma ünvanını mətnlə yazın.",
                replyMarkup: new ReplyKeyboardRemove());

            return true;
        }

        if (session.CheckoutStep == CheckoutStep.AwaitingNewAddress)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Ünvanı mətn şəklində yazın.");
                return true;
            }

            session.DeliveryAddress = message.Text.Trim();
            session.CheckoutStep = CheckoutStep.AwaitingNewLocation;
            await SendLocationRequestAsync(
                message.Chat.Id,
                "Yeni ünvan üçün lokasiyanı paylaşın və ya lokasiyasız davam edin.");
            return true;
        }

        if (session.CheckoutStep == CheckoutStep.AwaitingNewLocation)
        {
            var skipLocation = message.Text?.Equals(
                "Lokasiyasız davam et",
                StringComparison.OrdinalIgnoreCase) == true;

            if (message.Location is null && !skipLocation)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Lokasiyanı paylaşın və ya “Lokasiyasız davam et” seçin.");
                return true;
            }

            var customer = await _customerService.GetByTelegramUserIdAsync(
                telegramUserId)
                ?? throw new KeyNotFoundException("Customer was not found.");
            var phoneNumber = session.PhoneNumber ?? customer.PhoneNumber
                ?? throw new InvalidOperationException("Phone number is required.");
            var addressLine = session.DeliveryAddress
                ?? throw new InvalidOperationException("Delivery address is required.");

            var address = await _customerAddressService.CreateAsync(
                telegramUserId,
                new CreateCustomerAddressRequest(
                    addressLine,
                    message.Location?.Latitude,
                    message.Location?.Longitude));

            await _customerService.UpdateContactInformationAsync(
                telegramUserId,
                new UpdateContactInformationRequest(
                    phoneNumber,
                    address.AddressLine,
                    address.Latitude,
                    address.Longitude));

            session.CustomerAddressId = address.Id;
            session.DeliveryCompanyId = null;
            session.CheckoutStep = CheckoutStep.SelectingDeliveryCompany;

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Yeni ünvan saxlanıldı.",
                replyMarkup: new ReplyKeyboardRemove());
            await ShowDeliveryCompanySelectionAsync(
                telegramUserId,
                message.Chat.Id);
            return true;
        }

        if (session.CheckoutStep == CheckoutStep.AwaitingAddress)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Ünvanı mətn şəklində yazın.");

                return true;
            }

            session.DeliveryAddress = message.Text.Trim();
            session.CheckoutStep =
                CheckoutStep.AwaitingLocation;

            await SendLocationRequestAsync(
                message.Chat.Id,
                "İstəsəniz dəqiq lokasiyanı paylaşın.");

            return true;
        }

        if (session.CheckoutStep == CheckoutStep.AwaitingLocation)
        {
            var skipLocation =
                message.Text?.Equals(
                    "Lokasiyasız davam et",
                    StringComparison.OrdinalIgnoreCase) == true;

            if (message.Location is null && !skipLocation)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Lokasiyanı paylaşın və ya " +
                    "“Lokasiyasız davam et” seçin.");

                return true;
            }

            var continueToCheckout =
                session.ContinueToCheckout;

            await _customerService.UpdateContactInformationAsync(
                telegramUserId,
                new UpdateContactInformationRequest(
                    session.PhoneNumber!,
                    session.DeliveryAddress!,
                    message.Location?.Latitude,
                    message.Location?.Longitude));

            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Əlaqə və ünvan məlumatları saxlanıldı.",
                replyMarkup: new ReplyKeyboardRemove());

            if (continueToCheckout)
            {
                await ShowAddressSelectionAsync(telegramUserId, message.Chat.Id);
            }
            else
            {
                await SendProfileAsync(
                    telegramUserId,
                    message.Chat.Id);
            }

            return true;
        }

        return false;
    }
    private async Task ShowAddressSelectionAsync(
        long telegramUserId,
        long chatId)
    {
        var addresses = await _customerAddressService.GetActiveAsync(
            telegramUserId);
        var session = _sessionStore.GetOrCreate(telegramUserId);
        session.CheckoutStep = CheckoutStep.SelectingAddress;
        session.CustomerAddressId = null;
        session.DeliveryCompanyId = null;

        if (addresses.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "Saxlanmış aktiv ünvanınız yoxdur. Yeni ünvan əlavə edin.");
            await PromptForNewAddressAsync(telegramUserId, chatId);
            return;
        }

        var rows = addresses
            .Select(address => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"📍 {Truncate(address.AddressLine, 45)}",
                    $"checkout:address:{address.Id}")
            })
            .ToList();

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "➕ Yeni ünvan əlavə et",
                "checkout:new-address")
        ]);
        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "❌ İmtina et",
                "checkout:cancel")
        ]);

        await _botClient.SendMessage(
            chatId,
            "Sifariş üçün çatdırılma ünvanını seçin:",
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task PromptForNewAddressAsync(
        long telegramUserId,
        long chatId)
    {
        var session = _sessionStore.GetOrCreate(telegramUserId);
        session.CheckoutStep = CheckoutStep.AwaitingNewAddress;
        session.DeliveryAddress = null;
        session.CustomerAddressId = null;
        session.DeliveryCompanyId = null;

        await _botClient.SendMessage(
            chatId,
            "📍 Yeni çatdırılma ünvanını mətnlə yazın.\n" +
            "Ləğv etmək üçün /cancel yazın.",
            replyMarkup: new ReplyKeyboardRemove());
    }

    private async Task ShowDeliveryCompanySelectionAsync(
        long telegramUserId,
        long chatId)
    {
        var companies = await _deliveryCompanyService.GetActiveAsync();
        var session = _sessionStore.GetOrCreate(telegramUserId);

        if (session.CustomerAddressId is null)
        {
            await ShowAddressSelectionAsync(telegramUserId, chatId);
            return;
        }

        session.CheckoutStep = CheckoutStep.SelectingDeliveryCompany;
        session.DeliveryCompanyId = null;

        if (companies.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "Hazırda aktiv çatdırılma şirkəti yoxdur. Sonra yenidən cəhd edin.",
                replyMarkup: new InlineKeyboardMarkup(
                [
                    [InlineKeyboardButton.WithCallbackData(
                        "⬅️ Ünvan seçiminə qayıt",
                        "checkout:back-address")],
                    [InlineKeyboardButton.WithCallbackData(
                        "❌ İmtina et",
                        "checkout:cancel")]
                ]));
            return;
        }

        var rows = companies
            .Select(company => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"🚚 {Truncate(company.Name, 48)}",
                    $"checkout:company:{company.Id}")
            })
            .ToList();
        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "⬅️ Ünvan seçiminə qayıt",
                "checkout:back-address")
        ]);
        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "❌ İmtina et",
                "checkout:cancel")
        ]);

        await _botClient.SendMessage(
            chatId,
            "Çatdırılma şirkətini seçin:",
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task ShowCheckoutConfirmationAsync(
        long telegramUserId,
        long chatId)
    {
        var customer =
            await _customerService.GetByTelegramUserIdAsync(
                telegramUserId)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        var cart = await _cartService.GetAsync(telegramUserId);
        if (!_sessionStore.TryGet(telegramUserId, out var session) ||
            session?.CustomerAddressId is not Guid addressId ||
            session.DeliveryCompanyId is not Guid companyId)
        {
            await ShowAddressSelectionAsync(telegramUserId, chatId);
            return;
        }

        var address = (await _customerAddressService.GetActiveAsync(
                telegramUserId))
            .FirstOrDefault(item => item.Id == addressId);
        if (address is null)
        {
            await _botClient.SendMessage(
                chatId,
                "Seçilmiş ünvan artıq aktiv deyil.");
            await ShowAddressSelectionAsync(telegramUserId, chatId);
            return;
        }

        var company = (await _deliveryCompanyService.GetActiveAsync())
            .FirstOrDefault(item => item.Id == companyId);
        if (company is null)
        {
            await _botClient.SendMessage(
                chatId,
                "Seçilmiş çatdırılma şirkəti artıq aktiv deyil.");
            await ShowDeliveryCompanySelectionAsync(telegramUserId, chatId);
            return;
        }

        session.CheckoutStep = CheckoutStep.AwaitingConfirmation;

        await _botClient.SendMessage(
            chatId,
            $"Sifarişi təsdiqləyirsiniz?\n\n" +
            $"Məbləğ: {cart.ProductsTotal:0.00} AZN\n" +
            $"Telefon: {customer.PhoneNumber}\n" +
            $"Ünvan: {address.AddressLine}\n" +
            $"Çatdırılma şirkəti: {company.Name}\n\n" +
            "Çatdırılma haqqı adminlə razılaşdırılacaq.",
            replyMarkup: new InlineKeyboardMarkup(
                new[]
                {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "✅ Təsdiqlə",
                        "order:confirm-checkout"),

                    InlineKeyboardButton.WithCallbackData(
                        "⬅️ Dəyiş",
                        "checkout:back-address")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "❌ İmtina et",
                        "checkout:cancel")
                }
                }));
    }

    private async Task SendLocationRequestAsync(long chatId, string text)
    {
        var locationKeyboard = new ReplyKeyboardMarkup(
        [
            [new KeyboardButton("📍 Lokasiyanı paylaş") { RequestLocation = true }],
            [new KeyboardButton("Lokasiyasız davam et")]
        ])
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await _botClient.SendMessage(
            chatId,
            text,
            replyMarkup: locationKeyboard);
    }

    private static string Truncate(string value, int maximumLength)
    {
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : $"{normalized[..(maximumLength - 1)]}…";
    }
    private async Task SendNewOrderToAdminAsync(OrderDto order)
    {
        var customer = await _customerService.GetByIdAsync(order.CustomerId);
        var customerFullName = customer is null
            ? "Naməlum müştəri"
            : string.Join(
                " ",
                new[] { customer.FirstName, customer.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value)));

        var text = new StringBuilder();

        text.AppendLine($"🆕 Yeni sifariş #{order.OrderNumber}");
        text.AppendLine($"👤 Müştəri: {customerFullName}");
        text.AppendLine();
        text.AppendLine($"Telefon: {order.PhoneNumber}");
        text.AppendLine($"Ünvan: {order.DeliveryAddress}");
        text.AppendLine();

        foreach (var item in order.Items)
        {
            text.AppendLine(
                $"• {item.ProductName}");

            text.AppendLine(
                $"  {item.TotalQuantity} × " +
                $"{item.UnitPrice:0.00} AZN = " +
                $"{item.LineTotal:0.00} AZN");

            if (item.SpecialOrderQuantity > 0)
            {
                text.AppendLine(
                    $"  Stokdan: {item.FromStockQuantity}, " +
                    $"sifarişlə: {item.SpecialOrderQuantity}");
            }
        }

        text.AppendLine();
        text.AppendLine(
            $"Məhsulların cəmi: {order.ProductsTotal:0.00} AZN");

        await _botClient.SendMessage(
            _telegramOptions.AdminUserId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(
                new[]
                {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        "✅ Təsdiqlə",
                        $"admin:confirm:{order.OrderNumber}"),

                    InlineKeyboardButton.WithCallbackData(
                        "❌ Ləğv et",
                        $"admin:cancel:{order.OrderNumber}")
                }
                }));
    }
    private async Task NotifyOrderStatusAsync(
    long orderNumber,
    string statusMessage)
    {
        var order =
            await _orderService.GetByOrderNumberAsync(orderNumber)
            ?? throw new KeyNotFoundException(
                "Order was not found.");

        var customer =
            await _customerService.GetByIdAsync(order.CustomerId)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        await _botClient.SendMessage(
            customer.TelegramUserId,
            $"{statusMessage}\n\nSifariş: #{orderNumber}");
    }

    private async Task SendAdminOrderActionsAsync(
    long chatId,
    long orderNumber)
    {
        var order =
            await _orderService.GetByOrderNumberAsync(orderNumber)
            ?? throw new KeyNotFoundException(
                "Order was not found.");

        var customer =
            await _customerService.GetByIdAsync(order.CustomerId)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        var customerOrders =
            await _orderService.GetCustomerOrdersAsync(
                customer.TelegramUserId);

        var allOtherOrders = customerOrders
            .Where(customerOrder =>
                customerOrder.OrderNumber != order.OrderNumber)
            .ToList();

        var otherOrders = allOtherOrders.Take(5).ToList();

        var text = new StringBuilder();
        var activeAssignment = await _assignmentService.GetOrderAssignmentAsync(orderNumber);
        var shipments = await _shipmentService.GetOrderShipmentsAsync(orderNumber);
        var hasActiveShipment = shipments.Any(shipment =>
            shipment.Status is not ShipmentStatus.Delivered and not ShipmentStatus.Cancelled);

        text.AppendLine($"📦 Sifariş #{order.OrderNumber}");
        text.AppendLine(
            $"Status: {GetOrderStatusText(order.Status)}");
        text.AppendLine(
            $"Çatdırılma: {GetDeliveryModeText(order.DeliveryMode)}");
        text.AppendLine();

        var fullName = string.Join(
            " ",
            new[] { customer.FirstName, customer.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        text.AppendLine($"👤 Müştəri: {fullName}");

        if (!string.IsNullOrWhiteSpace(customer.Username))
            text.AppendLine($"Telegram: @{customer.Username}");

        text.AppendLine($"Telefon: {order.PhoneNumber}");
        text.AppendLine($"Ünvan: {order.DeliveryAddress}");
        text.AppendLine(
            $"Digər sifarişlər: {allOtherOrders.Count}");
        text.AppendLine(activeAssignment is null
            ? "İşçi: təyin edilməyib"
            : $"İşçi: {activeAssignment.StaffName}");
        text.AppendLine();

        foreach (var item in order.Items)
        {
            text.AppendLine(
                $"• {item.ProductName} — {item.TotalQuantity} ədəd");

            text.AppendLine(
                $"  Hazır: {item.ReadyQuantity} | " +
                $"Çatdırılıb: {item.DeliveredQuantity} | " +
                $"Qalıb: {item.RemainingQuantity}");

            if (item.SpecialOrderQuantity > 0)
            {
                text.AppendLine(
                    $"  Stokdan: {item.FromStockQuantity}");

                text.AppendLine(
                    $"  Sifarişlə: {item.SpecialOrderQuantity}");

                text.AppendLine(
                    $"  Xüsusi status: " +
                    $"{GetSpecialOrderStatusText(item.SpecialOrderStatus)}");
            }
        }

        var rows = new List<InlineKeyboardButton[]>();

        if (order.Status is not OrderStatus.PendingAdminApproval and
            not OrderStatus.Completed and not OrderStatus.Cancelled)
        {
            if (!hasActiveShipment)
            {
                rows.Add(
                [
                    InlineKeyboardButton.WithCallbackData(
                        "👷 İşçi təyin et / dəyiş",
                        $"admin:aw:{orderNumber}")
                ]);
                if (activeAssignment is not null)
                {
                    rows.Add(
                    [
                        InlineKeyboardButton.WithCallbackData(
                            "🚫 İşçi təyinatını ləğv et",
                            $"admin:unassign:{orderNumber}")
                    ]);
                }
            }
            if (activeAssignment is not null)
            {
                rows.Add(
                [
                    InlineKeyboardButton.WithCallbackData(
                        "📦 Göndəriş yarat",
                        $"admin:ship:new:{orderNumber}")
                ]);
            }
            if (shipments.Count > 0)
            {
                rows.Add(
                [
                    InlineKeyboardButton.WithCallbackData(
                        "🚚 Göndərişlər",
                        $"admin:ships:{orderNumber}")
                ]);
            }
        }

        if (order.ContainsSpecialOrder &&
            order.Items.All(item => item.DeliveredQuantity == 0) &&
            order.Status is OrderStatus.Confirmed or OrderStatus.Preparing)
        {
            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData(
                    "⏳ Hamısını birlikdə",
                    $"admin:delivery-mode:all:{orderNumber}"),
                InlineKeyboardButton.WithCallbackData(
                    "🚚 Hissə-hissə",
                    $"admin:delivery-mode:partial:{orderNumber}")
            ]);
        }

        foreach (var item in order.Items.Where(
                     item => item.SpecialOrderQuantity > 0))
        {
            var button = item.SpecialOrderStatus switch
            {
                SpecialOrderStatus.Pending =>
                    InlineKeyboardButton.WithCallbackData(
                        $"📤 Sifariş et: {item.ProductName}",
                        $"admin:sp:ordered:{orderNumber}:{item.Id}"),

                SpecialOrderStatus.OrderedFromSupplier =>
                    InlineKeyboardButton.WithCallbackData(
                        $"📥 Məhsul gəldi: {item.ProductName}",
                        $"admin:sp:arrived:{orderNumber}:{item.Id}"),

                SpecialOrderStatus.Arrived =>
                    InlineKeyboardButton.WithCallbackData(
                        $"✅ Xüsusi sifarişi tamamla: {item.ProductName}",
                        $"admin:sp:completed:{orderNumber}:{item.Id}"),

                _ => null
            };

            if (button is not null)
                rows.Add([button]);
        }

        if (!hasActiveShipment)
        {
            switch (order.Status)
            {
                case OrderStatus.Confirmed:
                    rows.Add(
                    [
                        InlineKeyboardButton.WithCallbackData(
                        "📦 Hazırlanır",
                        $"admin:preparing:{orderNumber}"),

                    InlineKeyboardButton.WithCallbackData(
                            "❌ Ləğv et",
                            $"admin:cancel:{orderNumber}")
                    ]);
                    break;

                case OrderStatus.Preparing:
                    if (order.FulfillmentStatus != FulfillmentStatus.PartiallyDelivered)
                    {
                        rows.Add(
                        [
                            InlineKeyboardButton.WithCallbackData(
                                "❌ Ləğv et",
                                $"admin:cancel:{orderNumber}")
                        ]);
                    }
                    break;
            }
        }

        if (!hasActiveShipment &&
            order.ReadyQuantity > 0 &&
            (order.ReadyQuantity == order.RemainingQuantity ||
             order.DeliveryMode == DeliveryMode.PartialAllowed) &&
            order.Status is not OrderStatus.PendingAdminApproval &&
            order.Status is not OrderStatus.Completed &&
            order.Status is not OrderStatus.Cancelled)
        {
            var label = order.ReadyQuantity == order.RemainingQuantity
                ? "🚚 Hamısını çatdır"
                : $"🚚 Hazır olan {order.ReadyQuantity} ədədi çatdır";

            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData(
                    label,
                    $"admin:deliver-ready:{orderNumber}")
            ]);
        }

        if (!hasActiveShipment &&
            order.RemainingQuantity > 0 &&
            order.FulfillmentStatus == FulfillmentStatus.PartiallyDelivered)
        {
            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData(
                    "❌ Qalan hissəni ləğv et",
                    $"admin:cancel-remaining:{orderNumber}")
            ]);
        }

        if (otherOrders.Count > 0)
        {
            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData(
                    "📜 Müştərinin digər sifarişləri",
                    $"admin:customer-orders:{orderNumber}")
            ]);
        }

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "⬅️ Admin menyusu",
            "admin:menu")
        ]);

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task SendCustomerOrdersAsync(
    long telegramUserId,
    long chatId)
    {
        var orders =
            await _orderService.GetCustomerOrdersAsync(
                telegramUserId);

        if (orders.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "📦 Hələ sifarişiniz yoxdur.",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData(
                        "🏠 Ana menyu",
                        "menu:main")));

            return;
        }

        var text = new StringBuilder();

        text.AppendLine("📦 Son sifarişləriniz");
        text.AppendLine();

        foreach (var order in orders)
        {
            text.AppendLine(
                $"#{order.OrderNumber} — " +
                $"{GetOrderStatusText(order.Status)} — " +
                $"{order.TotalAmount:0.00} AZN");
        }

        var buttons = orders
            .Select(order =>
                new[]
                {
                InlineKeyboardButton.WithCallbackData(
                    $"#{order.OrderNumber}",
                    $"order:view:{order.OrderNumber}")
                })
            .ToList();

        buttons.Add(
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 Ana menyu",
                    "menu:main")
            });

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(buttons));
    }

    private async Task SendCustomerOrderDetailAsync(
        long telegramUserId,
        long chatId,
        long orderNumber)
    {
        var order =
            await _orderService.GetByOrderNumberAsync(orderNumber)
            ?? throw new KeyNotFoundException(
                "Order was not found.");

        var customer =
            await _customerService.GetByTelegramUserIdAsync(
                telegramUserId)
            ?? throw new KeyNotFoundException(
                "Customer was not found.");

        if (order.CustomerId != customer.Id)
        {
            await _botClient.SendMessage(
                chatId,
                "Bu sifariş sizə aid deyil.");

            return;
        }

        var text = new StringBuilder();

        text.AppendLine($"📦 Sifariş #{order.OrderNumber}");
        text.AppendLine(
            $"Status: {GetOrderStatusText(order.Status)}");
        text.AppendLine();

        foreach (var item in order.Items)
        {
            text.AppendLine(
                $"• {item.ProductName}");

            text.AppendLine(
                $"  {item.TotalQuantity} × " +
                $"{item.UnitPrice:0.00} = " +
                $"{item.LineTotal:0.00} AZN");

            if (item.SpecialOrderQuantity > 0)
            {
                text.AppendLine(
                    $"  Çatdırılıb: {item.DeliveredQuantity}, " +
                    $"qalıb: {item.RemainingQuantity}");
            }
        }

        text.AppendLine();
        text.AppendLine(
            $"Məhsullar: {order.ProductsTotal:0.00} AZN");

        text.AppendLine(
            order.DeliveryFee.HasValue
                ? $"Çatdırılma: {order.DeliveryFee:0.00} AZN"
                : "Çatdırılma: adminlə razılaşdırılacaq");

        text.AppendLine(
            $"Ümumi: {order.TotalAmount:0.00} AZN");

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "⬅️ Sifarişlərim",
                            "menu:orders")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(
                            "🏠 Ana menyu",
                            "menu:main")
                    }
                }));
    }

    private static string GetOrderStatusText(
        OrderStatus status)
    {
        return status switch
        {
            OrderStatus.PendingAdminApproval =>
                "Admin təsdiqi gözlənilir",

            OrderStatus.Confirmed =>
                "Təsdiqləndi",

            OrderStatus.Preparing =>
                "Hazırlanır",

            OrderStatus.Delivering =>
                "Çatdırılır",

            OrderStatus.Completed =>
                "Tamamlandı",

            OrderStatus.Cancelled =>
                "Ləğv edildi",

            _ => status.ToString()
        };
    }

    private static string GetDeliveryModeText(DeliveryMode deliveryMode)
    {
        return deliveryMode switch
        {
            DeliveryMode.AllTogether => "Hamısı birlikdə",
            DeliveryMode.PartialAllowed => "Hissə-hissə",
            _ => "Admin qərarı gözlənilir"
        };
    }
    private async Task SendCustomerMainMenuAsync(long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🛍 Məhsullar",
                    "menu:products"),

                InlineKeyboardButton.WithCallbackData(
                    "🛒 Səbətim",
                    "menu:cart")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "📦 Sifarişlərim",
                    "menu:orders"),

                InlineKeyboardButton.WithCallbackData(
                    "📍 Məlumatlarım",
                    "menu:profile")
            }
            });

        await _botClient.SendMessage(
            chatId,
            "StockMate Bot-a xoş gəlmisiniz!\n\n" +
            "Məhsullara baxa, səbət yarada və sifariş verə bilərsiniz.",
            replyMarkup: keyboard);
    }
    private async Task SendAdminMainMenuAsync(long chatId)
    {
        var keyboard = new InlineKeyboardMarkup(
            new[]
            {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🆕 Yeni sifarişlər",
                    "admin:orders:new"),

                InlineKeyboardButton.WithCallbackData(
                    "📦 Aktiv sifarişlər",
                    "admin:orders:active")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🚚 Xüsusi sifarişlər",
                    "admin:orders:special"),

                InlineKeyboardButton.WithCallbackData(
                    "📜 Tarixçə",
                    "admin:orders:history")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🛍 Məhsullar",
                    "admin:products"),

                InlineKeyboardButton.WithCallbackData(
                    "📊 Stok",
                    "admin:stock")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "👷 İşçilər",
                    "admin:staff")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "👤 Müştəri menyusu",
                    "admin:customer-menu")
            }
            });

        await _botClient.SendMessage(
            chatId,
            "👨‍💼 StockMate Admin Panel",
            replyMarkup: keyboard);

        if (!string.IsNullOrWhiteSpace(_telegramOptions.MiniAppUrl))
        {
            await _botClient.SendMessage(
                chatId,
                "Mini App-da bütün idarəetmə bölmələrini aça bilərsiniz.",
                replyMarkup: BuildMiniAppKeyboard("📱 İdarəetmə panelini aç"));
        }
    }

    private InlineKeyboardMarkup BuildMiniAppKeyboard(string text)
    {
        return new InlineKeyboardMarkup(
            InlineKeyboardButton.WithWebApp(
                text,
                new WebAppInfo { Url = _telegramOptions.MiniAppUrl!.Trim() }));
    }
    private async Task SendAdminOrderListAsync(
    long chatId,
    string title,
    IReadOnlyList<OrderDto> orders)
    {
        if (orders.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                $"{title}\n\nSifariş yoxdur.",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData(
                        "⬅️ Admin menyusu",
                        "admin:menu")));

            return;
        }

        var text = new StringBuilder();

        text.AppendLine(title);
        text.AppendLine();

        foreach (var order in orders)
        {
            text.AppendLine(
                $"#{order.OrderNumber} — " +
                $"{GetOrderStatusText(order.Status)} — " +
                $"{order.TotalAmount:0.00} AZN");
        }

        var rows = orders
            .Select(order =>
                new[]
                {
                InlineKeyboardButton.WithCallbackData(
                    $"#{order.OrderNumber}",
                    $"admin:actions:{order.OrderNumber}")
                })
            .ToList();

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "⬅️ Admin menyusu",
            "admin:menu")
        ]);

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(rows));
    }
    private async Task SendAdminProductsPageAsync(
    long chatId,
    int pageNumber)
    {
        var result = await _productService.GetAdminPageAsync(
            pageNumber,
            pageSize: 5,
            search: null);

        if (result.Items.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "Məhsul yoxdur.",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData(
                        "➕ Yeni məhsul",
                        "admin:product:add")));

            return;
        }

        var text = new StringBuilder();

        text.AppendLine(
            $"🛍 Məhsullar — {result.PageNumber}/{result.TotalPages}");

        text.AppendLine();

        for (var index = 0; index < result.Items.Count; index++)
        {
            var product = result.Items[index];
            var status = product.IsActive ? "Aktiv" : "Passiv";

            text.AppendLine(
                $"{index + 1}. {product.Name}");

            text.AppendLine(
                $"   {product.Code} | " +
                $"{product.Price:0.00} AZN | " +
                $"Stok: {product.StockQuantity} | {status}");
        }

        var rows = result.Items
            .Select((product, index) =>
                InlineKeyboardButton.WithCallbackData(
                    (index + 1).ToString(),
                    $"admin:product:view:{product.Id}"))
            .Chunk(5)
            .Select(chunk => chunk.ToArray())
            .ToList();

        var navigation = new List<InlineKeyboardButton>();

        if (result.PageNumber > 1)
        {
            navigation.Add(
                InlineKeyboardButton.WithCallbackData(
                    "⬅️",
                    $"admin:products:page:{result.PageNumber - 1}"));
        }

        if (result.PageNumber < result.TotalPages)
        {
            navigation.Add(
                InlineKeyboardButton.WithCallbackData(
                    "➡️",
                    $"admin:products:page:{result.PageNumber + 1}"));
        }

        if (navigation.Count > 0)
            rows.Add(navigation.ToArray());

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "➕ Yeni məhsul",
            "admin:product:add"),

        InlineKeyboardButton.WithCallbackData(
            "⬅️ Admin menyusu",
            "admin:menu")
        ]);

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task SendAdminProductDetailAsync(
        long chatId,
        Guid productId)
    {
        var product = await _productService.GetByIdAsync(productId)
            ?? throw new KeyNotFoundException(
                "Product was not found.");

        var status = product.IsActive ? "Aktiv" : "Passiv";

        var rows = new List<InlineKeyboardButton[]>
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "💰 Qiyməti dəyiş",
                $"admin:price:{product.Id}"),

            InlineKeyboardButton.WithCallbackData(
                "📊 Stoku dəyiş",
                $"admin:stock-edit:{product.Id}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData(
                "📜 Stok tarixçəsi",
                $"admin:product:history:{product.Id}")
        }
    };

        rows.Add(
        [
            product.IsActive
            ? InlineKeyboardButton.WithCallbackData(
                "⛔ Passiv et",
                $"admin:product:deactivate:{product.Id}")
            : InlineKeyboardButton.WithCallbackData(
                "✅ Aktiv et",
                $"admin:product:activate:{product.Id}")
        ]);

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
            "⬅️ Məhsullar",
            "admin:products")
        ]);
        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
                "✏️ Adı dəyiş",
                $"admin:edit-name:{product.Id}"),

            InlineKeyboardButton.WithCallbackData(
                "📝 Açıqlamanı dəyiş",
                $"admin:edit-description:{product.Id}")
        ]);

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData(
        "🖼 Şəkli dəyiş",
        $"admin:edit-image:{product.Id}")
        ]);

        await _botClient.SendMessage(
            chatId,
            $"📦 {product.Name}\n\n" +
            $"Kod: {product.Code}\n" +
            $"Qiymət: {product.Price:0.00} AZN\n" +
            $"Stok: {product.StockQuantity}\n" +
            $"Status: {status}\n\n" +
            product.Description,
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task SendStockHistoryAsync(
        long chatId,
        Guid productId)
    {
        var history =
            await _productService.GetStockHistoryAsync(
                productId);

        if (history.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "Stok tarixçəsi yoxdur.");

            return;
        }

        var text = new StringBuilder();

        text.AppendLine("📜 Son stok dəyişiklikləri");
        text.AppendLine();

        foreach (var movement in history.Take(10))
        {
            var sign = movement.QuantityChange > 0 ? "+" : string.Empty;

            text.AppendLine(
                $"{movement.CreatedAtUtc:dd.MM.yyyy HH:mm} | " +
                $"{sign}{movement.QuantityChange} | " +
                $"{movement.PreviousQuantity} → {movement.NewQuantity}");

            text.AppendLine($"Səbəb: {movement.Reason}");
            text.AppendLine();
        }

        await _botClient.SendMessage(
            chatId,
            text.ToString());
    }
    private async Task<StaffDto> GetRequiredAdminStaffAsync(long telegramUserId)
    {
        var admin = await _staffService.GetByTelegramUserIdAsync(telegramUserId)
            ?? throw new UnauthorizedAccessException("Admin qeydiyyatı tapılmadı.");

        if (admin.Role != StaffRole.Admin || !admin.IsActive)
            throw new UnauthorizedAccessException("Bu əməliyyat yalnız aktiv admin üçündür.");

        return admin;
    }

    private async Task SendAdminStaffListAsync(long chatId)
    {
        var workers = (await _staffService.GetAllAsync())
            .Where(staff => staff.Role == StaffRole.Worker)
            .OrderByDescending(staff => staff.IsActive)
            .ThenBy(staff => staff.FirstName)
            .ToList();

        var rows = workers
            .Select(worker => new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    $"{(worker.IsActive ? "✅" : "⛔")} {GetStaffName(worker)}",
                    $"admin:staff:view:{worker.Id}")
            })
            .ToList();

        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData("➕ İşçi əlavə et", "admin:staff:add")
        ]);
        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData("⬅️ Admin menyusu", "admin:menu")
        ]);

        await _botClient.SendMessage(
            chatId,
            workers.Count == 0 ? "👷 Hələ işçi əlavə edilməyib." : "👷 İşçilər",
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task SendAdminStaffDetailAsync(long chatId, Guid staffId)
    {
        var worker = (await _staffService.GetAllAsync())
            .FirstOrDefault(staff => staff.Id == staffId)
            ?? throw new KeyNotFoundException("İşçi tapılmadı.");
        var assignments = await _assignmentService.GetWorkerAssignmentsAsync(worker.Id);
        var shipments = await _shipmentService.GetWorkerShipmentsAsync(worker.Id);

        var text = new StringBuilder()
            .AppendLine($"👷 {GetStaffName(worker)}")
            .AppendLine($"Status: {(worker.IsActive ? "Aktiv" : "Passiv")}")
            .AppendLine($"Telegram ID: {worker.TelegramUserId}")
            .AppendLine(string.IsNullOrWhiteSpace(worker.Username)
                ? "Username: yoxdur"
                : $"Username: @{worker.Username}")
            .AppendLine($"Aktiv sifarişlər: {assignments.Count}")
            .AppendLine($"Göndərişlər: {shipments.Count}");

        foreach (var assignment in assignments.Take(10))
            text.AppendLine($"• #{assignment.OrderNumber}");

        var toggle = worker.IsActive
            ? InlineKeyboardButton.WithCallbackData("⛔ Passiv et", $"admin:staff:off:{worker.Id}")
            : InlineKeyboardButton.WithCallbackData("✅ Aktiv et", $"admin:staff:on:{worker.Id}");

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(new[]
            {
                new[] { toggle },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ İşçilər", "admin:staff") }
            }));
    }

    private async Task SendWorkerSelectionAsync(long chatId, long orderNumber)
    {
        var workers = (await _staffService.GetAllAsync())
            .Where(staff => staff.Role == StaffRole.Worker && staff.IsActive)
            .OrderBy(staff => staff.FirstName)
            .ToList();

        if (workers.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "Aktiv işçi yoxdur. Əvvəlcə İşçilər menyusundan işçi əlavə edin.");
            return;
        }

        var rows = workers.Select(worker => new[]
        {
            InlineKeyboardButton.WithCallbackData(
                GetStaffName(worker),
                $"admin:a:{orderNumber}:{worker.Id}")
        }).ToList();
        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData("⬅️ Sifariş", $"admin:actions:{orderNumber}")
        ]);

        await _botClient.SendMessage(
            chatId,
            $"#{orderNumber} sifarişi üçün işçi seçin:",
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private async Task SendAdminShipmentsAsync(long chatId, long orderNumber)
    {
        var shipments = await _shipmentService.GetOrderShipmentsAsync(orderNumber);
        if (shipments.Count == 0)
        {
            await _botClient.SendMessage(
                chatId,
                "Bu sifariş üçün göndəriş yoxdur.",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("⬅️ Sifariş", $"admin:actions:{orderNumber}")));
            return;
        }

        foreach (var shipment in shipments)
            await SendAdminShipmentAsync(chatId, shipment);
    }

    private async Task SendAdminShipmentAsync(long chatId, ShipmentDto shipment)
    {
        var text = new StringBuilder()
            .AppendLine($"🚚 Sifariş #{shipment.OrderNumber}")
            .AppendLine($"İşçi: {shipment.StaffName}")
            .AppendLine($"Status: {GetShipmentStatusText(shipment.Status)}")
            .AppendLine($"Ünvan: {shipment.DeliveryAddress}")
            .AppendLine();
        foreach (var item in shipment.Items)
            text.AppendLine($"• {item.ProductName}: {item.Quantity} ədəd");

        var rows = new List<InlineKeyboardButton[]>();
        var nextButton = shipment.Status switch
        {
            ShipmentStatus.Assigned => InlineKeyboardButton.WithCallbackData(
                "📦 Hazırlanır", $"admin:ss:p:{shipment.Id}"),
            ShipmentStatus.Preparing => InlineKeyboardButton.WithCallbackData(
                "✅ Hazırdır", $"admin:ss:r:{shipment.Id}"),
            ShipmentStatus.Ready => InlineKeyboardButton.WithCallbackData(
                "🚗 Çatdırılmaya çıxdı", $"admin:ss:o:{shipment.Id}"),
            ShipmentStatus.OutForDelivery => InlineKeyboardButton.WithCallbackData(
                "✅ Çatdırıldı", $"admin:ss:d:{shipment.Id}"),
            _ => null
        };
        if (nextButton is not null)
            rows.Add([nextButton]);
        if (shipment.Status is not ShipmentStatus.Delivered and not ShipmentStatus.Cancelled)
            rows.Add(
            [
                InlineKeyboardButton.WithCallbackData("❌ Göndərişi ləğv et", $"admin:ss:c:{shipment.Id}")
            ]);
        rows.Add(
        [
            InlineKeyboardButton.WithCallbackData("⬅️ Sifariş", $"admin:actions:{shipment.OrderNumber}")
        ]);

        await _botClient.SendMessage(
            chatId,
            text.ToString(),
            replyMarkup: new InlineKeyboardMarkup(rows));
    }

    private static string GetStaffName(StaffDto staff)
        => string.Join(" ", new[] { staff.FirstName, staff.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string GetShipmentStatusText(ShipmentStatus status) => status switch
    {
        ShipmentStatus.Assigned => "İşçiyə təyin edilib",
        ShipmentStatus.Preparing => "Hazırlanır",
        ShipmentStatus.Ready => "Hazırdır",
        ShipmentStatus.OutForDelivery => "Çatdırılmaya çıxıb",
        ShipmentStatus.Delivered => "Çatdırılıb",
        ShipmentStatus.Cancelled => "Ləğv edilib",
        _ => status.ToString()
    };

    private async Task<bool> TryHandleAdminInputAsync(
    Message message)
    {
        var telegramUserId = message.From!.Id;

        if (telegramUserId != _telegramOptions.AdminUserId)
            return false;

        if (!_sessionStore.TryGet(
                telegramUserId,
                out var session) ||
            session is null ||
            session.AdminInputStep == AdminInputStep.None)
        {
            return false;
        }

        if (session.AdminInputStep == AdminInputStep.AwaitingWorkerTelegramId)
        {
            if (!long.TryParse(message.Text, out var workerTelegramId) || workerTelegramId <= 0)
            {
                await _botClient.SendMessage(message.Chat.Id, "Düzgün Telegram ID yazın.");
                return true;
            }

            session.WorkerTelegramUserId = workerTelegramId;
            session.AdminInputStep = AdminInputStep.AwaitingWorkerFirstName;
            await _botClient.SendMessage(message.Chat.Id, "İşçinin adını yazın:");
            return true;
        }

        if (session.AdminInputStep == AdminInputStep.AwaitingWorkerFirstName)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(message.Chat.Id, "Ad boş ola bilməz.");
                return true;
            }

            session.WorkerFirstName = message.Text.Trim();
            session.AdminInputStep = AdminInputStep.AwaitingWorkerLastName;
            await _botClient.SendMessage(
                message.Chat.Id,
                "İşçinin soyadını yazın və ya ‘Keç’ yazın:");
            return true;
        }

        if (session.AdminInputStep == AdminInputStep.AwaitingWorkerLastName)
        {
            session.WorkerLastName = message.Text?.Equals(
                "Keç", StringComparison.OrdinalIgnoreCase) == true
                ? null
                : message.Text?.Trim();
            session.AdminInputStep = AdminInputStep.AwaitingWorkerUsername;
            await _botClient.SendMessage(
                message.Chat.Id,
                "İşçinin Telegram username-ni yazın (@ olmadan) və ya ‘Keç’ yazın:");
            return true;
        }

        if (session.AdminInputStep == AdminInputStep.AwaitingWorkerUsername)
        {
            var username = message.Text?.Equals(
                "Keç", StringComparison.OrdinalIgnoreCase) == true
                ? null
                : message.Text?.Trim().TrimStart('@');

            var worker = await _staffService.CreateWorkerAsync(
                new CreateWorkerRequest(
                    session.WorkerTelegramUserId!.Value,
                    session.WorkerFirstName!,
                    session.WorkerLastName,
                    username));

            _sessionStore.Clear(telegramUserId);
            await _botClient.SendMessage(message.Chat.Id, "✅ İşçi əlavə edildi.");
            await SendAdminStaffDetailAsync(message.Chat.Id, worker.Id);
            return true;
        }

        if (session.AdminInputStep ==
    AdminInputStep.AwaitingProductName)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Məhsul adı boş ola bilməz.");

                return true;
            }

            session.ProductName = message.Text.Trim();
            session.AdminInputStep =
                AdminInputStep.AwaitingProductCode;

            await _botClient.SendMessage(
                message.Chat.Id,
                "Məhsul kodunu yazın.\nMəsələn: CC-1L");

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingProductCode)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Məhsul kodu boş ola bilməz.");

                return true;
            }

            session.ProductCode = message.Text.Trim();
            session.AdminInputStep =
                AdminInputStep.AwaitingProductPrice;

            await _botClient.SendMessage(
                message.Chat.Id,
                "Məhsulun qiymətini yazın.\nMəsələn: 1.80");

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingProductPrice)
        {
            var priceText = message.Text?
                .Trim()
                .Replace(',', '.');

            if (!decimal.TryParse(
                    priceText,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var productPrice) ||
                productPrice < 0)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Qiyməti düzgün formatda yazın.");

                return true;
            }

            session.ProductPrice = productPrice;
            session.AdminInputStep =
                AdminInputStep.AwaitingInitialStock;

            await _botClient.SendMessage(
                message.Chat.Id,
                "İlkin stok sayını yazın:");

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingInitialStock)
        {
            if (!int.TryParse(
                    message.Text,
                    out var initialStock) ||
                initialStock < 0)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Stok sıfır və ya müsbət tam ədəd olmalıdır.");

                return true;
            }

            session.InitialStockQuantity = initialStock;
            session.AdminInputStep =
                AdminInputStep.AwaitingProductDescription;

            await _botClient.SendMessage(
                message.Chat.Id,
                "Məhsulun açıqlamasını yazın:");

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingProductDescription)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Açıqlama boş ola bilməz.");

                return true;
            }

            session.ProductDescription = message.Text.Trim();
            session.AdminInputStep =
                AdminInputStep.AwaitingProductImage;

            await _botClient.SendMessage(
                message.Chat.Id,
                "Məhsulun şəklini göndərin və ya " +
                "“Şəkilsiz davam et” yazın.");

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingProductImage)
        {
            string? imageReference = null;

            if (message.Photo is { Length: > 0 })
            {
                imageReference = message.Photo[^1].FileId;
            }
            else if (message.Text?.Equals(
                         "Şəkilsiz davam et",
                         StringComparison.OrdinalIgnoreCase) != true)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Şəkil göndərin və ya “Şəkilsiz davam et” yazın.");

                return true;
            }

            var product =
                await _productService.CreateAsync(
                    new CreateProductRequest(
                        session.ProductName!,
                        session.ProductCode!,
                        session.ProductPrice!.Value,
                        session.InitialStockQuantity!.Value,
                        session.ProductDescription!,
                        imageReference));

            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Yeni məhsul əlavə edildi.");

            await SendAdminProductDetailAsync(
                message.Chat.Id,
                product.Id);

            return true;
        }

        if (session.ProductId is null)
        {
            _sessionStore.Clear(telegramUserId);
            return false;
        }

        if (session.AdminInputStep ==
    AdminInputStep.AwaitingProductNameEdit)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Məhsul adı boş ola bilməz.");

                return true;
            }

            var product =
                await _productService.GetByIdAsync(
                    session.ProductId.Value)
                ?? throw new KeyNotFoundException(
                    "Product was not found.");

            await _productService.UpdateDetailsAsync(
                product.Id,
                new UpdateProductRequest(
                    message.Text.Trim(),
                    product.Description,
                    product.ImageReference));

            var productId = product.Id;
            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Məhsulun adı dəyişdirildi.");

            await SendAdminProductDetailAsync(
                message.Chat.Id,
                productId);

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingProductDescriptionEdit)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Açıqlama boş ola bilməz.");

                return true;
            }

            var product =
                await _productService.GetByIdAsync(
                    session.ProductId.Value)
                ?? throw new KeyNotFoundException(
                    "Product was not found.");

            await _productService.UpdateDetailsAsync(
                product.Id,
                new UpdateProductRequest(
                    product.Name,
                    message.Text.Trim(),
                    product.ImageReference));

            var productId = product.Id;
            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Məhsulun açıqlaması dəyişdirildi.");

            await SendAdminProductDetailAsync(
                message.Chat.Id,
                productId);

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingProductImageEdit)
        {
            string? imageReference;

            if (message.Photo is { Length: > 0 })
            {
                imageReference = message.Photo[^1].FileId;
            }
            else if (message.Text?.Equals(
                         "Şəkli sil",
                         StringComparison.OrdinalIgnoreCase) == true)
            {
                imageReference = null;
            }
            else
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Yeni şəkli göndərin və ya “Şəkli sil” yazın.");

                return true;
            }

            var product =
                await _productService.GetByIdAsync(
                    session.ProductId.Value)
                ?? throw new KeyNotFoundException(
                    "Product was not found.");

            await _productService.UpdateDetailsAsync(
                product.Id,
                new UpdateProductRequest(
                    product.Name,
                    product.Description,
                    imageReference));

            var productId = product.Id;
            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Məhsulun şəkli yeniləndi.");

            await SendAdminProductDetailAsync(
                message.Chat.Id,
                productId);

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingPrice)
        {
            var priceText = message.Text?
                .Trim()
                .Replace(',', '.');

            if (!decimal.TryParse(
                    priceText,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var price) ||
                price < 0)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Qiyməti düzgün formatda yazın. Məsələn: 2.50");

                return true;
            }

            await _productService.ChangePriceAsync(
                session.ProductId.Value,
                price);

            var productId = session.ProductId.Value;
            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Qiymət dəyişdirildi.");

            await SendAdminProductDetailAsync(
                message.Chat.Id,
                productId);

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingStockQuantity)
        {
            if (!int.TryParse(message.Text, out var quantity) ||
                quantity < 0 ||
                session.AdminStockAction != AdminStockAction.Set &&
                quantity == 0)
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Düzgün miqdar yazın.");

                return true;
            }

            session.StockQuantity = quantity;
            session.AdminInputStep =
                AdminInputStep.AwaitingStockReason;

            await _botClient.SendMessage(
                message.Chat.Id,
                "Stok dəyişikliyinin səbəbini yazın.\n" +
                "Məsələn: Yeni mal qəbulu");

            return true;
        }

        if (session.AdminInputStep ==
            AdminInputStep.AwaitingStockReason)
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                await _botClient.SendMessage(
                    message.Chat.Id,
                    "Səbəb boş ola bilməz.");

                return true;
            }

            var productId = session.ProductId.Value;
            var quantity = session.StockQuantity!.Value;
            var reason = message.Text.Trim();

            switch (session.AdminStockAction)
            {
                case AdminStockAction.Add:
                    await _productService.AddStockAsync(
                        productId,
                        quantity,
                        reason);
                    break;

                case AdminStockAction.Remove:
                    await _productService.RemoveStockAsync(
                        productId,
                        quantity,
                        reason);
                    break;

                case AdminStockAction.Set:
                    await _productService.SetStockAsync(
                        productId,
                        quantity,
                        reason);
                    break;
            }

            _sessionStore.Clear(telegramUserId);

            await _botClient.SendMessage(
                message.Chat.Id,
                "✅ Stok yeniləndi.");

            await SendAdminProductDetailAsync(
                message.Chat.Id,
                productId);

            return true;
        }

        return false;
    }
    private static string GetSpecialOrderStatusText(
    SpecialOrderStatus status)
    {
        return status switch
        {
            SpecialOrderStatus.NotRequired =>
                "Tələb olunmur",

            SpecialOrderStatus.Pending =>
                "Admin əməliyyatı gözlənilir",

            SpecialOrderStatus.OrderedFromSupplier =>
                "Təchizatçıdan sifariş edilib",

            SpecialOrderStatus.Arrived =>
                "Məhsul gəlib",

            SpecialOrderStatus.Completed =>
                "Tamamlanıb",

            SpecialOrderStatus.Cancelled =>
                "Ləğv edilib",

            _ => status.ToString()
        };
    }
}
