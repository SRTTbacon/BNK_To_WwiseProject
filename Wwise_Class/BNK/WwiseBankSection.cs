using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BNKManager
{
    public class BankSection
    {
        public string sectionName;
        public long dataStartOffset;
        public byte[] sectionData;
        protected byte[] GetSectionBytes(byte[] sectionData)
        {
            byte[] sectionBytes = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mStream))
                {
                    bw.Write(Encoding.ASCII.GetBytes(sectionName));
                    bw.Write((uint)sectionData.Length);
                    bw.Write(sectionData);
                }
                sectionBytes = mStream.ToArray();
            }
            return sectionBytes;
        }
        public virtual byte[] GetBytes()
        {
            return GetSectionBytes(sectionData);
        }
        public BankSection(string sectionName, long dataStartOffset, byte[] sectionData)
        {
            this.dataStartOffset = dataStartOffset;
            this.sectionName = sectionName;
            this.sectionData = sectionData;
        }
    }
    public class BKHDSection : BankSection
    {
        public uint soundbankVersion;
        public uint soundbankId;
        public uint zero1;
        public uint zero2;
        public byte[] unknown;
        public BKHDSection(BinaryReader br, uint length) : base("BKHD", br.BaseStream.Position, null)
        {
            soundbankVersion = br.ReadUInt32();
            soundbankId = br.ReadUInt32();
            zero1 = br.ReadUInt32();
            zero2 = br.ReadUInt32();
            if (length > 16)
            {
                unknown = br.ReadBytes((int)length - 16);
            }
        }
        public override byte[] GetBytes()
        {
            byte[] sectionData = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mStream))
                {
                    bw.Write(soundbankVersion);
                    bw.Write(soundbankId);
                    bw.Write(zero1);
                    bw.Write(zero2);
                    bw.Write(unknown);
                }
                sectionData = mStream.ToArray();
            }
            return GetSectionBytes(sectionData);
        }
    }
    public class HIRCSection : BankSection
    {
        public List<WwiseObject> objects = new List<WwiseObject>();
        public HIRCSection(BinaryReader br) : base("HIRC", br.BaseStream.Position, null)
        {
            uint objectCount = br.ReadUInt32();
            for (uint i = 0; i < objectCount; i++)
            {
                if (br.BaseStream.Position >= br.BaseStream.Length)
                    break;
                WwiseObjectType objType = (WwiseObjectType)br.ReadByte();
                uint objLength = br.ReadUInt32();
                switch (objType)
                {
                    case WwiseObjectType.Sound_SFX__Sound_Voice:
                        objects.Add(new SoundSFXVoiceWwiseObject(br, objLength));
                        break;
                    case WwiseObjectType.Event_Action:
                        objects.Add(new EventActionWwiseObject(br, objLength));
                        break;
                    //本家WoTのpckファイルを読み込むとクラッシュするため廃止
                    case WwiseObjectType.Event:
                        objects.Add(new EventWwiseObject(br));
                        break;
                    default:
                        //this.objects.Add(new WwiseObject(objType, br.ReadBytes((int)objLength)));
                        break;
                }
            }
        }
        public override byte[] GetBytes()
        {
            byte[] sectionData = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mStream))
                {
                    bw.Write((uint)objects.Count);
                    foreach (WwiseObject obj in objects)
                        bw.Write(obj.GetBytes());
                }
                sectionData = mStream.ToArray();
            }
            return GetSectionBytes(sectionData);
        }
    }
    public class STIDSection : BankSection
    {
        private readonly uint unknown;
        private readonly List<ReferencedSoundbank> refSoundbanks = new List<ReferencedSoundbank>();
        public STIDSection(BinaryReader br) : base("STID", br.BaseStream.Position, null)
        {
            unknown = br.ReadUInt32();
            uint soundbanksCount = br.ReadUInt32();
            for (uint i = 0; i < soundbanksCount; i++)
                refSoundbanks.Add(new ReferencedSoundbank(br));
        }
        public override byte[] GetBytes()
        {
            byte[] sectionData = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mStream))
                {
                    bw.Write(unknown);
                    bw.Write((uint)refSoundbanks.Count);
                    foreach (ReferencedSoundbank refSoundbank in refSoundbanks)
                        refSoundbank.Write(bw);
                }
                sectionData = mStream.ToArray();
            }
            return GetSectionBytes(sectionData);
        }
        public class ReferencedSoundbank
        {
            public uint ID;
            public string name;
            public ReferencedSoundbank(BinaryReader br)
            {
                ID = br.ReadUInt32();
                name = Encoding.ASCII.GetString(br.ReadBytes(br.ReadByte()));
            }
            public void Write(BinaryWriter bw)
            {
                bw.Write(ID);
                bw.Write((byte)name.Length);
                bw.Write(Encoding.ASCII.GetBytes(name));
            }
        }
    }
    public class DIDXSection : BankSection
    {
        public List<EmbeddedWEM> embeddedWEMFiles = new List<EmbeddedWEM>();
        public DIDXSection(BinaryReader br, uint length) : base("DIDX", br.BaseStream.Position, null)
        {
            uint filesCount = length / 12;
            for (uint i = 0; i < filesCount; i++)
            {
                embeddedWEMFiles.Add(new EmbeddedWEM(br));
            }
        }
        public EmbeddedWEM GetEmbeddedWEM(uint fileID)
        {
            return embeddedWEMFiles.Find(x => x.ID == fileID);
        }
        public override byte[] GetBytes()
        {
            byte[] sectionData = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mStream))
                    foreach (EmbeddedWEM embeddedFile in embeddedWEMFiles)
                        embeddedFile.Write(bw);
                sectionData = mStream.ToArray();
            }
            return GetSectionBytes(sectionData);
        }
        public class EmbeddedWEM
        {
            public uint ID;
            public uint offset;
            public uint length;
            public EmbeddedWEM(BinaryReader br)
            {
                ID = br.ReadUInt32();
                offset = br.ReadUInt32();
                length = br.ReadUInt32();
            }
            public void Write(BinaryWriter bw)
            {
                bw.Write(ID);
                bw.Write(offset);
                bw.Write(length);
            }
        }
    }
    public class DATASection : BankSection
    {
        public List<WEMFile> wemFiles = new List<WEMFile>();
        public DIDXSection dataIndex;
        public DATASection(BinaryReader br, uint length, DIDXSection dataIndex) : base("DATA", br.BaseStream.Position, null)
        {
            this.dataIndex = dataIndex;
            long offset = br.BaseStream.Position;
            foreach (DIDXSection.EmbeddedWEM embWEM in dataIndex.embeddedWEMFiles)
                wemFiles.Add(new WEMFile(br, offset, embWEM));
            BinaryReader br1 = br;
            _ = br1.BaseStream.Seek(offset + length, SeekOrigin.Begin);
        }
        public override byte[] GetBytes()
        {
            byte[] sectionBytes = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(mStream))
                {
                    bw.Write(Encoding.ASCII.GetBytes(sectionName));
                    bw.Write(0);
                    foreach (WEMFile embWEM in wemFiles)
                    {
                        _ = bw.Seek((int)embWEM.info.offset + 8, SeekOrigin.Begin);
                        bw.Write(embWEM.data);
                    }
                    uint length = (uint)(bw.BaseStream.Position - 8);
                    _ = bw.BaseStream.Seek(4, SeekOrigin.Begin);
                    bw.Write(length);
                }
                sectionBytes = mStream.ToArray();
            }
            return sectionBytes;
        }
        public class WEMFile
        {
            public DIDXSection.EmbeddedWEM info;
            public byte[] data;
            public WEMFile(BinaryReader br, long DATASectionOffset, DIDXSection.EmbeddedWEM info)
            {
                this.info = info;
                _ = br.BaseStream.Seek(DATASectionOffset + info.offset, SeekOrigin.Begin);
                data = br.ReadBytes((int)info.length);
            }
        }
    }
}