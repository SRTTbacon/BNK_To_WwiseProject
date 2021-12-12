using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BNK_To_WwiseProject.Class
{
    public class Multithread
    {
        private static readonly List<string> From_Files = new List<string>();
        public static async Task Convert_Ogg_To_Wav(List<string> From_Dir, bool IsFromFileDelete)
        {
            try
            {
                From_Files.Clear();
                From_Files.AddRange(From_Dir);
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < From_Files.Count; i++)
                    tasks.Add(To_WAV(i, Path.GetDirectoryName(From_Files[i]), IsFromFileDelete));
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Sub_Code.Error_Log_Write(e.Message);
            }
        }
        //マルチスレッドで.mp3や.oggを.wav形式にエンコード
        //拡張子とファイル内容が異なっていた場合実行されない(ファイル拡張子が.mp3なのに実際は.oggだった場合など)
        public static async Task Convert_To_Wav(List<string> Files, List<string> ToFilePath, List<Music_Play_Time> Time = null, bool IsFromFileDelete = false)
        {
            try
            {
                From_Files.Clear();
                From_Files.AddRange(Files);
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < From_Files.Count; i++)
                {
                    if (Time != null)
                        tasks.Add(To_WAV(i, ToFilePath[i], Time[i], IsFromFileDelete));
                    else
                        tasks.Add(To_WAV(i, ToFilePath[i], IsFromFileDelete, true));
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Sub_Code.Error_Log_Write(e.Message);
            }
            From_Files.Clear();
        }
        private static async Task To_WAV(int File_Number, string ToFilePath, Music_Play_Time Time, bool IsFromFileDelete)
        {
            double End = Time.End_Time - Time.Start_Time;
            StreamWriter stw = File.CreateText(Sub_Code.Special_Path + "/Encode_Mp3/Audio_WAV_Encode" + File_Number + ".bat");
            stw.WriteLine("chcp 65001");
            stw.Write("\"" + Sub_Code.Special_Path + "/Encode_Mp3/ffmpeg.exe\" -y -i \"" + From_Files[File_Number] + "\" -vn -ac 2 -ar 44100 -acodec pcm_s24le -f wav -ss " + Time.Start_Time + " -t " + End + " \"" + ToFilePath + "\"");
            stw.Close();
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = Sub_Code.Special_Path + "/Encode_Mp3/Audio_WAV_Encode" + File_Number + ".bat",
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process p = Process.Start(processStartInfo);
            await Task.Run(() =>
            {
                p.WaitForExit();
                if (IsFromFileDelete)
                    File.Delete(From_Files[File_Number]);
                File.Delete(Sub_Code.Special_Path + "/Encode_Mp3/Audio_WAV_Encode" + File_Number + ".bat");
            });
        }
        private static async Task<bool> To_WAV(int File_Number, string To_Dir, bool IsFromFileDelete, bool IsUseBass = false)
        {
            if (!File.Exists(From_Files[File_Number]))
                return false;
            if (IsUseBass)
            {
                string To_Audio_File = To_Dir + "\\" + Path.GetFileNameWithoutExtension(From_Files[File_Number]) + ".wav";
                Un4seen.Bass.Misc.EncoderWAV w = new Un4seen.Bass.Misc.EncoderWAV(0)
                {
                    InputFile = From_Files[File_Number],
                    OutputFile = To_Audio_File,
                    WAV_BitsPerSample = 24
                };
                _ = w.Start(null, IntPtr.Zero, false);
                _ = w.Stop();
                if (IsFromFileDelete)
                    File.Delete(From_Files[File_Number]);
            }
            else
            {
                string Encode_Style = "-y -vn -ac 2 -ar 44100 -acodec pcm_s24le -f wav";
                StreamWriter stw = File.CreateText(Sub_Code.Special_Path + "/Encode_Mp3/Audio_Encode" + File_Number + ".bat");
                stw.WriteLine("chcp 65001");
                stw.Write("\"" + Sub_Code.Special_Path + "/Encode_Mp3/ffmpeg.exe\" -i \"" + From_Files[File_Number] + "\" " + Encode_Style + " \"" + To_Dir + "\\" +
                          Path.GetFileNameWithoutExtension(From_Files[File_Number]) + ".wav\"");
                stw.Close();
                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = Sub_Code.Special_Path + "/Encode_Mp3/Audio_Encode" + File_Number + ".bat",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process p = Process.Start(processStartInfo);
                await Task.Run(() =>
                {
                    p.WaitForExit();
                    if (IsFromFileDelete)
                        File.Delete(From_Files[File_Number]);
                    File.Delete(Sub_Code.Special_Path + "/Encode_Mp3/Audio_Encode" + File_Number + ".bat");
                });
            }
            return true;
        }
    }
}