using HarmonyLib;
using Assets.Scripts.Objects.Motherboards;
using StationeersMods.Interface;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using Assets.Scripts;
using System.Collections.Generic;
using System.Linq;
using System;
using BepInEx.Configuration;

namespace WriteTradeDataExtended
{
    [StationeersMod("WriteTradeDataExtended", "WriteTradeDataExtended", "1.0.0")]
    class WriteTradeDataExtended : ModBehaviour
    {
        public static ConfigEntry<int> opCodeLSB;
        public static ConfigEntry<int> opCodeMSB;

        public override void OnLoaded(ContentHandler contentHandler)
        {
            base.OnLoaded(contentHandler);

            opCodeLSB = Config.Bind("General", "Contact Id LSB Opcode", 19, "Opcode number of 32 LSB of the contact id.");
            opCodeMSB = Config.Bind("General", "Contact Id MSB Opcode", 20, "Opcode number of 32 MSB of the contact id.");

            Harmony harmony = new Harmony("WriteTradeDataExtended");
            harmony.PatchAll();
            UnityEngine.Debug.Log("Write Trade Data Extended Loaded!");
        }

        //[Flags]
        //enum ExcludeShuttleTypeBitFlag
        //{
        //    None = 0,
        //    Small = 1,
        //    SmallGas = 2,
        //    Medium = 4,
        //    MediumGas = 8,
        //    Large = 16,
        //    LargeGas = 32,
        //    MediumPlane = 64,
        //    LargePlane = 128,
        //}

        //[Flags]
        //enum ExcludeTierBitFlag
        //{
        //    None = 0,
        //    Close = 1,
        //    Medium = 2,
        //    Far = 4,
        //}

        //[Flags]
        //enum ExcludeContactedBitFlag
        //{
        //    None = 0,
        //    ExcludeUncontacted = 1,
        //    ExcludeContacted = 2,
        //}

        //[Flags]
        //enum ExcludeRequiredEnvBitFlag
        //{
        //    None = 0,
        //    Human = 1,
        //    Zrilian = 2,
        //}

        //[Flags]
        //enum ContactIdFlag
        //{
        //    None = 0,
        //    LSB = 1,
        //    HSB = 2,
        //}

        [HarmonyPatch(typeof(MediumSatelliteDish))]
        [HarmonyPatch("WriteTraderData")]
        public class WriteTraderDataPatch
        {
            static bool Prefix(MediumSatelliteDish __instance, StackAddress memory, LogicStack ____stack)
            {
                byte[] unpacked_bytes = BitConverter.GetBytes(memory.IntegerValue);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(unpacked_bytes);
                }
                int item = unpacked_bytes[1];
                int max_entries_contact_id = unpacked_bytes[2];
                int max_entries_to_write = max_entries_contact_id & 0x3F;
                int contact_id_bit_flag = max_entries_contact_id & 0xC0;
                int exclude_tier_bitflag = unpacked_bytes[3];
                int exclude_shuttle_bitflag = unpacked_bytes[4];
                int exclude_contacted_bitflag = unpacked_bytes[5];
                int exclude_env_required_bitflag = unpacked_bytes[6];

                if (max_entries_to_write == 0)
                {
                    return true;
                }
                else
                {
                    max_entries_to_write -= 1;
                }

                List<ScannedContactData> contacts = (__instance as SatelliteDish).DishScannedContacts.ScannedContactData;
                if (contacts.Count == 0)
                {
                    return false;
                }
                contacts = contacts.OrderBy(c => c.LastScannedDegreeOffset).ToList();
                int num_entries = 0;

