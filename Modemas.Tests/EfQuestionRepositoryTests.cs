using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modemas.Server.Data;
using Modemas.Server.Models;

public class EfQuestionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly EfQuestionRepository _repo;

    public EfQuestionRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _repo = new EfQuestionRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private MultipleChoiceQuestion MakeMCQ(string text = "Q1") =>
        new MultipleChoiceQuestion
        {
            Text = text,
            CorrectAnswerIndex = 0,
            Choices = new List<string> { "A", "B" }
        };

    [Fact]
    public async Task SaveAsync_CreatesNewTopic_WhenNotExists()
    {
        var questions = new[] { MakeMCQ() };

        await _repo.SaveAsync("math", questions);

        var group = await _context.QuestionTopicGroups
            .Include(g => g.Questions)
            .FirstOrDefaultAsync(g => g.Topic == "math");

        Assert.NotNull(group);
        Assert.Single(group.Questions);
        Assert.Equal("Q1", group.Questions.First().Text);
    }

    [Fact]
    public async Task SaveAsync_ReplacesQuestions_WhenTopicExists()
    {
        await _repo.SaveAsync("science", new[] { MakeMCQ("Old Q") });

        await _repo.SaveAsync("science", new[] { MakeMCQ("New Q") });

        var group = await _context.QuestionTopicGroups
            .Include(g => g.Questions)
            .FirstAsync(g => g.Topic == "science");

        Assert.Single(group.Questions);
        Assert.Equal("New Q", group.Questions.First().Text);
    }

    [Fact]
    public async Task GetByTopicAsync_ReturnsQuestions()
    {
        await _repo.SaveAsync("history", new[] { MakeMCQ("H1") });

        var result = await _repo.GetByTopicAsync("history");

        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("H1", list[0].Text);
    }

    [Fact]
    public async Task GetByTopicAsync_IgnoresCase()
    {
        await _repo.SaveAsync("GeOgRaPhY", new[] { MakeMCQ("GeoQ") });

        var result = await _repo.GetByTopicAsync("geography");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllTopicsAsync_ReturnsAllTopics()
    {
        await _repo.SaveAsync("math", new[] { MakeMCQ() });
        await _repo.SaveAsync("science", new[] { MakeMCQ() });

        var topics = (await _repo.GetAllTopicsAsync()).ToList();

        Assert.Contains("math", topics);
        Assert.Contains("science", topics);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTopic()
    {
        await _repo.SaveAsync("fun", new[] { MakeMCQ() });

        await _repo.DeleteAsync("fun");

        var exists = await _context.QuestionTopicGroups.AnyAsync(g => g.Topic == "fun");

        Assert.False(exists);
    }

    [Fact]
    public async Task CreateTopicAsync_AddsTopic_WhenMissing()
    {
        await _repo.CreateTopicAsync("space");

        var exists = await _context.QuestionTopicGroups.AnyAsync(g => g.Topic == "space");

        Assert.True(exists);
    }

    [Fact]
    public async Task CreateTopicAsync_DoesNothing_WhenExists()
    {
        await _repo.CreateTopicAsync("coding");
        await _repo.CreateTopicAsync("coding");

        var count = await _context.QuestionTopicGroups.CountAsync(g => g.Topic == "coding");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RenameTopicAsync_UpdatesTopicName()
    {
        await _repo.CreateTopicAsync("old");

        var ok = await _repo.RenameTopicAsync("old", "new");

        Assert.True(ok);
        Assert.True(await _context.QuestionTopicGroups.AnyAsync(g => g.Topic == "new"));
        Assert.False(await _context.QuestionTopicGroups.AnyAsync(g => g.Topic == "old"));
    }

    [Fact]
    public async Task RenameTopicAsync_ReturnsFalse_WhenTopicNotFound()
    {
        var result = await _repo.RenameTopicAsync("missing", "whatever");

        Assert.False(result);
    }
}
