// Code written by Gabriel Mailhot, 05/10/2026.
// Player report (IdentityCrysys, Nexus, 05/10/2026): "I tried leading a character to privacy through roleplay and
// after it happened, the witness was still present; then I pressed Request Private and it treated it as if we were
// in the same place we were to begin with." A private audience was recorded only when the player ASKED for one and
// the character ANSWERED; the catalogue even forbade privacy "assumed or implied". So a player who leads the
// character to a quiet corner, and a character who goes, left every witness listed as present: the next turns (and
// the Request Private button) were written as if they still stood in the hall.
//
// WHY IT MATTERS: the witnesses list IS where the scene is, for the model. If going apart is not recorded, the
// character keeps speaking for an audience that is no longer there, and the player cannot get them back to privacy
// through the story they are telling.

#region

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
   public sealed class PrivacyByGoingApartTests
   {
      private static string Prompt()
         => new PromptBuilder().BuildSystemPrompt(new NpcProfile {Id = "lady_ira", Name = "Ira", Faction = "Vlandia", Clan = "dey Meroc"},
            new WorldState {CurrentDay = 10},
            new EncounterContext {
               LeanLevel = LeanPromptLevel.Full,
               Witnesses = new List<WitnessEntry> {new() {Name = "Sir Aldo", RelationToNpc = "your guard", HeroStringId = "aldo_id"}}
            });

      // STAKE: the character writing the reply is told that going apart with the player IS the private audience,
      // and to record it, accepted when they go and refused when they will not.
      [Test]
      public void GIVEN_witnesses_present_WHEN_the_character_is_taught_the_private_audience_THEN_going_apart_with_the_player_counts()
      {
         string prompt = Prompt();
         string section = prompt.Substring(prompt.IndexOf("PRIVATE AUDIENCE:", System.StringComparison.Ordinal));

         section.Should().Contain("takes you aside or leads you away").And.Contain("when you go along with them");
      }

      // STAKE: the interpreter (Enhanced mode) reads the reply, not the request: it must report the private audience
      // when the reply shows the pair going apart, and no longer treat that as privacy merely "implied".
      [Test]
      public void GIVEN_the_interpreter_catalogue_WHEN_request_privacy_is_taught_THEN_going_apart_is_an_acceptance_it_reports()
      {
         GameActionSpec spec = GameActionCatalog.All.Single(s => s.Type == "request_privacy");

         spec.Tells.Should().Contain(t => t.Contains("goes apart with the player"));
         spec.AntiPatterns.Should().NotContain(t => t.Contains("assumed or implied"));
      }
   }
}
