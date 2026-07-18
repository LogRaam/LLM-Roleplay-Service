namespace NpcMemoryService.Core.Models
{
    /// <summary>
    /// Provider-agnostic generation settings.
    /// Each provider maps these to its own parameter names.
    /// </summary>
    public sealed class LlmParameters
    {
        public int   MaxTokens  { get; init; } = 1000;

        /// <summary>
        /// Creativity level from 0.0 (deterministic) to 1.0 (very creative).
        /// Maps to <c>temperature</c> or equivalent on each provider.
        /// </summary>
        public float Creativity { get; init; } = 0.7f;

        /// <summary>
        ///   Maps to <c>presence_penalty</c>: how strongly the model is pushed away from words it has
        ///   already used. Adult-prompt audit M13 added it because the anti-repetition requirement rested
        ///   ENTIRELY on prompt text, which is precisely what a weaker model ignores; a sampling penalty is
        ///   the one lever that also works on them.
        ///   <para>
        ///     Kept modest and OFF by default (0 omits the field entirely, so every provider keeps receiving
        ///     the payload it received before). It is a blunt instrument: it penalises legitimately repeated
        ///     tokens too, and character and place names are the ones that recur most in roleplay, so a high
        ///     value buys variety at the cost of the model drifting off its own cast.
        ///   </para>
        /// </summary>
        public float PresencePenalty { get; init; }
    }
}
