// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.Embeddings;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Http;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Sequential;
using Microsoft.SemanticKernel.Planning.Stepwise;
using Microsoft.SemanticKernel.Reliability;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.Services;
using Microsoft.SemanticKernel.SkillDefinition;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.TemplateEngine.Prompt;
using Microsoft.SemanticKernel.Text;
using Newtonsoft.Json;
using SemanticKernelExamples;
using System.Text.Json.Serialization;
using System.Threading;

Console.WriteLine("Starting...");

var modelName = "gpt-35-turbo";
var embeddingModelName = "text-embedding-ada-002";
var azureOpenAiEndpoint = "";
var azureOpenAiKey = "";

var serviceCollection = new ServiceCollection();
var aiServiceCollection = new AIServiceCollection();

var azureChatCompletion = new AzureChatCompletion(modelName, azureOpenAiEndpoint, azureOpenAiKey);
var azureChatCompletionDecorator = new AzureChatCompletionDecorator(azureChatCompletion);
var embeddingGenerationService = new AzureTextEmbeddingGeneration(embeddingModelName, azureOpenAiEndpoint, azureOpenAiKey);
var embeddingGenerationServiceDecorator = new AzureEmbeddingGenerationServiceDecorator(embeddingGenerationService);
var memoryStore = new VolatileMemoryStore();
var textMemory = new SemanticTextMemory(memoryStore, embeddingGenerationServiceDecorator);
await textMemory.SaveInformationAsync("quotes", id: "quote1", text: "All the world's a stage, and all the men and women merely players.", description: "William Shakespeare");
await textMemory.SaveInformationAsync("quotes", id: "quote2", text: "I`ve been writing this article for a long time", description: "VictorGu");

aiServiceCollection.SetService<IChatCompletion>(()=> azureChatCompletionDecorator);
aiServiceCollection.SetService<ITextCompletion>(() => azureChatCompletionDecorator);
aiServiceCollection.SetService<ITextEmbeddingGeneration>(() => embeddingGenerationServiceDecorator);

serviceCollection.AddScoped<ILoggerFactory>(_ => NullLoggerFactory.Instance);
serviceCollection.AddScoped<ISkillCollection, SkillCollection>();
serviceCollection.AddScoped<IPromptTemplateEngine, PromptTemplateEngine>();
serviceCollection.AddScoped<IAIServiceProvider>(_ => aiServiceCollection.Build());
serviceCollection.AddScoped<IKernel, Kernel>();
serviceCollection.AddScoped<IDelegatingHandlerFactory, DefaultHttpRetryHandlerFactory>();
serviceCollection.AddScoped<ISemanticTextMemory>(sp => textMemory);
serviceCollection.AddScoped<ISequentialPlanner, SequentialPlanner>();

serviceCollection.AddScoped<ComplexSkill>();
serviceCollection.AddScoped<CustomTextSkill>();

var serviceProvider = serviceCollection.BuildServiceProvider();

using var scope = serviceProvider.CreateAsyncScope();
var kernel = scope.ServiceProvider.GetRequiredService<IKernel>();
var sequentialPlanner = scope.ServiceProvider.GetRequiredService<ISequentialPlanner>();
var promptTemplateEngine = scope.ServiceProvider.GetRequiredService<IPromptTemplateEngine>();

//var customSkills = kernel.ImportSkill(new CustomTextSkill(), nameof(CustomTextSkill));


var complextSkills = kernel.ImportSkill(scope.ServiceProvider.GetRequiredService<ComplexSkill>(), nameof(ComplexSkill));

// simple behaviour done by LLM
var inlinePrompt = @"Encode {{$text}} And Add author name to the end";


while (true)
{
    Console.WriteLine("Enter a text to encode or 'exit' to exit:");
    var input = Console.ReadLine();
    if (string.IsNullOrEmpty(input))
    {
        continue;
    }

    if (input == "exit") break;
    

    try
    {
        var context = kernel.CreateNewContext();
        context.Variables.TryAdd("text", input);
        context.Variables.TryAdd("name", "VictorGu");
        context.Variables.TryAdd("chars", "aeiou");

        var updatedInput = await promptTemplateEngine.RenderAsync(inlinePrompt, context);

        // Get token`s count
        var tokenCount = GPT3Tokenizer.Encode(updatedInput).Count;
        Console.WriteLine($"Tokens count: {tokenCount}");

        var plan = await sequentialPlanner.CreatePlanAsync(updatedInput);

        var result = await plan.InvokeAsync(context);

        Console.WriteLine("Output: {0}", result.Result);

    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

}