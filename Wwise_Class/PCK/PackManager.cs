using BNK_To_WwiseProject.Class;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Reader = Zoltu.IO;

namespace WoTB_Voice_Mod_Creater.Wwise_Class
{
    public class Wwise_Pack
    {
        public struct Header
        {
            public uint headerSize, folderListSize, bankTableSize, soundTableSize;
        }
        public struct Folder
        {
            public uint offset, id;
            public string name;
        }
        public struct SoundBank
        {
            public uint id, headerOffset, headerSize, hircOffset, hircSize;
            public string relativePath, idString;
        }
        public struct SoundFile
        {
            public uint id, fileOffset, fileSize;
            public string relativePath, idString;
        }
        private Header _header = new Header();
        private Folder[] _folders;
        private Hashtable folderHashTable = new Hashtable();
        private readonly List<SoundBank> soundBankList = new List<SoundBank>();
        private readonly List<SoundFile> soundFileList = new List<SoundFile>();
        private readonly List<string> conversionList = new List<string>();
        private readonly string _packPath;
        private string _extractionPath;
        private BinaryReader binaryReader;
        private readonly string baseName;
        private bool isBigEndian = false;
        private readonly bool convertAfterExtraction;
        public Wwise_Pack(string packPath)
        {
            _packPath = packPath;
            baseName = Path.GetFileNameWithoutExtension(_packPath);
            binaryReader = new BinaryReader(File.Open(_packPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            convertAfterExtraction = true;
            if (ParseHeader(binaryReader))
            {
                GetFolders(binaryReader);
                GatherSoundBanks(binaryReader);
                GatherSoundFiles(binaryReader);
            }
        }
        public void ExtractPack(string To_Dir)
        {
            _extractionPath = To_Dir;
            if (ParseHeader(binaryReader))
            {
                GetFolders(binaryReader);
                GatherSoundBanks(binaryReader);
                GatherSoundFiles(binaryReader);
                Thread asyncExtraction = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    AsyncExtractFiles();
                });
                asyncExtraction.Start();
                Thread asyncConversion = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    AsyncConvertFiles();
                });
                asyncConversion.Start();
                while (asyncExtraction.IsAlive || asyncConversion.IsAlive)
                    Thread.Sleep(1000);
            }
        }
        private bool ParseHeader(BinaryReader br)
        {
            bool isPack = false;

            string ident = new string(br.ReadChars(4));
            if (ident == @"AKPK")
            {
                isPack = true;
                _ = br.BaseStream.Seek(0x8, 0);
                if (br.ReadUInt32() != 1)
                {
                    binaryReader.Close();
                    binaryReader = new Reader.BigEndianBinaryReader(File.Open(_packPath, FileMode.Open, FileAccess.Read, FileShare.Read));
                    br = binaryReader;
                    isBigEndian = true;
                }
                _ = br.BaseStream.Seek(0x4, 0);
                _header.headerSize = br.ReadUInt32();
                _ = br.ReadUInt32();
                _header.folderListSize = br.ReadUInt32();
                _header.bankTableSize = br.ReadUInt32();
                _header.soundTableSize = br.ReadUInt32();
                _ = br.ReadUInt32();
            }
            return isPack;
        }
        private void GetFolders(BinaryReader br)
        {
            uint folderListStartPos = (uint)br.BaseStream.Position;
            uint foldersCount = br.ReadUInt32();
            _folders = new Folder[foldersCount];
            folderHashTable = new Hashtable();
            for (int i = 0; i < foldersCount; i++)
            {
                _folders[i].offset = br.ReadUInt32() + folderListStartPos;
                _folders[i].id = br.ReadUInt32();
                uint folderListTempPos = (uint)br.BaseStream.Position;
                _ = br.BaseStream.Seek(_folders[i].offset, 0);
                StringBuilder sb = new StringBuilder();
                while (br.PeekChar() != (char)0)
                {
                    _ = sb.Append(br.ReadChar());
                    if (!isBigEndian)
                        _ = br.ReadChar();
                }
                _folders[i].name = sb.ToString();
                folderHashTable.Add(_folders[i].id, _folders[i].name);
                _ = br.BaseStream.Seek(folderListTempPos, 0);
            }
            _ = br.BaseStream.Seek(folderListStartPos + _header.folderListSize, 0);
        }
        private void GatherSoundBanks(BinaryReader br)
        {
            soundBankList.Clear();
            uint soundBankCount = br.ReadUInt32();
            for (int i = 0; i < soundBankCount; i++)
            {
                SoundBank soundBank = new SoundBank
                {
                    id = br.ReadUInt32()
                };
                soundBank.idString = "0x" + soundBank.id.ToString("X8");
                uint soundBankOffsetMult = br.ReadUInt32();
                _ = br.ReadUInt32();
                soundBank.headerOffset = br.ReadUInt32() * soundBankOffsetMult;
                uint soundBankFolder = br.ReadUInt32();
                string soundBankFolderName = (string)folderHashTable[soundBankFolder];
                soundBank.relativePath = soundBankFolderName + "\\" + baseName + "\\" + "SoundBank (" + soundBank.idString + ")\\";
                uint soundBankTempPos = (uint)br.BaseStream.Position;
                _ = br.BaseStream.Seek(soundBank.headerOffset, 0);
                _ = br.ReadUInt32();
                soundBank.headerSize = br.ReadUInt32() + 8;
                uint firstBankSectionPos = soundBank.headerOffset + soundBank.headerSize;
                _ = br.BaseStream.Seek(firstBankSectionPos, 0);
                string firstSectionIdent = new string(br.ReadChars(4));
                if (firstSectionIdent == @"DIDX")
                {
                    uint didxSize = br.ReadUInt32();
                    uint didxFilesCount = didxSize / 12;
                    uint dataFilesOffset = firstBankSectionPos + didxSize + 16;
                    for (int j = 0; j < didxFilesCount; j++)
                    {
                        SoundFile soundFile = new SoundFile
                        {
                            id = br.ReadUInt32()
                        };
                        soundFile.idString = "0x" + soundFile.id.ToString("X8");
                        soundFile.fileOffset = br.ReadUInt32() + dataFilesOffset;
                        soundFile.fileSize = br.ReadUInt32();
                        soundFile.relativePath = soundBank.relativePath;
                        soundFileList.Add(soundFile);
                    }
                    _ = br.BaseStream.Seek(firstBankSectionPos + didxSize + 12, 0);
                    uint dataSectionSize = br.ReadUInt32();
                    _ = br.ReadBytes((int)dataSectionSize + 4);
                }
                soundBank.hircOffset = (uint)br.BaseStream.Position - 4;
                soundBank.hircSize = br.ReadUInt32() + 8;
                _ = br.BaseStream.Seek(soundBankTempPos, 0);
                soundBankList.Add(soundBank);
            }
        }
        private void GatherSoundFiles(BinaryReader br)
        {
            soundFileList.Clear();
            uint soundFileCount = br.ReadUInt32();
            for (int i = 0; i < soundFileCount; i++)
            {
                SoundFile soundFile = new SoundFile
                {
                    id = br.ReadUInt32()
                };
                soundFile.idString = "0x" + soundFile.id.ToString("X8");
                uint soundFileOffsetMult = br.ReadUInt32();
                soundFile.fileSize = br.ReadUInt32();
                soundFile.fileOffset = br.ReadUInt32() * soundFileOffsetMult;
                uint soundFileFolder = br.ReadUInt32();
                string soundFileFolderName = (string)folderHashTable[soundFileFolder];
                soundFile.relativePath = soundFileFolderName + "\\" + baseName + "\\";
                soundFileList.Add(soundFile);
            }
        }
        private List<SoundFile> GetSoundFiles(BinaryReader br)
        {
            List<SoundFile> Temp = new List<SoundFile>();
            uint soundFileCount = br.ReadUInt32();
            for (int i = 0; i < soundFileCount; i++)
            {
                SoundFile soundFile = new SoundFile
                {
                    id = br.ReadUInt32()
                };
                soundFile.idString = "0x" + soundFile.id.ToString("X8");
                uint soundFileOffsetMult = br.ReadUInt32();
                soundFile.fileSize = br.ReadUInt32();
                soundFile.fileOffset = br.ReadUInt32() * soundFileOffsetMult;
                uint soundFileFolder = br.ReadUInt32();
                string soundFileFolderName = (string)folderHashTable[soundFileFolder];
                soundFile.relativePath = soundFileFolderName + "\\" + baseName + "\\";
                Temp.Add(soundFile);
            }
            return Temp;
        }
        public List<SoundFile> GetWEMFileList()
        {
            _header = new Header();
            folderHashTable = new Hashtable();
            soundBankList.Clear();
            List<SoundFile> Temp = new List<SoundFile>();
            BinaryReader br2 = new BinaryReader(File.Open(_packPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            if (ParseHeader(br2))
            {
                GetFolders(br2);
                GatherSoundBanks(br2);
                Temp = GetSoundFiles(br2);
            }
            br2.Close();
            return Temp;
        }
        public void ExtractFileIndex(int Index, string To_File)
        {
            BinaryReader asyncBinaryReader = new BinaryReader(File.Open(_packPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            BinaryReader br = asyncBinaryReader;
            SoundFile file = soundFileList[Index];
            string fileExtractionPath = To_File;
            _ = Directory.CreateDirectory(Path.GetDirectoryName(fileExtractionPath));
            using (FileStream writeStream = File.Create(fileExtractionPath))
            {
                _ = br.BaseStream.Seek(file.fileOffset, 0);
                byte[] fileBytes = new byte[file.fileSize];
                fileBytes = br.ReadBytes((int)file.fileSize);
                writeStream.Write(fileBytes, 0, fileBytes.Length);
                writeStream.Close();
            }
            br.Close();
            asyncBinaryReader.Close();
        }
        private void AsyncExtractFiles()
        {
            BinaryReader asyncBinaryReader = new BinaryReader(File.Open(_packPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            BinaryReader br = asyncBinaryReader;
            while ((soundBankList.Count + soundFileList.Count) > 0)
            {
                if (soundBankList.Count > 0)
                {
                    SoundBank bank = soundBankList.First();
                    string bankExtractionPath = _extractionPath + "\\" + bank.relativePath + bank.idString + ".bnk";
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(bankExtractionPath));
                    using (FileStream writeStream = File.Create(bankExtractionPath))
                    {
                        _ = br.BaseStream.Seek(bank.headerOffset, 0);
                        byte[] headerBytes = new byte[bank.headerSize];
                        headerBytes = br.ReadBytes((int)bank.headerSize);
                        _ = br.BaseStream.Seek(bank.hircOffset, 0);
                        byte[] hircBytes = new byte[bank.hircSize];
                        hircBytes = br.ReadBytes((int)bank.hircSize);
                        writeStream.Write(headerBytes, 0, headerBytes.Length);
                        writeStream.Write(hircBytes, 0, hircBytes.Length);
                        writeStream.Close();
                    }
                    _ = new BankReader(bankExtractionPath, isBigEndian);
                    _ = soundBankList.Remove(bank);
                }
                if (soundFileList.Count > 0)
                {
                    SoundFile file = soundFileList.First();
                    string fileExtractionPath = _extractionPath + "\\" + file.relativePath + "SoundFiles\\" + file.idString + ".wem";
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(fileExtractionPath));
                    using (FileStream writeStream = File.Create(fileExtractionPath))
                    {
                        _ = br.BaseStream.Seek(file.fileOffset, 0);
                        byte[] fileBytes = new byte[file.fileSize];
                        fileBytes = br.ReadBytes((int)file.fileSize);
                        writeStream.Write(fileBytes, 0, fileBytes.Length);
                        writeStream.Close();
                    }
                    if (convertAfterExtraction && !isBigEndian)
                        conversionList.Add(fileExtractionPath);
                    _ = soundFileList.Remove(file);
                }
            }
        }
        private void AsyncConvertFiles()
        {
            List<string> stageTwo = new List<string>();
            while ((conversionList.Count + soundBankList.Count + soundFileList.Count) > 0)
            {
                if (conversionList.Count > 0)
                {
                    string path = conversionList.ElementAt(0);
                    if (path == null) continue;
                    Process wwToOgg = new Process();
                    wwToOgg.StartInfo.FileName = Sub_Code.Special_Path + "/Wwise/ww2ogg.exe";
                    wwToOgg.StartInfo.WorkingDirectory = Sub_Code.Special_Path + "/Wwise";
                    wwToOgg.StartInfo.Arguments = "--pcb packed_codebooks_aoTuV_603.bin \"" + path + "\"";
                    wwToOgg.StartInfo.CreateNoWindow = true;
                    wwToOgg.StartInfo.UseShellExecute = false;
                    wwToOgg.StartInfo.RedirectStandardError = true;
                    wwToOgg.StartInfo.RedirectStandardOutput = true;
                    _ = wwToOgg.Start();
                    stageTwo.Add(path);
                    _ = conversionList.Remove(path);
                }
            }
            Thread.Sleep(1000);
            while (stageTwo.Count > 0)
            {
                string path = stageTwo.ElementAt(0);
                if (path == null) continue;
                Process revorb = new Process();
                revorb.StartInfo.FileName = Sub_Code.Special_Path + "/Wwise/revorb.exe";
                revorb.StartInfo.WorkingDirectory = Sub_Code.Special_Path + "/Wwise";
                revorb.StartInfo.Arguments = "\"" + path.Replace(".wem", ".ogg") + "\"";
                revorb.StartInfo.CreateNoWindow = true;
                revorb.StartInfo.UseShellExecute = false;
                revorb.StartInfo.RedirectStandardError = true;
                _ = revorb.Start();
                revorb.WaitForExit();
                _ = stageTwo.Remove(path);
            }
        }
        public void PCK_Clear()
        {
            if (binaryReader != null)
            {
                binaryReader.Close();
                binaryReader.Dispose();
                soundBankList.Clear();
                soundFileList.Clear();
            }
        }
    }
}