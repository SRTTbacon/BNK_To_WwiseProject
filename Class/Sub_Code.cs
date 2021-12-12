
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using WoTB_Voice_Mod_Creater.Wwise_Class;

namespace BNK_To_WwiseProject.Class
{
    public class Sub_Code
    {
        public enum Encode_Mode
        {
            MP3,
            WAV
        }
        public static readonly string Special_Path = Directory.GetCurrentDirectory() + "\\Resources";
        public const string Random_String = "0123456789abcdefghijklmnopqrstuvwxyz";
        public static Random r = new Random();
        public const double Window_Feed_Time = 0.04;
        //ファイル拡張子なしのパスから拡張子付きのファイルパスを取得
        //戻り値:拡張子付きのファイル名
        public static string File_Get_FileName_No_Extension(string File_Path)
        {
            try
            {
                string Dir = Path.GetDirectoryName(File_Path);
                string Name = Path.GetFileName(File_Path);
                string[] files = Directory.GetFiles(Dir, Name + ".*");
                return files.Length > 0 ? files[0] : "";
            }
            catch (Exception e)
            {
                Error_Log_Write(e.Message);
                return "";
            }
        }
        public static void File_Delete_V2(string File_Path)
        {
            try
            {
                File.Delete(File_Path);
            }
            catch
            {
            }
        }
        //ファイルを移動(正確にはコピーして元ファイルを削除)
        public static bool File_Move(string From_File_Path, string To_File_Path, bool IsOverWrite)
        {
            if (!File.Exists(From_File_Path))
                return false;
            if (File.Exists(To_File_Path) && !IsOverWrite)
                return false;
            try
            {
                File.Copy(From_File_Path, To_File_Path, true);
                File.Delete(From_File_Path);
                return true;
            }
            catch
            {
                return false;
            }
        }
        //↑の拡張子を指定しないバージョン
        public static bool File_Move_V2(string From_File_Path, string To_File_Path, bool IsOverWrite)
        {
            string From_Path = File_Get_FileName_No_Extension(From_File_Path);
            string To_Path = To_File_Path + Path.GetExtension(From_Path);
            return File_Move(From_Path, To_Path, IsOverWrite);
        }
        //必要なdllがない場合そのdll名のリストを返す
        public static List<string> DLL_Exists()
        {
            List<string> DLL_List = new List<string>();
            if (!File.Exists(Special_Path + "\\bass.dll"))
                DLL_List.Add("bass.dll");
            if (!File.Exists(Special_Path + "\\bass_fx.dll"))
                DLL_List.Add("bass_fx.dll");
            if (!File.Exists(Special_Path + "\\bassenc.dll"))
                DLL_List.Add("bassenc.dll");
            if (!File.Exists(Special_Path + "\\bassmix.dll"))
                DLL_List.Add("bassmix.dll");
            return DLL_List;
        }
        //音声ファイルを指定した拡張子へエンコード
        public static bool Audio_Encode_To_Other(string From_Audio_File, string To_Audio_File, string Encode_Mode, bool IsFromFileDelete)
        {
            try
            {
                if (!File.Exists(From_Audio_File))
                    return false;
                Encode_Mode = Encode_Mode.Replace(".", "");
                string Encode_Style = "";
                //変換先に合わせて.batファイルを作成
                if (Encode_Mode == "aac")
                    Encode_Style = "-y -vn -strict experimental -c:a aac -b:a 144k";
                else if (Encode_Mode == "flac")
                    Encode_Style = "-y -vn -ac 2 -ar 44100 -acodec flac -f flac";
                else if (Encode_Mode == "mp3")
                    Encode_Style = "-y -vn -ac 2 -ar 44100 -ab 144k -acodec libmp3lame -f mp3";
                else if (Encode_Mode == "ogg")
                    Encode_Style = "-y -vn -ac 2 -ar 44100 -ab 144k -acodec libvorbis -f ogg";
                else if (Encode_Mode == "wav")
                    Encode_Style = "-y -vn -ac 2 -ar 44100 -acodec pcm_s24le -f wav";
                else if (Encode_Mode == "webm")
                    Encode_Style = "-y -vn -f opus -acodec libopus -ab 144k";
                else if (Encode_Mode == "wma")
                    Encode_Style = "-y -vn -ac 2 -ar 44100 -ab 144k -acodec wmav2 -f asf";
                StreamWriter stw = File.CreateText(Special_Path + "/Encode_Mp3/Audio_Encode.bat");
                stw.WriteLine("chcp 65001");
                stw.Write("\"" + Special_Path + "/Encode_Mp3/ffmpeg.exe\" -i \"" + From_Audio_File + "\" " + Encode_Style + " \"" + To_Audio_File + "\"");
                stw.Close();
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = Special_Path + "/Encode_Mp3/Audio_Encode.bat",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process p = Process.Start(processStartInfo);
                p.WaitForExit();
                if (!File.Exists(To_Audio_File))
                    return false;
                if (IsFromFileDelete)
                    File.Delete(From_Audio_File);
                File.Delete(Special_Path + "/Encode_Mp3/Audio_Encode.bat");
                return true;
            }
            catch
            {
                return false;
            }
        }
        //現在の時間を文字列で取得
        //引数:DateTime.Now,間に入れる文字,どの部分から開始するか,どの部分で終了するか(その数字の部分は含まれる)
        //First,End->1 = Year,2 = Month,3 = Date,4 = Hour,5 = Minutes,6 = Seconds
        public static string Get_Time_Now(DateTime dt, string Between, int First, int End)
        {
            if (First > End)
                return "";
            if (First == End)
                return Get_Time_Index(dt, First);
            string Temp = "";
            for (int Number = First; Number <= End; Number++)
            {
                if (Number != End)
                    Temp += Get_Time_Index(dt, Number) + Between;
                else
                    Temp += Get_Time_Index(dt, Number);
            }
            return Temp;
        }
        private static string Get_Time_Index(DateTime dt, int Index)
        {
            if (Index > 0 && Index < 7)
            {
                if (Index == 1)
                    return dt.Year.ToString();
                else if (Index == 2)
                    return dt.Month.ToString();
                else if (Index == 3)
                    return dt.Day.ToString();
                else if (Index == 4)
                    return dt.Hour.ToString();
                else if (Index == 5)
                    return dt.Minute.ToString();
                else if (Index == 6)
                    return dt.Second.ToString();
            }
            return "";
        }
        //エラーをログに記録(改行コードはあってもなくてもよい)
        public static void Error_Log_Write(string Text)
        {
            DateTime dt = DateTime.Now;
            string Time = Get_Time_Now(dt, ".", 1, 6);
            if (Text.EndsWith("\n"))
                File.AppendAllText(Directory.GetCurrentDirectory() + "/Error_Log.txt", Time + ":" + Text);
            else
                File.AppendAllText(Directory.GetCurrentDirectory() + "/Error_Log.txt", Time + ":" + Text + "\n");
        }
        //ファイルを暗号化
        //引数:元ファイルのパス,暗号先のパス,元ファイルを削除するか
        public static bool File_Encrypt(string From_File, string To_File, string Password, bool IsFromFileDelete)
        {
            try
            {
                if (!File.Exists(From_File))
                    return false;
                using (FileStream eifs = new FileStream(From_File, FileMode.Open, FileAccess.Read))
                using (FileStream eofs = new FileStream(To_File, FileMode.Create, FileAccess.Write))
                    FileEncode.Encrypt(eifs, eofs, Password);
                if (IsFromFileDelete)
                    File.Delete(From_File);
                return true;
            }
            catch (Exception e)
            {
                Error_Log_Write(e.Message);
                return false;
            }
        }
        //ファイルを復号化
        //引数:元ファイルのパス,復号先のパス,元ファイルを削除するか
        public static bool File_Decrypt_To_File(string From_File, string To_File, string Password, bool IsFromFileDelete)
        {
            try
            {
                if (!File.Exists(From_File))
                    return false;
                using (FileStream eifs = new FileStream(From_File, FileMode.Open, FileAccess.Read))
                using (FileStream eofs = new FileStream(To_File, FileMode.Create, FileAccess.Write))
                    FileEncode.Decrypt_To_File(eifs, eofs, Password);
                if (IsFromFileDelete)
                    File.Delete(From_File);
                return true;
            }
            catch (Exception e)
            {
                Error_Log_Write(e.Message);
                return false;
            }
        }
        public static StreamReader File_Decrypt_To_Stream(string From_File, string Password)
        {
            try
            {
                StreamReader str = null;
                using (FileStream eifs = new FileStream(From_File, FileMode.Open, FileAccess.Read))
                    str = FileEncode.Decrypt_To_Stream(eifs, Password);
                return str;
            }
            catch (Exception e)
            {
                Error_Log_Write(e.Message);
                return null;
            }
        }
        //フォルダ選択画面の初期フォルダを取得
        public static string Get_OpenDirectory_Path()
        {
            string InDir = "C:\\";
            if (File.Exists(Special_Path + "/Configs/OpenDirectoryPath.dat"))
            {
                try
                {
                    StreamReader str = File_Decrypt_To_Stream(Special_Path + "/Configs/OpenDirectoryPath.dat", "Directory_Save_SRTTbacon");
                    string Read = str.ReadLine();
                    str.Close();
                    if (Directory.Exists(Read))
                        InDir = Read;
                }
                catch
                {
                }
            }
            return InDir;
        }
        //フォルダ選択画面の初期フォルダを更新
        public static bool Set_Directory_Path(string Dir)
        {
            if (!Directory.Exists(Dir))
                return false;
            try
            {
                StreamWriter stw = File.CreateText(Special_Path + "/Configs/OpenDirectoryPath.tmp");
                stw.Write(Dir);
                stw.Close();
                _ = File_Encrypt(Special_Path + "/Configs/OpenDirectoryPath.tmp", Special_Path + "/Configs/OpenDirectoryPath.dat", "Directory_Save_SRTTbacon", true);
                return true;
            }
            catch
            {
                return false;
            }
        }
        //指定したフォルダにアクセスできるか
        public static bool CanDirectoryAccess(string Dir_Path)
        {
            try
            {
                WindowsPrincipal principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                DirectorySecurity security = Directory.GetAccessControl(Dir_Path);
                AuthorizationRuleCollection authRules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
                foreach (FileSystemAccessRule accessRule in authRules)
                    if (principal.IsInRole(accessRule.IdentityReference as SecurityIdentifier))
                        if ((FileSystemRights.WriteData & accessRule.FileSystemRights) == FileSystemRights.WriteData)
                            if (accessRule.AccessControlType == AccessControlType.Allow)
                                return true;
                return false;
            }
            catch
            {
                return false;
            }
        }
        //ディレクトリをコピー(サブフォルダを含む)
        public static void Directory_Copy(string From_Dir, string To_Dir)
        {
            if (!Directory.Exists(From_Dir))
                return;
            try
            {
                if (!Directory.Exists(To_Dir))
                    _ = Directory.CreateDirectory(To_Dir);
                DirectoryInfo dir = new DirectoryInfo(From_Dir);
                if (!dir.Exists)
                    return;
                DirectoryInfo[] dirs = dir.GetDirectories();
                _ = Directory.CreateDirectory(To_Dir + "\\" + Path.GetFileName(From_Dir));
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    string tempPath = Path.Combine(To_Dir + "\\" + Path.GetFileName(From_Dir), file.Name);
                    _ = file.CopyTo(tempPath, true);
                }
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(To_Dir + "\\" + Path.GetFileName(From_Dir), subdir.Name);
                    Directory_Copy(subdir.FullName, tempPath);
                }
            }
            catch (Exception e)
            {
                Error_Log_Write(e.Message);
            }
        }
        //ランダムな英数字を生成
        public static string Generate_Random_String(int Min_Length, int Max_Length)
        {
            int Length = r.Next(Min_Length, Max_Length + 1);
            StringBuilder sb = new StringBuilder(Length);
            for (int i = 0; i < Length; i++)
            {
                int pos = r.Next(Random_String.Length);
                char c = Random_String[pos];
                _ = sb.Append(c);
            }
            return sb.ToString();
        }
        //Wwiserを用いて.bnkファイルを解析する
        public static void BNK_Parse_To_XML(string From_BNK_File, string To_XML_File)
        {
            StreamWriter stw = File.CreateText(Special_Path + "/Wwise_Parse/BNK_Parse_Start.bat");
            stw.WriteLine("chcp 65001");
            stw.Write("\"" + Special_Path + "/Wwise_Parse/Python/python.exe\" \"" + Special_Path + "/Wwise_Parse/wwiser.pyz\" -iv \"" + From_BNK_File + "\" -dn \"" +
                To_XML_File.Replace(".xml", "") + "\"");
            stw.Close();
            ProcessStartInfo processStartInfo1 = new ProcessStartInfo
            {
                FileName = Special_Path + "/Wwise_Parse/BNK_Parse_Start.bat",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process p = Process.Start(processStartInfo1);
            p.WaitForExit();
            File.Delete(Special_Path + "/Wwise_Parse/BNK_Parse_Start.bat");
        }
        //.wemファイルを指定した形式に変換
        public static bool WEM_To_File(string From_WEM_File, string To_Audio_File, string Encode_Mode, bool IsFromFileDelete)
        {
            try
            {
                if (!File.Exists(From_WEM_File))
                    return false;
                Process wwToOgg = new Process();
                wwToOgg.StartInfo.FileName = Special_Path + "/Wwise/ww2ogg.exe";
                wwToOgg.StartInfo.WorkingDirectory = Special_Path + "/Wwise";
                wwToOgg.StartInfo.Arguments = "--pcb packed_codebooks_aoTuV_603.bin -o \"" + Special_Path + "\\Wwise\\Temp.ogg\" \"" + From_WEM_File + "\"";
                wwToOgg.StartInfo.CreateNoWindow = true;
                wwToOgg.StartInfo.UseShellExecute = false;
                wwToOgg.StartInfo.RedirectStandardError = true;
                wwToOgg.StartInfo.RedirectStandardOutput = true;
                _ = wwToOgg.Start();
                wwToOgg.WaitForExit();
                //Wwise_Class.WEM_To_OGG.Create_OGG(From_WEM_File, Voice_Set.Special_Path + "\\Wwise\\Temp.ogg");
                if (File.Exists(Special_Path + "\\Wwise\\Temp.ogg"))
                {
                    if (Encode_Mode == "ogg")
                        _ = File_Move(Special_Path + "\\Wwise\\Temp.ogg", To_Audio_File, true);
                    else if (Encode_Mode == "wav")
                    {
                        Un4seen.Bass.Misc.EncoderWAV w = new Un4seen.Bass.Misc.EncoderWAV(0)
                        {
                            InputFile = Special_Path + "\\Wwise\\Temp.ogg",
                            OutputFile = To_Audio_File,
                            WAV_BitsPerSample = 24
                        };
                        _ = w.Start(null, IntPtr.Zero, false);
                        _ = w.Stop();
                        File.Delete(Special_Path + "\\Wwise\\Temp.ogg");
                    }
                    else
                        _ = Audio_Encode_To_Other(Special_Path + "\\Wwise\\Temp.ogg", To_Audio_File, Encode_Mode, true);
                    if (IsFromFileDelete)
                        File.Delete(From_WEM_File);
                    return true;
                }
                else
                {
                    Process WEMToWAV = new Process();
                    WEMToWAV.StartInfo.FileName = Special_Path + "/WEM_To_WAV/WEM_To_WAV.exe";
                    WEMToWAV.StartInfo.WorkingDirectory = Special_Path + "/Wwise";
                    WEMToWAV.StartInfo.Arguments = "-o \"" + Special_Path + "\\Wwise\\Temp.wav\" \"" + From_WEM_File + "\"";
                    WEMToWAV.StartInfo.CreateNoWindow = true;
                    WEMToWAV.StartInfo.UseShellExecute = false;
                    WEMToWAV.StartInfo.RedirectStandardError = true;
                    WEMToWAV.StartInfo.RedirectStandardOutput = true;
                    _ = WEMToWAV.Start();
                    WEMToWAV.WaitForExit();
                    if (File.Exists(Special_Path + "\\Wwise\\Temp.wav"))
                    {
                        _ = Encode_Mode == "wav"
                            ? File_Move(Special_Path + "\\Wwise\\Temp.wav", To_Audio_File, true)
                            : Audio_Encode_To_Other(Special_Path + "\\Wwise\\Temp.wav", To_Audio_File, Encode_Mode, true);
                        if (IsFromFileDelete)
                            File.Delete(From_WEM_File);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        //WEMをOGGファイルへ
        public static bool WEM_To_OGG_WAV(string From_WEM_File, string To_OGG_WAV_File, bool IsFromFileDelete)
        {
            try
            {
                if (!File.Exists(From_WEM_File))
                    return false;
                _ = WEM_To_OGG.Create_OGG(From_WEM_File, Special_Path + "\\Wwise\\Temp.ogg");
                if (File.Exists(Special_Path + "\\Wwise\\Temp.ogg"))
                {
                    _ = File_Move(Special_Path + "\\Wwise\\Temp.ogg", To_OGG_WAV_File + ".ogg", true);
                    if (IsFromFileDelete)
                        File.Delete(From_WEM_File);
                    return true;
                }
                else
                {
                    Process WEMToWAV = new Process();
                    WEMToWAV.StartInfo.FileName = Special_Path + "/WEM_To_WAV/WEM_To_WAV.exe";
                    WEMToWAV.StartInfo.WorkingDirectory = Special_Path + "/Wwise";
                    WEMToWAV.StartInfo.Arguments = "-o \"" + Special_Path + "\\Wwise\\Temp.wav\" \"" + From_WEM_File + "\"";
                    WEMToWAV.StartInfo.CreateNoWindow = true;
                    WEMToWAV.StartInfo.UseShellExecute = false;
                    WEMToWAV.StartInfo.RedirectStandardError = true;
                    WEMToWAV.StartInfo.RedirectStandardOutput = true;
                    _ = WEMToWAV.Start();
                    WEMToWAV.WaitForExit();
                    if (File.Exists(Special_Path + "\\Wwise\\Temp.wav"))
                    {
                        _ = File_Move(Special_Path + "\\Wwise\\Temp.wav", To_OGG_WAV_File + ".wav", true);
                        if (IsFromFileDelete)
                            File.Delete(From_WEM_File);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public static void Volume_Set_Start(string File_Import, Encode_Mode Mode, int Gain = 10)
        {
            if (Mode == Encode_Mode.MP3)
            {
                StreamWriter stw = File.CreateText(Special_Path + "/Encode_Mp3/Volume_Set.bat");
                stw.WriteLine("chcp 65001");
                stw.Write("\"" + Special_Path + "/Encode_Mp3/mp3gain.exe\" -r -c -p -d " + Gain + " " + File_Import);
                stw.Close();
                ProcessStartInfo processStartInfo1 = new ProcessStartInfo
                {
                    FileName = Special_Path + "/Encode_Mp3/Volume_Set.bat",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process p = Process.Start(processStartInfo1);
                p.WaitForExit();
                File.Delete(Special_Path + "/Encode_Mp3/Volume_Set.bat");
            }
            else if (Mode == Encode_Mode.WAV)
                Set_WAV_Gain(File_Import, Gain);
        }
        public static void Set_WAV_Gain(string WAV_File, double Gain)
        {
            if (WAV_File == "")
                return;
            if (Gain <= -20)
                Gain = -19.9;
            else if (Gain >= 12)
                Gain = 11.9;
            int Number = r.Next(0, 10000);
            StreamWriter stw = File.CreateText(Special_Path + "/Other/WAV_Set_Gain_" + Number + ".bat");
            stw.WriteLine("chcp 65001");
            if (WAV_File[0] == '"')
                stw.Write("\"" + Special_Path + "/Other/WaveGain.exe\" -r -y -n -g " + Gain + " " + WAV_File);
            else
                stw.Write("\"" + Special_Path + "/Other/WaveGain.exe\" -r -y -n -g " + Gain + " \"" + WAV_File + "\"");
            stw.Close();
            ProcessStartInfo processStartInfo1 = new ProcessStartInfo
            {
                FileName = Special_Path + "/Other/WAV_Set_Gain_" + Number + ".bat",
                WorkingDirectory = Special_Path + "\\Other",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process p = Process.Start(processStartInfo1);
            p.WaitForExit();
            File.Delete(Special_Path + "/Other/WAV_Set_Gain_" + Number + ".bat");
        }
        public static void Create_WAV(string To_File, double Time)
        {
            int Stream = Un4seen.Bass.Bass.BASS_StreamCreate(44100, 2, Un4seen.Bass.BASSFlag.BASS_STREAM_DECODE, Un4seen.Bass.BASSStreamProc.STREAMPROC_DUMMY);
            Un4seen.Bass.Misc.EncoderWAV l = new Un4seen.Bass.Misc.EncoderWAV(Stream)
            {
                InputFile = null,
                OutputFile = To_File,
                WAV_BitsPerSample = 24
            };
            _ = l.Start(null, IntPtr.Zero, false);
            byte[] encBuffer = new byte[65536];
            while (Un4seen.Bass.Bass.BASS_ChannelIsActive(Stream) == Un4seen.Bass.BASSActive.BASS_ACTIVE_PLAYING)
            {
                int len = Un4seen.Bass.Bass.BASS_ChannelGetData(Stream, encBuffer, encBuffer.Length);
                long Pos_Byte = Un4seen.Bass.Bass.BASS_ChannelGetPosition(Stream, Un4seen.Bass.BASSMode.BASS_POS_BYTES);
                if (len <= 0)
                    break;
                else if (Time <= Un4seen.Bass.Bass.BASS_ChannelBytes2Seconds(Stream, Pos_Byte))
                    break;
            }
            _ = l.Stop();
            _ = Un4seen.Bass.Bass.BASS_StreamFree(Stream);
        }
    }
}
//ウィンドウにフォーカスがないとき、アイコンを光らせる
public class Flash
{
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);
    public const uint FLASHW_STOP = 0;
    public const uint FLASHW_CAPTION = 1;
    public const uint FLASHW_TRAY = 2;
    public const uint FLASHW_ALL = 3;
    public const uint FLASHW_TIMER = 4;
    public const uint FLASHW_TIMERNOFG = 12;
    public static Window Handle = null;
    public static bool IsFlashing = false;
    private struct FLASHWINFO
    {
        public uint cbSize;
        public IntPtr hwnd;
        public uint dwFlags;
        public uint uCount;
        public uint dwTimeout;
    }
    //アイコンを光らせる
    public static void Flash_Start(uint count = uint.MaxValue)
    {
        if (Win2000OrLater && Handle != null && !IsFlashing)
        {
            if (Handle.IsActive) return;
            IsFlashing = true;
            WindowInteropHelper h = new WindowInteropHelper(Handle);
            FLASHWINFO info = new FLASHWINFO
            {
                hwnd = h.Handle,
                dwFlags = FLASHW_ALL | FLASHW_TIMER,
                uCount = count,
                dwTimeout = 0
            };
            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            _ = FlashWindowEx(ref info);
            Flash_Loop();
        }
    }
    //アイコンを戻す
    public static void Flash_Stop()
    {
        if (Win2000OrLater && Handle != null && IsFlashing)
        {
            WindowInteropHelper h = new WindowInteropHelper(Handle);
            FLASHWINFO info = new FLASHWINFO
            {
                hwnd = h.Handle,
                dwFlags = FLASHW_STOP,
                uCount = uint.MaxValue,
                dwTimeout = 0
            };
            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            _ = FlashWindowEx(ref info);
            IsFlashing = false;
        }
    }
    //ウィンドウにフォーカスが与えられたらアイコンを戻す
    private static async void Flash_Loop()
    {
        while (!Handle.IsActive && IsFlashing)
            await Task.Delay(500);
        Flash_Stop();
    }
    //Windows XP以上か判定(まぁ.NET FrameWork4.6はWindows7以上なので必ずtrueを返しますが...)
    private static bool Win2000OrLater => Environment.OSVersion.Version.Major >= 5;
}
public class Voice_Mod_Create
{
    //どの音声かを取得
    //例:"battle_01.mp3"->"battle"
    public static string Get_Voice_Type_V1(string FilePath)
    {
        string NameOnly = Path.GetFileNameWithoutExtension(FilePath);
        return !NameOnly.Contains("_") ? NameOnly : NameOnly.Substring(0, NameOnly.LastIndexOf('_'));
    }
    public static string Get_Voice_Type_V2(string FilePath)
    {
        string NameOnly = Path.GetFileNameWithoutExtension(FilePath);
        return !NameOnly.Contains("_") ? NameOnly : NameOnly.Substring(0, NameOnly.IndexOf('_'));
    }
    //音声の種類のみを抽出
    //種類が被っていたらスキップ
    public static string[] Get_Voice_Type_Only(string[] Voices)
    {
        List<string> Type_List = new List<string>();
        foreach (string Type in Voices)
        {
            string Type_Name = Get_Voice_Type_V1(Type);
            bool IsOK = true;
            for (int Number = 0; Number <= Type_List.Count - 1; Number++)
            {
                if (Type_Name == Type_List[Number])
                {
                    IsOK = false;
                    break;
                }
            }
            if (IsOK)
                Type_List.Add(Type_Name);
        }
        return Type_List.ToArray();
    }
}
//曲の開始位置と終了位置をメモリに保存(終了位置が0の場合は最後まで再生される)
public class Music_Play_Time
{
    public double Start_Time { get; set; }
    public double End_Time { get; set; }
    public Music_Play_Time(double Set_Start_Time, double Set_End_Time)
    {
        Start_Time = Set_Start_Time;
        End_Time = Set_End_Time;
    }
}