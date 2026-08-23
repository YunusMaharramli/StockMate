using StockMate.Application.Carts.Models;
using StockMate.Application.Persistence;
using StockMate.Domain.Entities;
using StockMate.Domain.Enums;

namespace StockMate.Application.Carts.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;

    public CartService(
        ICartRepository cartRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository)
    {
        _cartRepository = cartRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
    }

    public async Task<CartDto> GetAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var cart = await GetOrCreateCartAsync(
            customer.Id,
            cancellationToken);

        return await MapCartAsync(cart, cancellationToken);
    }

    public async Task<CartDto> AddOrUpdateItemAsync(
        long telegramUserId,
        AddCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var product = await _productRepository.GetByIdAsync(
                          request.ProductId,
                          cancellationToken)
                      ?? throw new KeyNotFoundException(
                          $"Product '{request.ProductId}' was not found.");

        if (!product.IsActive)
            throw new InvalidOperationException(
                "Inactive product cannot be added to the cart.");

        if (request.FulfillmentMode ==
                CartItemFulfillmentMode.StockOnly &&
            request.Quantity > product.StockQuantity)
        {
            throw new InvalidOperationException(
                "Requested quantity exceeds the available stock. " +
                "Choose special order or reduce the quantity.");
        }

        var cart = await GetOrCreateCartAsync(
            customer.Id,
            cancellationToken);

        var itemAlreadyExists = cart.Items.Any(
    item => item.ProductId == product.Id);

        var cartItem = cart.AddOrUpdateItem(
            product.Id,
            request.Quantity,
            request.FulfillmentMode);

        if (!itemAlreadyExists)
        {
            await _cartRepository.AddItemAsync(
                cartItem,
                cancellationToken);
        }

        await _cartRepository.SaveChangesAsync(cancellationToken);

        return await MapCartAsync(cart, cancellationToken);
    }

    public async Task<CartDto> RemoveItemAsync(
        long telegramUserId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var cart = await GetOrCreateCartAsync(
            customer.Id,
            cancellationToken);

        cart.RemoveItem(productId);

        await _cartRepository.SaveChangesAsync(cancellationToken);

        return await MapCartAsync(cart, cancellationToken);
    }

    public async Task ClearAsync(
        long telegramUserId,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetRequiredCustomerAsync(
            telegramUserId,
            cancellationToken);

        var cart = await _cartRepository.GetByCustomerIdAsync(
            customer.Id,
            cancellationToken);

        if (cart is null)
            return;

        cart.Clear();

        await _cartRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<Customer> GetRequiredCustomerAsync(
        long telegramUserId,
        CancellationToken cancellationToken)
    {
        return await _customerRepository.GetByTelegramUserIdAsync(
                   telegramUserId,
                   cancellationToken)
               ?? throw new KeyNotFoundException(
                   $"Customer with Telegram ID '{telegramUserId}' was not found.");
    }

    private async Task<Cart> GetOrCreateCartAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var cart = await _cartRepository.GetByCustomerIdAsync(
            customerId,
            cancellationToken);

        if (cart is null)
        {
            cart = new Cart(customerId);

            await _cartRepository.AddAsync(
                cart,
                cancellationToken);

            await _cartRepository.SaveChangesAsync(cancellationToken);

            return cart;
        }

        if (cart.IsExpired)
        {
            cart.Clear();
            cart.Renew();

            await _cartRepository.SaveChangesAsync(cancellationToken);
        }

        return cart;
    }

    private async Task<CartDto> MapCartAsync(
        Cart cart,
        CancellationToken cancellationToken)
    {
        var productIds = cart.Items
            .Select(item => item.ProductId)
            .ToArray();

        var products = await _productRepository.GetByIdsAsync(
            productIds,
            cancellationToken);

        var productsById = products.ToDictionary(
            product => product.Id);

        var items = new List<CartItemDto>();

        foreach (var cartItem in cart.Items)
        {
            if (!productsById.TryGetValue(
                    cartItem.ProductId,
                    out var product))
            {
                continue;
            }

            var fromStockQuantity = Math.Min(
                cartItem.Quantity,
                product.StockQuantity);

            var missingQuantity =
                cartItem.Quantity - fromStockQuantity;

            var specialOrderQuantity =
                cartItem.FulfillmentMode ==
                CartItemFulfillmentMode.IncludeSpecialOrder
                    ? missingQuantity
                    : 0;

            var unavailableQuantity =
                cartItem.FulfillmentMode ==
                CartItemFulfillmentMode.StockOnly
                    ? missingQuantity
                    : 0;

            items.Add(new CartItemDto(
                product.Id,
                product.Name,
                product.Code,
                product.Price,
                cartItem.Quantity,
                fromStockQuantity,
                specialOrderQuantity,
                unavailableQuantity,
                product.Price * cartItem.Quantity,
                product.IsActive,
                cartItem.FulfillmentMode));
        }

        return new CartDto(
            cart.Id,
            cart.CustomerId,
            items,
            items.Sum(item => item.LineTotal),
            cart.ExpiresAtUtc);
    }
}