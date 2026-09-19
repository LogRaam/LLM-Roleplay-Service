// Code written by Gabriel Mailhot, 15/09/2026.
// What these pin: that turning the page from one turn to the next does not cost the player money.
//
// PromptCacheStabilityTests guards the same seam from the KNOWLEDGE side - a pack that changes under the
// character must declare itself volatile so it renders below the marker. This fixture guards it from the
// TURN side: a flag describing which beat of the conversation we are on must never reach the prefix either.
//
// It was reached. fkasad (Nexus, 15/09/2026) diffed two successive prompts from one conversation - the same
// player, the same NPC, nothing changed but the turn - and reported the cacheable prefix diverging around
// line 29, killing "98% of the cache". The cause was IsConversationOpening, a fact about THIS TURN, rendered
// twice up in the format teaching and in NARRATIVE VOICE, thousands of characters above the marker. Turn 1
// wrote a cache that the turn 2 prompt could not read, so every conversation paid two full prefix writes
// instead of one - and a write costs more than a read.
//
// He found it from prompt dumps, without the source. Nothing in the suite was asserting it.
//
// These measure through PromptCacheSplit, the same function NpcChatService runs, so they cannot pass while
// the game does something else. They build with the real action catalogue, because the share of the prompt
// that is lost is only meaningful against a prompt of the size players actually send.

