// Code written by Gabriel Mailhot, 10/09/2026.
// Bug report (DuskSymphony, Nexus tracker, v2.5.7): "Compact Prompt mode is still sending a prompt with context
// over 9k", with his local server's own refusal quoted:
//
//   send_error: task id = 9, error: request (9496 tokens) exceeds the available context size (8192 tokens)
//
// "this is on a fresh install of CR on the first attempt at interacting with an NPC and I don't believe I have any
// mods that would affect it. Compact Prompt mode is turned on, and I've restarted just in case."
//
// He is right, and the cause is embarrassing in a useful way. The Lean prompt is guarded by a budget test that has
// been green for months. That test builds a PromptBuilder with NO ActionVocabulary, and AppendActionInstructions
// returns on its first line when the vocabulary is empty, so the test measured a prompt in which the entire GAME
// ACTIONS section does not exist. It measured 6.9k characters of a document the game never sends.
//
// The real thing carries all 69 actions with their full descriptions, in EVERY prompt, Lean included: 11,565
// characters, about 2,900 tokens, of action list alone. Nothing trimmed it, because nothing had ever measured it.
//
// So Compact mode was never compact, and a player with an 8k local model could not hold a single conversation on
// a fresh install. This file is the trim, and the budget tests now build with the real vocabulary so the number
// they print is the number that is sent.
//
// WHAT IS NOT DONE HERE, deliberately: no action is REMOVED from a small model's vocabulary. Dropping verbs would
// change what a character can do depending on which model the player runs, which is a design decision and not a
// budget one. Only the prose is shortened, so every verb stays emittable and the meaning survives; the long form
// exists to disambiguate near-neighbours for a strong model, and a small model was never reading 300 characters
// about execute_player with much care in the first place.

using System;

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>How much of each action's description a prompt can afford to spell out.</summary>
    public static class ActionVocabularyPolicy
    {
        /// <summary>
        ///   What one action's description is cut to in Lean. Chosen so all 69 fit in roughly a third of what
        ///   they cost unabridged, while still leaving room for the clause that distinguishes an action from its
        ///   nearest neighbour, which is what these descriptions are mostly for.
        /// </summary>
        public const int LeanDescriptionChars = 90;

        /// <summary>
        ///   The description as this prompt should carry it. Full prompts get every word. Lean gets the first
        ///   sentence when it is short enough, otherwise a clean cut at a word: never mid-word, and never mid
        ///   parenthesis, since a description that stops at "(never" reads as broken rather than as brief.
        /// </summary>
        public static string Describe(string description, LeanPromptLevel lean)
        {
            string text = (description ?? "").Trim();
            if (lean != LeanPromptLevel.Lean || text.Length <= LeanDescriptionChars) return text;

            int stop = text.IndexOf(". ", StringComparison.Ordinal);
            if (stop > 0 && stop + 1 <= LeanDescriptionChars) return text.Substring(0, stop + 1);

            int cut = text.LastIndexOf(' ', Math.Min(LeanDescriptionChars, text.Length - 1));
            if (cut <= 0) cut = Math.Min(LeanDescriptionChars, text.Length);

            string trimmed = text.Substring(0, cut).TrimEnd(',', ';', ':', '(', '[', ' ');

            // An unbalanced opening bracket reads as a truncation bug to anyone reading the log; close the phrase
            // at the bracket instead of leaving it hanging.
            int opened = trimmed.LastIndexOf('(');
            if (opened > 0 && trimmed.IndexOf(')', opened) < 0) trimmed = trimmed.Substring(0, opened).TrimEnd();

            return trimmed + "...";
        }
    }
}
