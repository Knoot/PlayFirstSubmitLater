using System;
using System.Reflection;
using System.Reflection.Emit;

namespace PlayFirst
{
    internal static class Logger
    {
        internal static readonly TestLogger log = new TestLogger();
        internal sealed class TestLogger
        {
            public void Warn(string message) { Console.WriteLine(message); }
        }
    }

    internal static class ReplayPlaybackDetectorTests
    {
        private static void Check(bool expected, string scenario)
        {
            if (ReplayPlaybackDetector.IsActive != expected)
                throw new Exception("FAILED: " + scenario);
            Console.WriteLine("PASS: " + scenario);
        }

        private static FieldInfo CreateFlag(string assemblyName, string typeName, string propertyName)
        {
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);
            var module = assembly.DefineDynamicModule(assemblyName);
            var type = module.DefineType(typeName, TypeAttributes.Public);
            var field = type.DefineField("Active", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
            var getter = type.DefineMethod("get_" + propertyName,
                MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
                typeof(bool), Type.EmptyTypes);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, field);
            il.Emit(OpCodes.Ret);
            var property = type.DefineProperty(propertyName, PropertyAttributes.None, typeof(bool), Type.EmptyTypes);
            property.SetGetMethod(getter);
            return type.CreateType().GetField("Active");
        }

        private static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "absent")
            {
                Check(false, "optional replay mods absent");
                return;
            }

            var beatLeader = CreateFlag("BeatLeader", "BeatLeader.Replayer.ReplayerLauncher", "IsStartedAsReplay");
            var multiLeaderboard = CreateFlag("MultiLeaderboard", "BeatLeader.Replayer.ReplayerLauncher", "IsStartedAsReplay");
            var scoreSaber = CreateFlag("ScoreSaber", "ScoreSaber.Features.Replays.ReplayStateRegistry", "IsPlaybackEnabled");
            Check(false, "normal gameplay with all replay mods installed");
            beatLeader.SetValue(null, true);
            Check(true, "BeatLeader replay");
            beatLeader.SetValue(null, false);
            multiLeaderboard.SetValue(null, true);
            Check(true, "MultiLeaderboard replay while standalone BeatLeader is inactive");
            multiLeaderboard.SetValue(null, false);
            scoreSaber.SetValue(null, true);
            Check(true, "ScoreSaber replay");
            scoreSaber.SetValue(null, false);
            Check(false, "normal gameplay after replay ends");
        }
    }
}
