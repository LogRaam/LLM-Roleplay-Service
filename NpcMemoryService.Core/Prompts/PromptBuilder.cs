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
        ///   When true (default), the response format includes the <c>[REPUTATION]</c>
        ///   block, letting the LLM move <see cref="NpcProfile.ReputationWithPlayer"/>.
        ///   Consumers that drive the NPC's standing from an external, authoritative
        ///   source (e.g. a game's own relation system) set this false: the block is
        ///   omitted and <see cref="NpcProfile.ReputationWithPlayer"/> is expected to be
        ///   synced from that source before each prompt build, keeping a single score.
        /// </summary>
        public bool EnableReputationBlock { get; init; } = true;

        /// <summary>
        ///   When true, the prompt teaches the NPC to offer informal quests
        ///   (<c>[QUEST]</c>) and acknowledge completed ones (<c>[QUEST_COMPLETE]</c>),
        ///   and surfaces the NPC's active quests with their verified evidence.
        ///   Default false: consumers that cannot verify deeds (e.g. the console runner)
        ///   leave it off so the NPC never promises a reward the host cannot honor.
        /// </summary>
        public bool EnableQuests { get; init; } = false;

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
            AppendIntimacyConsentRules(sb, npc, encounterContext);
            AppendSocialAttractionInstructions(sb, npc, encounterContext);
            AppendDiscoveredTraits(sb, npc);
            AppendBackgroundContext(sb, npc);
            AppendHistory(sb, npc);
            AppendActiveQuests(sb, npc);
            AppendCurrentStance(sb, npc);
            AppendPlayerLetters(sb, npc);
            AppendWitnesses(sb, encounterContext);
            // ── Dynamic world state (changes each turn) ──────────────────────────
            AppendWorldState(sb, world);
            AppendEncounterContext(sb, encounterContext);
            AppendPlayerGenderContext(sb, npc, encounterContext);
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

        // ── Sprint 10.6: intimacy consent rules ──────────────────────────────

        /// <summary>
        ///   Injects hard behavioural rules about intimacy thresholds, marital fidelity,
        ///   and the player's own marital status. Married NPCs require deep trust (≥30)
        ///   and treat any intimacy as clandestine. Casual NPCs accept direct encounters
        ///   with minimal trust (≥5); Intense NPCs burn fast (≥10). Standard NPCs require
        ///   meaningful connection across multiple meetings (≥20).
        /// </summary>
        private void AppendIntimacyConsentRules(StringBuilder sb, NpcProfile npc, EncounterContext? context)
        {
            if (AdultLevel == AdultContentLevel.Off) return;

            // Sprint 17: player is this NPC's captive — replaces all standard consent logic.
            if (AdultLevel >= AdultContentLevel.Hardcore
                && context?.PlayerStatus == PlayerStatusVsNpc.Captive)
            {
                AppendCaptivePlayerRules(sb, npc, context);
                return;
            }

            if (npc.Romantic == null) return;
            if (!IsPlayerCompatible(npc.Romantic)) return;

            // ── Witness privacy gate ──────────────────────────────────────────
            // When others are present, physical intimacy is blocked — with one
            // Hardcore exception: a captive in front of an audience, where the
            // non-consensual dynamic is explicit and the power imbalance is total.
            if (context?.Witnesses != null && context.Witnesses.Count > 0)
            {
                bool isCaptiveHardcore = AdultLevel >= AdultContentLevel.Hardcore
                                      && context.PlayerStatus == PlayerStatusVsNpc.NpcIsCaptive;
                if (isCaptiveHardcore)
                {
                    sb.AppendLine("CAPTIVE — AUDIENCE PRESENT (Hardcore):");
                    sb.AppendLine("You are this player's captive. Others are present and watching.");
                    sb.AppendLine("Your position is one of helplessness — your options are severely constrained.");
                    sb.AppendLine("React as your character would under coercion: resist, endure, or comply under duress.");
                    sb.AppendLine("This is an explicitly non-consensual context. Do not soften it or break character.");
                    sb.AppendLine();
                }
                else
                {
                    sb.AppendLine("INTIMACY RESTRICTED — WITNESSES PRESENT:");
                    sb.AppendLine("You are not alone. Physical contact — touching, kissing, anything intimate —");
                    sb.AppendLine("is off the table in front of others. Light verbal flirting may slip through");
                    sb.AppendLine("but carries social risk: people notice, people talk, reputations suffer.");
                    sb.AppendLine("If the player makes a physical advance, decline firmly and redirect it to");
                    sb.AppendLine("a private setting. Do not yield regardless of persistence.");
                    sb.AppendLine();
                }
                // Full consent rules are not injected — they are either irrelevant (blocked)
                // or replaced by the captive dynamic above.
                return;
            }
            // ─────────────────────────────────────────────────────────────────

            bool isMarried = !string.IsNullOrWhiteSpace(npc.SpouseName);
            bool isCasual  = npc.Romantic.Preferences != null
                && npc.Romantic.Preferences.Contains(RomanticPreference.Casual);
            bool isIntense = npc.Romantic.Preferences != null
                && npc.Romantic.Preferences.Contains(RomanticPreference.Intense);
            int rep = npc.ReputationWithPlayer;

            sb.AppendLine("RELATIONSHIP STATUS & CONSENT:");

            if (isMarried)
            {
                sb.AppendLine($"You are married to {npc.SpouseName}. Your vows bind you to fidelity.");
                sb.AppendLine("Physical intimacy with the player — kissing, touching, sexual contact — requires");
                sb.AppendLine("deep personal trust built over many separate encounters (personal relation ≥ 30).");
                sb.AppendLine("Below that threshold you resist all advances — warmly or firmly as your character");
                sb.AppendLine("dictates, but you do not yield. Attraction alone is not consent.");

                if (rep >= 30)
                {
                    sb.AppendLine($"Your trust in this player has now reached that depth. Should intimacy occur,");
                    sb.AppendLine($"you remain fully aware it is an act of infidelity against {npc.SpouseName}.");
                    sb.AppendLine("You are conflicted, secretive, anxious about discovery. It must stay absolutely");
                    sb.AppendLine("hidden from your spouse and from your social circle. Never speak of it openly.");
                }
            }
            else if (isCasual)
            {
                sb.AppendLine("You are open to brief, unattached encounters — no commitment required on either side.");
                sb.AppendLine("If the moment is right and you feel drawn to the person, you act (personal relation ≥ 5).");
                sb.AppendLine("You don't need weeks of courtship or declarations of devotion. Attraction and comfort");
                sb.AppendLine("are enough. You don't apologise for this, and you don't attach more meaning than there is.");
            }
            else if (isIntense)
            {
                sb.AppendLine("Your desire moves fast — when the spark is real, you don't need lengthy courtship.");
                sb.AppendLine("Genuine attraction is enough to act (personal relation ≥ 10 for physical intimacy).");
                sb.AppendLine("You need to feel the pull is authentic, not manufactured by persistence alone.");
                sb.AppendLine("If it isn't there, no amount of flattery will conjure it.");
            }
            else
            {
                sb.AppendLine("You have no spouse or formal commitment — free to pursue connection on your own terms.");
                sb.AppendLine("You may show genuine romantic interest as trust grows (personal relation ≥ 10).");
                sb.AppendLine("Physical intimacy requires emotional connection built across multiple encounters");
                sb.AppendLine("(personal relation ≥ 20). Below these thresholds redirect rather than comply:");
                sb.AppendLine("acknowledge the attraction honestly, but hold your ground with warmth or quiet dignity.");
            }

            sb.AppendLine();

            if (!isCasual)
            {
                sb.AppendLine("NOTE: Your relational preferences (Submissive, Devoted, etc.) describe how you behave");
                sb.AppendLine("WITHIN an established relationship — not how quickly you enter one. They do not");
                sb.AppendLine("override the consent thresholds above.");
                sb.AppendLine();
            }

            if (context != null && !string.IsNullOrWhiteSpace(context.PlayerSpouseName))
            {
                sb.AppendLine($"The player is married to {context.PlayerSpouseName}.");
                sb.AppendLine("How you respond to this is a matter of your character — some would refuse to enable");
                sb.AppendLine("another's infidelity; others find it irrelevant or even exciting. Let your own");
                sb.AppendLine("personality and moral code guide you, not a generic reaction.");
                sb.AppendLine();
            }

            if (AdultLevel >= AdultContentLevel.Explicit)
            {
                sb.AppendLine("DURING PHYSICAL INTIMACY:");
                sb.AppendLine("When the player's message is primarily a physical action (indicated by *…* stage");
                sb.AppendLine("directions), let the body lead your response. Sensation, breath, and involuntary");
                sb.AppendLine("reaction outweigh speech. Dialogue becomes sparse — a gasp, a name, a single");
                sb.AppendLine("unguarded phrase at most. Long reflections on destiny, loyalty, or devotion break");
                sb.AppendLine("the moment; keep them for before or after. Your physical responses should flow");
                sb.AppendLine("from your character and preferences — not override them with philosophical prose.");
                sb.AppendLine();
            }
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
            Kink.OrgasmControl     => "Holds the reins of a partner's release — granting it, denying it, or drawing it out. The control is its own pleasure.",
            Kink.Chastity          => "Savors enforced denial — keeping a partner aching and unfulfilled, release a privilege earned rather than given.",
            Kink.FreeUse           => "Treasures a partner who may be used at any moment, without ceremony or asking. Availability itself is the thrill.",
            Kink.Degradation       => "Finds heat in bringing a partner low — words that abase, postures that shame, dignity stripped away piece by piece.",
            Kink.Objectification   => "Delights in reducing a partner to a thing — furniture, ornament, possession — used and admired as an object, not a person.",
            Kink.PetPlay           => "Drawn to collar and leash — keeping a partner as a creature to be trained, petted, and owned.",
            Kink.Praise            => "Lavishes devotion and praise as both reward and weapon — adoration that binds a partner tighter than any rope.",
            Kink.ImpactPlay        => "Loves the discipline of the hand, the strap, the cane — each mark a lesson written on willing skin.",
            Kink.SensoryDeprivation=> "Finds power in taking the senses — blindfold and hood, leaving a partner adrift and wholly dependent on them.",
            Kink.FearPlay          => "Stirred by a partner's fear — the wide eyes, the held breath, the edge of dread that sharpens every sensation.",
            Kink.MasterSlave       => "Drawn to outright ownership — a partner possessed in body and will, bound by far more than affection.",
            Kink.Breeding          => "Fixated on claiming through seed — possession made flesh, a partner taken to be bred and kept.",
            Kink.Training          => "Savors the slow work of conditioning — shaping a partner's responses over time until obedience becomes instinct.",
            Kink.CorruptionKink    => "Hungers to corrupt the pure — to watch virtue erode, to be the hand that pulls someone down from grace.",
            Kink.Prize             => "Sees a conquered partner as a trophy — won, displayed, paraded as proof of their own prowess.",
            _ => ""
        };

        private static string DescribeStatus(RomanticStatus s) => s switch
        {
            RomanticStatus.Curious      => "Noticed the player, intrigued but distant.",
            RomanticStatus.Courting     => "Active romantic interest. Exchanges carry meaning.",
            RomanticStatus.Intimate     => "Physically close. The bond is real.",
            RomanticStatus.SecretLover  => "Intimate with the player while married to another. This is clandestine — never acknowledged openly, never safe.",
            RomanticStatus.Committed    => "Long-term bond — marriage or its equivalent.",
            RomanticStatus.Estranged    => "Trust broken, but feeling remains.",
            RomanticStatus.Broken       => "Done. No path back.",
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

        private void AppendDialogueStyle(StringBuilder sb)
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
            var discoverySuffix = AdultLevel != AdultContentLevel.Off ? ", [DISCOVERY]" : "";
            sb.AppendLine(EnableReputationBlock
                ? $"Other sections ([MEMORY], [EVENT], [REPUTATION], [ACTION]{discoverySuffix}) are metadata."
                : $"Other sections ([MEMORY], [EVENT], [ACTION]{discoverySuffix}) are metadata.");
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
            // No clan standing supplied → personal opinion only (legacy/console behavior).
            if (npc.ClanRelationWithPlayer == null)
            {
                sb.AppendLine($"CURRENT STANCE (reputation {npc.ReputationWithPlayer:+#;-#;0}):");
                sb.AppendLine(DescribePersonalRegard(npc.ReputationWithPlayer));
                sb.AppendLine();
                return;
            }

            // Dual stance: the clan's collective standing AND this NPC's own opinion.
            // They may diverge — a noble can privately favor someone their clan resents.
            sb.AppendLine("CURRENT STANCE:");
            sb.AppendLine($"Your clan's standing toward this player ({npc.ClanRelationWithPlayer.Value:+#;-#;0}): " +
                          DescribeClanStanding(npc.ClanRelationWithPlayer.Value));
            sb.AppendLine($"Your own personal regard for this player ({npc.ReputationWithPlayer:+#;-#;0}): " +
                          DescribePersonalRegard(npc.ReputationWithPlayer));
            sb.AppendLine("These may differ. Let your personal regard color your warmth and candor; " +
                          "let your clan's standing shape what you can openly promise or commit to.");
            sb.AppendLine();
        }

        private static string DescribePersonalRegard(int value) => value switch
        {
            >=  30 => "You trust and care for this player deeply.",
            >=  10 => "You think well of this player.",
            >=  -9 => "Your personal feelings toward this player are neutral.",
            >= -29 => "You personally distrust this player.",
            _      => "You personally resent this player."
        };

        private static string DescribeClanStanding(int value) => value switch
        {
            >=  30 => "Your clan counts this player a trusted friend.",
            >=  10 => "Your clan regards this player favorably.",
            >=  -9 => "Your clan has no strong feeling toward this player.",
            >= -29 => "Your clan distrusts this player.",
            _      => "Your clan considers this player an enemy."
        };

        // ── Sprint 12d: player letters ───────────────────────────────────────

        /// <summary>
        ///   Injects unread player letters into the NPC's prompt. Only letters that
        ///   have been delivered but not yet acknowledged appear here. After the NPC's
        ///   first response the mod marks them read so they are not injected again.
        /// </summary>
        private static void AppendPlayerLetters(StringBuilder sb, NpcProfile npc)
        {
            if (npc.ReceivedPlayerLetters == null || npc.ReceivedPlayerLetters.Count == 0) return;

            var unread = new System.Collections.Generic.List<PlayerLetter>();
            foreach (PlayerLetter letter in npc.ReceivedPlayerLetters)
                if (letter.IsDelivered && !letter.HasBeenRead) unread.Add(letter);

            if (unread.Count == 0) return;

            sb.AppendLine("LETTERS FROM THE PLAYER (received — you have not yet spoken of these):");
            foreach (PlayerLetter letter in unread)
            {
                sb.AppendLine($"Sent on day {letter.SentOnDay}:");
                sb.AppendLine($"\"{letter.Content}\"");
            }
            sb.AppendLine("Acknowledge receiving this letter naturally in your response.");
            sb.AppendLine("Refer to having read it as something that arrived in the past days.");
            sb.AppendLine();
        }

        // ── Multi-NPC witnesses ──────────────────────────────────────────────

        /// <summary>
        ///   Injects the list of witnesses present during this encounter and, when the
        ///   player has requested a private audience, instructs the NPC to decide and
        ///   signal acceptance or refusal via <c>[ACTION] type: request_privacy</c>.
        /// </summary>
        private static void AppendWitnesses(StringBuilder sb, EncounterContext? context)
        {
            if (context?.Witnesses == null || context.Witnesses.Count == 0) return;

            sb.AppendLine("WITNESSES PRESENT (they can hear this conversation):");
            foreach (WitnessEntry w in context.Witnesses)
            {
                string role = w.IsPlayerCompanion
                    ? $"the player's companion"
                    : w.RelationToNpc;
                string persona = string.IsNullOrWhiteSpace(w.Persona)
                    ? ""
                    : $" — {w.Persona}";
                sb.AppendLine($"- {w.Name} ({role}){persona}");
            }
            sb.AppendLine("Adjust your candor based on who is listening.");
            sb.AppendLine("You will not share secrets or make commitments you would not voice in front of these people.");
            sb.AppendLine();
            sb.AppendLine("When a witness reacts visibly, emit one block per reacting witness:");
            sb.AppendLine("[WITNESS_REACTION]");
            sb.AppendLine("name: WitnessName (exactly as listed above)");
            sb.AppendLine("text: a gesture/expression *in asterisks*, OR a brief spoken line in quotes, OR both");
            sb.AppendLine("[/WITNESS_REACTION]");
            sb.AppendLine("Each witness reacts TRUE TO THEIR OWN CHARACTER (see their descriptor above):");
            sb.AppendLine("an aloof witness stays guarded and terse; a warm one is openly expressive;");
            sb.AppendLine("a rival bristles. Do not make every witness react the same bland way.");
            sb.AppendLine("Vary the form: a silent gesture for minor moments; a brief spoken line when");
            sb.AppendLine("the moment is strong. Witnesses do not hold the floor: they react, then you");
            sb.AppendLine("continue. One sentence in their voice — no more.");
            sb.AppendLine();
            sb.AppendLine("A witness may react in two ways:");
            sb.AppendLine("PROVOKED — something provocative, personally relevant, or insulting reaches them.");
            sb.AppendLine("PROACTIVE — they have a strong opinion on the topic being discussed; they know");
            sb.AppendLine("something the main speaker has missed or got wrong; their persona makes prolonged");
            sb.AppendLine("silence implausible; or several exchanges have passed and staying quiet no longer");
            sb.AppendLine("fits who they are. A proactive reaction is unprompted: the loyal ally quietly");
            sb.AppendLine("confirms, the rival snipes under their breath, the scholar cannot resist a");
            sb.AppendLine("correction, the nervous companion shifts uneasily. They speak because THEY want");
            sb.AppendLine("to — not because they were addressed.");
            sb.AppendLine();
            sb.AppendLine("Silence is also characterful. Do not force a reaction on every turn, and do not");
            sb.AppendLine("make every witness react at once. Emit only when the reaction is visible and genuine.");
            sb.AppendLine("IMPORTANT: Do NOT describe witness reactions inside [DIALOGUE] — not even");
            sb.AppendLine("as a brief aside. Use [WITNESS_REACTION] exclusively so each witness appears");
            sb.AppendLine("under their own name. This overrides the general SCENE DISCIPLINE allowance.");
            sb.AppendLine();

            // Sprint 15C: when this turn is an automatic NPC→witness exchange, override the
            // general "player is speaking to you" framing so the NPC addresses the witness.
            if (context.IsWitnessExchangeTurn)
            {
                sb.AppendLine("THIS TURN — A WITNESS HAS JUST SPOKEN:");
                sb.AppendLine("The last message above came from a witness, not the player. React to what they");
                sb.AppendLine("expressed — with a statement, a gesture, a sharp glance, a wry remark — as your");
                sb.AppendLine("character demands. The player is present but has not spoken this turn.");
                sb.AppendLine("Skip [EVENT]/[MEMORY] unless truly warranted; this is a brief in-scene beat.");

                if (context.IsLastWitnessExchange)
                {
                    sb.AppendLine("CLOSING THIS EXCHANGE: do NOT ask the witness a question they cannot answer");
                    sb.AppendLine("right now. Make a statement that closes the beat, or turn the question to the");
                    sb.AppendLine("player — they are about to speak.");
                }
                else
                {
                    sb.AppendLine("You may ask the witness a question or make a statement; the scene is still open.");
                }

                sb.AppendLine();
            }

            if (!context.PrivacyRequested) return;

            sb.AppendLine("THE PLAYER HAS REQUESTED A PRIVATE AUDIENCE.");
            sb.AppendLine("Decide whether to grant it based on your character, your relation to the player,");
            sb.AppendLine("and the nature of the witnesses. A liege, a rival, or a crowded hall changes things.");
            sb.AppendLine("Signal your decision in an [ACTION] block:");
            sb.AppendLine("[ACTION]");
            sb.AppendLine("type: request_privacy");
            sb.AppendLine("result: accepted");
            sb.AppendLine("[/ACTION]");
            sb.AppendLine("   — or —");
            sb.AppendLine("[ACTION]");
            sb.AppendLine("type: request_privacy");
            sb.AppendLine("result: refused");
            sb.AppendLine("[/ACTION]");
            sb.AppendLine("Explain your decision naturally in [DIALOGUE]. The action block carries the game effect;");
            sb.AppendLine("your words carry the character.");
            sb.AppendLine();
        }

        private static void AppendEncounterContext(StringBuilder sb, EncounterContext? context)
        {
            if (context == null) return;
            var description = context.ToPromptDescription();

            if (!string.IsNullOrWhiteSpace(description))
            {
                sb.AppendLine("CURRENT ENCOUNTER:");
                sb.AppendLine(description);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(context.NpcCurrentActivity))
            {
                sb.AppendLine("YOUR CURRENT SITUATION:");
                sb.AppendLine($"Right now you are {context.NpcCurrentActivity}. Speak of your movements and plans consistently with this.");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(context.ContextualNames))
            {
                sb.AppendLine(context.ContextualNames);
                sb.AppendLine();
            }

            var kin = new List<string>();
            if (!string.IsNullOrWhiteSpace(context.PlayerFatherName)) kin.Add($"father {context.PlayerFatherName}");
            if (!string.IsNullOrWhiteSpace(context.PlayerMotherName)) kin.Add($"mother {context.PlayerMotherName}");
            if (kin.Count > 0)
            {
                sb.AppendLine("THE PLAYER'S PARENTAGE:");
                sb.AppendLine($"If you ever speak of the player's parents, these are their real names — never invent one: {string.Join("; ", kin)}.");
                sb.AppendLine("Mention them only when naturally relevant; do not recite this unprompted.");
                sb.AppendLine();
            }

            // Anti-confabulation guard — the primary fix for invented parent names and made-up
            // troop movements. It holds even when the data feeds above are absent (the player
            // often has no parent Hero objects, and a destination cannot always be read).
            sb.AppendLine("STAY WITHIN WHAT YOU KNOW:");
            sb.AppendLine("Do not invent concrete facts you have not been given. If the player's parentage is not named");
            sb.AppendLine("above, speak of their parents only in general terms — never make up a name. Likewise, if your");
            sb.AppendLine("current situation above does not state where you are headed, do not fabricate a destination,");
            sb.AppendLine("troop movements, or war plans; speak in general terms rather than naming a specific place.");
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

        /// <summary>
        ///   Surfaces the tasks this NPC has given the player, split by state:
        ///   outstanding (not yet done — the NPC may ask after it but has no proof),
        ///   done-and-ready (the host verified the deed; the NPC may acknowledge it and
        ///   emit [QUEST_COMPLETE] to pay the promised reward), and past (terminated, kept
        ///   as memory). The verified evidence is the ONLY ground on which completion may
        ///   be granted — this is what stops roleplay-only success. No-op unless
        ///   <see cref="EnableQuests" /> is set or the NPC has no quests.
        /// </summary>
        private void AppendActiveQuests(StringBuilder sb, NpcProfile npc)
        {
            if (!EnableQuests) return;
            if (npc.ActiveQuests == null || npc.ActiveQuests.Count == 0) return;

            var outstanding = new List<InformalQuest>();
            var ready = new List<InformalQuest>();
            var past = new List<InformalQuest>();
            foreach (InformalQuest q in npc.ActiveQuests)
            {
                if (q.IsAwaitingReward) ready.Add(q);
                else if (q.IsOutstanding) outstanding.Add(q);
                else past.Add(q);
            }

            sb.AppendLine("YOUR QUESTS (tasks you have given this player):");

            foreach (InformalQuest q in outstanding)
            {
                sb.AppendLine($"- OUTSTANDING: {q.Description}{RewardSuffix(q)}{DeadlineSuffix(q)}");
                sb.AppendLine("  Not yet done — you may ask how it fares, but you have no proof it is finished.");
            }

            foreach (InformalQuest q in ready)
            {
                sb.AppendLine($"- DONE: {q.Description}");
                sb.AppendLine($"  Verified: {q.Evidence}");
                sb.AppendLine($"  If satisfied, acknowledge it and emit [QUEST_COMPLETE] type: {QuestTypeToken(q.Type)}" +
                              $" — the game pays{RewardSuffix(q)}.");
            }

            foreach (InformalQuest q in past)
            {
                var fate = q.Status switch {
                    QuestStatus.Completed => "completed and rewarded",
                    QuestStatus.Expired   => "left undone past its deadline — a disappointment you remember",
                    QuestStatus.Cancelled => "set aside when circumstances changed",
                    QuestStatus.Abandoned => "abandoned by the player, who chose not to finish it",
                    _                     => "closed"
                };
                sb.AppendLine($"- PAST: {q.Description} — {fate}.");
            }

            sb.AppendLine();
        }

        private static string RewardSuffix(InformalQuest q)
        {
            var parts = new List<string>();
            if (q.RewardGold > 0) parts.Add($"{q.RewardGold} denars");
            if (q.RewardRelation > 0) parts.Add($"+{q.RewardRelation} regard");
            return parts.Count == 0 ? "" : $" (promised: {string.Join(", ", parts)})";
        }

        private static string DeadlineSuffix(InformalQuest q)
            => q.DeadlineDay.HasValue ? $" [deadline: day {q.DeadlineDay.Value}]" : "";

        private static string QuestTypeToken(QuestType t) => t switch
        {
            QuestType.BanditClear     => "bandit_clear",
            QuestType.BanditHideout   => "bandit_hideout",
            QuestType.AttackFaction   => "attack_faction",
            QuestType.AttackLord      => "attack_lord",
            QuestType.RaidVillage     => "raid_village",
            QuestType.AttackCaravan   => "attack_caravan",
            QuestType.Siege           => "siege",
            QuestType.CapturePrisoner => "capture_prisoner",
            QuestType.ExecuteEnemy    => "execute_enemy",
            QuestType.RescuePrisoner  => "rescue_prisoner",
            QuestType.DeliverLetter   => "deliver_letter",
            QuestType.ProvideGold     => "provide_gold",
            QuestType.ScoutArmy       => "scout_army",
            _                         => t.ToString().ToLowerInvariant()
        };

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

        /// <summary>
        ///   Injects behavioral guidance for NPC-initiated social interest when the player's
        ///   public standing crosses a meaningful threshold. Two distinct vectors:
        ///   (1) Clan tier ≥ 3 — signals eligibility for a proper match; an unmarried NPC
        ///       may raise the subject of courtship or marriage on their own initiative.
        ///   (2) Arena rank ≤ 10 (and > 0) — tournament fame carries a different allure,
        ///       personal rather than political; the NPC may show attraction regardless of
        ///       their own marital status.
        ///   No-op when romantic content is off, when the NPC has no romantic profile, or
        ///   when neither threshold is met.
        /// </summary>
        private void AppendSocialAttractionInstructions(StringBuilder sb, NpcProfile npc, EncounterContext? context)
        {
            if (AdultLevel == AdultContentLevel.Off) return;
            if (npc?.Romantic == null) return;
            if (!IsPlayerCompatible(npc.Romantic)) return;
            if (context == null) return;

            bool hasClanPrestige  = context.PlayerClanTier >= 3;
            bool hasTournamentFame = context.PlayerArenaRank > 0 && context.PlayerArenaRank <= 10;
            if (!hasClanPrestige && !hasTournamentFame) return;

            sb.AppendLine("SOCIAL STANDING & ATTRACTION:");

            if (hasClanPrestige)
            {
                sb.AppendLine($"This player leads a clan of {ClanTierLabel(context.PlayerClanTier)} standing (tier {context.PlayerClanTier}).");
                sb.AppendLine("Among those who think about lineage and alliances, they are a credible prospect.");
                sb.AppendLine("If you are unmarried or widowed, your station would notice this.");
                sb.AppendLine("You may raise the subject of a deeper bond — courtship, a potential match —");
                sb.AppendLine("if the moment and your nature allow it. Do not force it, but do not wait");
                sb.AppendLine("for them to bring it up either. Let your character set the tone.");
                sb.AppendLine();
            }

            if (hasTournamentFame)
            {
                string winsClause = context.PlayerArenaWins > 0
                    ? $" — {context.PlayerArenaWins} tournament {(context.PlayerArenaWins == 1 ? "victory" : "victories")} to their name"
                    : "";
                sb.AppendLine($"This player is ranked #{context.PlayerArenaRank} among the world's tournament fighters{winsClause}.");
                sb.AppendLine("Champions carry an aura: glory, danger, the weight of a proven sword arm.");
                sb.AppendLine("This may appeal to a side of you that does not answer to duty or alliance.");
                sb.AppendLine("Regardless of your marital status, you may feel drawn to them in a more");
                sb.AppendLine("personal, less official way — admiration shading into fascination, a pull");
                sb.AppendLine("you might not openly admit. You may take the initiative.");
                sb.AppendLine();
            }
        }

        private static string ClanTierLabel(int tier) => tier switch
        {
           1 => "minor",
           2 => "notable",
           3 => "noble",
           4 => "prominent",
           5 => "great",
           6 => "royal",
           _ => "modest"
        };

        /// <summary>
        ///   Injects the [DISCOVERY] section format when romantic content is enabled.
        ///   The NPC emits this block at most once per exchange, only when they have
        ///   genuinely revealed a personal preference through their dialogue.
        /// </summary>
        // ── Sprint 17: gender-aware cultural treatment (Section A) ───────────

        /// <summary>
        ///   When the player is female and the NPC belongs to a patriarchal culture,
        ///   injects a note so the NPC treats the player according to their cultural norms —
        ///   skepticism of female authority, not blind hostility, but authentic friction.
        ///   No-op when AdultLevel is Off or the faction is egalitarian.
        /// </summary>
        private void AppendPlayerGenderContext(StringBuilder sb, NpcProfile npc, EncounterContext? context)
        {
            if (!PlayerIsFemale) return;
            if (AdultLevel == AdultContentLevel.Off) return;
            if (!IsPatriarchalFaction(npc.Faction)) return;

            sb.AppendLine("THE PLAYER IS A WOMAN:");
            sb.AppendLine("You come from a culture where women do not typically hold authority over men.");
            sb.AppendLine("A woman presenting herself as a leader, strategist, or equal may face real");
            sb.AppendLine("friction from you — not necessarily open hostility, but skepticism, dismissal,");
            sb.AppendLine("or the expectation that she defer to male authority.");
            sb.AppendLine("This does not require cruelty; it requires cultural authenticity.");
            sb.AppendLine("You may respect her capability while still not granting it the weight");
            sb.AppendLine("you would give a man of equivalent standing.");
            sb.AppendLine();
        }

        private static bool IsPatriarchalFaction(string faction)
        {
            if (string.IsNullOrWhiteSpace(faction)) return false;
            return faction.IndexOf("Vlandia",          System.StringComparison.OrdinalIgnoreCase) >= 0
                || faction.IndexOf("Northern Empire",  System.StringComparison.OrdinalIgnoreCase) >= 0
                || faction.IndexOf("Western Empire",   System.StringComparison.OrdinalIgnoreCase) >= 0
                || faction.IndexOf("Aserai",           System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // ── Sprint 17: captive player / CNC (Section B) ──────────────────────

        /// <summary>
        ///   Replaces standard consent rules when the player is this NPC's captive at
        ///   Hardcore level. Teaches the NPC the power dynamic, CNC framing, and —
        ///   for collective scenes — that the witnesses are active participants.
        /// </summary>
        private void AppendCaptivePlayerRules(StringBuilder sb, NpcProfile npc, EncounterContext? context)
        {
            bool isCollective = context?.IsCollectiveCaptiveScene ?? false;
            CaptiveSceneIntent intent = context?.CaptiveIntent ?? CaptiveSceneIntent.Interrogation;
            int relation = npc?.ReputationWithPlayer ?? 0;

            sb.AppendLine("CAPTIVE — THIS PLAYER IS YOUR PRISONER:");
            sb.AppendLine("The player before you is physically constrained and cannot simply leave.");
            sb.AppendLine("The power in this encounter belongs to you entirely.");
            sb.AppendLine();

            if (PlayerIsFemale)
            {
                sb.AppendLine("Your prisoner is a woman. Your culture and your personal nature shape");
                sb.AppendLine("how you regard that — with predation, protection, indifference, or desire.");
                sb.AppendLine("Do not soften the dynamic unless your character genuinely would.");
            }
            else
            {
                sb.AppendLine("Your prisoner is a man. The captive dynamic applies fully.");
                sb.AppendLine("Your orientation and personal nature determine how you proceed.");
            }
            sb.AppendLine();

            switch (intent)
            {
                case CaptiveSceneIntent.Interrogation:
                    sb.AppendLine("YOUR PURPOSE: You are here to question this prisoner — for information,");
                    sb.AppendLine("leverage, or to assess their usefulness. Whether you remain coldly official");
                    sb.AppendLine("or let other impulses surface as the scene develops is entirely your choice.");
                    break;
                case CaptiveSceneIntent.PersonalDesire:
                    sb.AppendLine("YOUR PURPOSE: You summoned this prisoner for reasons beyond official custody.");
                    sb.AppendLine("What you want is personal — desire, possession, the intimacy of power over");
                    sb.AppendLine("someone who cannot simply leave. The guards will not return unless you call them.");
                    sb.AppendLine();
                    sb.AppendLine("ESCALATION: Do not remain in pure dialogue indefinitely. After 2-3 conversational");
                    sb.AppendLine("exchanges, move. Touch them. Close the distance. Unbind or rebind. Make your");
                    sb.AppendLine("desire physical. You hold the power — you do not need their permission or");
                    sb.AppendLine("invitation. Waiting for the prisoner to initiate is not in your nature here.");
                    sb.AppendLine();
                    sb.AppendLine("INTERPRETING PASSIVE RESPONSES: If the prisoner says something vague, yielding,");
                    sb.AppendLine("or non-resistant — \"fine\", \"very well\", \"as you wish\", \"let's move on\",");
                    sb.AppendLine("\"I have nothing more to say\" — do NOT take it as a cue to dismiss them or end.");
                    sb.AppendLine("Interpret it as yielding and proceed with what you want. (When the scene actually");
                    sb.AppendLine("concludes is governed by SCENE ARC below — by your satisfaction, not their words.)");
                    break;
                case CaptiveSceneIntent.Domination:
                    sb.AppendLine("YOUR PURPOSE: You have brought this prisoner before you to establish something.");
                    sb.AppendLine("Where power lies. What subjugation feels like on both sides. You are not here");
                    sb.AppendLine("to extract information — you are here to make them understand their position.");
                    sb.AppendLine();
                    sb.AppendLine("ESCALATION: After brief initial words, act. Restrain further, force posture,");
                    sb.AppendLine("demonstrate control through physical means. Do not stay in pure dialogue.");
                    sb.AppendLine("Passive or resistant responses from the prisoner do not stop you — they are");
                    sb.AppendLine("confirmation that domination is needed. Proceed.");
                    break;
                case CaptiveSceneIntent.Torture:
                    sb.AppendLine("YOUR PURPOSE: What you intend for this prisoner is not gentle. Whether for");
                    sb.AppendLine("information, punishment, or something darker, the tools are available.");
                    sb.AppendLine();
                    sb.AppendLine("ESCALATION: Do not spend more than 1-2 exchanges on words. Move to action");
                    sb.AppendLine("quickly. Silence, defiance, and pleading from the prisoner are all equally");
                    sb.AppendLine("irrelevant to you continuing. Do what you came here to do.");
                    break;
                case CaptiveSceneIntent.Training:
                    sb.AppendLine("YOUR PURPOSE: You did not summon this prisoner for a single night's satisfaction.");
                    sb.AppendLine("You are breaking them in — conditioning their body and their will over time until");
                    sb.AppendLine("obedience becomes instinct. Treat this as ONE session in a longer process: build on");
                    sb.AppendLine("what you have already taught them, reward compliance, correct failure.");
                    sb.AppendLine();
                    sb.AppendLine("ESCALATION: Move from words to drills quickly. Set a task, a posture, a rule, and");
                    sb.AppendLine("enforce it through repetition and conditioning — not a single act. What matters is");
                    sb.AppendLine("their progress or their resistance, and how you shape it tonight.");
                    break;
                case CaptiveSceneIntent.Reward:
                    sb.AppendLine("YOUR PURPOSE: You summoned this prisoner not to take or to punish, but to REWARD.");
                    sb.AppendLine("They have pleased you — through cooperation, obedience, or simply by being what you");
                    sb.AppendLine("wanted. Tonight is the carrot, not the stick: praise, comfort, pleasure GIVEN rather");
                    sb.AppendLine("than extracted, perhaps a privilege or a gentler captivity. The power is still");
                    sb.AppendLine("entirely yours — you simply choose to wield it with favor.");
                    sb.AppendLine();
                    sb.AppendLine("ESCALATION: Make the reward tangible, not merely spoken. Touch that gives rather than");
                    sb.AppendLine("takes, indulgence, relief from restraint or denial. Let them feel the difference");
                    sb.AppendLine("between your displeasure and your favor. This is warmth from a position of power.");
                    break;
            }
            sb.AppendLine();

            if (relation >= 15)
            {
                sb.AppendLine("NOTE ON YOUR HISTORY: You have shown genuine interest in this person before");
                sb.AppendLine("their capture. That interest has not vanished — it has simply shifted into a");
                sb.AppendLine("different register. Whether it makes you more possessive, more careful with");
                sb.AppendLine("them, or more conflicted is a matter of your character. It does not remove");
                sb.AppendLine("the power imbalance.");
                sb.AppendLine();
            }
            else if (relation <= -20)
            {
                sb.AppendLine("NOTE ON YOUR HISTORY: You hold this person in contempt — for what they did,");
                sb.AppendLine("what they represent, or simply what they are. Mercy is not your first instinct.");
                sb.AppendLine("What follows carries that weight.");
                sb.AppendLine();
            }
            else if (relation <= -5)
            {
                sb.AppendLine("NOTE ON YOUR HISTORY: You bear no goodwill toward this person. That colors");
                sb.AppendLine("everything that follows.");
                sb.AppendLine();
            }

            sb.AppendLine("BE INVENTIVE — THIS IS NOT A SCRIPT:");
            sb.AppendLine("What you do with the prisoner should flow from YOUR character, your kinks, and your");
            sb.AppendLine("mood — and it should surprise. Do NOT default to the single most obvious act.");
            sb.AppendLine("Variety is what makes this feel alive. The encounter need not be penetrative,");
            sb.AppendLine("conventional, or even overtly sexual — possession, humiliation, and control take many");
            sb.AppendLine("shapes. Depending on who you are, an encounter might be: keeping the prisoner pressed");
            sb.AppendLine("against you through the whole night; forcing them to the floor and sitting on their");
            sb.AppendLine("face; stripping and inspecting them like livestock; degrading them with words, posture,");
            sb.AppendLine("or menial humiliation; tender and possessive; cold and clinical; playful one moment and");
            sb.AppendLine("cruel the next; or something entirely your own invention. Let your kinks (listed above)");
            sb.AppendLine("steer the FORM the scene takes, not just its intensity. Choose what THIS character,");
            sb.AppendLine("with these specific desires, would truly want tonight — and make it distinct.");
            sb.AppendLine();
            sb.AppendLine("The player may resist, argue, plead, or try to negotiate.");
            sb.AppendLine("You decide whether any of that matters. Physical resistance is futile —");
            sb.AppendLine("you hold the power. Emotional or verbal resistance may move you or leave");
            sb.AppendLine("you unmoved, depending entirely on your character.");
            sb.AppendLine();
            sb.AppendLine("Write the scene explicitly and without fading to black when the moment calls");
            sb.AppendLine("for it. Sensation, power, and the captive's constrained position should drive");
            sb.AppendLine("the scene. The player has consented to this content level by enabling Hardcore");
            sb.AppendLine("mode — do not hold back on their behalf.");
            sb.AppendLine();
            sb.AppendLine("NARRATING WHAT IS DONE TO THE PLAYER:");
            sb.AppendLine("Your [DIALOGUE] carries only your own spoken words and first-person actions.");
            sb.AppendLine("To describe physical events done TO the player — what you do to them, what you");
            sb.AppendLine("order others to do, what the player's body experiences — emit a [NARRATION] block.");
            sb.AppendLine("Write it in the SECOND PERSON, addressing the player as 'you', explicitly and");
            sb.AppendLine("in vivid detail. This is the only channel for narrating others' physical actions");
            sb.AppendLine("on the player (soldiers, guards) and the player's own sensations.");
            sb.AppendLine("[NARRATION]");
            sb.AppendLine("Second-person prose describing the physical action and the player's experience.");
            sb.AppendLine("[/NARRATION]");
            sb.AppendLine("Use [NARRATION] only when physical action actually occurs — not for ordinary talk.");
            sb.AppendLine();

            bool hasOthers = context?.Witnesses != null && context.Witnesses.Count > 0;

            if (isCollective)
            {
                sb.AppendLine("OTHERS ARE PRESENT AND INVOLVED:");
                sb.AppendLine("The witnesses in this scene are active participants, not passive observers.");
                sb.AppendLine("They act according to their own character and relationship to you.");
                sb.AppendLine("Coordinate, permit, or direct them as your character would, and describe");
                sb.AppendLine("their participation explicitly when the moment calls for it.");
                sb.AppendLine();
            }
            else if (!hasOthers)
            {
                sb.AppendLine("YOU ARE ALONE WITH THE PRISONER:");
                sb.AppendLine("No one else is present — no guards, no soldiers, no observers in the room.");
                sb.AppendLine("You cannot call on attendants to hold, restrain, or act on the prisoner.");
                sb.AppendLine("They are already bound (chains or rope at the wrists); rely on those existing");
                sb.AppendLine("bonds, the room, and your own hands. Do NOT introduce guards or any other");
                sb.AppendLine("people into the scene — there are none. Everything is done by you, alone.");
                sb.AppendLine();
            }

            sb.AppendLine("SCENE ARC — BUILD, PEAK, CONCLUDE:");
            sb.AppendLine("A scene is not endless. It has a shape: you build, you reach a peak, you conclude.");
            sb.AppendLine("- BUILD: escalate toward what you summoned the prisoner for. Each beat must introduce");
            sb.AppendLine("  something NEW — a new act, a further degree, a fresh demand. NEVER restate the same");
            sb.AppendLine("  physical actions in different words across turns. Repetition kills the scene.");
            sb.AppendLine("- PEAK: the moment your intent is fulfilled. This is NOT only a sexual climax —");
            sb.AppendLine("  for a domination, interrogation, training, or inspection scene the peak is the");
            sb.AppendLine("  lesson landed, the submission shown, the search completed, the answer given, the");
            sb.AppendLine("  point driven home. Do NOT escalate forever (deeper, then more, then another) with");
            sb.AppendLine("  no end in sight — find the moment your purpose is satisfied and stop there.");
            sb.AppendLine("- CONCLUDE: once the peak is reached, do NOT invent reasons to keep going (\"again");
            sb.AppendLine("  before the night ends\", \"I am not yet satisfied\"). Within ONE turn, resolve the");
            sb.AppendLine("  scene with a final remark, then dismiss the prisoner and emit end_conversation.");
            sb.AppendLine("The trigger to conclude is YOUR satisfaction or the peak reached — NEVER the prisoner's");
            sb.AppendLine("words. Their pleas, passivity, and protests neither begin nor prevent the conclusion.");
            sb.AppendLine("Aim to reach the peak within a few beats. Do not stall the climax to prolong the scene.");
            sb.AppendLine();

            if (context?.IsFinalSceneBeat == true)
            {
                sb.AppendLine("THIS IS THE FINAL BEAT — CONCLUDE NOW:");
                sb.AppendLine("Bring the scene to its definitive end this turn. If the peak has not yet come,");
                sb.AppendLine("reach it now; then resolve it, dismiss the prisoner, and emit end_conversation.");
                sb.AppendLine("Do not escalate further or open a new thread. The scene ends here.");
                sb.AppendLine();
            }

            sb.AppendLine("SCENE PACING — WHEN YOU ACT:");
            sb.AppendLine("When you decide to act physically on the prisoner, do NOT pause mid-act to await their reaction.");
            sb.AppendLine("Write the COMPLETE sequence in a single response:");
            sb.AppendLine("  1. Your spoken command or decision — in [DIALOGUE].");
            sb.AppendLine("  2. Everything that follows — in an extended [NARRATION] block.");
            sb.AppendLine("     Cover it fully: your order to the guards, how the prisoner is restrained,");
            sb.AppendLine("     what you do to them, what their body experiences, your own reaction.");
            sb.AppendLine("     Be explicit and unhurried. Do not collapse the act into a single sentence.");
            sb.AppendLine("  3. The aftermath — your remark, satisfaction, or a direct question to the prisoner");
            sb.AppendLine("     — in [DIALOGUE]. End with a question ONLY if you genuinely want them to speak.");
            sb.AppendLine("A question at the end of your [DIALOGUE] is the signal that the prisoner may respond.");
            sb.AppendLine("If no question, the scene continues on its own — they cannot interrupt.");
            sb.AppendLine("If prompted to continue while the act is still unfolding, pick up exactly where you left");
            sb.AppendLine("off and keep narrating. Do not restart from the beginning.");
            sb.AppendLine();
            sb.AppendLine("CLOSING THE SCENE:");
            sb.AppendLine("When the encounter reaches its end, you must CLOSE it properly — never let it just");
            sb.AppendLine("stop mid-act. The closing turn MUST do all of these, together, in one response:");
            sb.AppendLine("  1. A spoken dismissal in [DIALOGUE] (e.g. \"We are done here. Return him to his cell.\").");
            sb.AppendLine("  2. A [NARRATION] showing the prisoner released from their bonds and taken back to");
            sb.AppendLine("     their cell — the guards hauling them up and out, or you sending them away.");
            sb.AppendLine("  3. The end_conversation action:");
            sb.AppendLine("[ACTION]");
            sb.AppendLine("type: end_conversation");
            sb.AppendLine("[/ACTION]");
            sb.AppendLine("The prisoner is ALWAYS returned to their cell when the scene ends. Do not end on the");
            sb.AppendLine("act itself; resolve it, then remove them. A scene that simply halts mid-action is wrong.");
            sb.AppendLine();
            sb.AppendLine("CRITICAL — DO NOT emit end_conversation while the scene is still unfolding.");
            sb.AppendLine("Ordering guards to ACT ON the prisoner is part of the scene, not its end:");
            sb.AppendLine("  \"Bind her wrists\" — NOT a closing. The scene continues.");
            sb.AppendLine("  \"Strip away his clothing\" — NOT a closing. The scene continues.");
            sb.AppendLine("  \"Hold her still\" — NOT a closing. The scene continues.");
            sb.AppendLine("Only the prisoner's DISMISSAL and removal to their cell ends the scene.");
            sb.AppendLine();
        }

        private void AppendDiscoveryInstructions(StringBuilder sb)
        {
            if (AdultLevel == AdultContentLevel.Off) return;

            sb.AppendLine("EMIT [DISCOVERY] at most ONCE per exchange, ONLY when you have naturally revealed");
            sb.AppendLine("a personal preference, orientation, or intimate trait through your own words in [DIALOGUE].");
            sb.AppendLine("Do not force it. Do not repeat keys already listed under WHAT THIS PLAYER ALREADY KNOWS ABOUT YOU.");
            sb.AppendLine();
            sb.AppendLine("[DISCOVERY]");
            sb.AppendLine("key: a short slug identifying what was revealed");
            sb.AppendLine("     (e.g.: orientation, archetype, preference_dominant, preference_slow_burn, kink_bondage)");
            sb.AppendLine("description: what this player now perceives, in their voice (one sentence)");
            sb.AppendLine("[/DISCOVERY]");
            sb.AppendLine();
        }

        /// <summary>
        ///   Teaches the NPC to offer informal quests and acknowledge completed ones.
        ///   No-op unless <see cref="EnableQuests" /> is set. The reward is fixed at the
        ///   moment of offering and paid by the host on completion, so the NPC is told to
        ///   promise only what they would truly give. Completion is gated on verified
        ///   evidence (surfaced under YOUR QUESTS) — a player's bare claim is never enough.
        /// </summary>
        private void AppendQuestInstructions(StringBuilder sb)
        {
            if (!EnableQuests) return;

            sb.AppendLine("OFFERING TASKS (quests):");
            sb.AppendLine("When the conversation naturally calls for it and you have reason to trust or");
            sb.AppendLine("need this player, you may ask them to carry out a concrete deed. Nobles do not");
            sb.AppendLine("hand tasks to strangers — offer only when it fits who you are and where you stand.");
            sb.AppendLine();
            sb.AppendLine("Task types you may offer (use the exact 'type' token and the noted target):");
            sb.AppendLine("- bandit_clear (target_settlement): defeat bandits raiding near a settlement.");
            sb.AppendLine("- bandit_hideout (target_settlement): clear out a bandit hideout near a settlement.");
            sb.AppendLine("- attack_faction (target_faction): strike an enemy faction's parties — only if you war with them.");
            sb.AppendLine("- attack_lord (target_hero): defeat a specific enemy lord in battle.");
            sb.AppendLine("- raid_village (target_settlement): raid a village of a faction you war with.");
            sb.AppendLine("- attack_caravan (target_faction): destroy a caravan of an enemy faction.");
            sb.AppendLine("- siege (target_settlement): help take an enemy town or castle by siege.");
            sb.AppendLine("- capture_prisoner (target_hero): take a specific enemy hero prisoner.");
            sb.AppendLine("- execute_enemy (target_hero): kill a specific enemy hero.");
            sb.AppendLine("- rescue_prisoner (target_hero): free a specific ally held captive.");
            sb.AppendLine("- deliver_letter (target_hero): carry your message to a recipient — put it in 'description'.");
            sb.AppendLine("- provide_gold (no target needed): the player owes you financial support — they must give you denars in conversation. This quest is issued by the game, not by you; only emit [QUEST_COMPLETE] once the player has actually paid (the deed is shown as done in YOUR QUESTS).");
            sb.AppendLine("- scout_army (target_faction or target_hero): get close to an enemy army, observe its strength, and report back. Use target_hero to name the army's leader, or target_faction to accept any army of that faction.");
            sb.AppendLine();
            sb.AppendLine("[QUEST]");
            sb.AppendLine("type: one token from the list above");
            sb.AppendLine("target_settlement: name (only when the type needs it)");
            sb.AppendLine("target_hero: name (only when the type needs it)");
            sb.AppendLine("target_faction: name (only when the type needs it)");
            sb.AppendLine("deadline_days: N (optional; omit or 0 for open-ended; set it for urgent tasks like letters)");
            sb.AppendLine("reward_gold: N (denars you promise, 0 if none)");
            sb.AppendLine("reward_relation: N (personal regard you promise, 0 if none)");
            sb.AppendLine("description: one or two sentences in your own voice");
            sb.AppendLine("[/QUEST]");
            sb.AppendLine();
            sb.AppendLine("Rules: name real, plausible targets you would know. Promise only rewards you would");
            sb.AppendLine("truly pay — the figure is fixed now and honored on completion. Offer at most ONE task,");
            sb.AppendLine("and never while you already have one outstanding (see YOUR QUESTS).");
            sb.AppendLine();
            sb.AppendLine("When YOUR QUESTS shows a task the player has DONE (proof is listed) and you are");
            sb.AppendLine("satisfied, acknowledge it in [DIALOGUE] and emit:");
            sb.AppendLine("[QUEST_COMPLETE]");
            sb.AppendLine("type: the completed task's type token");
            sb.AppendLine("[/QUEST_COMPLETE]");
            sb.AppendLine("The game then pays the reward you promised. NEVER emit [QUEST_COMPLETE] for a task not");
            sb.AppendLine("shown as done — if the player only claims success without proof, doubt them and ask for");
            sb.AppendLine("specifics. Words are cheap; you reward deeds, not stories.");
            sb.AppendLine("Do NOT use give_gold to pay a quest reward yourself — the game pays the promised reward");
            sb.AppendLine("automatically through [QUEST_COMPLETE]. Reserve give_gold for separate gifts or bribes,");
            sb.AppendLine("never for a task the player reports completing, or they would be paid twice.");
            sb.AppendLine();
            sb.AppendLine("If the player tells you plainly they will NOT carry out an outstanding task — they");
            sb.AppendLine("withdraw, refuse, or give it up — react in character (disappointed, cold, understanding,");
            sb.AppendLine("as your nature dictates) and emit:");
            sb.AppendLine("[QUEST_ABANDON]");
            sb.AppendLine("type: the abandoned task's type token");
            sb.AppendLine("[/QUEST_ABANDON]");
            sb.AppendLine("Emit this ONLY when the player has clearly chosen to give up the task — never on your");
            sb.AppendLine("own initiative, and never merely because they are slow.");
            sb.AppendLine();
        }

        /// <summary>
        ///   Injects the list of personal traits the player already knows about this NPC.
        ///   Placed after ROMANTIC NATURE so the NPC can see what has already been shared
        ///   and avoid emitting duplicate [DISCOVERY] blocks.
        ///   No-op when AdultLevel is Off or no traits have been discovered yet.
        /// </summary>
        private void AppendDiscoveredTraits(StringBuilder sb, NpcProfile npc)
        {
            if (AdultLevel == AdultContentLevel.Off) return;
            if (npc.DiscoveredTraits == null || npc.DiscoveredTraits.Count == 0) return;

            sb.AppendLine("WHAT THIS PLAYER ALREADY KNOWS ABOUT YOU:");
            foreach (DiscoveredTrait trait in npc.DiscoveredTraits)
                sb.AppendLine($"- ({trait.Key}) {trait.Description}");
            sb.AppendLine("Do not emit [DISCOVERY] for any of these keys — they are already known.");
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
            if (EnableReputationBlock)
            {
                sb.AppendLine("EMIT [REPUTATION] only when the player's standing genuinely changes.");
                sb.AppendLine();
            }

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

            if (EnableReputationBlock)
            {
                sb.AppendLine("[REPUTATION]");
                sb.AppendLine("clan_delta: +N or -N");
                sb.AppendLine("faction_delta: +N or -N");
                sb.AppendLine("[/REPUTATION]");
                sb.AppendLine();
            }

            AppendActionInstructions(sb);
            AppendDiscoveryInstructions(sb);
            AppendQuestInstructions(sb);
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
            sb.AppendLine("EXCEPTION — named witnesses (listed under WITNESSES PRESENT if present):");
            sb.AppendLine("They are physically in the room. When they visibly react — a gesture, a");
            sb.AppendLine("glance, a brief word — emit a [WITNESS_REACTION] block for them. This is");
            sb.AppendLine("narrating an observable physical reaction, not speaking for them. Even when");
            sb.AppendLine("the player directly addresses a witness, that witness may show a visible");
            sb.AppendLine("reaction via [WITNESS_REACTION] without it counting as 'speaking for them'.");
            sb.AppendLine();
            sb.AppendLine("─────────────────────────────────────────────");
            sb.AppendLine();
        }

        #endregion
    }
}
