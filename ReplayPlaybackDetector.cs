using System;
using System.Linq;
using System.Reflection;

namespace PlayFirst
{
    internal static class ReplayPlaybackDetector
    {
        private static readonly ReplayFlag BeatLeader = new ReplayFlag(
            "BeatLeader.Replayer.ReplayerLauncher", "IsStartedAsReplay");
        private static readonly ReplayFlag ScoreSaber = new ReplayFlag(
            "ScoreSaber.Features.Replays.ReplayStateRegistry", "IsPlaybackEnabled");

        internal static bool IsActive => BeatLeader.IsSet() || ScoreSaber.IsSet();

        private sealed class ReplayFlag
        {
            private readonly string typeName;
            private readonly string propertyName;
            private PropertyInfo[] properties;

            internal ReplayFlag(string typeName, string propertyName)
            {
                this.typeName = typeName;
                this.propertyName = propertyName;
            }

            internal bool IsSet()
            {
                if (properties == null)
                {
                    // MultiLeaderboard embeds BeatLeader types in its own assembly.
                    // Check every matching type rather than only the BeatLeader plugin ID.
                    properties = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(assembly => assembly.GetType(typeName, false))
                        .Where(type => type != null)
                        .Select(type => type.GetProperty(propertyName,
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        .Where(property => property != null && property.PropertyType == typeof(bool)
                            && property.GetGetMethod(true) != null && property.GetIndexParameters().Length == 0)
                        .ToArray();
                }

                foreach (PropertyInfo property in properties)
                {
                    try
                    {
                        if ((bool)property.GetValue(null, null))
                            return true;
                    }
                    catch (Exception exception)
                    {
                        Logger.log.Warn($"Could not read replay state from {property.DeclaringType.Assembly.GetName().Name}: {exception.Message}");
                    }
                }

                return false;
            }
        }
    }
}