                foreach (ScannedContactData contact in contacts)
                {
                    if (num_entries > max_entries_to_write)
                    {
                        break;
                    }
                    if (contact.Contact == null)
                    {
                        continue;
                    }

                    if (exclude_tier_bitflag != 0)
                    {
                        int tier_bit = 1 << ((byte)contact.Contact.Tier);
                        if ((exclude_tier_bitflag & tier_bit) == tier_bit)
                        {
                            continue;
                        }
                    }

                    if (exclude_shuttle_bitflag != 0 && contact.Contact.ShuttleType != 0)
                    {
                        int shuttle_type_bit = 1 << ((byte)contact.Contact.ShuttleType - 1);
                        if ((exclude_shuttle_bitflag & shuttle_type_bit) == shuttle_type_bit)
                        {
                            continue;
                        }
                    }

                    if (((exclude_contacted_bitflag & 1) == 1) && (contact.Contact.Contacted == false))
                    {
                        continue;
                    }
                    if (((exclude_contacted_bitflag & 2) == 2) && contact.Contact.Contacted)
                    {
                        continue;
                    }

                    if (contact.Contact.HasEnvironmentRequirement)
                    {
                        if (((exclude_env_required_bitflag & 1) == 1) && (contact.Contact.RequiredPadEnvironment == CharacterCustomisation.SpeciesClass.Human))
                        {
                            continue;
                        }
                        if (((exclude_env_required_bitflag & 2) == 2) && (contact.Contact.RequiredPadEnvironment == CharacterCustomisation.SpeciesClass.Zrilian))
                        {
                            continue;
                        }
                    }


                    int value = contact.Contact.TradeData.TraderData.IdHash;
                    byte @byte = (byte)contact.Contact.ShuttleType;
                    byte byte2 = (byte)(contact.Contact.Contacted ? 1 : 0);
                    byte byte3 = (byte)contact.Contact.Tier;
                    ushort value2 = (ushort)contact.Contact.WattsToResolve;
                    ushort value3 = (ushort)contact.Contact.Lifetime;
                    long value4 = contact.Contact.ReferenceId;

                    try
                    {
                        ____stack.Poke(ref item, LogicStack.PackInt32(2, value));
                        ____stack.Poke(ref item, LogicStack.PackByteX3(3, @byte, byte3, byte2));
                        ____stack.Poke(ref item, LogicStack.PackUInt16X2(4, value2, value3));
                        if ((contact_id_bit_flag & 0x40) == 0x40)
                        {
                            ____stack.Poke(ref item, (long)((byte)opCodeLSB.Value) | (value4 & 0xFFFFFFFFFFFF) << 8);
                        }
                        if ((contact_id_bit_flag & 0x80) == 0x80)
                        {
                            ____stack.Poke(ref item, (long)((byte)opCodeMSB.Value) | (value4 >> 40) & 0xFFFF00);
                        }
                        num_entries++;
                    }
                    catch
                    {
                        continue;
                    }
                }
                return false;
            }
        }


        [HarmonyPatch(typeof(MediumSatelliteDish))]
        [HarmonyPatch(nameof(MediumSatelliteDish.GetInstructionDescription))]
        public class GetInstructionDescriptionPatch
        {
            static void Postfix(ref string __result, int i)
            {
                if (EnumCollections.TraderInstructions[i] == TraderInstruction.WriteTraderData)
                {
                    __result = LogicStack.FormatInstruction(new LogicStack.InstructionFormat[]
                    {
                    LogicStack.OpCode,
                    new LogicStack.InstructionFormat("Write_Index", typeof(byte)),
                    new LogicStack.InstructionFormat("Contact_ID_And_Max_Entry_Cnt", typeof(byte)),
                    new LogicStack.InstructionFormat("Exclude_Tier_Bitflag", typeof(byte)),
                    new LogicStack.InstructionFormat("Exclude_Shuttle_Type_Bitflag", typeof(byte)),
                    new LogicStack.InstructionFormat("Exclude_Contacted_Bitflag", typeof(byte)),
                    new LogicStack.InstructionFormat("Exclude_Required_Env_Bitflag", typeof(byte)),
                    });
                }
                return;
            }
        }
    }
}
