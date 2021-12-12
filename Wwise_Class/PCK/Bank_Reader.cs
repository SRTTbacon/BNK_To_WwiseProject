using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Reader = Zoltu.IO;

namespace WoTB_Voice_Mod_Creater.Wwise_Class
{
    public class BankReader
    {
        private readonly string _bankPath;
        private readonly BinaryReader binaryReader;
        public struct Header
        {
            public uint length, version, id, unknown1, unknown2;
        }
        public struct Hirc
        {
            public struct WwiseObject
            {
                public enum WwiseObjectType
                {
                    Settings = 1, Sound = 2, EventAction = 3, Event = 4, SequenceContainer = 5,
                    SwitchContainer = 6, ActorMixer = 7, AudioBus = 8, BlendContainer = 9, MusicSegment = 10,
                    MusicTrack = 11, MusicSwitchContainer = 12, MusicPlaylistContainer = 13, Attenuation = 14, DialogueEvent = 15,
                    MotionBus = 16, MotionFX = 17, Effect = 18, AuxillaryBus = 20
                }
                public WwiseObjectType type;
                public uint length, id;
                public byte[] otherBytes;
                public struct WwiseSoundObject
                {
                    public uint soundFileID;
                    // More data left, if that matters
                }
                public struct WwiseEventActionData
                {
                    public byte eventActionScope, eventActionType;
                    public uint soundObjectID;
                    // More data left, if that matters
                }
                public struct WwiseEventData
                {
                    public uint eventActionCount;
                    public uint[] eventActions;
                }
                public WwiseSoundObject soundObject;
                public WwiseEventActionData eventActionData;
                public WwiseEventData eventData;
            }
            public uint length, objectCount;
            public List<WwiseObject> objects;
        }
        private Header _header = new Header();
        private Hirc _hirc = new Hirc();
        private readonly Dictionary<Hirc.WwiseObject.WwiseObjectType, int> hircStats = new Dictionary<Hirc.WwiseObject.WwiseObjectType, int>();
        public BankReader(string bankPath, bool isBigEndian)
        {
            _bankPath = bankPath;
            binaryReader = isBigEndian
                ? new Reader.BigEndianBinaryReader(File.Open(_bankPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                : new BinaryReader(File.Open(_bankPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            ParseBank();
        }
        private void ParseBank()
        {
            if (ParseHeader(binaryReader))
            {
                ParseHirc(binaryReader);
                //File.WriteAllText(_bankPath.Replace(".bnk", ".json"), JsonConvert.SerializeObject(_hirc, Formatting.Indented));
                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartObject();
                    writer.WritePropertyName("Objects");
                    writer.WriteStartArray();
                    if (_hirc.objects != null)
                    {
                        foreach (Hirc.WwiseObject hircObject in _hirc.objects)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("Type");
                            writer.WriteValue(hircObject.type.ToString());
                            writer.WritePropertyName("Length");
                            writer.WriteValue(hircObject.length);
                            writer.WritePropertyName("ID");
                            writer.WriteValue("0x" + hircObject.id.ToString("X8"));
                            switch (hircObject.type)
                            {
                                case Hirc.WwiseObject.WwiseObjectType.Sound:
                                    writer.WritePropertyName("soundFileID");
                                    writer.WriteValue("0x" + hircObject.soundObject.soundFileID.ToString("X8"));
                                    break;
                                case Hirc.WwiseObject.WwiseObjectType.EventAction:
                                    writer.WritePropertyName("eventActionScope");
                                    writer.WriteValue(hircObject.eventActionData.eventActionScope);
                                    writer.WritePropertyName("eventActionType");
                                    writer.WriteValue(hircObject.eventActionData.eventActionType);
                                    writer.WritePropertyName("soundObjectID");
                                    writer.WriteValue("0x" + hircObject.eventActionData.soundObjectID.ToString("X8"));
                                    break;
                                case Hirc.WwiseObject.WwiseObjectType.Event:
                                    writer.WritePropertyName("eventActions");
                                    writer.WriteStartArray();
                                    foreach (uint eventActionID in hircObject.eventData.eventActions)
                                        writer.WriteValue("0x" + eventActionID.ToString("X8"));
                                    writer.WriteEndArray();
                                    break;
                            }
                            writer.WritePropertyName("otherBytes");
                            writer.WriteValue(BitConverter.ToString(hircObject.otherBytes));
                            writer.WriteEndObject();
                        }
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                File.WriteAllText(_bankPath.Replace(".bnk", ".json"), sb.ToString());
            }
        }
        private bool ParseHeader(BinaryReader br)
        {
            bool isBank = false;
            string ident = new string(br.ReadChars(4));
            if (ident == @"BKHD")
            {
                isBank = true;
                _header.length = br.ReadUInt32();
                _header.version = br.ReadUInt32();
                _header.id = br.ReadUInt32();
                _header.unknown1 = br.ReadUInt32();
                _header.unknown2 = br.ReadUInt32();
                _ = br.BaseStream.Seek(_header.length + 8, 0);
            }
            return isBank;
        }
        private void ParseHirc(BinaryReader br)
        {
            string ident = new string(br.ReadChars(4));
            if (ident != @"HIRC")
                return;
            _hirc.objects = new List<Hirc.WwiseObject>();
            _hirc.length = br.ReadUInt32();
            _hirc.objectCount = br.ReadUInt32();
            for (int i = 0; i < _hirc.objectCount; i++)
            {
                Hirc.WwiseObject wwiseObject = new Hirc.WwiseObject
                {
                    type = (Hirc.WwiseObject.WwiseObjectType)br.ReadByte(),
                    length = br.ReadUInt32(),
                    id = br.ReadUInt32()
                };
                long otherBytesPos = br.BaseStream.Position;
                wwiseObject.otherBytes = br.ReadBytes((int)wwiseObject.length - 4);
                long tempPos = br.BaseStream.Position;
                switch (wwiseObject.type)
                {
                    case Hirc.WwiseObject.WwiseObjectType.Sound:
                        wwiseObject.soundObject = new Hirc.WwiseObject.WwiseSoundObject();
                        _ = br.BaseStream.Seek(otherBytesPos, SeekOrigin.Begin);
                        _ = br.ReadBytes(5); // 4 unknown + 1 SoundSource
                        wwiseObject.soundObject.soundFileID = br.ReadUInt32();
                        _ = br.BaseStream.Seek(tempPos, SeekOrigin.Begin);
                        break;
                    case Hirc.WwiseObject.WwiseObjectType.EventAction:
                        wwiseObject.eventActionData = new Hirc.WwiseObject.WwiseEventActionData();
                        _ = br.BaseStream.Seek(otherBytesPos, SeekOrigin.Begin);
                        wwiseObject.eventActionData.eventActionScope = br.ReadByte();
                        wwiseObject.eventActionData.eventActionType = br.ReadByte();
                        wwiseObject.eventActionData.soundObjectID = br.ReadUInt32();
                        _ = br.BaseStream.Seek(tempPos, SeekOrigin.Begin);
                        break;
                    case Hirc.WwiseObject.WwiseObjectType.Event:
                        wwiseObject.eventData = new Hirc.WwiseObject.WwiseEventData();
                        _ = br.BaseStream.Seek(otherBytesPos, SeekOrigin.Begin);
                        wwiseObject.eventData.eventActionCount = br.ReadUInt32();
                        wwiseObject.eventData.eventActions = new uint[wwiseObject.eventData.eventActionCount];
                        for (int action = 0; action < wwiseObject.eventData.eventActionCount; action++)
                            wwiseObject.eventData.eventActions[action] = br.ReadUInt32();
                        _ = br.BaseStream.Seek(tempPos, SeekOrigin.Begin);
                        break;
                }
                _hirc.objects.Add(wwiseObject);
                if (hircStats.ContainsKey(wwiseObject.type))
                    hircStats[wwiseObject.type] += 1;
                else
                    hircStats[wwiseObject.type] = 1;
            }
        }
    }
}