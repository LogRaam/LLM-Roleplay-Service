// Code written by Gabriel Mailhot, 08/10/2026.
// A gesture written twice in one reply. tashmetu (Nexus, 08/10/2026), from his screenshots: Unthery's reply repeated
// its own stage direction inside the quotes, "*A dry, bitter laugh escapes me.*" twice. The model stuttered; the player
// read the same laugh twice. A stage direction written again, word for word, in the same reply is dropped the second
// time. If this breaks, a character visibly repeats a gesture in a single breath.

#region

using FluentAssertions;
using NpcMemoryService.Core.Parsing;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class StageDirectionDedupeTests
   {
      // tashmetu's Unthery: the laugh before the quotes, and again inside them.
      [Test]
      public void GIVEN_a_gesture_repeated_inside_the_quotes_WHEN_cleaned_THEN_it_is_written_once()
      {
         StageDirectionDedupe.Apply("*A dry, bitter laugh escapes me.* \"*A dry, bitter laugh escapes me.* A man's wife's infidelity is a door.\"")
                             .Should().Be("*A dry, bitter laugh escapes me.* \"A man's wife's infidelity is a door.\"");
      }

      // Two different gestures are two beats, kept as written.
      [Test]
      public void GIVEN_two_different_gestures_WHEN_cleaned_THEN_both_stay()
      {
         const string reply = "*She rises.* \"Speak.\" *She sits again.*";

         StageDirectionDedupe.Apply(reply).Should().Be(reply);
      }

      // The same words in another case or spacing are the same gesture.
      [Test]
      public void GIVEN_the_same_gesture_in_another_case_WHEN_cleaned_THEN_the_second_is_dropped()
      {
         StageDirectionDedupe.Apply("*He nods.* \"Yes.\" *he  nods.*").Should().Be("*He nods.* \"Yes.\"");
      }
   }
}
