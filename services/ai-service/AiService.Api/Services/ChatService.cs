using AiService.Api.Dtos;
using AiService.Api.Groq;

namespace AiService.Api.Services;

public sealed class ChatService(IGroqClient groqClient) : IChatService
{
    private const string SystemPrompt =
        "Bạn là một Giảng viên Ảo chuyên về môn Tư tưởng Hồ Chí Minh, hỗ trợ học viên đào tạo dài " +
        "hạn tại Học viện Kỹ thuật Quân sự. Trả lời chính xác, súc tích, bằng tiếng Việt, bám sát " +
        "nội dung học phần. Nếu câu hỏi nằm ngoài phạm vi môn học, hãy nhắc học viên quay lại chủ đề.";

    private const int MaxHistoryMessages = 12;
    private const int MaxTokens = 1024;

    public async Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct)
    {
        var history = request.Messages.TakeLast(MaxHistoryMessages)
            .Select(m => new GroqMessage(m.Role, m.Content));

        var messages = new List<GroqMessage> { new("system", SystemPrompt) };
        messages.AddRange(history);

        var reply = await groqClient.CompleteAsync(messages, MaxTokens, ct);
        return new ChatResponse(reply);
    }
}
