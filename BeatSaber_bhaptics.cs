using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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
        public static Stopwatch timerLastEffect = new Stopwatch();
        public static Stopwatch timerSameTime = new Stopwatch();
        public static int numberOfEvents = 0;
        public static int defaultTriggerNumber = 4;
        public static int currentTriggerNumber = 4;
        public static List<float> highWeights = new List<float> { };
        public static float weightFactor = 1.0f;
        public static bool reducedWeight = false;
        public static bool ringEffectOff = false;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }

        #region Player effects

        /*
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
        

        [HarmonyPatch(typeof(LevelFailedTextEffect), "ShowEffect", new Type[] {  })]
        public class bhaptics_LevelFailed
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("LevelFailed");
            }
        }
        */

        #endregion

        #region Lighting effects

        [HarmonyPatch(typeof(TrackLaneRingsRotationEffect), "SpawnRingRotationEffect", new Type[] {  })]
        public class bhaptics_RingRotationEffect
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (ringEffectOff) return;
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
                
                // If last effects has been a while, reduce threshold
                if (!timerLastEffect.IsRunning) timerLastEffect.Start();
                if (timerLastEffect.ElapsedMilliseconds >= 2000)
                {
                    if (currentTriggerNumber > 1) currentTriggerNumber -= 1;
                    timerLastEffect.Restart();
                }

                // Count number of effects at the "same time"
                if (timerSameTime.ElapsedMilliseconds <= 100)
                {
                    numberOfEvents += 1;
                    timerSameTime.Restart();
                    //tactsuitVr.LOG("Number of events: " + numberOfEvents.ToString());
                }
                else
                {
                    numberOfEvents = 0;
                    timerSameTime.Restart();
                    //tactsuitVr.LOG("Timer reset");
                }

                // If number of simultaneous events is above threshold, trigger effect
                if (numberOfEvents >= currentTriggerNumber)
                {
                    currentTriggerNumber = defaultTriggerNumber;
                    string effectName = "None";
                    float weight = (float)numberOfEvents / (float)defaultTriggerNumber / weightFactor;
                    tactsuitVr.LOG("Trigger: " + currentTriggerNumber.ToString());
                    tactsuitVr.LOG("Weight: " + weight.ToString());
                    if (weight > 1.0f) effectName = "LightEffect3";
                    if (weight > 1.5f) effectName = "LightEffect2";
                    if (weight > 2.0f) effectName = "LightEffect1";
                    if (weight == 1.0f) effectName = "LightEffect4";
                    if (weight < 1.0) effectName = "LightEffect5";
                    if (weight < 0.7) effectName = "LightEffect6";
                    tactsuitVr.PlaySpecialEffect(effectName);
                    if (weight > 5.0f) highWeights.Add(weight);
                    if (weight < 0.1f) highWeights.Add(weight);
                    if (highWeights.Count >= 4)
                    {
                        weightFactor = highWeights.Average();
                        if (weightFactor < 1.0f)
                        {
                            if ((!reducedWeight) && (defaultTriggerNumber > 2))
                            {
                                defaultTriggerNumber -= 1;
                                tactsuitVr.LOG("Trigger adjusted! " + defaultTriggerNumber.ToString() + " " + weightFactor.ToString());
                            }
                        } else reducedWeight = true;
                        highWeights.Clear();
                    }
                }

                /*
                if (myEffects.ContainsKey(beatmapEventData.type)) effectName = myEffects[beatmapEventData.type];
                if (effectName == "None") return;
                tactsuitVr.PlaySpecialEffect(effectName);
                */
            }
        }

        #endregion

        #region Map analysis

        public static List<BeatmapEventType> getLeastUsedEvents(BeatmapData beatmapData)
        {
            List<BeatmapEventType> myEventTypes = new List<BeatmapEventType>();
            Dictionary<BeatmapEventType, int> allEffects = new Dictionary<BeatmapEventType, int>();
            numberOfEvents = 0;
            foreach (BeatmapEventData data in beatmapData.beatmapEventsData)
            {
                if ((data.type == BeatmapEventType.VoidEvent) | (data.type == BeatmapEventType.BpmChange) | (data.type == BeatmapEventType.Special0) | (data.type == BeatmapEventType.Special1) | (data.type == BeatmapEventType.Special2) | (data.type == BeatmapEventType.Special3))
                    break;
                if (allEffects.ContainsKey(data.type)) allEffects[data.type] += 1;
                else allEffects.Add(data.type, 1);
                numberOfEvents += 1;
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
                //tactsuitVr.LOG("Event: " + minEvent.ToString());
                //tactsuitVr.LOG("Number: " + numberOfEffects.ToString());
            }
            highWeights.Clear();
            weightFactor = 1.0f;
            reducedWeight = false;
            tactsuitVr.LOG("Events: " + numberOfEvents.ToString());
            defaultTriggerNumber = numberOfEvents / 500;
            if (defaultTriggerNumber <= 1) defaultTriggerNumber = 2;
            currentTriggerNumber = defaultTriggerNumber;
            tactsuitVr.LOG("DefaulTrigger: " + defaultTriggerNumber.ToString());
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

        #endregion

    }
}
