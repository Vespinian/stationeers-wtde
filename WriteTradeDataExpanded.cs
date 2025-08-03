using HarmonyLib;
using StationeersMods.Interface;


namespace WriteTradeDataExpanded
{
    class WriteTradeDataExpanded : ModBehaviour
    {
        public override void OnLoaded(ContentHandler contentHandler)
        {
            Harmony harmony = new Harmony("WriteTradeDataExpanded");
            harmony.PatchAll();
            UnityEngine.Debug.Log("Write Trade Data Expanded Loaded!");
        }
    }
}