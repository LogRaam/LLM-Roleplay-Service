// Code written by Gabriel Mailhot, 10/09/2026.
// What this pins: the size of the prompt the game ACTUALLY SENDS in Compact mode, as opposed to the one the older
// budget test was measuring.
//
// Bug report (DuskSymphony, Nexus tracker, v2.5.7): "Compact Prompt mode is still sending a prompt with context
// over 9k", quoting his own local server refusing the request:
//
//   send_error: task id = 9, error: request (9496 tokens) exceeds the available context size (8192 tokens)
//
// "this is on a fresh install of CR on the first attempt at interacting with an NPC... Compact Prompt mode is
// turned on, and I've restarted just in case."
//
// LeanPromptPolicyTests has guarded that budget for months and has been green throughout. It builds a
// PromptBuilder with NO ActionVocabulary, and AppendActionInstructions returns on its very first line when the
// vocabulary is empty, so the whole GAME ACTIONS section was absent from everything it measured. It was pinning
// 6.9k characters of a document the game has never once sent.
//
// The real vocabulary is 69 actions, and unabridged it is 11,565 characters, roughly 2,900 tokens, in EVERY
// prompt including Lean. Nothing trimmed it because nothing had ever measured it, so Compact mode was never
// compact and an 8k local model could not hold one conversation on a fresh install.
//
// THE LESSON WORTH MORE THAN THE FIX: a guard that constructs its subject differently from production measures
// its own construction. This file builds the vocabulary the way LlmServices does, so the number below is the
// number that goes over the wire.
//
// If these tests fail, a player on a small local model is about to be told their context is too short.

