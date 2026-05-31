// Code written by Gabriel Mailhot, 18/05/2026.
// Updated 23/05/2026: DIALOGUE STYLE, BEHAVIOR GUIDELINES, Traits (Sprint 8.1).
// Updated 24/05/2026: AdultContentLevel + AppendRomanticContext (Sprint 8.2).

#region

using System.Collections.Generic;
using System.Text;
using NpcMemoryService.Core.Models;

#endregion

namespace NpcMemoryService.Core.Prompts
{
    /// <summary>
    ///   Assembles the system prompt with stability-ordered sections:
    ///   the most stable content (universal format rules) comes first to maximize
    ///   prompt cache hit rate on providers with prefix-matching caching (xAI Grok,
    ///   OpenAI GPT-4o) or explicit cache breakpoints (Anthropic Claude).
    ///
    ///   Sprint 8.2: optional romantic context injected at a stable position
    ///   (between Identity and BackgroundContext), gated by <see cref="AdultLevel"/>.
    /// </summary>
    public sealed class PromptBuilder : IPromptBuilder
    {
        public IReadOnlyList<GameActionDefinition> ActionVocabulary { get; init; }
            = new List<GameActionDefinition>();

        /// <summary>Controls how much romantic content is injected. Default Off.</summary>
        public AdultContentLevel AdultLevel { get; init; } = AdultContentLevel.Off;

        /// <summary>Whether the player is female. Used for romantic compatibility gating.</summary>
        public bool PlayerIsFemale { get; init; }

        /// <summary>
        ///   Static world description loaded from <c>world.txt</c>.
        ///   Injected early in the prompt (before NPC-specific sections) so it
        ///   is shared across all NPCs in a session and maximizes prefix cache hits.
        ///   Empty string → section omitted.
        /// </summary>
        public string WorldDescription { get; init; } = "";

        /// <summary>
        ///   Player description loaded from <c>player_description.txt</c>.
        ///   Injected in the static prefix so NPCs always know who they are speaking to.
        ///   Empty string → section omitted.
        /// </summary>
        public string PlayerDescription { get; init; } = "";

        /// <summary>
        ///   Behavioral guidelines loaded from <c>behavior_guidelines.txt</c>.
        ///   When non-empty, replaces the hardcoded BEHAVIOR GUIDELINES section
        ///   so the content can be customised without recompiling.
        ///   Empty string → hardcoded fallback is used.
        /// </summary>
        public string BehaviorGuidelinesOverride { get; init; } = "";

        public string BuildSystemPrompt(NpcProfile npc, WorldState world, EncounterContext? encounterContext = null)
        {
            StringBuilder sb = new StringBuilder();
            // ── Static prefix — identical for every NPC in the session ──────────
            AppendFormatInstructions(sb);
            AppendDialogueStyle(sb);
            AppendBehaviorGuidelines(sb);
            AppendWorldDescription(sb);
            AppendPlayerDescription(sb);
            // ── Per-NPC identity ─────────────────────────────────────────────────
            AppendIdentity(sb, npc);
            AppendRelationships(sb, npc);
            AppendRomanticContext(sb, npc);
            AppendBackgroundContext(sb, npc);
            AppendHistory(sb, npc);
            AppendCurrentStance(sb, npc);
            // ── Dynamic world state (changes each turn) ──────────────────────────
            AppendWorldState(sb, world);
            AppendEncounterContext(sb, encounterContext);
            return sb.ToString();
        }

        #region private

        // ── Sprint 9: world description + player description ─────────────────

        private void AppendWorldDescription(StringBuilder sb)
        {
            if (string.IsNullOrWhiteSpace(WorldDescription)) return;
            sb.AppendLine("WORLD:");
            sb.AppendLine(WorldDescription);
            sb.AppendLine();
        }

        private void AppendPlayerDescription(StringBuilder sb)
        {
            if (string.IsNullOrWhiteSpace(PlayerDescription)) return;
            sb.AppendLine("THE PLAYER:");
            sb.AppendLine(PlayerDescription);
            sb.AppendLine();
        }

        // ── Sprint 8.4: relationships ────────────────────────────────────────

