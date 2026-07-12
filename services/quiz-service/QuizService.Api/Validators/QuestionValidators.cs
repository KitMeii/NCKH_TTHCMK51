using FluentValidation;
using QuizService.Api.Dtos;

namespace QuizService.Api.Validators;

public sealed class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty();
        RuleFor(x => x.OptionA).NotEmpty();
        RuleFor(x => x.OptionB).NotEmpty();
        RuleFor(x => x.OptionC).NotEmpty();
        RuleFor(x => x.OptionD).NotEmpty();
        RuleFor(x => x.CorrectAnswer).InclusiveBetween(0, 3);
    }
}

public sealed class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{
    public UpdateQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty();
        RuleFor(x => x.OptionA).NotEmpty();
        RuleFor(x => x.OptionB).NotEmpty();
        RuleFor(x => x.OptionC).NotEmpty();
        RuleFor(x => x.OptionD).NotEmpty();
        RuleFor(x => x.CorrectAnswer).InclusiveBetween(0, 3);
    }
}

public sealed class CreateOralQuestionRequestValidator : AbstractValidator<CreateOralQuestionRequest>
{
    public CreateOralQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty();
        RuleFor(x => x.Difficulty).InclusiveBetween(1, 3);
    }
}

public sealed class UpdateOralQuestionRequestValidator : AbstractValidator<UpdateOralQuestionRequest>
{
    public UpdateOralQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty();
        RuleFor(x => x.Difficulty).InclusiveBetween(1, 3);
    }
}

public sealed class SubmitQuizRequestValidator : AbstractValidator<SubmitQuizRequest>
{
    public SubmitQuizRequestValidator()
    {
        RuleFor(x => x.Answers).NotEmpty();
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.SelectedOption).InclusiveBetween(0, 3);
        });
    }
}

public sealed class SubmitExamRequestValidator : AbstractValidator<SubmitExamRequest>
{
    public SubmitExamRequestValidator()
    {
        RuleFor(x => x.Answers).NotEmpty();
        RuleFor(x => x.TimeSpentSeconds).GreaterThanOrEqualTo(0);
        RuleForEach(x => x.Answers).ChildRules(answer =>
        {
            answer.RuleFor(a => a.SelectedOption).InclusiveBetween(0, 3);
        });
    }
}

public sealed class SubmitOralRequestValidator : AbstractValidator<SubmitOralRequest>
{
    public SubmitOralRequestValidator()
    {
        RuleFor(x => x.MainAnswer).NotEmpty();
    }
}
