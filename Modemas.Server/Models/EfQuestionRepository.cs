using Microsoft.EntityFrameworkCore;
using Modemas.Server.Data;
using Modemas.Server.Interfaces;

namespace Modemas.Server.Models;

public class EfQuestionRepository : IQuestionRepository
{
    private readonly AppDbContext _context;

    public EfQuestionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Question>> GetByTopicAsync(string topic)
    {
        var group = await _context.QuestionTopicGroups
            .Include(g => g.Questions)
            .FirstOrDefaultAsync(g => g.Topic.ToLower() == topic.ToLower());

        return group?.Questions ?? new List<Question>();
    }

    public async Task SaveAsync(string topic, IEnumerable<Question> questions)
    {
        var group = await _context.QuestionTopicGroups
            .Include(g => g.Questions)
            .FirstOrDefaultAsync(g => g.Topic.ToLower() == topic.ToLower());

        if (group == null)
        {
            group = new QuestionTopicGroup { Topic = topic };
            _context.QuestionTopicGroups.Add(group);
        }

        group.Questions.Clear();
        foreach (var q in questions)
        {
            group.Questions.Add(q);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<string>> GetAllTopicsAsync()
        => await _context.QuestionTopicGroups
            .Select(g => g.Topic)
            .ToListAsync();

    public async Task DeleteAsync(string topic)
    {
        var group = await _context.QuestionTopicGroups

            .FirstOrDefaultAsync(g => g.Topic.ToLower() == topic.ToLower());
        if (group == null) return;

        _context.QuestionTopicGroups.Remove(group);
        await _context.SaveChangesAsync();
    }

    public async Task CreateTopicAsync(string topic)
    {
        if (await _context.QuestionTopicGroups.AnyAsync(g => g.Topic.ToLower() == topic.ToLower()))
            return;

        _context.QuestionTopicGroups.Add(new QuestionTopicGroup
        {
            Topic = topic,
            Questions = new List<Question>()
        });

        await _context.SaveChangesAsync();
    }

    public async Task<bool> RenameTopicAsync(string oldTopic, string newTopic)
    {
        var group = await _context.QuestionTopicGroups
            .FirstOrDefaultAsync(g => g.Topic.ToLower() == oldTopic.ToLower());
        if (group == null) return false;

        group.Topic = newTopic;
        await _context.SaveChangesAsync();
        return true;
    }
}
