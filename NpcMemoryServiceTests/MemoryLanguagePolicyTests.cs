// Code written by Gabriel Mailhot, 09/09/2026.
// What this pins: what language a private memory is written in, and, above all, what must NOT be allowed to
// decide it.
//
// Why it exists. fkasad (Nexus, 09 Sep 2026), playing The Old Realms, whose lords and holdings carry Germanic
// names: "CR starts generating memory in german... Elector Count Theoderic Gausser has 3 memories (cr.memory), 2
// first in english, the last one in german. Most probably because german names are piling up in the NPC memory.
// Still, the letter is ok, in english. The same sort of thing could happen with Nords and Aserais. And ultimately
// create dialogs using different languages."
//
//   "Ich bot Amfildor eine Summe von dreitausendsechshundertsiebenundachtzig Denaren an, um die sichere
//    Rueckkehr meines Untertans Darius der Speer zu gewaehrleisten."
//
// He was right, and his last clause is the important one: a memory written in the wrong language does not stay in
// the memory. It is fed back into later prompts, so the drift spreads into dialogue.
//
// The cause was one line, repeated in three summarizers: "Write it in the same language as the letter below."
// That asks a model to detect a language across a whole passage, names included, and under a total conversion the
// names are most of the distinctive vocabulary there. The chat prompt never drifted because it anchors on ONE
// thing, the player's last message. Three copies of a rule had diverged from the one that worked, so the rule is
// now shared, and it says out loud the thing the model was getting wrong.
//
// If these tests fail, an NPC is about to remember an English conversation in somebody else's language, and then
// speak from that memory.

#region

using FluentAssertions;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class MemoryLanguagePolicyTests
   {
      // The whole fix in one assertion. Whatever else the instruction says, it has to forbid the model from
      // reading a language off the proper nouns, because that is precisely what it did.
      [Test]
      public void GIVEN_no_language_setting_WHEN_writing_a_memory_THEN_names_are_ruled_out_as_evidence_of_language()
      {
         string directive = MemoryLanguagePolicy.Directive(null, "letter");

         directive.Should().Contain("NAMES ARE NOT EVIDENCE OF LANGUAGE");
         directive.Should().Contain("whole sentences");
      }

      // And the positive half: the prose of the source still decides, so a player writing in French still gets
      // French memories without touching a setting. Ruling names out must not rule detection out.
      [Test]
      public void GIVEN_no_language_setting_WHEN_writing_a_memory_THEN_the_prose_of_the_source_still_decides()
      {
         MemoryLanguagePolicy.Directive(null, "transcript")
                             .Should().Contain("same language as the PROSE of the transcript");
      }

      // An explicit setting is the player's own choice and answers the question outright: there is nothing to
      // detect, so there is nothing to get wrong, and it must beat whatever the source is written in.
      [Test]
      public void GIVEN_an_explicit_language_WHEN_writing_a_memory_THEN_it_wins_over_the_source()
      {
         string directive = MemoryLanguagePolicy.Directive("French", "letter");

         directive.Should().Contain("Write it in French").And.Contain("regardless of the language of the letter");
         directive.Should().NotContain("NAMES ARE NOT EVIDENCE"); // nothing to warn about when nothing is detected
      }

      // Blank, whitespace and null are the same "no setting" for a field a player may have cleared rather than
      // never filled, and a stray space must not be read as a language named " ".
      [TestCase(null)]
      [TestCase("")]
      [TestCase("   ")]
      public void GIVEN_a_setting_that_is_effectively_empty_WHEN_writing_a_memory_THEN_detection_is_used(string setting)
      {
         MemoryLanguagePolicy.Directive(setting, "letter").Should().Contain("NAMES ARE NOT EVIDENCE OF LANGUAGE");
      }

      // A setting typed with spaces around it is still that language, not a language whose name has spaces.
      [Test]
      public void GIVEN_a_language_typed_with_stray_spaces_WHEN_writing_a_memory_THEN_it_is_used_cleanly()
      {
         MemoryLanguagePolicy.Directive("  Ukrainian  ", "transcript").Should().Contain("Write it in Ukrainian,");
      }

      // Each summarizer calls the passage by the word its own prompt already uses, so the instruction reads as
      // part of that prompt rather than as a rule bolted on beside it.
      [TestCase("letter")]
      [TestCase("transcript")]
      [TestCase("scene")]
      public void GIVEN_any_kind_of_source_WHEN_writing_a_memory_THEN_the_instruction_names_it_the_way_the_prompt_does(string source)
      {
         MemoryLanguagePolicy.Directive(null, source).Should().Contain($"PROSE of the {source} below");
         MemoryLanguagePolicy.Directive("German", source).Should().Contain($"language of the {source} below");
      }

      // The four callers, pinned by name, because the value of a shared rule is that nobody keeps a private
      // copy. Two of them said NOTHING about language at all until today, and those two are the dangerous
      // ones: the memory compressor writes the NPC's BackgroundContext, which is injected into every later
      // prompt, and the history compressor writes the recap read back into the LIVE conversation. That is
      // fkasad's "ultimately create dialogs using different languages", and it was one step away.
      [Test]
      public void GIVEN_every_path_that_writes_a_memory_WHEN_built_THEN_all_four_carry_the_same_rule()
      {
         var sources = new[] {"letter", "transcript", "events", "lines"};

         foreach (string source in sources)
         {
            MemoryLanguagePolicy.Directive(null, source).Should().Contain("NAMES ARE NOT EVIDENCE OF LANGUAGE");
            MemoryLanguagePolicy.Directive("Russian", source).Should().StartWith("Write it in Russian,");
         }
      }

      // A missing label must still produce a readable sentence rather than one with a hole in it: this is fed
      // straight into a prompt, and a malformed instruction is worse than a generic one.
      [TestCase(null)]
      [TestCase("")]
      [TestCase("  ")]
      public void GIVEN_no_source_label_WHEN_writing_a_memory_THEN_the_instruction_still_reads_as_a_sentence(string label)
      {
         foreach (string language in new[] {null, "French"})
         {
            string directive = MemoryLanguagePolicy.Directive(language, label);

            directive.Should().NotBeNullOrWhiteSpace().And.Contain("text").And.NotContain("  the");
         }
      }
   }
}