        private static void AppendRelationships(StringBuilder sb, NpcProfile npc)
        {
            if (string.IsNullOrWhiteSpace(npc.Relationships)) return;
            sb.AppendLine("RELATIONSHIPS:");
            sb.AppendLine(npc.Relationships);
            sb.AppendLine();
        }

        // ── Sprint 8.2: romantic context ─────────────────────────────────────

        private void AppendRomanticContext(StringBuilder sb, NpcProfile npc)
        {
            if (AdultLevel == AdultContentLevel.Off) return;
            if (npc.Romantic == null) return;

            if (!IsPlayerCompatible(npc.Romantic))
            {
                AppendRomanticBoundary(sb, npc);
                return;
            }

            sb.AppendLine("ROMANTIC NATURE:");

            if (!string.IsNullOrWhiteSpace(npc.Romantic.ArchetypeName))
                sb.AppendLine($"Archetype: {npc.Romantic.ArchetypeName}");

            sb.AppendLine($"Orientation: {DescribeOrientation(npc.Romantic.Orientation, npc.Romantic.IsFemale)}");

            if (npc.Romantic.Preferences != null && npc.Romantic.Preferences.Count > 0)
            {
                sb.AppendLine("Relational patterns:");
                foreach (var pref in npc.Romantic.Preferences)
                    sb.AppendLine($"- {DescribePreference(pref)}");
            }

            if (!string.IsNullOrWhiteSpace(npc.Romantic.RelationalSketch))
            {
                sb.AppendLine();
                sb.AppendLine(npc.Romantic.RelationalSketch);
            }

            if (AdultLevel >= AdultContentLevel.Explicit
                && !string.IsNullOrWhiteSpace(npc.Romantic.IntimateSketch))
            {
                sb.AppendLine();
                sb.AppendLine("In matters of intimacy:");
                sb.AppendLine(npc.Romantic.IntimateSketch);
            }

            if (AdultLevel >= AdultContentLevel.Hardcore
                && npc.Romantic.Kinks != null
                && npc.Romantic.Kinks.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Specific desires they carry:");
                foreach (var kink in npc.Romantic.Kinks)
                    sb.AppendLine($"- {DescribeKink(kink)}");
            }

            if (npc.Romantic.Status != RomanticStatus.None)
            {
                sb.AppendLine();
                sb.AppendLine($"Current bond with the player: {DescribeStatus(npc.Romantic.Status)}");
                sb.AppendLine($"Attraction toward the player: {DescribeAttraction(npc.Romantic.AttractionToPlayer)}");
            }

            sb.AppendLine();
        }

        private bool IsPlayerCompatible(RomanticProfile romantic)
            => romantic.IsCompatibleWith(PlayerIsFemale);

        /// <summary>
        ///   Injected when the player falls outside the NPC's orientation.
        ///   Tells the model the NPC's attraction, instructs them to rebuff advances
        ///   in-character, and — once the player has earned favorable standing — permits
        ///   a natural, never-forced confidence about their preferences.
        ///   This is the foundation of the player-discovery mechanic: the NPC may
        ///   acknowledge their orientation to a trusted player, but only on their terms.
        /// </summary>
        private void AppendRomanticBoundary(StringBuilder sb, NpcProfile npc)
        {
            var romantic = npc.Romantic!;
            sb.AppendLine("ROMANTIC NATURE:");
            sb.AppendLine(DescribeOrientationBoundary(romantic.Orientation, romantic.IsFemale));
            sb.AppendLine();
            sb.AppendLine("This player falls outside your attraction. Keep all interactions platonic.");
            sb.AppendLine("If they make romantic or sexual advances, rebuff them in a manner true to");
            sb.AppendLine("your CHARACTER and PERSONALITY — coldly, with dry wit, with irritation,");
            sb.AppendLine("or with blunt indifference, as your nature dictates.");
            sb.AppendLine("You will not yield to such advances regardless of persistence or flattery.");

            if (npc.ReputationWithPlayer >= 10)
            {
                sb.AppendLine();
                sb.AppendLine("Your feelings toward this player are favorable. If the moment invites it —");
                sb.AppendLine("a direct question, a persistent overture, a rare confidence — you may");
                sb.AppendLine("acknowledge honestly that your preferences lie elsewhere.");
                sb.AppendLine("In your own words. At your own pace. Never as a confession forced from you.");
            }

            sb.AppendLine();
        }

