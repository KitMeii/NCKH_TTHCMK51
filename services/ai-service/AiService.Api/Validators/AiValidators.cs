using AiService.Api.Dtos;
using FluentValidation;

namespace AiService.Api.Validators;

public sealed class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Messages).NotEmpty();
        RuleForEach(x => x.Messages).ChildRules(message =>
        {
            message.RuleFor(m => m.Role).Must(role => role is "user" or "assistant")
                .WithMessage("Role phải là 'user' hoặc 'assistant' — không được ghi đè system prompt.");
            message.RuleFor(m => m.Content).NotEmpty().MaximumLength(8000);
        });
    }
}

public sealed class GenerateLectureRequestValidator : AbstractValidator<GenerateLectureRequest>
{
    public GenerateLectureRequestValidator()
    {
        RuleFor(x => x.Chapter).NotEmpty();
        RuleFor(x => x.Topic).NotEmpty();
        RuleFor(x => x.SourceText).NotEmpty().MaximumLength(20000);
    }
}

public sealed class GenerateComprehensionQuestionsRequestValidator : AbstractValidator<GenerateComprehensionQuestionsRequest>
{
    public GenerateComprehensionQuestionsRequestValidator()
    {
        RuleFor(x => x.Chapter).NotEmpty();
        RuleFor(x => x.SourceText).NotEmpty().MaximumLength(20000);
    }
}

public sealed class GradeOralRequestValidator : AbstractValidator<GradeOralRequest>
{
    public GradeOralRequestValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty();
        RuleFor(x => x.MainAnswer).NotEmpty();
    }
}

public sealed class ExtractQuestionsRequestValidator : AbstractValidator<ExtractQuestionsRequest>
{
    public ExtractQuestionsRequestValidator()
    {
        RuleFor(x => x.Chapter).NotEmpty();
        RuleFor(x => x.SourceText).NotEmpty().MaximumLength(50000);
        RuleFor(x => x.Count).InclusiveBetween(1, 30);
    }
}
