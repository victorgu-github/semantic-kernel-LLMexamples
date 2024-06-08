using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace SemanticKernelExamples
{
    public class AzureChatCompletionDecorator : IChatCompletion, ITextCompletion
    {
        // Using GPT-3.5 Turbo 4k model
        private const double _minPricePer1000Tokens = 0.0015f;
        // Using GPT-4 32k model
        private const double _maxPricePer1000Tokens = 0.06f;

        private const double _minPricePer1000OutputTokens = 0.002f;
        private const double _maxPricePer1000OutputTokens = 0.12f;

        private AzureChatCompletion _chatCompletion;

        public AzureChatCompletionDecorator(AzureChatCompletion chatCompletion)
        {
            this._chatCompletion = chatCompletion;
        }

        public ChatHistory CreateNewChat(string? instructions = null)
        {
           return _chatCompletion.CreateNewChat(instructions);
        }

        public async Task<IReadOnlyList<IChatResult>> GetChatCompletionsAsync(ChatHistory chat, ChatRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
        {
            var stringData = string.Join("", chat.Select(c => c.Content));
            double tokensCount = GPT3Tokenizer.Encode(stringData).Count / 1000d;

            Console.WriteLine($"{nameof(GetChatCompletionsAsync)} MIN: {tokensCount * _minPricePer1000Tokens:00.00000000}$ MAX: {tokensCount * _maxPricePer1000Tokens:00.00000000}$");

            var result = await _chatCompletion.GetChatCompletionsAsync(chat, requestSettings, cancellationToken);
            foreach(var r in result)
            {
                var chatMessage = await r.GetChatMessageAsync();
                var tokens = GPT3Tokenizer.Encode(chatMessage.Content);
                double outputTokensCount = tokens.Count / 1000d;
                Console.WriteLine($"{nameof(GetChatCompletionsAsync)} OUTPUT: {outputTokensCount * _minPricePer1000OutputTokens:00.00000000}$ MAX: {outputTokensCount * _maxPricePer1000OutputTokens:00.00000000}$");
            }
            return result;
        }

        public async Task<IReadOnlyList<ITextResult>> GetCompletionsAsync(string text, CompleteRequestSettings requestSettings, CancellationToken cancellationToken = default)
        {
            double tokensCount = GPT3Tokenizer.Encode(text).Count / 1000d;
            Console.WriteLine($"{nameof(GetCompletionsAsync)} MIN: {tokensCount * _minPricePer1000Tokens:00.00000000}$ MAX: {tokensCount * _maxPricePer1000Tokens:00.00000000}$");
            var result = await _chatCompletion.GetCompletionsAsync(text, requestSettings, cancellationToken);
            foreach (var r in result)
            {
                var chatMessage = await r.GetCompletionAsync();
                var tokens = GPT3Tokenizer.Encode(chatMessage);
                double outputTokensCount = tokens.Count / 1000d;
                Console.WriteLine($"{nameof(GetCompletionsAsync)} OUTPUT: {outputTokensCount * _minPricePer1000OutputTokens:00.00000000}$ MAX: {outputTokensCount * _maxPricePer1000OutputTokens:00.00000000}$");
            }
            return result;
        }

        public async IAsyncEnumerable<IChatStreamingResult> GetStreamingChatCompletionsAsync(ChatHistory chat, ChatRequestSettings? requestSettings = null, CancellationToken cancellationToken = default)
        {
            var stringData = string.Join("", chat.Select(c => c.Content));
            double tokensCount = GPT3Tokenizer.Encode(stringData).Count / 1000d;
            Console.WriteLine($"{nameof(GetStreamingChatCompletionsAsync)} MIN: {tokensCount * _minPricePer1000Tokens:00.00000000}$ MAX: {tokensCount * _maxPricePer1000Tokens:00.00000000}$");

            var result = _chatCompletion.GetStreamingChatCompletionsAsync(chat, requestSettings, cancellationToken);
            await foreach (var r in result)
            {
                var chatMessages = r.GetStreamingChatMessageAsync();
                double min = 0;
                double max = 0;

                await foreach(var chatMessage in chatMessages)
                {
                    var tokens = GPT3Tokenizer.Encode(chatMessage.Content);
                    double outputTokensCount = tokens.Count / 1000d;
                    min += outputTokensCount * _minPricePer1000OutputTokens;
                    max += outputTokensCount * _maxPricePer1000OutputTokens;
                }
                Console.WriteLine($"{nameof(GetStreamingChatCompletionsAsync)} OUTPUT: {min:00.00000000}$ MAX: {max:00.00000000}$");

                yield return r;
            }
        }

        public async IAsyncEnumerable<ITextStreamingResult> GetStreamingCompletionsAsync(string text, CompleteRequestSettings requestSettings, CancellationToken cancellationToken = default)
        {
            double tokensCount = GPT3Tokenizer.Encode(text).Count / 1000d;
            Console.WriteLine($"{nameof(GetStreamingCompletionsAsync)} MIN: {tokensCount * _minPricePer1000Tokens:00.00000000}$ MAX: {tokensCount * _maxPricePer1000Tokens:00.00000000}$");
            var result = _chatCompletion.GetStreamingCompletionsAsync(text, requestSettings, cancellationToken);
            await foreach (var r in result)
            {
                var chatMessages = r.GetCompletionStreamingAsync();
                double min = 0;
                double max = 0;

                await foreach (var chatMessage in chatMessages)
                {
                    var tokens = GPT3Tokenizer.Encode(chatMessage);
                    double outputTokensCount = tokens.Count / 1000d;
                    min += outputTokensCount * _minPricePer1000OutputTokens;
                    max += outputTokensCount * _maxPricePer1000OutputTokens;
                }
                Console.WriteLine($"{nameof(GetStreamingCompletionsAsync)} OUTPUT: {min:00.00000000}$ MAX: {max:00.00000000}$");

                yield return r;
            }
        }
    }
}