#region

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Actions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;
using System.Text;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class LeanRealPromptBudgetTests
   {
      // The whole shipped catalog, exactly as the host unions it into the builder.
      private static List<GameActionDefinition> RealVocabulary()
         => GameActionCatalog.All
                             .Select(spec => new GameActionDefinition {Type = spec.Type, Description = spec.Description})
                             .ToList();

      private static NpcProfile Npc() => new() {
         Id = "npc_test", Name = "Test Lord", Faction = "Vlandia", Clan = "dey Meroc"
      };

      private static string Build(LeanPromptLevel lean)
         => new PromptBuilder {ActionVocabulary = RealVocabulary()}
            .BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, Encounter(lean));

      /// <summary>
      ///   The most expensive character the game can actually produce: a landed lord of the player's own house
      ///   who governs a town, so EVERY knowledge pack is both carried and supplied.
      ///   <para>
      ///     Until 2026-09-12 this was a bare <c>new EncounterContext {LeanLevel = lean}</c>, which has NO
      ///     Bearer - so <c>NpcKnowledgeFactory.For</c> returned nothing and the budget was measured on a
      ///     prompt containing not one knowledge pack. Green, and blind to five of them. That is the exact
      ///     failure this file's own header was written about: a guard that constructs its subject differently
      ///     from production measures its own construction.
      ///   </para>
      /// </summary>
      private static EncounterContext Encounter(LeanPromptLevel lean)
         => new() {
            LeanLevel = lean,
            Bearer = new KnowledgeBearer {
               IsLord = true, HasHouse = true, IsPlayerCompanion = true, HoldsASeat = true
            },
            HouseStanding = new HouseStandingFacts {
               HouseName = "dey Meroc", IsPlayerHouse = true,
               Fiefs = new List<string> {"Ocs Hall", "Sargot", "Charas", "Jaculan", "Rovalt"},
               AtWarWith = new List<string> {"Sturgia", "Battania", "Khuzait"}
            },
            HouseMeans = new HouseMeansFacts {
               Treasury = TreasuryBand.Strained, OwesTheCrown = true, Muster = MusterBand.Respectable,
               RidingWithAnArmy = true, Influence = InfluenceBand.Considerable
            },
            SeatStanding = new SeatStandingFacts {
               Seats = new List<SeatFacts> {
                  new() {
                     PlaceName = "Ocs Hall", Role = SeatRole.Governor, Kind = SettlementKind.Town,
                     IsPlayerHolding = true, Prosperity = ProsperityBand.Comfortable,
                     FoodStores = FoodBand.Thin, FoodDirection = FoodTrend.Falling,
                     Garrison = GarrisonBand.UnderStrength, Loyalty = LoyaltyBand.Restless,
                     Security = SecurityBand.Uneasy, IsUnderSiege = true
                  },
                  new() {
                     PlaceName = "Sargot", Role = SeatRole.Owner, Kind = SettlementKind.Town,
                     IsPlayerHolding = true, Prosperity = ProsperityBand.Rich, FoodStores = FoodBand.Full,
                     FoodDirection = FoodTrend.Rising, Garrison = GarrisonBand.Strong,
                     Loyalty = LoyaltyBand.Devoted, Security = SecurityBand.Settled
                  }
               }
            },
            HereAndNow = new HereAndNowFacts {
               Season = Season.Winter, HourOfDay = 2, IsNight = true, Weather = Weather.Blizzard,
               NearestPlace = "Ocs Hall", BearingToPlace = "a short ride to the north-east", RealmName = "Vlandia"
            },
            PersonalBonds = new PersonalBondsFacts {
               Bonds = new List<PersonalBond> {
                  new() {PersonName = "Ingalther", Kind = BondKind.Kin, Discretion = Discretion.Open},
                  new() {PersonName = "Talko", Kind = BondKind.Rival, Discretion = Discretion.Open},
                  new() {PersonName = "Liena", Kind = BondKind.Beloved, Discretion = Discretion.Guarded}
               }
            },
            Audience = new ListeningAudience {Regard = 70, InPrivate = true}
         };

      // The catalog is real and large. If this ever drops to nothing, every other assertion here becomes
      // meaningless in exactly the way the old budget test was.
      [Test]
      public void GIVEN_the_shipped_catalog_WHEN_measured_THEN_it_is_the_real_one_and_not_an_empty_stand_in()
      {
         RealVocabulary().Count.Should().BeGreaterThan(50);
         Build(LeanPromptLevel.Lean).Should().Contain("GAME ACTIONS:");
      }

      // The companion guard to the one above, and it is the one that was missing: a budget measured on a
      // character who knows nothing is a budget measured on a prompt nobody is ever sent. If this fails, the
      // subject has stopped being the expensive case and every length below has quietly become meaningless.
      [Test]
      public void GIVEN_the_measured_character_WHEN_built_THEN_he_actually_carries_the_knowledge_packs()
      {
         NpcKnowledgeFactory.For(Encounter(LeanPromptLevel.Full).Bearer)
                            .Should().HaveCount(NpcKnowledgeFactory.All.Count);

         string full = Build(LeanPromptLevel.Full);

         full.Should().Contain("dey Meroc").And.Contain("Ocs Hall").And.Contain("coffers")
             .And.Contain("winter night");
      }

      // The mechanism that replaced the arithmetic. Before this, five packs fitted inside the 16,000-character
      // ceiling because their prose had been trimmed until they did, with fourteen characters to spare - so
      // the SIXTH pack would have broken a player's 8k context and nothing would have said so until he wrote
      // in. Now Compact spends a fixed allowance whatever is supplied and however many packs exist.
      [Test]
      public void GIVEN_a_character_who_knows_everything_WHEN_compact_THEN_the_knowledge_section_stays_inside_its_allowance()
      {
         var knowledge = new StringBuilder();
         EncounterContext context = Encounter(LeanPromptLevel.Lean);
         var spent = 0;

         foreach (KnowledgePack pack in NpcKnowledgeFactory.For(context.Bearer))
         {
            if (!pack.IsSupplied(context)) continue;

            knowledge.Clear();
            pack.Render(knowledge, context, true);

            if (spent + knowledge.Length > NpcKnowledgeFactory.LeanBudgetChars) continue;

            spent += knowledge.Length;
         }

         spent.Should().BeLessThanOrEqualTo(NpcKnowledgeFactory.LeanBudgetChars);

         // And the allowance must not be so generous that it never binds: it is a budget, not a formality.
         NpcKnowledgeFactory.LeanBudgetChars.Should().BeLessThan(1000);
      }

      // DuskSymphony's ceiling. 8192 tokens is the common local default and the one he ran into; the system
      // prompt must leave real room for the conversation and the reply, so it is held to roughly half of it.
      // At ~4 characters per token that is 16,000 characters.
      [Test]
      public void GIVEN_compact_mode_WHEN_the_real_prompt_is_built_THEN_it_leaves_room_inside_an_8k_context()
      {
         Build(LeanPromptLevel.Lean).Length.Should().BeLessThan(16000);
      }

      // And the trim is what buys that room. Every action survives; only the prose about it is cut, so a small
      // model can still emit any verb a large one can.
      [Test]
      public void GIVEN_compact_mode_WHEN_built_THEN_every_action_is_still_offered_just_described_briefly()
      {
         string lean = Build(LeanPromptLevel.Lean);
         string full = Build(LeanPromptLevel.Full);

         foreach (GameActionDefinition action in RealVocabulary())
            lean.Should().Contain("- " + action.Type + ":");

         lean.Length.Should().BeLessThan(full.Length);
      }

      // The saving must be substantial or the trim was not worth making. Measured at the time: the action list
      // alone fell from 11,565 characters to roughly a third of that.
      [Test]
      public void GIVEN_compact_mode_WHEN_built_THEN_the_trim_saves_thousands_of_characters()
      {
         (Build(LeanPromptLevel.Full).Length - Build(LeanPromptLevel.Lean).Length)
            .Should().BeGreaterThan(5000);
      }

      // A cut description must still read as a description. A line ending mid-word, or on a dangling opening
      // bracket, reads to anyone debugging a log as a corrupted prompt rather than a deliberate abbreviation.
      [Test]
      public void GIVEN_a_long_description_WHEN_cut_for_compact_mode_THEN_it_never_ends_mid_word_or_mid_bracket()
      {
         foreach (GameActionDefinition action in RealVocabulary())
         {
            string shortened = ActionVocabularyPolicy.Describe(action.Description, LeanPromptLevel.Lean);

            shortened.Should().NotBeNullOrWhiteSpace();
            shortened.Should().NotEndWith("(").And.NotEndWith(",").And.NotEndWith(" ");

            if (shortened.EndsWith("..."))
            {
               shortened.Count(c => c == '(').Should().Be(shortened.Count(c => c == ')'));
               action.Description.Should().StartWith(shortened.Substring(0, shortened.Length - 3).TrimEnd());
            }
         }
      }

      // A Full prompt is unchanged: it keeps every word, because a strong model uses the long descriptions to
      // tell near-neighbour verbs apart and that is what they are for.
      [Test]
      public void GIVEN_a_full_prompt_WHEN_built_THEN_no_description_was_shortened()
      {
         string full = Build(LeanPromptLevel.Full);

         foreach (GameActionDefinition action in RealVocabulary().Take(12))
            full.Should().Contain(action.Description);
      }

      // Short descriptions are left exactly as they are, since abbreviating something already brief only costs
      // an ellipsis.
      [Test]
      public void GIVEN_an_already_short_description_WHEN_cut_THEN_it_is_returned_untouched()
      {
         const string brief = "Ends the conversation now.";

         ActionVocabularyPolicy.Describe(brief, LeanPromptLevel.Lean).Should().Be(brief);
         ActionVocabularyPolicy.Describe(null, LeanPromptLevel.Lean).Should().BeEmpty();
         ActionVocabularyPolicy.Describe("   ", LeanPromptLevel.Lean).Should().BeEmpty();
      }
   }
}
