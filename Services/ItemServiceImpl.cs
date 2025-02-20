using Grpc.Core;
using ItemService.Data;
using ItemService.Models;
using Microsoft.EntityFrameworkCore;

namespace ItemService.Services;

public class ItemServiceImpl : ItemService.ItemServiceBase
{
    private readonly ItemDbContext _context;
    private readonly RabbitMQService _rabbitMQService;

    public ItemServiceImpl(ItemDbContext context, RabbitMQService rabbitMQService)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
    }

    public override async Task<ItemResponse> CreateItem(CreateItemRequest request, ServerCallContext context)
    {
        var item = new Item
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        // Publish item created event
        _rabbitMQService.PublishItemEvent("item.created", item);

        return new ItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price
        };
    }

    public override async Task<ItemResponse> GetItem(GetItemRequest request, ServerCallContext context)
    {
        var item = await _context.Items.FindAsync(request.Id);
        if (item == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Item not found"));
        }

        return new ItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price
        };
    }

    public override async Task<ListItemsResponse> ListItems(ListItemsRequest request, ServerCallContext context)
    {
        var response = new ListItemsResponse();
        var items = await _context.Items.ToListAsync();
        
        response.Items.AddRange(items.Select(item => new ItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price
        }));

        return response;
    }

    public override async Task<ItemResponse> UpdateItem(UpdateItemRequest request, ServerCallContext context)
    {
        var item = await _context.Items.FindAsync(request.Id);
        if (item == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Item not found"));
        }

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;

        await _context.SaveChangesAsync();

        return new ItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            Price = item.Price
        };
    }

    public override async Task<DeleteItemResponse> DeleteItem(DeleteItemRequest request, ServerCallContext context)
    {
        var item = await _context.Items.FindAsync(request.Id);
        if (item == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Item not found"));
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return new DeleteItemResponse { Success = true };
    }
} // Add closing brace for the class