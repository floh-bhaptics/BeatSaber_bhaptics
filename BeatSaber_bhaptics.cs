using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MelonLoader;
using HarmonyLib;
using MyBhapticsTactsuit;

namespace BeatSaber_bhaptics
{
    public class BeatSaber_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;
        public static Dictionary<BeatmapEventType, string> myEffects = new Dictionary<BeatmapEventType, string>();
        public static List<string> myEffectStrings = new List<string> { "LightEffect1", "LightEffect2", "LightEffect3", "LightEffect4", "LightEffect5", "LightEffect6" };


        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        [HarmonyPatch(typeof(MissedNoteEffectSpawner), "HandleNoteWasMissed", new Type[] { typeof(NoteController) })]
        public class bhaptics_NoteMissed
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("MissedNote");
            }
        }

        [HarmonyPatch(typeof(BombExplosionEffect), "SpawnExplosion", new Type[] { typeof(UnityEngine.Vector3) })]
        public class bhaptics_BombExplosion
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionBelly");
            }
        }
        /*
        [HarmonyPatch(typeof(NoteCutter), "Cut", new Type[] { typeof(Saber) })]
        public class bhaptics_NoteCut
        {
            [HarmonyPostfix]
            public static void Postfix(Saber saber)
            {
                bool isRight = false;
                if (saber.name == "RightSaber") isRight = true;
                tactsuitVr.Recoil("Blade", isRight);
                //tactsuitVr.LOG("Hit: " + saber.name);
                //tactsuitVr.PlaybackHaptics("HeartBeat");
            }
        }
        */

        [HarmonyPatch(typeof(LevelFailedTextEffect), "ShowEffect", new Type[] {  })]
        public class bhaptics_LevelFailed
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("LevelFailed");
            }
        }

        [HarmonyPatch(typeof(TrackLaneRingsRotationEffect), "SpawnRingRotationEffect", new Type[] {  })]
        public class bhaptics_RingRotationEffect
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaySpecialEffect("RingRotation");
            }
        }
        /*
        [HarmonyPatch(typeof(ShockwaveEffect), "SpawnShockwave", new Type[] { typeof(UnityEngine.Vector3) })]
        public class bhaptics_ShockWaveEffect
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ShockWave");
            }
        }
        */
        [HarmonyPatch(typeof(EnvironmentSpawnRotation), "BeatmapEventAtNoteSpawnCallback", new Type[] { typeof(BeatmapEventData)})]
        public class bhaptics_LightChangeEffect
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapEventData beatmapEventData)
            {
                if ((beatmapEventData.type == BeatmapEventType.Special0) | (beatmapEventData.type == BeatmapEventType.Special1) | (beatmapEventData.type == BeatmapEventType.Special2) | (beatmapEventData.type == BeatmapEventType.Special3))
                    tactsuitVr.PlaySpecialEffect("ShockWave");
                string effectName = "None";
                if (myEffects.ContainsKey(beatmapEventData.type)) effectName = myEffects[beatmapEventData.type];
                if (effectName == "None") return;
                tactsuitVr.PlaySpecialEffect(effectName);
            }
        }


        public static List<BeatmapEventType> getLeastUsedEvents(BeatmapData beatmapData)
        {
            List<BeatmapEventType> myEventTypes = new List<BeatmapEventType>();
            Dictionary<BeatmapEventType, int> allEffects = new Dictionary<BeatmapEventType, int>();
            foreach (BeatmapEventData data in beatmapData.beatmapEventsData)
            {
                if ((data.type == BeatmapEventType.VoidEvent) | (data.type == BeatmapEventType.BpmChange) | (data.type == BeatmapEventType.Special0) | (data.type == BeatmapEventType.Special1) | (data.type == BeatmapEventType.Special2) | (data.type == BeatmapEventType.Special3))
                    break;
                if (allEffects.ContainsKey(data.type)) allEffects[data.type] += 1;
                else allEffects.Add(data.type, 1);
            }
            int numberOfEffects = 0;
            int numberOfRuns = 0;
            int maxEffects = 300;
            while ((numberOfEffects < maxEffects) && (numberOfRuns < 1000))
            {
                if (allEffects.Count() == 0) break;
                numberOfEffects += allEffects.Values.Min();
                numberOfRuns += 1;
                BeatmapEventType minEvent = allEffects.Aggregate((l, r) => l.Value < r.Value ? l : r).Key;
                myEventTypes.Add(minEvent);
                allEffects.Remove(minEvent);
                tactsuitVr.LOG("Event: " + minEvent.ToString());
                tactsuitVr.LOG("Number: " + numberOfEffects.ToString());
            }
            return myEventTypes;
        }

        public static void mapEventTypesToEffects(List<BeatmapEventType> myEventTypes)
        {
            int numberOfEffects = myEffectStrings.Count();
            int counter = 0;
            myEffects.Clear();
            foreach (BeatmapEventType myType in myEventTypes)
            {
                myEffects.Add(myType, myEffectStrings[counter % numberOfEffects]);
                counter++;
            }
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromBinary", new Type[] { typeof(byte[]), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetBinaryData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                List<BeatmapEventType> myEventTypes = getLeastUsedEvents(__result);
                mapEventTypesToEffects(myEventTypes);
            }
        }

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromJson", new Type[] { typeof(string), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetJsonData
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapData __result)
            {
                List<BeatmapEventType> myEventTypes = getLeastUsedEvents(__result);
                mapEventTypesToEffects(myEventTypes);
            }
        }



    }
}