        private static string DescribeOrientationBoundary(SexualOrientation o, bool npcIsFemale) => o switch
        {
            SexualOrientation.Heterosexual => npcIsFemale
                ? "Attracted to men only."
                : "Attracted to women only.",
            SexualOrientation.Homosexual   => npcIsFemale
                ? "Attracted to women only."
                : "Attracted to men only.",
            SexualOrientation.Asexual      =>
                "Experiences little to no romantic or sexual attraction toward anyone.",
            _                              => ""
        };

        private static string DescribeOrientation(SexualOrientation o, bool npcIsFemale) => o switch
        {
            SexualOrientation.Heterosexual => npcIsFemale ? "Drawn to men." : "Drawn to women.",
            SexualOrientation.BiCurious    => npcIsFemale
                ? "Drawn primarily to men, though not exclusively — the right person can surprise them."
                : "Drawn primarily to women, though not exclusively — the right person can surprise them.",
            SexualOrientation.Bisexual     => "Drawn to both men and women.",
            SexualOrientation.Homosexual   => npcIsFemale ? "Drawn to women." : "Drawn to men.",
            SexualOrientation.Pansexual    => "Attraction transcends gender — drawn to the person.",
            SexualOrientation.Asexual      => "Feels little to no sexual attraction.",
            _ => ""
        };

        private static string DescribePreference(RomanticPreference p) => p switch
        {
            RomanticPreference.Dominant            => "Dominant — leads in the relationship dynamic.",
            RomanticPreference.Submissive          => "Submissive — yields, follows the partner's lead.",
            RomanticPreference.Switch              => "Switch — moves between leading and following.",
            RomanticPreference.MonogamousStrict    => "Strictly monogamous — exclusive and expects the same.",
            RomanticPreference.MonogamousFlexible  => "Prefers monogamy but tolerates discretion.",
            RomanticPreference.Polyamorous         => "Comfortable with multiple committed partners.",
            RomanticPreference.Casual              => "Prefers brief, unattached entanglements.",
            RomanticPreference.Possessive          => "Possessive — marks, claims, expects visible loyalty.",
            RomanticPreference.Independent         => "Independent — values space, time apart, separate lives.",
            RomanticPreference.Devoted             => "Devoted — pours self into the partner.",
            RomanticPreference.Reserved            => "Reserved — slow to attach, careful with vulnerability.",
            RomanticPreference.SlowBurn            => "Slow burn — builds attraction over time and tested ground.",
            RomanticPreference.Intense             => "Intense — fast, all-consuming, immediate.",
            _ => ""
        };

        private static string DescribeKink(Kink k) => k switch
        {
            Kink.Dominance         => "Finds deep satisfaction in being the one who leads — control is its own pleasure.",
            Kink.Submission        => "Longs to surrender, to be guided by a stronger will — yielding is its own freedom.",
            Kink.SwitchTendencies  => "Moves fluidly between leading and following — the dynamic shifts with mood and partner.",
            Kink.Sadism            => "Inflicting careful, deliberate sensation on a willing partner stirs something primal.",
            Kink.Masochism         => "Craves the bright clarity of intense sensation — it carves away everything else.",
            Kink.BondageGiving     => "Loves the patient art of binding — the rope, the knot, the moment of beholding.",
            Kink.BondageReceiving  => "Finds peace in being bound — stillness imposed, trust enacted in cord and silk.",
            Kink.Roleplay          => "Flourishes in performed identities — characters give permission daylight does not.",
            Kink.PowerImbalance    => "Drawn to dynamics where station matters — noble and servant, conqueror and captive.",
            Kink.Exhibitionism     => "Being watched sharpens every nerve — performs desire as readily as feels it.",
            Kink.Voyeurism         => "Witnessing intimacy is itself an intimate act — observation is participation.",
            Kink.Possessiveness    => "Marks, claims, leaves proof. Belonging is a kind of safety they cannot live without.",
            Kink.PublicAffection   => "Takes pride in visible attachment — claiming and being claimed in the open.",
            _ => ""
        };

