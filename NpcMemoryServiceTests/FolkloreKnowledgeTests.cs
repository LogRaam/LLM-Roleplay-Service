// Code written by Gabriel Mailhot, 03/11/2026.
// What these pin: a character knows the tales of their people AS tales, can tell the one they love best without
// inventing it, and refuses to invent the ones they know only by name.
//
// Why it exists. Gabriel, 03/11/2026: "des contes qui existeraient dans la culture des factions de Calradia... Les NPC
// sauraient que c'est des contes, mais pourraient parfois en faire reference, ou meme raconter le conte si on leur
// demande." If these fail, a character tells a fireside tale as something that happened, two characters of one people
// tell two different stories under one title, or every meeting opens on a tale (kurushimi00's recital, 01/11/2026).

#region

using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluentAssertions;
using NpcMemoryService.Core.Knowledge;
using NpcMemoryService.Core.Models;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class FolkloreKnowledgeTests
   {
      private static readonly FolklorePack Pack = new();

      private static FolkloreFacts Tales() => new() {
         Tales = new List<FolkTaleFacts> {
            new() {
               Title = "The Hound of the Grey Ford",
               Gist = "a hound that guards a ford until its master returns.",
               Beats = new List<string> {"A chief leaves his hound at the ford.", "The hound waits through three winters.", "The chief returns as a ghost."}
            },
            new() {Title = "The Piper Under the Hill", Gist = "a piper who plays for the hollow folk and comes back old."}
         }
      };

      private static string Render(FolkloreFacts facts, bool lean = false)
      {
         var sb = new StringBuilder();
         Pack.Render(sb, new EncounterContext {Folklore = facts, Bearer = new KnowledgeBearer {IsLord = true}}, lean);

         return sb.ToString();
      }

      // STAKE: a tale told as history is a lie by the character; Gabriel's ruling is that they know it is a tale.
      [Test]
      public void GIVEN_tales_of_their_people_WHEN_rendered_THEN_they_are_framed_as_stories_not_history()
      {
         string text = Render(Tales());

         text.Should().Contain("stories, not history");
         text.Should().Contain("never speak of one as something that happened");
      }

      // STAKE: the reason a character can tell a tale when asked; without the parts in order, a model invents it.
      [Test]
      public void GIVEN_a_favourite_tale_WHEN_rendered_THEN_its_parts_are_given_in_order()
      {
         string text = Render(Tales());

         text.Should().Contain("The one you love best, \"The Hound of the Grey Ford\"");
         text.IndexOf("1. A chief leaves his hound").Should().BeLessThan(text.IndexOf("3. The chief returns"));
      }

      // STAKE: "tell me the one about the piper" must not produce an invented story under a shared title.
      [Test]
      public void GIVEN_a_tale_known_only_by_outline_WHEN_rendered_THEN_the_character_is_told_not_to_invent_it()
      {
         string text = Render(Tales());

         text.Should().Contain("\"The Piper Under the Hill\"");
         text.Should().Contain("never invent the rest of its story");
      }

      // STAKE: kurushimi00's recital; a tale at every meeting would be the same fault in a new costume.
      [Test]
      public void GIVEN_tales_WHEN_rendered_THEN_allusion_is_rare()
      {
         Render(Tales()).Should().Contain("Allude to a tale rarely");
      }

      // STAKE: a small model's prompt has no room for every tale; it keeps the favourite and no invented telling.
      [Test]
      public void GIVEN_a_compact_prompt_WHEN_rendered_THEN_only_the_favourite_is_named_and_told_in_outline()
      {
         string text = Render(Tales(), lean: true);

         text.Should().Contain("The Hound of the Grey Ford");
         text.Should().NotContain("The Piper Under the Hill");
         text.Should().Contain("adding no event of your own");
         text.Should().Contain("guards a ford until its master returns. A story");
      }

      // STAKE: a people with no tales on file (a minor culture, another mod's) is a life; nothing is said or invented.
      [Test]
      public void GIVEN_no_tales_on_file_WHEN_checked_THEN_nothing_is_supplied_and_it_is_not_a_fault()
      {
         Pack.IsSupplied(new EncounterContext {Folklore = new FolkloreFacts()}).Should().BeFalse();
         Pack.IsSupplied(new EncounterContext()).Should().BeFalse();
         Pack.RequiresContent.Should().BeFalse();
      }

      // STAKE: a tale must never push out what changes a scene (wanted player, own wounds) in a short prompt.
      [Test]
      public void GIVEN_the_pack_order_WHEN_compact_runs_short_THEN_folklore_is_dropped_first()
      {
         NpcKnowledgeFactory.All.Where(p => p != NpcKnowledgeFactory.All.Single(x => x.Name == "folklore"))
            .Should().OnlyContain(p => p.DropPriority < Pack.DropPriority);
      }
   }
}
