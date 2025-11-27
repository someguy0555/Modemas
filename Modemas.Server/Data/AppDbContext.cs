using Microsoft.EntityFrameworkCore;
using Modemas.Server.Models;

namespace Modemas.Server.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionTopicGroup> QuestionTopicGroups => Set<QuestionTopicGroup>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Question>()
            .HasDiscriminator<string>("QuestionType")
            .HasValue<MultipleChoiceQuestion>("MultipleChoice")
            .HasValue<MultipleAnswerQuestion>("MultipleAnswer")
            .HasValue<TrueFalseQuestion>("TrueFalse");

        modelBuilder.Entity<QuestionTopicGroup>()
            .HasMany(qtg => qtg.Questions)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuestionTopicGroup>()
            .HasIndex(q => q.Topic)
            .IsUnique();
    }
}
