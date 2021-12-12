using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BNKManager
{
    public class WwiseBank : LoLSoundBank
    {
        public List<BankSection> bankSections = new List<BankSection>();
        public WwiseBank(string fileLocation) : base(fileLocation)
        {
            using (BinaryReader br = new BinaryReader(File.Open(fileLocation, FileMode.Open)))
                Read(br);
        }
        public override void Save()
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(fileLocation, FileMode.Create)))
                Write(bw);
        }
        public override void Save(string fileLocation)
        {
            using (BinaryWriter bw = new BinaryWriter(File.Open(fileLocation, FileMode.Create)))
                Write(bw);
        }
        private void Read(BinaryReader br)
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                string sectionName = Encoding.ASCII.GetString(br.ReadBytes(4));
                uint sectionLength = br.ReadUInt32();
                if (sectionName == "HIRC")
                    break;
                switch (sectionName)
                {
                    case "BKHD":
                        bankSections.Add(new BKHDSection(br, sectionLength));
                        break;
                    case "HIRC":
                        bankSections.Add(new HIRCSection(br));
                        break;
                    case "STID":
                        bankSections.Add(new STIDSection(br));
                        break;
                    case "DIDX":
                        bankSections.Add(new DIDXSection(br, sectionLength));
                        break;
                    case "DATA":
                        bankSections.Add(new DATASection(br, sectionLength, (DIDXSection)GetSection("DIDX")));
                        break;
                    default:
                        bankSections.Add(new BankSection(sectionName, br.BaseStream.Position, br.ReadBytes((int)sectionLength)));
                        break;
                }
            }
        }
        public BankSection GetSection(string sectionName)
        {
            return bankSections.Find(x => x.sectionName == sectionName);
        }
        public uint GetID()
        {
            BKHDSection headerSection = (BKHDSection)GetSection("BKHD");
            return headerSection == null ? 0 : headerSection.soundbankId;
        }
        private void Write(BinaryWriter bw)
        {
            foreach (BankSection bnkSection in bankSections)
            {
                bnkSection.dataStartOffset = bw.BaseStream.Position + 8;
                bw.Write(bnkSection.GetBytes());
            }
        }
    }
}