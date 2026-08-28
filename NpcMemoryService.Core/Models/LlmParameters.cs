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

        /// <summary>
        ///   Whether <see cref="NpcMemoryService.Core.LlmClient.OpenRouter.OpenRouterClient.CompleteAsync" /> may
        ///   retry once on a truncated/empty/content-filtered reply (see that method's own XML doc for the full
        ///   rule). Default TRUE preserves that existing safety net for every ordinary call (Integrated chat, the
        ///   action interpreter, the summarizers). Set to FALSE to make a single request fail fast on the first
        ///   incomplete reply instead of paying for the client's own bigger-budget retry: the PROSE call in the
        ///   mod's Prose + Interpreter composition mode uses this so a beat the prose model could not finish is
        ///   handed, immediately, to the mod's own fallback (redo the turn once on a reliable explicit model)
        ///   rather than two slow calls back to back.
        /// </summary>
        public bool AllowTruncationRetry { get; init; } = true;

        /// <summary>
        ///   Per-request override of the reasoning keyword. Null (default) defers to the global config
        ///   reasoning dial exactly as today, so an ordinary call (Integrated chat, the action interpreter)
        ///   is unaffected. A non-blank keyword (e.g. "off") overrides the global dial for THIS request
        ///   only: a mechanical housekeeping call (memory compression, the summarizers) can force reasoning
        ///   off even when the player has set the global Mod Options dial to Medium/High, because a
        ///   reasoning model asked to do a purely mechanical extraction can loop on internal reasoning
        ///   ("Wait, let me re-read [7]...") and burn its entire reply budget without producing any text
        ///   (player report).
        /// </summary>
        public string? ReasoningOverride { get; init; }
    }
}
