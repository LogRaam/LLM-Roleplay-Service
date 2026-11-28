// Code written by Gabriel Mailhot, 28/11/2026.
// Stage 5 of the group-scene plan, the novel's presentation. Gabriel (28/11/2026), on "la meilleure forme qui se
// rapproche de la qualité d'un roman": "oui ok j'aime ça": speech in quotation marks, gestures as plain third-person
// prose, no asterisks. PromptBuilder.EnableNovelStyle (the mod's "Narrative style" set to Novel) decides which style an
// ordinary conversation is taught. If it is wrong one way, a player who kept Roleplay reads a changed prompt (every
// character speaks differently); the other way, a Novel player still gets "*I narrow my eyes*". And the two risks the
// plan names: the player's *asterisks* must stay real acts, and a captive scene keeps its own voice contract untouched.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NovelStylePromptTests
   {
      private static NpcProfile Npc(bool female = false) => new() {
         Id = "npc_test",
         Name = "Test Lady",
         Faction = "Vlandia",
         Clan = "dey Meroc",
         IsFemale = female
      };

      private static string Build(bool novel, EncounterContext context, AdultContentLevel level = AdultContentLevel.Mature, bool female = false)
         => new PromptBuilder {AdultLevel = level, EnableNovelStyle = novel}.BuildSystemPrompt(Npc(female), new WorldState {CurrentDay = 10}, context);

      // STAKE: a Novel player who still sees "*I narrow my eyes*" taught gets the roleplay style they turned away from.
      [Test]
      public void GIVEN_novel_style_WHEN_a_full_prompt_is_built_THEN_it_asks_for_quoted_speech_and_third_person_gestures()
      {
         string prompt = Build(true, new EncounterContext {LeanLevel = LeanPromptLevel.Full}, female: true);

         prompt.Should().Contain("your spoken words inside double quotation");
         prompt.Should().Contain("plain prose in the THIRD person");
         prompt.Should().Contain("She narrows her eyes. \"You came back.\"");
         prompt.Should().Contain("with NO asterisks");
         prompt.Should().NotContain("in the FIRST person - \"*I narrow my eyes*\"");
         prompt.Should().NotContain("*She raises an eyebrow.*");
         prompt.Should().NotContain("*She sets down her cup");
      }

      // STAKE: the novel style invites a model to narrate the room, the player included; it must be told never to write
      // the player's words, thoughts or actions.
      [Test]
      public void GIVEN_novel_style_WHEN_built_THEN_it_forbids_writing_the_player()
      {
         string full = Build(true, new EncounterContext {LeanLevel = LeanPromptLevel.Full});
         string compact = Build(true, new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

         full.Should().Contain("Never write the player's words, thoughts or actions");
         compact.Should().Contain("never the player's words or acts");
      }

      // STAKE: the player's *asterisks* are their acts in both styles; a novel style that dropped this teaching would have
      // characters ignore what the player does.
      [Test]
      public void GIVEN_novel_style_WHEN_built_THEN_the_players_asterisks_are_still_taught_as_acts()
      {
         string full = Build(true, new EncounterContext {LeanLevel = LeanPromptLevel.Full});
         string compact = Build(true, new EncounterContext {LeanLevel = LeanPromptLevel.Lean});

         full.Should().Contain("text wrapped in asterisks, e.g. *I take you by the wrist*");
         full.Should().Contain("The player's *stage directions* are accomplished fact");
         compact.Should().Contain("WHAT THE PLAYER DOES: *asterisks* or a first-person physical act is real, now.");
      }

      // STAKE: Compact serves small local models, which copy the example they are shown: the example must be the novel
      // one, or the model writes asterisks whatever the rule says.
      [Test]
      public void GIVEN_novel_style_WHEN_a_compact_prompt_is_built_THEN_its_examples_carry_no_asterisks()
      {
         string prompt = Build(true, new EncounterContext {LeanLevel = LeanPromptLevel.Lean}, female: true);

         prompt.Should().Contain("speech in double quotes, your own gestures in plain third-person prose, no asterisks");
         prompt.Should().Contain("Brief gestures in plain third-person prose (never \"I\", no asterisks), speech in quotes:");
         prompt.Should().Contain("\nShe sets down her cup and studies you a moment.");
         prompt.Should().NotContain("*She sets down her cup");
         prompt.Should().NotContain("*She glances away, toward the hearth.*");
      }

      // STAKE: the default is Roleplay; a player who never touched the setting must read exactly today's teaching.
      [Test]
      public void GIVEN_the_default_style_WHEN_built_THEN_the_roleplay_teaching_stands()
      {
         var builder = new PromptBuilder {AdultLevel = AdultContentLevel.Mature};
         string prompt = builder.BuildSystemPrompt(Npc(), new WorldState {CurrentDay = 10}, new EncounterContext());

         builder.EnableNovelStyle.Should().BeFalse();
         prompt.Should().Contain("in the FIRST person - \"*I narrow my eyes*\"");
         prompt.Should().NotContain("with NO asterisks");
      }

      // STAKE: a captive scene has its own third-person contract (what the captor sees and hears); the novel style must
      // leave it exactly as it is.
      [Test]
      public void GIVEN_novel_style_WHEN_the_player_is_held_captive_THEN_the_prompt_is_unchanged()
      {
         var captive = new EncounterContext {PlayerStatus = PlayerStatusVsNpc.Captive, CaptiveIntent = CaptiveSceneIntent.PersonalDesire};

         string novel = Build(true, captive, AdultContentLevel.Hardcore);
         string roleplay = Build(false, captive, AdultContentLevel.Hardcore);

         novel.Should().Be(roleplay);
         new PromptBuilder {AdultLevel = AdultContentLevel.Hardcore, EnableNovelStyle = true}.NovelStyleApplies(captive).Should().BeFalse();
      }

      // STAKE: the player as captor runs the captor scene rules; the novel style must not reach them either.
      [Test]
      public void GIVEN_novel_style_WHEN_the_player_holds_a_captive_THEN_the_prompt_is_unchanged()
      {
         var captor = new EncounterContext {IsCaptorScene = true};

         Build(true, captor, AdultContentLevel.Hardcore).Should().Be(Build(false, captor, AdultContentLevel.Hardcore));
      }
   }
}
