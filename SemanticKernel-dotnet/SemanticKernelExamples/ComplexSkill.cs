using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.TemplateEngine;
using System.ComponentModel;

namespace SemanticKernelExamples
{
    public class ComplexSkill
    {
        private readonly ISemanticTextMemory _semanticTextMemory;
        private readonly IChatCompletion _chatCompletion;
        private readonly IPromptTemplateEngine _promptTemplateEngine;
        private readonly IKernel _kernel;

        private const string _encodingPrompt = "Encode message {{$text}} return only encoded message without other text";
        private const string _defaultName = "Unknown Author";
        private const string _historyCollection = "quotes";

        public ComplexSkill(IKernel kernel)
        {
            this._semanticTextMemory = kernel.Memory;
            this._chatCompletion = kernel.GetService<IChatCompletion>();
            _promptTemplateEngine = kernel.PromptTemplateEngine;
            _kernel = kernel;
        }

        [SKFunction, Description("Encode message")]
        [SKParameter("input", "Message to encode")]
        public async Task<string> SimpleMessageEncodingAsync(SKContext context)
        {
            var prompt = await _promptTemplateEngine.RenderAsync(_encodingPrompt, context);

            var response = "";

            var chatHistory = _chatCompletion.CreateNewChat("You are an AI assistant that helps to encode messages with base64");
            chatHistory.AddUserMessage(prompt);

            await foreach (string message in _chatCompletion.GenerateMessageStreamAsync(chatHistory, new ChatRequestSettings
            {
                MaxTokens = 1000
            }))
            {
                response += message;
            }

            return response;
        }

        [SKFunction, Description("Append author name to message")]
        public async Task<string> AppendAuthorNameAsync(
            [Description("encoded message")] string message, SKContext context)
        {
            const int maxResults = 1;
            const double minRelevanceScore = 0.3;
            var text = context.Variables["text"];
            var authorsInfo = await _semanticTextMemory.SearchAsync(_historyCollection, text, maxResults, minRelevanceScore).ToListAsync();
            var author = authorsInfo.FirstOrDefault();

            Console.WriteLine("Original phrase could be `{0}` by `{1}`", author?.Metadata.Text, author?.Metadata.Description);
            var name = author?.Metadata.Description ?? _defaultName;

            return $"{message} (C) {name}";
        }
    }
}
