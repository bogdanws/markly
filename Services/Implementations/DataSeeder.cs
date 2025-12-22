using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using markly.Data;
using markly.Data.Entities;
using markly.Services.Interfaces;

namespace markly.Services.Implementations;

public class DataSeeder : IDataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DataSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        // Only seed if database is empty
        if (await _context.Users.AnyAsync())
        {
            return;
        }

        await SeedRolesAsync();
        var users = await SeedUsersAsync();
        var tags = await SeedTagsAsync();
        var categories = await SeedCategoriesAsync(users);
        var bookmarks = await SeedBookmarksAsync(users, tags, categories);
        await SeedCommentsAsync(users, bookmarks);
        await SeedVotesAsync(users, bookmarks);
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private async Task<List<ApplicationUser>> SeedUsersAsync()
    {
        var users = new List<(ApplicationUser User, string Password, string Role)>
        {
            (new ApplicationUser
            {
                UserName = "admin@markly.com",
                Email = "admin@markly.com",
                FirstName = "Alex",
                LastName = "Morgan",
                Bio = "Platform administrator and bookmark enthusiast. I collect everything from cute animals to programming resources!",
                ProfilePictureUrl = null,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            }, "Admin123!", "Admin"),

            (new ApplicationUser
            {
                UserName = "sarah@example.com",
                Email = "sarah@example.com",
                FirstName = "Sarah",
                LastName = "Chen",
                Bio = "Full-stack developer who loves coding tutorials and cute pet photos. Also really into digital art and stickers!",
                ProfilePictureUrl = null,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-4)
            }, "Sarah123!", "User"),

            (new ApplicationUser
            {
                UserName = "mike@example.com",
                Email = "mike@example.com",
                FirstName = "Michael",
                LastName = "Johnson",
                Bio = "Gamer and hobbyist programmer. I bookmark recipes, gaming stuff, and random things that make me laugh.",
                ProfilePictureUrl = null,
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            }, "Mike1234!", "User")
        };

        var createdUsers = new List<ApplicationUser>();

        foreach (var (user, password, role) in users)
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                createdUsers.Add(user);
            }
        }

        return createdUsers;
    }

    private async Task<List<Tag>> SeedTagsAsync()
    {
        var tagNames = new[]
        {
            // Pets & animals
            "pets", "cats", "dogs", "animals", "cute", "adorable",
            // Coding & tech
            "javascript", "typescript", "csharp", "cpp", "programming",
            "web-development", "tutorial", "documentation", "i18n", "aspnet",
            // Art & design
            "art", "drawing", "memes", "stickers", "digital-art", "illustration",
            // Seasonal
            "christmas", "winter", "holiday", "diy", "crafts",
            // Gaming & entertainment
            "gaming", "fps", "counter-strike",
            // Food & lifestyle
            "recipes", "cooking", "food",
            // General
            "video", "youtube", "reference", "funny", "inspiration"
        };

        var tags = tagNames.Select(name => new Tag
        {
            Name = name,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 180))
        }).ToList();

        await _context.Tags.AddRangeAsync(tags);
        await _context.SaveChangesAsync();

        return tags;
    }

    private async Task<List<Category>> SeedCategoriesAsync(List<ApplicationUser> users)
    {
        var admin = users.First(u => u.UserName == "admin@markly.com");
        var sarah = users.First(u => u.UserName == "sarah@example.com");
        var mike = users.First(u => u.UserName == "mike@example.com");

        var categories = new List<Category>
        {
            // Admin's categories
            new Category
            {
                Name = "Cute Pets",
                Description = "Adorable animals that brighten your day",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-5)
            },
            new Category
            {
                Name = "Christmas",
                Description = "Festive holiday inspiration and winter vibes",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            },

            // Sarah's categories
            new Category
            {
                Name = "Coding",
                Description = "Programming tutorials, documentation, and developer resources",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-4)
            },
            new Category
            {
                Name = "Drawings",
                Description = "Art, illustrations, memes, and creative inspiration",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            },
            new Category
            {
                Name = "Private Notes",
                Description = "Personal bookmarks and reminders",
                IsPublic = false,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            },

            // Mike's categories
            new Category
            {
                Name = "Gaming",
                Description = "Games, esports, and gaming culture",
                IsPublic = true,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            },
            new Category
            {
                Name = "Food & Recipes",
                Description = "Delicious recipes and cooking inspiration",
                IsPublic = true,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            }
        };

        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        return categories;
    }

    private async Task<List<Bookmark>> SeedBookmarksAsync(
        List<ApplicationUser> users,
        List<Tag> tags,
        List<Category> categories)
    {
        var admin = users.First(u => u.UserName == "admin@markly.com");
        var sarah = users.First(u => u.UserName == "sarah@example.com");
        var mike = users.First(u => u.UserName == "mike@example.com");

        var bookmarks = new List<Bookmark>
        {
            // ==================== CUTE PETS ====================
            new Bookmark
            {
                Title = "Adorable Kitten - Gaming Vibes",
                Description = "A super cute kitten photo that reminds me to take breaks while gaming. Counter-Strike can wait!",
                Content = "{\"textContent\":\"This adorable kitten is the perfect reminder that even the most intense CS2 sessions need a break. Look at those eyes!\\n\\nCheck out Counter-Strike: https://www.counter-strike.net/\",\"imageUrl\":\"https://i.pinimg.com/736x/28/df/22/28df2223147fd6962b2e15b0cd21dbd9.jpg\"}",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-120)
            },
            new Bookmark
            {
                Title = "Adorable Rat in the Kitchen",
                Description = "This precious little rat looks ready to help with cooking. BBC's recipe collection is perfect for cozy kitchen sessions!",
                Content = "{\"textContent\":\"This cute rat seems excited about cooking! BBC Food has the best recipes - perfect for creating delicious meals.\\n\\nFind recipes at: https://www.bbc.co.uk/food/recipes\",\"imageUrl\":\"https://i.pinimg.com/736x/8d/16/f0/8d16f01fda57e531888d2cff27a72593.jpg\"}",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-100)
            },
            new Bookmark
            {
                Title = "Peaceful Puppy in Flowers",
                Description = "A serene image of a puppy surrounded by beautiful flowers. Nature at its finest!",
                Content = "{\"textContent\":\"Dogs and flowers - two of nature's greatest gifts. This image is pure serenity.\\n\\nLearn more about flowers: https://en.wikipedia.org/wiki/Flower\",\"imageUrl\":\"https://i.pinimg.com/736x/17/f9/91/17f991633ba35be93d1e62ce56db1cda.jpg\"}",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-85)
            },
            new Bookmark
            {
                Title = "Sleepy Kitten in Garden",
                Description = "Another precious pet photo with floral vibes. These animals really know how to appreciate nature!",
                Content = "{\"textContent\":\"The Wikipedia article on flowers is fascinating, but this kitten might be more interested in napping among them.\\n\\nhttps://en.wikipedia.org/wiki/Flower\",\"imageUrl\":\"https://i.pinimg.com/736x/bf/25/79/bf257945b3496486263d2c846c045b46.jpg\"}",
                IsPublic = true,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-70)
            },
            new Bookmark
            {
                Title = "Cat Fashion Inspiration",
                Description = "Stylish cat photo that inspired me to check out cat apparel. Yes, that's a thing!",
                Content = "{\"textContent\":\"Did you know Amazon has a whole section for cat clothes? This fashionable feline got me curious!\\n\\nShop cat apparel: https://www.amazon.com/Best-Sellers-Cat-Apparel/zgbs/pet-supplies/2975242011\",\"imageUrl\":\"https://i.pinimg.com/736x/15/0c/f3/150cf32844e8db6ccd33acd68990df88.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-55)
            },

            // ==================== CODING ====================
            new Bookmark
            {
                Title = "i18next - Internationalization Framework",
                Description = "The best i18n library for JavaScript applications. Essential for building multilingual apps!",
                Content = "{\"textContent\":\"i18next is incredibly powerful for internationalization. Works great with React, Vue, and vanilla JS. The documentation is comprehensive and the plugin ecosystem is huge.\\n\\nDocumentation: https://www.i18next.com/\",\"imageUrl\":\"https://i.pinimg.com/736x/75/e0/61/75e06170e12c918187a64335c0a99d42.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            },
            new Bookmark
            {
                Title = "The Art of Binary",
                Description = "A beautiful hacker aesthetic image. Links to an interesting cybersecurity resource.",
                Content = "{\"textContent\":\"There's something mesmerizing about green binary cascading down a terminal. HackerOne is a great platform for learning about ethical hacking and bug bounties.\\n\\nExplore: https://www.hackerone.com/\",\"imageUrl\":\"https://i.pinimg.com/736x/99/0f/c7/990fc7961720fe41f46d5380bac55023.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-80)
            },
            new Bookmark
            {
                Title = "Cozy C++ Programming Setup",
                Description = "A warm and inviting coding environment. Makes me want to write some C++!",
                Content = "{\"textContent\":\"The ISO C++ website has great resources for modern C++ development. This cozy setup with VS Code is exactly how I like to code on cold evenings.\\n\\nC++ resources: https://isocpp.org/\",\"imageUrl\":\"https://i.pinimg.com/736x/5c/16/fd/5c16fd714bc640089900c8b1eb37c878.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-65)
            },
            new Bookmark
            {
                Title = "Blue Matrix Aesthetic",
                Description = "Binary code in blue - a different take on the classic hacker aesthetic.",
                Content = "{\"textContent\":\"GitHub is where all the magic happens. This blue binary aesthetic is giving me cyberpunk vibes. Time to push some code!\\n\\nhttps://github.com/\",\"imageUrl\":\"https://i.pinimg.com/736x/f7/8e/f4/f78ef408135170687ba842863d19af1a.jpg\"}",
                IsPublic = true,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new Bookmark
            {
                Title = "JavaScript String Methods Cheatsheet",
                Description = "Visual guide to JS string methods. Super helpful for quick reference!",
                Content = "{\"textContent\":\"MDN is the ultimate reference for JavaScript. This image is a great cheatsheet for string methods - I reference it all the time!\\n\\nFull docs: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String\",\"imageUrl\":\"https://i.pinimg.com/1200x/ad/59/2d/ad592d7a5afb28d8ac2ab8f48019cd22.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            },
            new Bookmark
            {
                Title = "ASP.NET Core Crash Course",
                Description = "Comprehensive video tutorial for getting started with ASP.NET Core. Great for beginners!",
                Content = "{\"textContent\":\"This crash course covers everything you need to know to get started with ASP.NET Core. Perfect for C# developers wanting to build web APIs and MVC applications.\",\"videoUrl\":\"https://www.youtube.com/watch?v=BfEjDD8mWYg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },

            // ==================== DRAWINGS ====================
            new Bookmark
            {
                Title = "Duckling with a Knife",
                Description = "An unexpectedly menacing yet adorable duckling illustration. The internet is a weird place.",
                Content = "{\"textContent\":\"Don't let the cute face fool you - this duckling means business! A hilarious piece of internet art.\\n\\nMore cute stuff: https://www.reddit.com/r/aww/\",\"imageUrl\":\"https://i.pinimg.com/736x/20/44/8f/20448fb4c6c278be29c79695fbd02ff7.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-75)
            },
            new Bookmark
            {
                Title = "Stressed Doodle Man Meme",
                Description = "A relatable doodle of someone having a rough day. We've all been there!",
                Content = "{\"textContent\":\"This doodle perfectly captures the feeling of debugging code at 2 AM. Know Your Meme has the history of all these iconic internet moments.\\n\\nhttps://knowyourmeme.com/\",\"imageUrl\":\"https://i.pinimg.com/736x/2b/4f/6a/2b4f6a40e60e528a5cc6e159aa643d41.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new Bookmark
            {
                Title = "Flaticon Stickers Collection",
                Description = "Amazing collection of stickers and icons for your projects. Great for UI design!",
                Content = "{\"textContent\":\"Flaticon has thousands of free stickers and icons. Perfect for adding personality to your apps and presentations!\\n\\nBrowse stickers: https://www.flaticon.com/stickers\",\"imageUrl\":\"https://i.pinimg.com/736x/22/d5/86/22d586f935cdc754751197229d4f4f3f.jpg\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-45)
            },

            // ==================== CHRISTMAS ====================
            new Bookmark
            {
                Title = "Cute Snowman Illustration",
                Description = "Adorable snowman for the holiday season. Includes a guide on making real ones!",
                Content = "{\"textContent\":\"WikiHow has a surprisingly detailed guide on building the perfect snowman.\\n\\nLearn how: https://www.wikihow.com/Make-a-Snowman\",\"imageUrl\":\"https://i.pinimg.com/736x/b0/65/0c/b0650c4834f95bffe79e2733ddba6162.jpg\"}",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Bookmark
            {
                Title = "Winter Wonderland Scene",
                Description = "Another beautiful winter-themed illustration. Time to build some snowmen!",
                Content = "{\"textContent\":\"The perfect winter aesthetic. Can't wait for the snow to fall so I can try out these snowman-building tips!\\n\\nhttps://www.wikihow.com/Make-a-Snowman\",\"imageUrl\":\"https://i.pinimg.com/736x/5e/bc/7f/5ebc7fc7983f982092224d5b1e0c878b.jpg\"}",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new Bookmark
            {
                Title = "Christmas Cat",
                Description = "A festive feline celebrating the holidays. Did you know cats have been companions for thousands of years?",
                Content = "{\"textContent\":\"Cats and Christmas - a perfect combination! The Wikipedia article on cats is fascinating - they've been domesticated for over 10,000 years.\\n\\nhttps://en.wikipedia.org/wiki/Cat\",\"imageUrl\":\"https://i.pinimg.com/736x/fe/b8/b8/feb8b83d5939493f66e956efaf942cf3.jpg\"}",
                IsPublic = true,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Bookmark
            {
                Title = "Christmas Tree Lights Shopping",
                Description = "Beautiful holiday lights inspiration. Link to Amazon for getting your own!",
                Content = "{\"textContent\":\"Nothing beats the warm glow of Christmas lights. Amazon has so many options - from classic warm white to colorful LED strings!\\n\\nShop lights: https://www.amazon.com/christmas-tree-lights/s?k=christmas+tree+lights\",\"imageUrl\":\"https://i.pinimg.com/736x/cb/67/6e/cb676e280c51c97c1aa2ac311fd0c3ec.jpg\"}",
                IsPublic = true,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },

            // ==================== TEXT-ONLY BOOKMARKS (No images) ====================
            new Bookmark
            {
                Title = "TypeScript Handbook",
                Description = "The official TypeScript documentation. A must-read for any TS developer.",
                Content = "{\"textContent\":\"The TypeScript handbook is comprehensive and well-written. Essential reading for understanding the type system.\\n\\nhttps://www.typescriptlang.org/docs/handbook/\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-95)
            },
            new Bookmark
            {
                Title = "Git Cheat Sheet",
                Description = "Quick reference for Git commands. I use this daily!",
                Content = "{\"textContent\":\"git checkout, git rebase, git stash... this cheat sheet has saved me countless times when I forget the exact syntax.\\n\\nhttps://education.github.com/git-cheat-sheet-education.pdf\"}",
                IsPublic = true,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-88)
            },
            new Bookmark
            {
                Title = "Counter-Strike Wiki",
                Description = "Everything you need to know about CS2. Maps, weapons, strategies, and more.",
                Content = "{\"textContent\":\"The CS wiki is incredibly detailed. Great for learning callouts, weapon stats, and the history of the game.\\n\\nhttps://counterstrike.fandom.com/\"}",
                IsPublic = true,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-82)
            },
            new Bookmark
            {
                Title = "How to Make Hot Chocolate",
                Description = "Perfect for cold winter evenings. A simple but delicious recipe.",
                Content = "{\"textContent\":\"The secret is using real cocoa powder and a splash of vanilla extract. Pairs perfectly with Christmas movies!\\n\\nhttps://www.allrecipes.com/article/how-to-make-hot-chocolate/\"}",
                IsPublic = true,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            },
            new Bookmark
            {
                Title = "My Secret Project Ideas",
                Description = "Private list of app ideas I want to build someday.",
                Content = "{\"textContent\":\"1. AI-powered bookmark organizer\\n2. Real-time collaborative whiteboard\\n3. Browser extension for saving recipes\\n4. Pet photo sharing app\\n\\nNotes: https://notion.so/\"}",
                IsPublic = false,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-35)
            },
            new Bookmark
            {
                Title = "Interview Prep Resources",
                Description = "Collection of resources for my upcoming job interviews.",
                Content = "{\"textContent\":\"Focus areas: Dynamic programming, graph algorithms, system design. Remember to practice explaining thought process out loud!\\n\\nPractice: https://leetcode.com/\"}",
                IsPublic = false,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-28)
            }
        };

        await _context.Bookmarks.AddRangeAsync(bookmarks);
        await _context.SaveChangesAsync();

        // Associate bookmarks with tags
        var tagLookup = tags.ToDictionary(t => t.Name, t => t);

        var bookmarkTagAssociations = new Dictionary<string, string[]>
        {
            // Cute Pets
            { "Adorable Kitten - Gaming Vibes", new[] { "pets", "cats", "cute", "gaming", "counter-strike" } },
            { "Adorable Rat in the Kitchen", new[] { "pets", "cute", "recipes", "cooking", "animals" } },
            { "Peaceful Puppy in Flowers", new[] { "pets", "dogs", "cute", "animals" } },
            { "Sleepy Kitten in Garden", new[] { "pets", "cats", "cute", "animals" } },
            { "Cat Fashion Inspiration", new[] { "pets", "cats", "cute", "funny" } },

            // Coding
            { "i18next - Internationalization Framework", new[] { "javascript", "i18n", "web-development", "documentation" } },
            { "The Art of Binary", new[] { "programming", "inspiration" } },
            { "Cozy C++ Programming Setup", new[] { "cpp", "programming", "inspiration" } },
            { "Blue Matrix Aesthetic", new[] { "programming", "inspiration" } },
            { "JavaScript String Methods Cheatsheet", new[] { "javascript", "web-development", "documentation", "reference" } },
            { "ASP.NET Core Crash Course", new[] { "csharp", "aspnet", "web-development", "tutorial", "video", "youtube" } },

            // Drawings
            { "Duckling with a Knife", new[] { "art", "drawing", "funny", "memes" } },
            { "Stressed Doodle Man Meme", new[] { "art", "drawing", "memes", "funny" } },
            { "Flaticon Stickers Collection", new[] { "art", "stickers", "digital-art", "reference" } },

            // Christmas
            { "Cute Snowman Illustration", new[] { "christmas", "winter", "art", "diy" } },
            { "Winter Wonderland Scene", new[] { "christmas", "winter", "art", "crafts" } },
            { "Christmas Cat", new[] { "christmas", "pets", "cats", "holiday" } },
            { "Christmas Tree Lights Shopping", new[] { "christmas", "holiday", "diy" } },

            // Text-only
            { "TypeScript Handbook", new[] { "typescript", "javascript", "documentation", "web-development" } },
            { "Git Cheat Sheet", new[] { "programming", "reference", "documentation" } },
            { "Counter-Strike Wiki", new[] { "gaming", "counter-strike", "fps", "reference" } },
            { "How to Make Hot Chocolate", new[] { "recipes", "cooking", "food", "winter" } },
            { "My Secret Project Ideas", new[] { "programming", "inspiration" } },
            { "Interview Prep Resources", new[] { "programming", "reference" } }
        };

        var bookmarkTags = new List<BookmarkTag>();
        foreach (var bookmark in bookmarks)
        {
            if (bookmarkTagAssociations.TryGetValue(bookmark.Title, out var tagNames))
            {
                foreach (var tagName in tagNames)
                {
                    if (tagLookup.TryGetValue(tagName, out var tag))
                    {
                        bookmarkTags.Add(new BookmarkTag
                        {
                            BookmarkId = bookmark.Id,
                            TagId = tag.Id
                        });
                    }
                }
            }
        }

        await _context.BookmarkTags.AddRangeAsync(bookmarkTags);

        // Associate bookmarks with categories
        var categoryLookup = categories.ToDictionary(c => c.Name, c => c);

        var bookmarkCategoryAssociations = new Dictionary<string, string[]>
        {
            // Cute Pets category
            { "Adorable Kitten - Gaming Vibes", new[] { "Cute Pets", "Gaming" } },
            { "Adorable Rat in the Kitchen", new[] { "Cute Pets", "Food & Recipes" } },
            { "Peaceful Puppy in Flowers", new[] { "Cute Pets" } },
            { "Sleepy Kitten in Garden", new[] { "Cute Pets" } },
            { "Cat Fashion Inspiration", new[] { "Cute Pets" } },

            // Coding category
            { "i18next - Internationalization Framework", new[] { "Coding" } },
            { "The Art of Binary", new[] { "Coding" } },
            { "Cozy C++ Programming Setup", new[] { "Coding" } },
            { "Blue Matrix Aesthetic", new[] { "Coding" } },
            { "JavaScript String Methods Cheatsheet", new[] { "Coding" } },
            { "ASP.NET Core Crash Course", new[] { "Coding" } },

            // Drawings category
            { "Duckling with a Knife", new[] { "Drawings" } },
            { "Stressed Doodle Man Meme", new[] { "Drawings" } },
            { "Flaticon Stickers Collection", new[] { "Drawings" } },

            // Christmas category
            { "Cute Snowman Illustration", new[] { "Christmas" } },
            { "Winter Wonderland Scene", new[] { "Christmas" } },
            { "Christmas Cat", new[] { "Christmas", "Cute Pets" } },
            { "Christmas Tree Lights Shopping", new[] { "Christmas" } },

            // Text-only bookmarks
            { "TypeScript Handbook", new[] { "Coding" } },
            { "Git Cheat Sheet", new[] { "Coding" } },
            { "Counter-Strike Wiki", new[] { "Gaming" } },
            { "How to Make Hot Chocolate", new[] { "Food & Recipes", "Christmas" } },
            { "My Secret Project Ideas", new[] { "Private Notes" } },
            { "Interview Prep Resources", new[] { "Private Notes" } }
        };

        var bookmarkCategories = new List<BookmarkCategory>();
        foreach (var bookmark in bookmarks)
        {
            if (bookmarkCategoryAssociations.TryGetValue(bookmark.Title, out var categoryNames))
            {
                foreach (var categoryName in categoryNames)
                {
                    if (categoryLookup.TryGetValue(categoryName, out var category))
                    {
                        bookmarkCategories.Add(new BookmarkCategory
                        {
                            BookmarkId = bookmark.Id,
                            CategoryId = category.Id
                        });
                    }
                }
            }
        }

        await _context.BookmarkCategories.AddRangeAsync(bookmarkCategories);
        await _context.SaveChangesAsync();

        return bookmarks;
    }

    private async Task SeedCommentsAsync(List<ApplicationUser> users, List<Bookmark> bookmarks)
    {
        var admin = users.First(u => u.UserName == "admin@markly.com");
        var sarah = users.First(u => u.UserName == "sarah@example.com");
        var mike = users.First(u => u.UserName == "mike@example.com");

        var comments = new List<Comment>
        {
            // Comments on pet bookmarks
            new Comment
            {
                Content = "This kitten is so cute! Almost makes me want to stop playing CS2... almost.",
                BookmarkId = bookmarks.First(b => b.Title == "Adorable Kitten - Gaming Vibes").Id,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-115)
            },
            new Comment
            {
                Content = "I showed this to my cat. She was unimpressed, as usual.",
                BookmarkId = bookmarks.First(b => b.Title == "Adorable Kitten - Gaming Vibes").Id,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-110)
            },
            new Comment
            {
                Content = "BBC recipes are the best! And this adorable rat seems to agree!",
                BookmarkId = bookmarks.First(b => b.Title == "Adorable Rat in the Kitchen").Id,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-95)
            },

            // Comments on coding bookmarks
            new Comment
            {
                Content = "i18next is amazing! We use it at work and it handles all our translation needs perfectly.",
                BookmarkId = bookmarks.First(b => b.Title == "i18next - Internationalization Framework").Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-85)
            },
            new Comment
            {
                Content = "The pluralization feature alone is worth it. No more awkward '1 items' bugs!",
                BookmarkId = bookmarks.First(b => b.Title == "i18next - Internationalization Framework").Id,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-80)
            },
            new Comment
            {
                Content = "This cozy setup is goals. I need to upgrade my workspace!",
                BookmarkId = bookmarks.First(b => b.Title == "Cozy C++ Programming Setup").Id,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            },
            new Comment
            {
                Content = "C++ in 2024 is actually pretty nice with modern standards. This image captures the vibe perfectly.",
                BookmarkId = bookmarks.First(b => b.Title == "Cozy C++ Programming Setup").Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-55)
            },
            new Comment
            {
                Content = "This ASP.NET course is perfect for getting started. Bookmarking this for my team!",
                BookmarkId = bookmarks.First(b => b.Title == "ASP.NET Core Crash Course").Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },

            // Comments on drawing bookmarks
            new Comment
            {
                Content = "The duckling chose violence today. I love it!",
                BookmarkId = bookmarks.First(b => b.Title == "Duckling with a Knife").Id,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-70)
            },
            new Comment
            {
                Content = "This is me every Monday morning trying to debug weekend deployments.",
                BookmarkId = bookmarks.First(b => b.Title == "Stressed Doodle Man Meme").Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-55)
            },
            new Comment
            {
                Content = "Flaticon has saved me so many times when I need quick icons for mockups!",
                BookmarkId = bookmarks.First(b => b.Title == "Flaticon Stickers Collection").Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            },

            // Comments on Christmas bookmarks
            new Comment
            {
                Content = "I tried making a snowman following this guide. It... looked like a blob. But a festive blob!",
                BookmarkId = bookmarks.First(b => b.Title == "Cute Snowman Illustration").Id,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-22)
            },
            new Comment
            {
                Content = "That cat is so photogenic! My cat would never sit still for a Christmas photo.",
                BookmarkId = bookmarks.First(b => b.Title == "Christmas Cat").Id,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            },
            new Comment
            {
                Content = "Just ordered some of these lights! Can't wait to decorate.",
                BookmarkId = bookmarks.First(b => b.Title == "Christmas Tree Lights Shopping").Id,
                UserId = sarah.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },

            // Comments on text-only bookmarks
            new Comment
            {
                Content = "The TypeScript handbook is SO good. Everyone should read it cover to cover.",
                BookmarkId = bookmarks.First(b => b.Title == "TypeScript Handbook").Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-90)
            },
            new Comment
            {
                Content = "I print this cheat sheet out and keep it on my desk. Lifesaver!",
                BookmarkId = bookmarks.First(b => b.Title == "Git Cheat Sheet").Id,
                UserId = mike.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-85)
            },
            new Comment
            {
                Content = "This hot chocolate recipe is *chef's kiss*. Added some cinnamon and it was perfect.",
                BookmarkId = bookmarks.First(b => b.Title == "How to Make Hot Chocolate").Id,
                UserId = admin.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        await _context.Comments.AddRangeAsync(comments);
        await _context.SaveChangesAsync();
    }

    private async Task SeedVotesAsync(List<ApplicationUser> users, List<Bookmark> bookmarks)
    {
        var admin = users.First(u => u.UserName == "admin@markly.com");
        var sarah = users.First(u => u.UserName == "sarah@example.com");
        var mike = users.First(u => u.UserName == "mike@example.com");

        var votes = new List<Vote>();

        // Admin votes
        var adminVotedBookmarks = new[]
        {
            "i18next - Internationalization Framework",
            "Cozy C++ Programming Setup",
            "ASP.NET Core Crash Course",
            "Flaticon Stickers Collection",
            "Adorable Kitten - Gaming Vibes",
            "Christmas Cat",
            "TypeScript Handbook",
            "Duckling with a Knife"
        };

        foreach (var title in adminVotedBookmarks)
        {
            var bookmark = bookmarks.FirstOrDefault(b => b.Title == title);
            if (bookmark != null)
            {
                votes.Add(new Vote
                {
                    BookmarkId = bookmark.Id,
                    UserId = admin.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60))
                });
            }
        }

        // Sarah votes
        var sarahVotedBookmarks = new[]
        {
            "Adorable Kitten - Gaming Vibes",
            "Fluffy Cat Cooking Companion",
            "Christmas Tree Lights Shopping",
            "Cute Snowman Illustration",
            "How to Make Hot Chocolate",
            "Counter-Strike Wiki",
            "The Art of Binary",
            "Stressed Doodle Man Meme"
        };

        foreach (var title in sarahVotedBookmarks)
        {
            var bookmark = bookmarks.FirstOrDefault(b => b.Title == title);
            if (bookmark != null)
            {
                votes.Add(new Vote
                {
                    BookmarkId = bookmark.Id,
                    UserId = sarah.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60))
                });
            }
        }

        // Mike votes
        var mikeVotedBookmarks = new[]
        {
            "i18next - Internationalization Framework",
            "JavaScript String Methods Cheatsheet",
            "ASP.NET Core Crash Course",
            "Duckling with a Knife",
            "Cat Fashion Inspiration",
            "Peaceful Puppy in Flowers",
            "Git Cheat Sheet",
            "TypeScript Handbook",
            "Blue Matrix Aesthetic"
        };

        foreach (var title in mikeVotedBookmarks)
        {
            var bookmark = bookmarks.FirstOrDefault(b => b.Title == title);
            if (bookmark != null)
            {
                votes.Add(new Vote
                {
                    BookmarkId = bookmark.Id,
                    UserId = mike.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60))
                });
            }
        }

        await _context.Votes.AddRangeAsync(votes);
        await _context.SaveChangesAsync();
    }
}
