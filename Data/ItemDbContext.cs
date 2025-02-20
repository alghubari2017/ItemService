using Microsoft.EntityFrameworkCore;
using ItemService.Models;

namespace ItemService.Data;

public class ItemDbContext : DbContext
{
    public ItemDbContext(DbContextOptions<ItemDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items { get; set; }
}