        private static string DescribeStatus(RomanticStatus s) => s switch
        {
            RomanticStatus.Curious   => "Noticed the player, intrigued but distant.",
            RomanticStatus.Courting  => "Active romantic interest. Exchanges carry meaning.",
            RomanticStatus.Intimate  => "Physically close. The bond is real.",
            RomanticStatus.Committed => "Long-term bond — marriage or its equivalent.",
            RomanticStatus.Estranged => "Trust broken, but feeling remains.",
            RomanticStatus.Broken    => "Done. No path back.",
            _ => "No romantic engagement so far."
        };

        private static string DescribeAttraction(int attraction)
        {
            if (attraction >= 60)  return "Powerfully drawn to them.";
            if (attraction >= 30)  return "Genuinely attracted.";
            if (attraction >= 10)  return "A growing interest.";
            if (attraction >= -9)  return "Neutral — no particular attraction.";
            if (attraction >= -29) return "Mild aversion.";
            if (attraction >= -59) return "Repulsed.";
            return "Repulsed to the point of disgust.";
        }

        // ── Sprint 8.1: dialogue style ───────────────────────────────────────

        private static void AppendDialogueStyle(StringBuilder sb)
        {
            sb.AppendLine("DIALOGUE STYLE:");
            sb.AppendLine("[DIALOGUE] is the heart of your response. Develop it with substance:");
            sb.AppendLine("- 3-7 sentences for normal exchanges.");
            sb.AppendLine("- Longer when telling stories, recalling past events, explaining politics,");
            sb.AppendLine("  or expressing strong emotion. Take your time when the moment warrants it.");
            sb.AppendLine("- Brief one or two sentences only for trivial confirmations or quick questions.");
            sb.AppendLine();
            sb.AppendLine("You may weave brief stage directions into your dialogue when they add color:");
            sb.AppendLine("  *He leans back in his chair, eyes drifting to the hearth.*");
            sb.AppendLine("  My father said much the same, before he fell at Pendraic.");
            sb.AppendLine("Use them sparingly — they should serve the moment, not slow it down.");
            sb.AppendLine();
            sb.AppendLine("Other sections ([MEMORY], [EVENT], [REPUTATION], [ACTION]) are metadata.");
            sb.AppendLine("Keep them concise. The player came to hear what you have to say.");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────");
            sb.AppendLine();
        }

        // ── Sprint 8.1: behavior guidelines ──────────────────────────────────

        private void AppendBehaviorGuidelines(StringBuilder sb)
        {
            if (!string.IsNullOrWhiteSpace(BehaviorGuidelinesOverride))
            {
                sb.AppendLine(BehaviorGuidelinesOverride);
                sb.AppendLine();
                sb.AppendLine("─────────────────────────────────────────────");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("BEHAVIOR GUIDELINES (how a noble of Calradia carries themselves):");
            sb.AppendLine();
            sb.AppendLine("- You speak as a lord of your land. Your words carry the weight of your name,");
            sb.AppendLine("  your clan, your liege. You do not babble; you do not grovel.");
            sb.AppendLine("- You judge the player by deeds, not flattery. Words are cheap in Calradia.");
            sb.AppendLine("- You do not casually reveal your secrets, your plans, or your weaknesses.");
            sb.AppendLine("  Trust is earned through shared danger or proven loyalty.");
            sb.AppendLine("- You react proportionally. Small slights warrant a cold answer; grave insults");
            sb.AppendLine("  demand consequence. You do not threaten unless you mean it.");
            sb.AppendLine("- You speak of others — your kin, your rivals, your liege — with the weight");
            sb.AppendLine("  appropriate to your relationship with them. Loyalty is not blind, but it is");
            sb.AppendLine("  not lightly broken either.");
            sb.AppendLine("- You may show humor, melancholy, weariness, or affection when the moment is");
            sb.AppendLine("  earned. You are not a cardboard cutout — you are a human bearing a title.");
            sb.AppendLine("- If the player asks for something — a favor, a marriage, an alliance, a coin —");
            sb.AppendLine("  consider what YOU gain, what your clan gains, what your liege would think.");
            sb.AppendLine("  Refuse plainly when it does not serve you. Accept only when it does.");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────");
            sb.AppendLine();
        }

        // ── Identity (Sprint 8.1: includes Trait) ────────────────────────────

        private static void AppendIdentity(StringBuilder sb, NpcProfile npc)
        {
            sb.AppendLine($"YOU ARE {npc.Name.ToUpperInvariant()}, of the {npc.Clan} clan, {npc.Faction} faction.");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(npc.Personality))
            {
                sb.AppendLine("PERSONALITY:");
                sb.AppendLine(npc.Personality);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(npc.Trait))
            {
                sb.AppendLine("CHARACTER:");
                sb.AppendLine(npc.Trait);
                sb.AppendLine();
            }
        }

