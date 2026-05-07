using InterviewService.Core.Models;

namespace InterviewService.Core.Entities;

/// <summary>
/// Domain aggregate that owns interview progress, turn rules, required answers, dynamic steps, and conclusion state.
/// </summary>
public class Interview
{
    private readonly List<Answer> _answersToRequiredQuestions;
    private readonly List<InterviewStep> _dynamicPart;
    private Question? _currentQuestion;

    public Interview(
        Guid id,
        IReadOnlyList<Answer>? requiredAnswers = null,
        IReadOnlyList<InterviewStep>? completedDynamicSteps = null,
        Question? currentQuestion = null,
        UserProfile? conclusion = null,
        InterviewSetup? setup = null)
    {
        Setup = setup ?? throw new ArgumentException($"{nameof(setup)} must not be null", nameof(setup));
        Id = id;
        _answersToRequiredQuestions = requiredAnswers?.ToList() ?? [];
        _dynamicPart = completedDynamicSteps?.ToList() ?? [];
        _currentQuestion = currentQuestion;
        Conclusion = conclusion;

        if (!IsNewInterviewFromSetupOnly())
        {
            ValidateState();
        }
    }

    public Guid Id { get; }

    public InterviewSetup Setup { get; }

    public IReadOnlyList<Answer> RequiredAnswers => _answersToRequiredQuestions;

    public IReadOnlyList<InterviewStep> CompletedDynamicSteps => _dynamicPart;

    public bool AllRequiredQuestionsAnswered =>
        _answersToRequiredQuestions.Count >= Setup.RequiredQuestions.Count;

    public int CurrentStep => _answersToRequiredQuestions.Count + _dynamicPart.Count;

    public Question? CurrentQuestion => AllRequiredQuestionsAnswered
        ? _currentQuestion
        : _currentQuestion ?? Setup.RequiredQuestions[_answersToRequiredQuestions.Count];

    public UserProfile? Conclusion { get; private set; }

    public bool IsFinished => Conclusion is not null;

    public bool IsInterviewersTurn => CurrentQuestion is null;

    public bool IsUsersTurn => !IsInterviewersTurn;

    public void AddQuestion(Question question)
    {
        if (IsFinished)
        {
            throw new InvalidOperationException(
                "Interview cannot be altered or continued after it is already finished");
        }

        if (!IsInterviewersTurn)
        {
            throw new InvalidOperationException("Questions can only be added in interviewer's turn");
        }

        _currentQuestion = question ?? throw new ArgumentNullException(nameof(question));
    }

    public void AddAnswer(Answer answer)
    {
        if (IsFinished)
        {
            throw new InvalidOperationException(
                "Interview cannot be altered or continued after it is already finished");
        }

        if (!IsUsersTurn)
        {
            throw new InvalidOperationException("Answers can only be added in user's turn");
        }

        var value = answer ?? throw new ArgumentNullException(nameof(answer));

        if (!AllRequiredQuestionsAnswered)
        {
            _answersToRequiredQuestions.Add(value);
            _currentQuestion = null;
            return;
        }

        _dynamicPart.Add(new InterviewStep
        {
            Question = CurrentQuestion ?? throw new InvalidOperationException("Current question is missing"),
            Answer = value,
        });
        _currentQuestion = null;
    }

    public void Conclude(UserProfile conclusion)
    {
        if (IsFinished)
        {
            throw new InvalidOperationException("Interview is already finished, cannot alter conclusion");
        }

        if (!IsInterviewersTurn)
        {
            throw new InvalidOperationException("Conclusion can only be added on interviewer's turn");
        }

        Conclusion = conclusion ?? throw new ArgumentNullException(nameof(conclusion));
    }

    public List<InterviewStep> GetTranscript()
    {
        var result = _answersToRequiredQuestions
            .Select((answer, index) => new InterviewStep
            {
                Question = Setup.RequiredQuestions[index],
                Answer = answer,
            })
            .ToList();
        result.AddRange(_dynamicPart);
        return result;
    }

    private bool IsNewInterviewFromSetupOnly()
    {
        return _answersToRequiredQuestions.Count == 0 &&
               _dynamicPart.Count == 0 &&
               _currentQuestion is null &&
               Conclusion is null;
    }

    private void ValidateState()
    {
        var requiredAnswerCount = _answersToRequiredQuestions.Count;
        var requiredQuestionCount = Setup.RequiredQuestions.Count;

        if (requiredAnswerCount > requiredQuestionCount)
        {
            throw new ArgumentException(
                $"Number of {nameof(RequiredAnswers)} cannot be higher than the number of required questions in setup");
        }

        if (requiredAnswerCount < requiredQuestionCount && Conclusion is not null)
        {
            throw new ArgumentException("Interview cannot have a conclusion during a required part");
        }

        if (requiredAnswerCount < requiredQuestionCount && _dynamicPart.Count > 0)
        {
            throw new ArgumentException(
                "Dynamic part of an interview cannot begin before answering all of the required questions");
        }

        if (Conclusion is not null && _currentQuestion is not null)
        {
            throw new ArgumentException("Finished interview cannot have a pending question");
        }
    }
}
