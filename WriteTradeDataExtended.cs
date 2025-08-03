using HarmonyLib;
using StationeersMods.Interface;


namespace WriteTradeDataExtended
{
    class WriteTradeDataExtended : ModBehaviour
    {
        public override void OnLoaded(ContentHandler contentHandler)
        {
            Harmony harmony = new Harmony("WriteTradeDataExtended");
            harmony.PatchAll();
            UnityEngine.Debug.Log("Write Trade Data Extended Loaded!");
        }
    }
}