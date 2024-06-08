using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelExamples
{
    public class AzureEmbeddingGenerationServiceDecorator : ITextEmbeddingGeneration
    {
        private const double _pricePer1000Tokens = 0.0001f;

        private readonly AzureTextEmbeddingGeneration _azureTextEmbeddingGeneration;

        public AzureEmbeddingGenerationServiceDecorator(AzureTextEmbeddingGeneration azureTextEmbeddingGeneration)
        {
            _azureTextEmbeddingGeneration = azureTextEmbeddingGeneration;
        }

        public Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> data, CancellationToken cancellationToken = default)
        {
            var stringData = string.Join("", data);
            double tokensCount = GPT3Tokenizer.Encode(stringData).Count / 1000d;
            Console.WriteLine($"{nameof(GenerateEmbeddingsAsync)} {tokensCount * _pricePer1000Tokens:00.00000000}$");
            return _azureTextEmbeddingGeneration.GenerateEmbeddingsAsync(data, cancellationToken);
        }
    }
}
