// Code written by Gabriel Mailhot, 11/10/2026.
// The character prompt's section registry (PromptBuilder.CharacterSections), increment 1 of the Prompt Composer (mod repo
// docs/design/prompt-composer/DESIGN.md, agreed by Gabriel 11/10/2026). Power users and modders will name these sections
// in their layouts and text files, so a key is a contract: renamed or removed, every profile that names it silently loses
// that section. The zones decide what is cached: a per-turn section above the marker breaks the cache on every reply. The
// locks are the ones Gabriel chose ("le plus de liberte"): only where the game breaks or the player's safety is at stake.
// That the registry writes the same prompt as before is GoldenPromptTests' job.

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;
using NpcMemoryService.Core.Prompts;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public class PromptSectionRegistryTests
   {
      private const string UpdateVariable = "CR_UPDATE_SECTION_KEYS";
      private const string RemovalVariable = "CR_ALLOW_SECTION_KEY_REMOVAL";

      private static string KeysFile([CallerFilePath] string here = "")
         => Path.Combine(Path.GetDirectoryName(here)!, "Golden", "section_keys.txt");

      private static IReadOnlyList<PromptSectionSpec> Sections => PromptBuilder.CharacterSections;

      // STAKE: two sections under one key would make a layout that names it move or drop both, and a text file reword
      // whichever the loader met first.
      [Test]
      public void GIVEN_the_registry_WHEN_its_keys_are_listed_THEN_each_is_unique_and_snake_case()
      {
         Sections.Select(s => s.Key).Should().OnlyHaveUniqueItems();
         Sections.Should().OnlyContain(s => Regex.IsMatch(s.Key, "^[a-z][a-z0-9]*(_[a-z0-9]+)*$"));
      }

      // STAKE: the cache is split on the marker. A section filed as Tail but written above it, or the reverse, would make
      // the editor and the layout checks tell a power user the wrong thing about what is cached.
      [Test]
      public void GIVEN_the_stock_order_WHEN_zones_are_read_THEN_every_prefix_section_precedes_the_one_marker_and_every_tail_section_follows_it()
      {
         Sections.Count(s => s.Zone == PromptZone.Marker).Should().Be(1);
         int marker = Sections.ToList().FindIndex(s => s.Zone == PromptZone.Marker);

         Sections.Take(marker).Should().OnlyContain(s => s.Zone == PromptZone.Prefix);
         Sections.Skip(marker + 1).Should().OnlyContain(s => s.Zone == PromptZone.Tail);
         Sections[marker].Key.Should().Be("encounter_context");
      }

      // STAKE: Gabriel's lock list. Sealed: the parser's format contract (reworded, the game stops understanding replies)
      // and the two consent rules (a protection, not a style). Required: the five the game acts on or the player depends
      // on. Anything else locked takes freedom away from power users; anything here unlocked lets a profile break the game.
      [Test]
      public void GIVEN_the_registry_WHEN_locks_are_read_THEN_they_are_exactly_the_agreed_ones()
      {
         Sections.Where(s => s.Lock == PromptSectionLock.Sealed).Select(s => s.Key)
                 .Should().BeEquivalentTo("format_instructions", "intimacy_consent_rules", "narration_frequency");
         Sections.Where(s => s.Lock == PromptSectionLock.Required).Select(s => s.Key)
                 .Should().BeEquivalentTo("format_reminder", "role_anchor", "stay_within_what_you_know", "claims_are_not_proof", "player_action_narration");
      }

      // STAKE: a key that disappears silently drops a section from every profile that names it. Keys are only ever added;
      // removing one is a decision (CR_ALLOW_SECTION_KEY_REMOVAL=1), named in the release notes for modders.
      [Test]
      public void GIVEN_the_shipped_keys_WHEN_the_registry_is_read_THEN_none_has_disappeared()
      {
         List<string> current = Sections.Select(s => s.Key).ToList();
         string path = KeysFile();

         if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
         {
            List<string> known = File.Exists(path) ? File.ReadAllLines(path).Where(l => l.Length > 0).ToList() : new List<string>();
            if (Environment.GetEnvironmentVariable(RemovalVariable) == "1") known = known.Where(current.Contains).ToList();
            known.AddRange(current.Where(k => !known.Contains(k)));
            File.WriteAllLines(path, known);
         }

         File.Exists(path).Should().BeTrue($"write it with {UpdateVariable}=1");
         List<string> shipped = File.ReadAllLines(path).Where(l => l.Length > 0).ToList();

         shipped.Except(current).Should().BeEmpty($"a section key is a contract; removing one needs {RemovalVariable}=1 and a release note");
         current.Except(shipped).Should().BeEmpty($"a new section key must be recorded: {UpdateVariable}=1");
      }
   }
}
