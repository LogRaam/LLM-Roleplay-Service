// Code written by Gabriel Mailhot, 11/09/2026.
//
// Bug report (fkasad + iwasajedionce, Nexus, 2026-09-11), with the server's own words attached:
//
//   Jinja Exception: After the optional system message, conversation roles must alternate user and
//   assistant roles except for tool calls and results.
//
// fkasad read the mechanism correctly and then blamed the wrong party: "ministral-3 is too strict/not smart
// enough for CR". The template is doing what the OpenAI protocol allows a server to do. It is CR that emits a
// shape a strict template must refuse, in two distinct ways, both of them deliberate features:
//
//   1. A room speaks. AddWitnessStatement writes each witness as a USER message carrying a "[Name]: " prefix,
//      so two witnesses reacting in one beat put two or three user messages in a row. AddPriorExchange does
//      the same thing once more, immediately before the player's own first message, which makes a doubled
//      user turn certain on the FIRST turn of any chat opened out of a vanilla conversation.
//   2. A conversation can open with the NPC. SeedNpcOpening carries the line the player just heard in the
//      vanilla dialogue, as an ASSISTANT message, before any user turn exists at all.
//
// OpenRouter and the other aggregators normalise both away before the model sees them, which is why this was
// invisible for as long as everyone was on OpenRouter, and why iwasajedionce saw the interpreter succeed while
// prose failed: the interpreter call is a short mechanical extraction whose message list happens to alternate,
// and the prose call is the one carrying the room. He read that asymmetry as one model being unable to send to
// the other. There is no such channel; there are two independent HTTP clients, and only one of them was
// sending a shape the server would refuse.
//
// So this is ours, and the repair belongs at the wire, not in the features: the room, the bridge and the
// opening line are all worth keeping exactly as they are.

#region

using System;
using System.Collections.Generic;

#endregion

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Turns the conversation as CR models it into the shape every chat template can accept: a strictly
    ///   alternating user/assistant sequence that begins with the user.
    ///   <para>
    ///     Nothing is lost in the translation, and that is the point. Who said what is already carried INSIDE
    ///     the text by the "[Name]: " prefixes the witness path writes, so merging two neighbouring turns of
    ///     the same role costs no attribution: the model reads the same room either way.
    ///   </para>
    /// </summary>
    public static class MessageAlternationPolicy
    {
        /// <summary>
        ///   Frames an NPC line that has no player turn before it. A conversation may open with the NPC (the
        ///   line the player just heard in the vanilla dialogue), but a template that demands the user speak
        ///   first cannot be given an assistant turn to start with. It is presented as what it actually is,
        ///   speech that already happened, in the same words <see cref="ChatSession.AddPriorExchange" /> uses
        ///   for the vanilla exchange: not a re-labelling of who spoke, but a statement of when.
        /// </summary>
        public const string OpeningLabel =
            "[Said to you a moment ago, before this exchange began. It is the ground you are standing on, "
            + "not a question waiting on you.]";

        /// <summary>Blank line between two merged turns, so a reader (and a model) still sees two utterances.</summary>
        private const string Join = "\n\n";

        /// <summary>
        ///   The same conversation, alternating. Blank turns are dropped, an opening NPC line becomes labelled
        ///   history on the user side, and neighbouring turns of one role become a single turn.
        /// </summary>
        public static IReadOnlyList<LlmMessage> Normalize(IReadOnlyList<LlmMessage> messages)
        {
            var result = new List<LlmMessage>();
            if (messages == null) return result;

            foreach (LlmMessage message in messages)
            {
                if (message == null || string.IsNullOrWhiteSpace(message.Content)) continue;

                MessageRole role = message.Role;
                string content = message.Content.Trim();

                // Nothing has been said yet and the NPC is speaking: say WHEN, and let the user side carry it.
                if (result.Count == 0 && role == MessageRole.Assistant)
                {
                    role = MessageRole.User;
                    content = OpeningLabel + "\n" + content;
                }

                if (result.Count > 0 && result[result.Count - 1].Role == role)
                {
                    result[result.Count - 1] = new LlmMessage(role, result[result.Count - 1].Content + Join + content);

                    continue;
                }

                result.Add(new LlmMessage(role, content));
            }

            return result;
        }

        /// <summary>
        ///   True when a sequence is already what a strict template demands: it starts with the user and no two
        ///   neighbours share a role. Exposed so a guard can assert the property rather than re-describe the
        ///   algorithm that produces it.
        /// </summary>
        public static bool Alternates(IReadOnlyList<LlmMessage> messages)
        {
            if (messages == null || messages.Count == 0) return true;
            if (messages[0].Role != MessageRole.User) return false;

            for (var i = 1; i < messages.Count; i++)
                if (messages[i].Role == messages[i - 1].Role)
                    return false;

            return true;
        }
    }
}
