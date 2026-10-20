// Code written by Gabriel Mailhot, 20/10/2026.
// When a bond with the player began, and when it last changed. Gabriel (20/10/2026) asked for a Heart section in the
// encyclopedia, told in a personal tone, on the page of every character engaged with the player ("your secret lover since
// spring 1091"). Until then nothing kept the day a romantic status changed, and a rebuilt romantic profile silently
// dropped the secret lover's day and her own lover. If these are wrong, the page dates a bond to the last conversation
// rather than to when it began, or forgets it after an orientation fix.

#region

using System.Linq;
using System.Reflection;
using FluentAssertions;
using NpcMemoryService.Core.Models;
using NpcMemoryService.Core.Services;
using NUnit.Framework;

#endregion

namespace NpcMemoryServiceTests
{
   [TestFixture]
   public sealed class RomanticBondDatesTests
   {
      private static NpcProfile Courted(int reputation = 20)
         => new() {Id = "npc_1", Name = "Vesha", Clan = "clan_test", Faction = "faction_test", ReputationWithPlayer = reputation,
                   Romantic = new RomanticProfile {Status = RomanticStatus.Courting}};

      private static ParsedResponse Event(NotableEventType type, string summary)
         => new() {Dialogue = "", NewEventData = new ParsedEventData(type, summary)};

      // STAKE: "lovers since" is the day they became lovers, and the page dates the bond from it.
      [Test]
      public void GIVEN_a_courtship_WHEN_it_becomes_intimate_on_a_day_THEN_the_bond_is_dated_to_that_day()
      {
         NpcProfile profile = Courted();

         ProfileMutator.Apply(profile, Event(NotableEventType.Intimacy, "They spend the night."), gameDay: 400);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Intimate);
         profile.Romantic.StatusSinceDay.Should().Be(400);
         profile.Romantic.LoversSinceDay.Should().Be(400);
      }

      // STAKE: a later night together changes nothing in the bond; restamping it would date a year-old love to yesterday.
      [Test]
      public void GIVEN_lovers_since_a_day_WHEN_another_night_follows_later_THEN_neither_date_moves()
      {
         NpcProfile profile = Courted();
         ProfileMutator.Apply(profile, Event(NotableEventType.Intimacy, "They spend the night."), gameDay: 400);

         ProfileMutator.Apply(profile, Event(NotableEventType.Intimacy, "Again, by the fire."), gameDay: 520);

         profile.Romantic!.StatusSinceDay.Should().Be(400);
         profile.Romantic.LoversSinceDay.Should().Be(400);
      }

      // STAKE: a bond that soured keeps when it began; the page can say both "lovers since" and "estranged since".
      [Test]
      public void GIVEN_lovers_WHEN_a_quarrel_estranges_them_THEN_the_change_is_dated_and_the_beginning_kept()
      {
         NpcProfile profile = Courted();
         ProfileMutator.Apply(profile, Event(NotableEventType.Intimacy, "They spend the night."), gameDay: 400);

         ProfileMutator.Apply(profile, Event(NotableEventType.Conflict, "A bitter quarrel."), gameDay: 450);

         profile.Romantic!.Status.Should().Be(RomanticStatus.Estranged);
         profile.Romantic.StatusSinceDay.Should().Be(450);
         profile.Romantic.LoversSinceDay.Should().Be(400);
      }

      // STAKE: a marriage or a consort bond made by the game's own actions is dated the same way; setting the same status
      // again (a heal on load) never restamps it.
      [Test]
      public void GIVEN_a_bond_moved_by_the_mod_WHEN_moved_again_to_the_same_status_THEN_its_day_stays()
      {
         var romantic = new RomanticProfile {Status = RomanticStatus.Intimate};

         romantic.MoveTo(RomanticStatus.Committed, 600);
         romantic.MoveTo(RomanticStatus.Committed, 900);

         romantic.StatusSinceDay.Should().Be(600);
         romantic.LoversSinceDay.Should().BeNull("a wedding is not the night they became lovers");
      }

      // STAKE: an orientation fix rebuilds the romantic profile on load; everything the player and she have lived must
      // survive it, or the page and the prompt forget a secret lover, her own lover, or when it all began.
      [Test]
      public void GIVEN_a_lived_bond_WHEN_the_profile_is_rebuilt_THEN_every_lived_fact_is_carried_and_the_orientation_is_not()
      {
         var lived = new RomanticProfile
         {
            Status = RomanticStatus.SecretLover, StatusSinceDay = 3, LoversSinceDay = 2, AttractionToPlayer = 40, IsConsort = true,
            IsSecretLover = true, SecretLoverSinceDay = 3, IsOpenArrangement = true, AcceptsPlayersOtherPartners = true,
            TakenLoverId = "lord_x", LoverIsSecret = true, LoverTrystCount = 5, LoverKnownToPlayer = true, AffairDiscovered = true,
            Orientation = SexualOrientation.Bisexual
         };
         var rebuilt = new RomanticProfile {Orientation = SexualOrientation.Homosexual};

         rebuilt.CarryRelationshipFrom(lived);

         foreach (PropertyInfo p in SettableProperties().Where(p => p.Name != nameof(RomanticProfile.Orientation)))
            p.GetValue(rebuilt).Should().Be(p.GetValue(lived), $"{p.Name} is something lived, and a rebuild must keep it");
         rebuilt.Orientation.Should().Be(SexualOrientation.Homosexual);
      }

      // Every property with a plain setter: the mutable, lived state. Init-only ones are derived at creation (the SDK's
      // netstandard2.0 IsExternalInit is its own polyfill, hence the match by name).
      private static PropertyInfo[] SettableProperties()
         => typeof(RomanticProfile).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                   .Where(p => p.SetMethod is {IsPublic: true}
                                               && !p.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit"))
                                   .ToArray();
   }
}
