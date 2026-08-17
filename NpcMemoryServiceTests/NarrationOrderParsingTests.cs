// The host used to force every turn into a dialogue-then-narration layout whatever the model
// emitted. The vary-the-shape voice teaching (audit 2026-07-17) lets the model LEAD with its
// narration when the moment calls for it, so the parser now reports the emitted order:
// ParsedResponse.NarrationBeforeDialogue is true exactly when [NARRATION] opens before [DIALOGUE]
// in the raw reply, and the host renders narration first on those turns.

#region

using FluentAssertions;
using NpcMemoryService.Core.Models;
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

      // Adult-prompt audit M8: [NARRATION] was the only channel with no open-tag tolerance. A climactic beat
      // is the longest thing the model writes, so it is the likeliest to hit the token ceiling before it can
      // close the tag, and the entire beat used to evaporate: the scene rendered as nothing at all. The body
      // written so far must reach the player instead.
      [Test]
      public void GIVEN_a_narration_truncated_before_its_closing_tag_WHEN_parsed_THEN_the_body_still_survives()
      {
         const string raw = "[DIALOGUE]\nHold still.\n[/DIALOGUE]\n" +
                            "[NARRATION]\nThe cold bites deeper than the rope, and the lamp gutters as";

         ParsedResponse parsed = _parser.Parse(raw);

         parsed.Narration.Should().Contain("The cold bites deeper than the rope");
         parsed.Dialogue.Should().Be("Hold still.");
      }

      // The other side of the tolerance: a truncated narration must not swallow the blocks that follow it,
      // or a cut-off beat would eat the turn's [ACTION] and [EVENT] along with it.
      [Test]
      public void GIVEN_a_truncated_narration_followed_by_another_block_WHEN_parsed_THEN_it_stops_at_the_boundary()
      {
         const string raw = "[NARRATION]\nShe turns away from the bars\n[EVENT]\ntype: Captivity\nsummary: I left her there.\n[/EVENT]";

         ParsedResponse parsed = _parser.Parse(raw);

         parsed.Narration.Should().Contain("She turns away from the bars");
         parsed.Narration.Should().NotContain("type: Captivity");
      }

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

      // Player report (mimo, 2026-08-16): a model wrote its scene prose as "*NARRATION ...*" in asterisks instead
      // of the taught [NARRATION] block, so the bracket parser missed it and the literal label leaked into the
      // spoken bubble. The stray label must be lifted into the narration channel and removed from the dialogue.
      [Test]
      public void GIVEN_a_stray_asterisk_narration_label_in_the_dialogue_WHEN_parsed_THEN_it_is_lifted_to_narration()
      {
         const string raw = "[DIALOGUE]\nUnderstood?\n*NARRATION The riders stir as she approaches, horses snorting in the cold.*\n[/DIALOGUE]";

         ParsedResponse parsed = _parser.Parse(raw);

         parsed.Dialogue.Should().Be("Understood?");
         parsed.Dialogue.Should().NotContain("NARRATION");
         parsed.Narration.Should().Contain("The riders stir as she approaches");
      }

      // The recovery keys on the ALL-CAPS label only: a lowercase "narration" mentioned inside a real line is
      // ordinary dialogue and must be left exactly as written, never stripped or lifted.
      [Test]
      public void GIVEN_a_lowercase_narration_word_in_the_dialogue_WHEN_parsed_THEN_it_is_untouched()
      {
         const string raw = "[DIALOGUE]\nSpare me the narration and speak plainly.\n[/DIALOGUE]";

         ParsedResponse parsed = _parser.Parse(raw);

         parsed.Dialogue.Should().Be("Spare me the narration and speak plainly.");
         parsed.Narration.Should().BeNull();
      }
   }
}