#region

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class PromptCacheTurnStabilityTests
   {
      /// <summary>
      ///   Facts about WHICH BEAT of an ongoing conversation this is. Each flips between two turns the
      ///   player never leaves, so none of them may reach the cached prefix.
      /// </summary>
      private static IEnumerable<TestCaseData> PerTurnFlags()
      {
         yield return Flag("IsConversationOpening", on => Ctx(opening: on));
         yield return Flag("IsWitnessExchangeTurn", on => Ctx(witnessTurn: on));
         yield return Flag("IsLastWitnessExchange", on => Ctx(lastWitness: on));
         yield return Flag("IsFinalSceneBeat", on => Ctx(finalBeat: on));
         yield return Flag("IsBroughtCaptiveTurn", on => Ctx(broughtCaptive: on));
         yield return Flag("IsRoundTableTurn", on => Ctx(roundTable: on));
         yield return Flag("IsCouncilNarratorTurn", on => Ctx(councilNarrator: on));

         // THE ONES THIS FIXTURE MISSED. The 15/09 fix listed the booleans it knew about, and two one-shot
         // STRINGS with the same contract were left above the marker: both are consumed on read by the host, so
         // they are there on one turn and gone on the next, which is the very definition of a per-turn fact.
         // Fakade's report (19/09/2026) was the interception one surfacing as a role swap rather than as a bill.
         yield return Flag("InterceptionReason", on => Ctx(interception: on ? "It is YOU who sought this meeting." : null));
         yield return Flag("LlmFormatFeedbackNote", on => Ctx(formatNote: on ? "Your last [QUEST] named no target." : null));
         yield return Flag("NpcSoughtThePlayer", on => Ctx(sought: on));
      }

      // THE REGRESSION, as fkasad measured it from the outside.
      [TestCaseSource(nameof(PerTurnFlags))]
      public void GIVEN_only_the_turn_has_changed_WHEN_the_prompt_is_rebuilt_THEN_the_cacheable_prefix_is_unchanged(
         string flagName, Func<bool, EncounterContext> contextWith)
      {
         string? off = PrefixOf(contextWith(false));
         string? on = PrefixOf(contextWith(true));

         off.Should().NotBeNull("a real encounter always emits the marker the prefix is split on");
         on.Should().NotBeNull("a real encounter always emits the marker the prefix is split on");

         on.Should().Be(off, Because(flagName, off!, on!));
      }

      // POSITIVE CONTROL, and it earned its place: the first draft of this fixture built its contexts without
      // a Scene, so no marker was emitted, every prefix came back null, and the per-turn tests "passed" while
      // measuring nothing at all. NarrativeStyle is documented in PromptBuilder as the LAST thing in the
      // cacheable prefix, so changing it MUST move the prefix. If this ever goes green, the harness is blind.
      [Test]
      public void GIVEN_a_conversation_scoped_fact_changes_WHEN_the_prompt_is_rebuilt_THEN_the_cacheable_prefix_DOES_move()
      {
         string? plain = PrefixOf(Ctx());
         string? styled = PrefixOf(Ctx(style: "Novelistic"));

         plain.Should().NotBeNull();
         styled.Should().NotBe(plain, "NarrativeStyle sits inside the cacheable prefix by design, so the harness must see it move");
      }

      // NEGATIVE CONTROL: a fact belonging to the dynamic tail must leave the prefix alone, proving the split
      // really splits rather than handing back the whole prompt.
      [Test]
      public void GIVEN_a_tail_fact_changes_WHEN_the_prompt_is_rebuilt_THEN_the_cacheable_prefix_is_unchanged()
      {
         string? ashore = PrefixOf(Ctx());
         string? afloat = PrefixOf(Ctx(atSea: true));

         ashore.Should().NotBeNull();
         afloat.Should().Be(ashore, "AtSea renders after the marker, in the per-turn tail");
      }

      // The split is load-bearing: with no marker in the prompt, StablePrefixOf returns null and the client
      // falls back to caching the prompt WHOLE - volatile tail included - so every later turn busts all of it.
      [Test]
      public void GIVEN_an_ordinary_encounter_WHEN_the_prompt_is_built_THEN_it_carries_a_cache_marker()
      {
         PromptCacheSplit.StablePrefixOf(Build(Ctx())).Should().NotBeNull();
      }

      #region private

      private static TestCaseData Flag(string name, Func<bool, EncounterContext> factory)
         => new TestCaseData(name, factory).SetName($"the_turn_stays_out_of_the_cached_prefix({name})");

      /// <summary>
      ///   An ordinary conversation, varied by ONE fact. Scene is set because AppendEncounterContext only
      ///   emits the marker when ToPromptDescription() has something to say, and a context without one
      ///   builds a prompt no player is ever sent.
      /// </summary>
      private static EncounterContext Ctx(
         bool opening = false,
         bool witnessTurn = false,
         bool lastWitness = false,
         bool finalBeat = false,
         bool broughtCaptive = false,
         bool roundTable = false,
         bool councilNarrator = false,
         bool atSea = false,
         bool sought = false,
         string? interception = null,
         string? formatNote = null,
         string? style = null) => new() {
         LeanLevel = LeanPromptLevel.Full,
         Scene = SceneType.Keep,
         WarStatus = DiplomaticStatus.AtPeace,
         IsConversationOpening = opening,
         IsWitnessExchangeTurn = witnessTurn,
         IsLastWitnessExchange = lastWitness,
         IsFinalSceneBeat = finalBeat,
         IsBroughtCaptiveTurn = broughtCaptive,
         IsRoundTableTurn = roundTable,
         IsCouncilNarratorTurn = councilNarrator,
         AtSea = atSea,
         NpcSoughtThePlayer = sought,
         InterceptionReason = interception,
         LlmFormatFeedbackNote = formatNote,
         NarrativeStyle = style
      };

      /// <summary>Built with the real catalogue, so a percentage of the prefix means what it says.</summary>
      private static string Build(EncounterContext context)
         => new PromptBuilder {
               ActionVocabulary = GameActionCatalog.All
                                                   .Select(s => new GameActionDefinition {
                                                      Type = s.Type, Description = s.Description
                                                   })
                                                   .ToList()
            }
            .BuildSystemPrompt(
               new NpcProfile {Id = "n", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc", Age = 40},
               new WorldState {CurrentDay = 10},
               context);

      /// <summary>The cacheable prefix the GAME would send, via the same rule NpcChatService runs.</summary>
      private static string? PrefixOf(EncounterContext context) => PromptCacheSplit.StablePrefixOf(Build(context));

      /// <summary>
      ///   Names the damage in the terms that matter: how early the prefix diverges is how much of it dies,
      ///   reported the way a player measures it from two prompt dumps.
      /// </summary>
      private static string Because(string flagName, string off, string on)
      {
         int at = FirstDifference(off, on);
         double lost = off.Length == 0 ? 0 : 100.0 * (off.Length - at) / off.Length;

         return $"{flagName} is a fact about THIS TURN and must render after "
              + $"{PromptBuilder.EncounterSectionHeading}, never in the cached prefix - "
              + $"it diverges at char {at} of {off.Length}, discarding {lost:F1}% of the prefix on the next turn";
      }

      private static int FirstDifference(string a, string b)
      {
         int n = Math.Min(a.Length, b.Length);

         for (var i = 0; i < n; i++)
            if (a[i] != b[i])
               return i;

         return n;
      }

      #endregion
   }
}