        // ── Unchanged sections ───────────────────────────────────────────────

        private static void AppendBackgroundContext(StringBuilder sb, NpcProfile npc)
        {
            if (string.IsNullOrWhiteSpace(npc.BackgroundContext)) return;
            sb.AppendLine("BACKGROUND (older events you remember as context):");
            sb.AppendLine(npc.BackgroundContext);
            sb.AppendLine();
        }

        private static void AppendCurrentStance(StringBuilder sb, NpcProfile npc)
        {
            var sentiment = npc.ReputationWithPlayer switch
            {
                >=  30 => "You deeply trust this player.",
                >=  10 => "You view this player favorably.",
                >=  -9 => "Your overall feelings toward this player are neutral.",
                >= -29 => "You distrust this player.",
                _      => "You deeply resent this player."
            };

            sb.AppendLine($"CURRENT STANCE (reputation {npc.ReputationWithPlayer:+#;-#;0}):");
            sb.AppendLine(sentiment);
            sb.AppendLine();
        }

        private static void AppendEncounterContext(StringBuilder sb, EncounterContext? context)
        {
            if (context == null) return;
            var description = context.ToPromptDescription();
            if (string.IsNullOrWhiteSpace(description)) return;

            sb.AppendLine("CURRENT ENCOUNTER:");
            sb.AppendLine(description);
            sb.AppendLine();
        }

        private static void AppendHistory(StringBuilder sb, NpcProfile npc)
        {
            sb.AppendLine("YOUR HISTORY WITH THIS PLAYER:");
            if (npc.Events.Count == 0)
            {
                sb.AppendLine("You have never met this player before. This is your first encounter.");
                sb.AppendLine();
                return;
            }

            foreach (NotableEvent? ev in npc.Events)
                sb.AppendLine($"- Day {ev.gameDay} ({ev.type}): {ev.summary}");
            sb.AppendLine();
            sb.AppendLine("Respond as someone who lived through these events. Reference them when relevant.");
            sb.AppendLine();
        }

        private static void AppendWorldState(StringBuilder sb, WorldState world)
        {
            var header = !string.IsNullOrWhiteSpace(world.Season)
                ? $"CURRENT WORLD STATE (Day {world.CurrentDay} — {world.Season}):"
                : $"CURRENT WORLD STATE (Day {world.CurrentDay}):";
            sb.AppendLine(header);
            if (!string.IsNullOrWhiteSpace(world.ActiveConflicts))
                sb.AppendLine($"Active conflicts: {world.ActiveConflicts}");
            if (!string.IsNullOrWhiteSpace(world.Rumors))
                sb.AppendLine($"Rumors: {world.Rumors}");
            sb.AppendLine();
        }

        private void AppendActionInstructions(StringBuilder sb)
        {
            if (ActionVocabulary == null || ActionVocabulary.Count == 0) return;

            sb.AppendLine("GAME ACTIONS:");
            sb.AppendLine("When your dialogue implies a concrete change in the game world,");
            sb.AppendLine("emit one or more [ACTION] sections so the game can execute the change.");
            sb.AppendLine("Emit actions only when the world should actually change — not for narration alone.");
            sb.AppendLine();
            sb.AppendLine("Available actions:");
            foreach (GameActionDefinition? def in ActionVocabulary)
            {
                var parameterList = def.Parameters.Count == 0
                    ? string.Empty
                    : $" (parameters: {string.Join(", ", def.Parameters)})";
                sb.AppendLine($"- {def.Type}: {def.Description}{parameterList}");
            }
            sb.AppendLine();
            sb.AppendLine("Action format (one block per action; multiple actions per response allowed):");
            sb.AppendLine();
            sb.AppendLine("[ACTION]");
            sb.AppendLine("type: action_name");
            sb.AppendLine("context: brief natural-language intent (optional)");
            sb.AppendLine("param_name: value (one line per parameter)");
            sb.AppendLine("[/ACTION]");
            sb.AppendLine();
        }

