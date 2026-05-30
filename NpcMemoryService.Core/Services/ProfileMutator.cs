// Code written by Gabriel Mailhot, 18/05/2026.
// Updated 28/05/2026: Guard against empty/blank event summaries to keep history clean.

#region

using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Services
{
    /// <summary>
    ///   Applies the structured outcome of an LLM dialogue turn (a <see cref="ParsedResponse" />)
    ///   to an <see cref="NpcProfile" />. Centralizes the mutation logic so it can be reused
    ///   by the mod, the ConsoleRunner, and tests without duplication.
    ///   Keeps <see cref="NpcProfile" /> as a clean POCO — it does not know about parsing,
    ///   and parsing does not know about NpcProfile. The mutator bridges the two.
    /// </summary>
    public static class ProfileMutator
    {
        /// <summary>
        ///   Applies the response to the profile:
        ///   - <see cref="ParsedResponse.NewEventData" /> → appends a <see cref="NotableEvent" />
        ///     (skipped if Summary is empty or whitespace-only — LLMs occasionally emit
        ///     syntactically valid but semantically empty [EVENT] blocks).
        ///   - <see cref="ParsedResponse.Reputation" />   → adjusts <see cref="NpcProfile.ReputationWithPlayer" />.
        ///   The Memory section is descriptive only (topic/sentiment/decision) and is NOT
        ///   persisted to the profile by default — it is information the LLM emitted for
        ///   its own future reasoning, already implicit in the event summaries.
        /// </summary>
        /// <param name="profile">The NPC whose memory is being updated. Must not be null.</param>
        /// <param name="response">The parsed LLM response.</param>
        /// <param name="gameDay">The current game day, used to timestamp the new event.</param>
        public static void Apply(NpcProfile profile, ParsedResponse response, int gameDay)
        {
            // Append event when present and non-trivial.
            // An empty or whitespace summary would pollute the history with lines
            // like "Day N (Other):" that carry no information for future prompts.
            if (response.NewEventData != null
                && !string.IsNullOrWhiteSpace(response.NewEventData.Summary))
            {
                profile.Events.Add(new NotableEvent(
                    gameDay,
                    response.NewEventData.Type,
                    response.NewEventData.Summary));
            }

            // Adjust reputation when present
            if (response.Reputation != null) profile.ReputationWithPlayer += response.Reputation.ClanDelta ?? 0;
        }
    }
}
