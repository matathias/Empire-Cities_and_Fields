using FactionColonies;
using Verse;

namespace FactionColonies.UrbanRural
{
    public static class LogUR
    {
        private const string Slug = "[Empire-UrbanRural]";

        public static void Message(string message)
        {
            if (FCURSettings.PrintDebug)
                Log.Message($"{Slug} {message}");
        }
        public static void MessageForce(string message)
        {
            Log.Message($"{Slug} {message}");
        }

        public static void Warning(string message)
        {
            Log.Warning($"{Slug} {message}");
        }

        public static void Error(string message)
        {
            Log.Error($"{Slug} {message}");
        }
    }
}
