using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace SemanticKernelExamples
{
    public class CustomTextSkill
    {
        [SKFunction, Description("Append author name to message")]
        public string AppendAuthorName(
            [Description("Message to add")] string message,
            [Description("Name to add")] string name)
        {
            Console.WriteLine("Adding author name to text");
            return $"{message} (C){name}";
        }

        [SKFunction, Description("Encode message")]
        [SKParameter("input", "Message to encode")]
        public string SimpleMessageEncoding(SKContext context)
        {
            var input = context.Variables["text"];
            var charsToRemove = context.Variables["chars"];
            if(charsToRemove == null) {
                return input;
            }
            foreach(var ch in charsToRemove)
            {
                input = Regex.Replace(
                    input, 
                    ch.ToString(),
                    string.Empty,
                    RegexOptions.IgnoreCase
                );
            }
            return input;
        }
    }
}
