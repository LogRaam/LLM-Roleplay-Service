// The host used to force every turn into a dialogue-then-narration layout whatever the model
// emitted. The vary-the-shape voice teaching (audit 2026-07-17) lets the model LEAD with its
// narration when the moment calls for it, so the parser now reports the emitted order:
// ParsedResponse.NarrationBeforeDialogue is true exactly when [NARRATION] opens before [DIALOGUE]
// in the raw reply, and the host renders narration first on those turns.

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class NarrationOrderParsingTests
   {
      private SectionResponseParser _parser = null!;

      [SetUp]
      public void SetUp() => _parser = new SectionResponseParser();

      // A narration-led turn: the flag is raised, and both sections still parse as before.
      [Test]
      public void GIVEN_narration_before_dialogue_WHEN_parsed_THEN_the_flag_is_true()
      {
         const string raw = "[NARRATION]\nThe rope bites her wrists.\n[/NARRATION]\n" +
                            "[DIALOGUE]\nQuiet now.\n[/DIALOGUE]";

         var result = _parser.Parse(raw);

         result.NarrationBeforeDialogue.Should().BeTrue();
         result.Narration.Should().Be("The rope bites her wrists.");
         result.Dialogue.Should().Be("Quiet now.");
      }

      // The usual layout: dialogue first — the flag stays down and the host keeps its old order.
      [Test]
      public void GIVEN_dialogue_before_narration_WHEN_parsed_THEN_the_flag_is_false()
      {
         const string raw = "[DIALOGUE]\nQuiet now.\n[/DIALOGUE]\n" +
                            "[NARRATION]\nThe rope bites her wrists.\n[/NARRATION]";

         _parser.Parse(raw).NarrationBeforeDialogue.Should().BeFalse();
      }

      // With only one of the two tags present the order is moot — the flag stays down so the host
      // never reorders a turn that has nothing to reorder against.
      [Test]
      public void GIVEN_only_one_of_the_two_sections_WHEN_parsed_THEN_the_flag_is_false()
      {
         _parser.Parse("[DIALOGUE]\nQuiet now.\n[/DIALOGUE]").NarrationBeforeDialogue.Should().BeFalse();
         _parser.Parse("[NARRATION]\nThe rope bites her wrists.\n[/NARRATION]").NarrationBeforeDialogue.Should().BeFalse();
      }

      // Tag matching is case-insensitive everywhere else in this parser; the order comparison
      // follows the same tolerance.
      [Test]
      public void GIVEN_lowercase_tags_with_narration_first_WHEN_parsed_THEN_the_flag_is_true()
      {
         const string raw = "[narration]\nThe rope bites her wrists.\n[/narration]\n" +
                            "[dialogue]\nQuiet now.\n[/dialogue]";

         _parser.Parse(raw).NarrationBeforeDialogue.Should().BeTrue();
      }
   }
}
