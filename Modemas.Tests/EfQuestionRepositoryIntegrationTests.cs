using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modemas.Server.Data;
using Modemas.Server.Models;

public class EfQuestionRepositorySqliteTests
{
    private async Task<AppDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task SaveAndRetrieve_Works_WithSQLite()
    {
        var context = await CreateContextAsync();
        var repo = new EfQuestionRepository(context);

        var topic = "Science";
        var questions = new List<Question>
        {
            new MultipleChoiceQuestion
            {
                Text = "What is H2O?",
                Choices = new() { "Water", "Oxygen", "Hydrogen" },
                CorrectAnswerIndex = 0,
                Points = 50
            }
        };

        await repo.SaveAsync(topic, questions);
        var loaded = await repo.GetByTopicAsync(topic);

        var q = loaded.FirstOrDefault();
        Assert.NotNull(q);
        Assert.Equal("What is H2O?", q!.Text);
        Assert.Equal(50, q.Points);
    }
}
