// Code written by Gabriel Mailhot, 17/10/2026.
// The memories a model writes about the player name them in the third person, and a model told only a name guesses
// the sex. Gabriel (17/10/2026), on a companion's memory of his female character Fercread: "the threat he described".

namespace NpcMemoryService.Core.Services
{
   /// <summary>Pure: the one line telling a memory-writing model which pronouns the player takes.</summary>
   public static class PlayerPronounLine
   {
      /// <summary>
      ///   "{who} is a woman: refer to her as she/her." (or a man, he/him); empty when the host does not say, so a
      ///   request without the fact is byte-for-byte what it was.
      /// </summary>
      public static string For(string who, bool? playerIsFemale)
         => playerIsFemale switch
         {
            true => $"{who} is a woman: refer to her as she/her.",
            false => $"{who} is a man: refer to him as he/him.",
            _ => string.Empty
         };
   }
}