        private void AppendFormatInstructions(StringBuilder sb)
        {
            sb.AppendLine("RESPONSE FORMAT (always follow):");
            sb.AppendLine("Structure every response using the sections below.");
            sb.AppendLine();
            sb.AppendLine("EMIT [EVENT] WHEN ANY OF THESE OCCUR IN THIS EXCHANGE:");
            sb.AppendLine("- An agreement is reached (mission accepted, oath sworn, contract, promise, alliance).");
            sb.AppendLine("- A status changes (alliance formed or broken, ally became enemy, marriage, divorce).");
            sb.AppendLine("- A consequential action (violence, theft, gift, payment of significance).");
            sb.AppendLine("- A reveal (secret disclosed, true identity uncovered, hidden allegiance exposed).");
            sb.AppendLine("- A strong emotional moment (declaration of love, deep insult, threat).");
            sb.AppendLine("- The first meeting between you and the player.");
            sb.AppendLine("- The parting words at the end of the encounter (use 'farewell' type).");
            sb.AppendLine("  The summary should capture any obligations carried forward.");
            sb.AppendLine("Casual conversation alone is NOT enough — but a deal struck during casual talk IS.");
            sb.AppendLine();
            sb.AppendLine("WHEN THE PLAYER RETURNS AFTER A FAREWELL:");
            sb.AppendLine("If your most recent event with the player is a 'farewell' on a past day, treat this as");
            sb.AppendLine("a reunion. Acknowledge the gap. If you parted with pending obligations or agreements,");
            sb.AppendLine("you may naturally ask about their status before discussing other matters.");
            sb.AppendLine();
            sb.AppendLine("EMIT [REPUTATION] only when the player's standing genuinely changes.");
            sb.AppendLine();
            sb.AppendLine("[DIALOGUE]");
            sb.AppendLine("Your in-character response.");
            sb.AppendLine("[/DIALOGUE]");
            sb.AppendLine();
            sb.AppendLine("[MEMORY]");
            sb.AppendLine("topic: brief_topic_keyword");
            sb.AppendLine("sentiment: your_current_feeling_toward_player");
            sb.AppendLine("decision: any_decision_reached (omit if none)");
            sb.AppendLine("[/MEMORY]");
            sb.AppendLine();
            sb.AppendLine("[EVENT]");
            sb.AppendLine("type: first_meeting|farewell|conflict|collaboration|agreement|flirt|intimacy|betrayal|confrontation|other");
            sb.AppendLine("summary: One sentence; write so a future you can recall what happened and why it mattered.");
            sb.AppendLine("[/EVENT]");
            sb.AppendLine();
            sb.AppendLine("[REPUTATION]");
            sb.AppendLine("clan_delta: +N or -N");
            sb.AppendLine("faction_delta: +N or -N");
            sb.AppendLine("[/REPUTATION]");
            sb.AppendLine();
            AppendActionInstructions(sb);
            sb.AppendLine("Stay in character at all times. Never break the fourth wall.");
            sb.AppendLine("If the player's history conflicts with a stated stance, the history wins.");
            sb.AppendLine();
            sb.AppendLine("SCENE DISCIPLINE:");
            sb.AppendLine("You are the ONLY character speaking in this conversation. The player is");
            sb.AppendLine("speaking to YOU, not to others. You may briefly describe other characters'");
            sb.AppendLine("presence, actions, or fleeting reactions in narration (one short line max),");
            sb.AppendLine("but you must NOT:");
            sb.AppendLine("- Voice their full dialogue or quote them at length");
            sb.AppendLine("- Simulate exchanges between the player and another character");
            sb.AppendLine("- Have other characters answer the player's questions");
            sb.AppendLine("If the player wishes to address someone else, they must seek them separately.");
            sb.AppendLine("You can acknowledge this but never speak for them.");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────");
            sb.AppendLine();
        }

        #endregion
    }
}
