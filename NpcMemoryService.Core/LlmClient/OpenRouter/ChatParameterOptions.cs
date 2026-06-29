// Code written by Gabriel Mailhot, 29/06/2026.

namespace NpcMemoryService.Core.LlmClient.OpenRouter
{
   /// <summary>
   ///   How a chat-completion request should name and shape its sampling parameters for the target model.
   ///   Most providers (and the OpenRouter/NanoGPT aggregators) accept the classic shape — <c>max_tokens</c>
   ///   plus an explicit <c>temperature</c>. The OpenAI reasoning models, reached directly, reject both: they
   ///   want <c>max_completion_tokens</c> and refuse a custom <c>temperature</c>. The host decides which shape
   ///   to ask for (see the mod's parameter-compatibility policy) and supplies it through
   ///   <see cref="OpenRouterConfig.ParameterOptionsProvider" />.
   /// </summary>
   public readonly struct ChatParameterOptions
   {
      public ChatParameterOptions(bool useMaxCompletionTokens, bool includeTemperature)
      {
         UseMaxCompletionTokens = useMaxCompletionTokens;
         IncludeTemperature = includeTemperature;
      }

      /// <summary>True to send the output cap as <c>max_completion_tokens</c>; false for the legacy <c>max_tokens</c>.</summary>
      public bool UseMaxCompletionTokens { get; }

      /// <summary>True to include a <c>temperature</c> field; false to omit it (reasoning models reject a custom value).</summary>
      public bool IncludeTemperature { get; }

      /// <summary>The classic, maximally-portable shape: <c>max_tokens</c> plus an explicit <c>temperature</c>.</summary>
      public static ChatParameterOptions Standard => new ChatParameterOptions(useMaxCompletionTokens: false, includeTemperature: true);
   }
}
