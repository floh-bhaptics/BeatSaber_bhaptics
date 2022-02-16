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
        public static Random rnd = new Random();

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
        
        [HarmonyPatch(typeof(CuttableBySaber), "CallWasCutBySaberEvent", new Type[] { typeof(Saber), typeof(UnityEngine.Vector3), typeof(UnityEngine.Quaternion), typeof(UnityEngine.Vector3) })]
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
        
        [HarmonyPatch(typeof(BeatmapObjectExecutionRatingsRecorder), "Update", new Type[] {  })]
        public class bhaptics_EnergyChange
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapObjectExecutionRatingsRecorder __instance)
            {
                BeatmapObjectExecutionRating lastRating = __instance.beatmapObjectExecutionRatings[__instance.beatmapObjectExecutionRatings.Count() - 1];
                if (lastRating.beatmapObjectRatingType == BeatmapObjectExecutionRating.BeatmapObjectExecutionRatingType.Obstacle)
                {
                    if (((ObstacleExecutionRating)lastRating).rating == ObstacleExecutionRating.Rating.NotGood) tactsuitVr.PlaySpecialEffect("HitByWall");
                }
            }
        }
        */

        [HarmonyPatch(typeof(LevelCompletionResultsHelper), "ProcessScore", new Type[] { typeof(PlayerData), typeof(PlayerLevelStatsData), typeof(LevelCompletionResults), typeof(IDifficultyBeatmap), typeof(PlatformLeaderboardsModel)})]
        public class bhaptics_LevelResults
        {
            [HarmonyPostfix]
            public static void Postfix(LevelCompletionResults levelCompletionResults)
            {
                if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared) tactsuitVr.PlaybackHaptics("LevelSuccess");
                if (levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed) tactsuitVr.PlaybackHaptics("LevelFailed");
                resetGlobalParameters();
            }
        }



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
        
        [HarmonyPatch(typeof(EnvironmentSpawnRotation), "BeatmapEventAtNoteSpawnCallback", new Type[] { typeof(BeatmapEventData)})]
        public class bhaptics_LightChangeEffect
        {
            [HarmonyPostfix]
            public static void Postfix(BeatmapEventData beatmapEventData)
            {
                if ((beatmapEventData.type == BeatmapEventType.Special0) | (beatmapEventData.type == BeatmapEventType.Special1) | (beatmapEventData.type == BeatmapEventType.Special2) | (beatmapEventData.type == BeatmapEventType.Special3))
                    tactsuitVr.PlaySpecialEffect(myEffectStrings[rnd.Next(myEffectStrings.Count())]);
                
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
                    effectName = myEffectStrings[rnd.Next(myEffectStrings.Count())];
                    tactsuitVr.PlaySpecialEffect(effectName);
                    if (weight > 5.0f) highWeights.Add(weight);
                    if (weight < 0.24f) highWeights.Add(weight);
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

            }
        }
        
        #endregion

        #region Map analysis

        public static List<BeatmapEventType> getLeastUsedEvents(BeatmapData beatmapData)
        {
            List<BeatmapEventType> myEventTypes = new List<BeatmapEventType>();
            Dictionary<BeatmapEventType, int> allEffects = new Dictionary<BeatmapEventType, int>();
            resetGlobalParameters();
            numberOfEvents = beatmapData.beatmapEventsData.Count();
            ringEffectOff = (beatmapData.spawnRotationEventsCount > 50);
            // tactsuitVr.LOG("Events: " + numberOfEvents.ToString());
            defaultTriggerNumber = numberOfEvents / 500;
            if (defaultTriggerNumber <= 1) defaultTriggerNumber = 2;
            currentTriggerNumber = defaultTriggerNumber;
            // tactsuitVr.LOG("DefaultTrigger: " + defaultTriggerNumber.ToString());
            return myEventTypes;
        }

        public static void resetGlobalParameters()
        {
            highWeights.Clear();
            weightFactor = 1.0f;
            reducedWeight = false;
            defaultTriggerNumber = 4;
            currentTriggerNumber = 4;
            ringEffectOff = false;
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

        [HarmonyPatch(typeof(BeatmapDataLoader), "GetBeatmapDataFromBeatmapSaveData", new Type[] { typeof(List<BeatmapSaveData.NoteData>), typeof(List<BeatmapSaveData.WaypointData>), typeof(List<BeatmapSaveData.ObstacleData>), typeof(List<BeatmapSaveData.EventData>), typeof(BeatmapSaveData.SpecialEventKeywordFiltersData), typeof(float), typeof(float), typeof(float) })]
        public class bhaptics_GetMemoryData
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
