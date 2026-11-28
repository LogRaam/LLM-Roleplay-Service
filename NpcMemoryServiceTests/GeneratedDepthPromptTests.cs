// Code written by Gabriel Mailhot, 28/11/2026.
// Generated depth (Stage 4 of the group-scene plan; Gabriel, 28/11/2026: "go pour tout le système ... je testerai demain"). A
// character known only from its game sheet is given, once, a voice, a mask, a fear, a limit and a private secret, kept for good.
// Gabriel on the secret (27/11/2026): private life "si ça ne touche pas à une mécanique du jeu".
//
// These tests pin how the prompt shows it. If they are wrong: a save that never used the feature sees its prompt move (and the
// goldens with it); a character gets a second voice line beside its own habit (the ruling of 15/11/2026: no invented tics); an
// authored voice or the game's own fear is overridden by a generated one; a secret already told to the player is still called
// "known only to you"; or the depth appears mid-conversation and changes the character between two lines.

#region

using System.Linq;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class GeneratedDepthPromptTests
   {
      private const string Player = "Arwa";
      private const string Habit = "You answer a question with a question when you can.";

      private static GeneratedDepth Depth() => new() {
         Voice = "slow and exact, short sentences, never a flourish",
         Mask = "a governor who has every ledger in hand",
         Fear = "being thought a fool by the young",
         Limit = "will not beg a favour twice",
         Secret = "Once a suitor wanted to court her friend Irana, and she talked him out of it; Irana never knew.",
         GeneratedOnDay = 40
      };

      private static NpcProfile Npc(GeneratedDepth depth = null) => new() {Id = "lord_test", Name = "Alympia", Faction = "Empire", Clan = "Pethros", GeneratedDepth = depth};

      private static string Prompt(NpcProfile npc, EncounterContext ctx = null, LeanPromptLevel lean = LeanPromptLevel.Full, bool enabled = true, bool meetingMemory = true)
      {
         ctx ??= new EncounterContext();
         var c = new EncounterContext {
            CharacterVoice = ctx.CharacterVoice ?? Habit, LeanLevel = lean, GeneratedDepthHidden = ctx.GeneratedDepthHidden,
            MeetingMemoryFrozen = ctx.MeetingMemoryFrozen, MeetingMemoryAtOpening = ctx.MeetingMemoryAtOpening
         };

         return new PromptBuilder {AdultLevel = AdultContentLevel.Mature, PlayerName = Player, EnableGeneratedDepth = enabled, EnableMeetingMemory = meetingMemory}
               .BuildSystemPrompt(npc, new WorldState {CurrentDay = 50}, c)
               .Replace("\r\n", "\n");
      }

      // STAKE: the default must not move. No depth, an empty one, or the host's switch off write the very same prompt.
      [Test]
      public void GIVEN_no_depth_an_empty_one_or_the_switch_off_WHEN_the_prompt_is_built_THEN_it_is_the_same_as_without()
      {
         string bare = Prompt(Npc());

         Prompt(Npc(new GeneratedDepth())).Should().Be(bare);
         Prompt(Npc(new GeneratedDepth {Mask = "  ", Secret = ""})).Should().Be(bare);
         Prompt(Npc(Depth()), enabled: false).Should().Be(bare);
         new PromptBuilder {AdultLevel = AdultContentLevel.Mature, PlayerName = Player}.EnableGeneratedDepth.Should().BeFalse("a host that never generates depth keeps its prompt");
      }

      // STAKE: the character carries its depth in the cached prefix, worded exactly, beside its voice and above the marker.
      [Test]
      public void GIVEN_a_depth_WHEN_the_prompt_is_built_THEN_the_section_reads_exactly_so_above_the_marker()
      {
         string prompt = Prompt(Npc(Depth()));

         prompt.Should().Contain(
            "BENEATH WHAT OTHERS SEE (who you are under the surface; let it shape how you act and speak, never recite it):\n"
            + "- How you want to be seen: a governor who has every ledger in hand.\n"
            + "- What you fear most: being thought a fool by the young.\n"
            + "- What you will never do: will not beg a favour twice.\n"
            + "- Known only to you: Once a suitor wanted to court her friend Irana, and she talked him out of it; Irana never knew. "
            + "Yours to keep or to tell; if pressed, deflect or stay silent, and never deny it with a falsehood.\n");
         prompt.IndexOf("BENEATH WHAT OTHERS SEE", System.StringComparison.Ordinal).Should().BeLessThan(prompt.IndexOf("CURRENT ENCOUNTER", System.StringComparison.Ordinal));
         prompt.IndexOf("BENEATH WHAT OTHERS SEE", System.StringComparison.Ordinal).Should().BeGreaterThan(prompt.IndexOf("YOUR VOICE", System.StringComparison.Ordinal));
      }

      // STAKE: never a second voice line. The generated voice joins the character's own habit inside YOUR VOICE, once.
      [Test]
      public void GIVEN_a_generated_voice_WHEN_the_prompt_is_built_THEN_it_joins_the_one_voice_block()
      {
         string prompt = Prompt(Npc(Depth()));

         prompt.Should().Contain("YOUR VOICE (how you, and no one else, speak):\n" + Habit + "\nBeneath that habit, your manner of speech: slow and exact, short sentences, never a flourish.\n");
         prompt.Split("YOUR VOICE").Length.Should().Be(2, "one voice block only");
      }

      // STAKE: a character the player gave a voice keeps it: the generated voice never shows beside an authored backstory.
      [Test]
      public void GIVEN_an_authored_backstory_WHEN_the_prompt_is_built_THEN_the_generated_voice_is_not_shown()
      {
         NpcProfile npc = Npc(Depth());
         npc.AuthoredBackstory = "A former captain who speaks in the manner of an old sailor.";

         string prompt = Prompt(npc);

         prompt.Should().NotContain("your manner of speech");
         prompt.Should().Contain("How you want to be seen", "the rest of the depth still complements the authored character");
      }

      // STAKE: the game's own fear wins; a fear rolled after the depth was generated hides the generated one.
      [Test]
      public void GIVEN_the_game_gave_a_fear_WHEN_the_prompt_is_built_THEN_the_generated_fear_is_not_shown()
      {
         NpcProfile npc = Npc(Depth());
         npc.Fear = Fear.Fire;

         Prompt(npc).Should().NotContain("What you fear most");
      }

      // STAKE: a conversation that opened before the depth landed keeps the character as it was: nothing appears mid-talk.
      [Test]
      public void GIVEN_a_conversation_opened_without_depth_WHEN_it_lands_THEN_nothing_shows_until_the_next_one()
      {
         string bare = Prompt(Npc());

         Prompt(Npc(Depth()), new EncounterContext {GeneratedDepthHidden = true}).Should().Be(bare);
      }

      // STAKE: once the meeting record holds the secret, it owns it: no double listing, and never "known only to you" once told.
      [Test]
      public void GIVEN_the_meeting_record_holds_the_secret_WHEN_the_prompt_is_built_THEN_the_depth_leaves_it_to_the_record()
      {
         NpcProfile npc = Npc(Depth());
         npc.MeetingMemory = new MeetingMemory {
            AboutPlayer = Player,
            Secrets = {new MeetingSecret {Text = Depth().Secret, SinceDay = 40, ToldTo = {Player}, ToldOnDay = 45}}
         };

         string prompt = Prompt(npc);
         prompt.Should().NotContain("Known only to you");
         prompt.Should().Contain("How you want to be seen");

         // With the meeting record not shown (its switch off), the depth keeps its secret line.
         Prompt(npc, meetingMemory: false).Should().Contain("Known only to you");
      }

      // STAKE: a small model's Lean prompt carries the depth short, so the budget holds.
      [Test]
      public void GIVEN_a_full_depth_on_Lean_WHEN_built_THEN_the_section_stays_short()
      {
         var big = new GeneratedDepth {
            Voice = new string('v', GeneratedDepth.MaxVoiceChars), Mask = new string('m', GeneratedDepth.MaxTraitChars), Fear = new string('f', GeneratedDepth.MaxTraitChars),
            Limit = new string('l', GeneratedDepth.MaxTraitChars), Secret = new string('s', GeneratedDepth.MaxSecretChars)
         };

         int added = Prompt(Npc(big), lean: LeanPromptLevel.Lean).Length - Prompt(Npc(), lean: LeanPromptLevel.Lean).Length;

         added.Should().BeLessThan(1700, "the depth at its fullest adds a bounded block");
         Prompt(Npc(big), lean: LeanPromptLevel.Lean).Should().Contain("BENEATH WHAT OTHERS SEE");
      }

      // STAKE: the depth is kept for good: it survives the save as written, and an old save reads null.
      [Test]
      public void GIVEN_a_profile_with_depth_WHEN_round_tripped_as_JSON_THEN_it_is_the_same_and_an_old_one_reads_null()
      {
         NpcProfile npc = Npc(Depth());
         string json = Newtonsoft.Json.JsonConvert.SerializeObject(npc);
         NpcProfile back = Newtonsoft.Json.JsonConvert.DeserializeObject<NpcProfile>(json);

         back.GeneratedDepth.Should().BeEquivalentTo(Depth());

         string old = Newtonsoft.Json.JsonConvert.SerializeObject(Npc()).Replace(",\"GeneratedDepth\":null", "");
         Newtonsoft.Json.JsonConvert.DeserializeObject<NpcProfile>(old).GeneratedDepth.Should().BeNull();
      }
   }
}
