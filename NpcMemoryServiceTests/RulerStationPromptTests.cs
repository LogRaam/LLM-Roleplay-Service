// Code written by Gabriel Mailhot, 08/09/2026.
// What this pins: whether an NPC is told what they ARE in the realm, in the identity section that opens every
// prompt, rather than only inside an offer that may or may not be on the table.
//
// Why it exists. A player reported on 2026-09-08: "Kings and Emperors sometimes do not know they are king. I
// tried to address Derthert and Garios as kings but they deny the fact and claim that they are just a lord
// serving the throne. This is pretty annoying when you try to do diplomacy via dialogue."
//
// They were not being modest. They were never told. The mod knew perfectly well who ruled (the host reads
// Kingdom.Leader), but the ONLY rulership fact in the prompt was VassalOfferKingdom, which is doubly gated: the
// NPC must rule AND the player's clan must be free to swear fealty. So a king learned his own rank in exactly one
// conversation, the one where an oath could be sworn, and denied it in every other. That gate is the reporter's
// "sometimes", and it is why the fix belongs in IDENTITY: a crown is not an offer, it is who someone is.
//
// If these tests fail, a monarch is about to start calling himself a lord in service again, and diplomacy through
// dialogue, which is what the reporter was actually trying to do, stops working.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class RulerStationPromptTests
   {
      private static NpcProfile Npc() => new() {
         Id = "npc_test",
         Name = "Derthert",
         Faction = "Vlandia",
         Clan = "dey Meroc"
      };

      private static string Build(EncounterContext context) =>
         new PromptBuilder().BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, context);

      // The report itself. A ruler must know he rules, and be told so where he cannot miss it.
      [Test]
      public void GIVEN_an_npc_who_rules_a_realm_WHEN_the_prompt_is_built_THEN_they_are_told_so_by_name()
      {
         string prompt = Build(new EncounterContext {RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King"});

         prompt.Should().Contain("YOU RULE VLANDIA");
         prompt.Should().Contain("King");
      }

      // The exact sentence the reporter got back was a courteous denial, so the denial is what must be forbidden.
      // Telling the model the fact is not enough on its own: it had a whole conversation's worth of habit saying
      // that a lord defers to his throne.
      [Test]
      public void GIVEN_a_ruler_WHEN_the_prompt_is_built_THEN_denying_the_crown_is_ruled_out()
      {
         string prompt = Build(new EncounterContext {RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King"});

         prompt.Should().Contain("Never deny it");
         prompt.Should().Contain("merely a lord");
      }

      // The reporter wrote "Kings AND Emperors". The title comes from the game's own EncyclopediaRulerTitle, so
      // Garios is an Emperor and a Khuzait ruler is a Khan. Flattening every crown into "king" would answer half
      // the report and quietly break the other half.
      [Test]
      public void GIVEN_a_realm_with_its_own_word_for_its_ruler_WHEN_built_THEN_that_word_is_used()
      {
         string prompt = Build(new EncounterContext {RuledRealmName = "Western Empire", RuledRealmRulerTitle = "Emperor"});

         prompt.Should().Contain("Emperor");
         prompt.Should().NotContain("its King");
      }

      // A missing title must still produce a usable sentence. This reads off a live engine object, and a future
      // game build that renames or empties it must leave a ruler knowing he rules, not silently uncrowned again.
      [Test]
      public void GIVEN_a_ruler_whose_title_is_unavailable_WHEN_built_THEN_they_are_still_told_they_rule()
      {
         string prompt = Build(new EncounterContext {RuledRealmName = "Vlandia", RuledRealmRulerTitle = null});

         prompt.Should().Contain("YOU RULE VLANDIA");
         prompt.Should().Contain("its ruler");
      }

      // The same blind spot one rung down, which Gabriel asked to include: the identity line has always named the
      // house and never said whether this person leads it, so a clan head spoke as one of its members.
      [Test]
      public void GIVEN_an_npc_who_heads_their_clan_WHEN_built_THEN_they_are_told_they_lead_it()
      {
         Build(new EncounterContext {LeadsOwnClan = true}).Should().Contain("You HEAD the dey Meroc clan");
      }

      // A king also heads the ruling clan, which is true and worth nothing beside a crown. Saying both would spend
      // prompt on a smaller fact that can only dilute the larger one.
      [Test]
      public void GIVEN_a_ruler_who_also_heads_their_clan_WHEN_built_THEN_only_the_crown_is_stated()
      {
         string prompt = Build(new EncounterContext {
            RuledRealmName = "Vlandia", RuledRealmRulerTitle = "King", LeadsOwnClan = true
         });

         prompt.Should().Contain("YOU RULE VLANDIA");
         prompt.Should().NotContain("You HEAD the");
      }

      // And an ordinary character gets no station line at all. Every unconditional section is paid for by every
      // conversation in the game, so one that has nothing to say must say nothing.
      [Test]
      public void GIVEN_an_npc_of_no_particular_station_WHEN_built_THEN_no_station_line_is_printed()
      {
         string prompt = Build(new EncounterContext());

         prompt.Should().NotContain("YOU RULE");
         prompt.Should().NotContain("You HEAD the");
      }
   }
}
