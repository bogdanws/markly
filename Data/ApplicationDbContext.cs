using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using markly.Data.Entities;

namespace markly.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Vote> Votes { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<BookmarkTag> BookmarkTags { get; set; }
    public DbSet<BookmarkCategory> BookmarkCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // many-to-many relationship: Bookmark <-> Tag
        modelBuilder.Entity<BookmarkTag>()
            .HasKey(bt => new { bt.BookmarkId, bt.TagId });

        modelBuilder.Entity<BookmarkTag>()
            .HasOne(bt => bt.Bookmark)
            .WithMany(b => b.BookmarkTags)
            .HasForeignKey(bt => bt.BookmarkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookmarkTag>()
            .HasOne(bt => bt.Tag)
            .WithMany(t => t.BookmarkTags)
            .HasForeignKey(bt => bt.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // many-to-many relationship: Bookmark <-> Category
        modelBuilder.Entity<BookmarkCategory>()
            .HasKey(bc => new { bc.BookmarkId, bc.CategoryId });

        modelBuilder.Entity<BookmarkCategory>()
            .HasOne(bc => bc.Bookmark)
            .WithMany(b => b.BookmarkCategories)
            .HasForeignKey(bc => bc.BookmarkId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BookmarkCategory>()
            .HasOne(bc => bc.Category)
            .WithMany(c => c.BookmarkCategories)
            .HasForeignKey(bc => bc.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete from category side

        // Vote - ensure one vote per user per bookmark
        modelBuilder.Entity<Vote>()
            .HasIndex(v => new { v.BookmarkId, v.UserId })
            .IsUnique();

        // indexes for better search performance
        modelBuilder.Entity<Bookmark>()
            .HasIndex(b => b.CreatedAt);

        modelBuilder.Entity<Bookmark>()
            .HasIndex(b => b.Title);

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .HasIndex(c => new { c.UserId, c.Name })
            .IsUnique();
    }
}