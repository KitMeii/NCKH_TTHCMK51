using FluentValidation;
using QuizService.Api.Dtos;

namespace QuizService.Api.Validators;

public sealed class CreateQuestionRequestValidator : AbstractValidator<CreateQuestionRequest>
{
    public CreateQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.OptionA).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OptionB).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OptionC).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OptionD).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Explanation).MaximumLength(2000);
        RuleFor(x => x.CorrectAnswer).InclusiveBetween(0, 3);
    }
}

public sealed class UpdateQuestionRequestValidator : AbstractValidator<UpdateQuestionRequest>
{
    public UpdateQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.OptionA).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OptionB).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OptionC).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OptionD).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Explanation).MaximumLength(2000);
        RuleFor(x => x.CorrectAnswer).InclusiveBetween(0, 3);
    }
}

public sealed class CreateOralQuestionRequestValidator : AbstractValidator<CreateOralQuestionRequest>
{
    public CreateOralQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ExpectedAnswer).MaximumLength(4000);
        RuleFor(x => x.Difficulty).InclusiveBetween(1, 3);
    }
}

public sealed class UpdateOralQuestionRequestValidator : AbstractValidator<UpdateOralQuestionRequest>
{
    public UpdateOralQuestionRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.ExpectedAnswer).MaximumLength(4000);
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
        RuleFor(x => x.MainAnswer).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.FollowupAnswers).Must(a => a is null || a.Count <= 10)
            .WithMessage("Tối đa 10 câu trả lời bổ sung.");
        RuleForEach(x => x.FollowupAnswers).MaximumLength(4000);
    }
}
