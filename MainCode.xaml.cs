using BNK_To_WwiseProject.Class;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using WK.Libraries.BetterFolderBrowserNS;
using WoTB_Voice_Mod_Creater.Wwise_Class;
using WoTB_Voice_Mod_Creater.Wwise_Class.BNK_To_Wwise_Project;

namespace BNK_To_WwiseProject
{
    public partial class MainCode : Window
    {
        private readonly List<string> BNK_File = new List<string>();
        private readonly List<string> PCK_File = new List<string>();
        private readonly List<uint> BNK_Sound_Count = new List<uint>();
        private string Init_File = null;
        private string SoundbankInfo_File = null;
        private bool IsClosing = false;
        private bool IsMessageShowing = false;
        private bool IsBusy = false;
        private bool IsOpenDialog = false;
        public MainCode()
        {
            //必要なdllがなかったら強制終了
            List<string> DLL_Error_List = Sub_Code.DLL_Exists();
            if (DLL_Error_List.Count > 0)
            {
                string DLLs = "";
                foreach (string DLL_None in DLL_Error_List)
                    DLLs += DLL_None + "\n";
                MessageBox.Show("The following files did not exist in the Resources folder.\n" + DLLs + "The software will be killed.");
                Application.Current.Shutdown();
            }
            InitializeComponent();
            Info_List_Clear();
            Name_Generate_C.Visibility = Visibility.Hidden;
            Name_Generate_T.Visibility = Visibility.Hidden;
            Opacity = 0;
            Window_Show();
            Flash.Handle = this;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        private async void Window_Show()
        {
            while (Opacity < 1 && !IsClosing)
            {
                Opacity += Sub_Code.Window_Feed_Time;
                await Task.Delay(1000 / 60);
            }
        }
        private void Info_List_Clear()
        {
            Info_List.Items.Clear();
            _ = Info_List.Items.Add("BNK file Count: 0");
            _ = Info_List.Items.Add("PCK file Count: 0");
            _ = Info_List.Items.Add("Init file: Not Selected");
            _ = Info_List.Items.Add("SoundbanksInfo file: Not Selected");
            _ = Info_List.Items.Add("Sound Count: 0");
        }
        private async void Message_Feed_Out(string Message)
        {
            if (IsMessageShowing)
            {
                IsMessageShowing = false;
                await Task.Delay(1000 / 30);
            }
            Message_T.Text = Message;
            IsMessageShowing = true;
            Message_T.Opacity = 1;
            int Number = 0;
            while (Message_T.Opacity > 0 && IsMessageShowing)
            {
                Number++;
                if (Number >= 120)
                    Message_T.Opacity -= 0.025;
                await Task.Delay(1000 / 60);
            }
            IsMessageShowing = false;
            Message_T.Text = "";
            Message_T.Opacity = 1;
        }
        private async void Back_B_Click(object sender, RoutedEventArgs e)
        {
            if (!IsClosing && !IsBusy)
            {
                IsClosing = true;
                while (Opacity > 0)
                {
                    Opacity -= Sub_Code.Window_Feed_Time;
                    await Task.Delay(1000 / 60);
                }
                IsClosing = false;
                System.Windows.Application.Current.Shutdown();
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.Drawing.Size MaxSize = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Size;
            MaxWidth = MaxSize.Width;
            MaxHeight = MaxSize.Height;
            Left = 0;
            Top = 0;
            Width = MaxWidth;
            Height = MaxHeight;
        }
        private void Open_BNK_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy)
                return;
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Select .bnk files. ",
                Multiselect = true,
                Filter = ".bnk file(*.bnk)|*.bnk"
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    foreach (string FileNames in ofd.FileNames)
                    {
                        string Select_Name = Path.GetFileName(FileNames);
                        bool IsExist = false;
                        foreach (string File_Now in BNK_File)
                        {
                            if (Path.GetFileName(File_Now) == Select_Name)
                            {
                                _ = MessageBox.Show(Select_Name + " is already selected. Please specify a different file.");
                                IsExist = true;
                                break;
                            }
                        }
                        if (IsExist)
                            continue;
                        BNK_Parse p = new BNK_Parse(FileNames);
                        int Count = p.Get_File_Count();
                        if (Count == 0)
                        {
                            _ = MessageBox.Show(Select_Name + " does not contain the sound file.");
                            p.Clear();
                            continue;
                        }
                        p.Clear();
                        BNK_Sound_Count.Add((uint)Count);
                        BNK_File.Add(FileNames);
                        _ = Info_List.Items.Add("Add:" + Select_Name);
                    }
                    Info_List.Items[0] = "BNK file Count: " + BNK_File.Count;
                    uint All_Count = 0;
                    foreach (uint Counts in BNK_Sound_Count)
                        All_Count += Counts;
                    Info_List.Items[4] = "Sound Count: " + All_Count;
                }
                catch (Exception e1)
                {
                    Sub_Code.Error_Log_Write(e1.Message);
                    Message_Feed_Out("Error: The selected file may be corrupted.");
                }
            }
        }
        private void Open_PCK_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy)
                return;
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Select .pck files.",
                Multiselect = true,
                Filter = ".pck file(*.pck)|*.pck"
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    foreach (string FileNames in ofd.FileNames)
                    {
                        string Select_Name = Path.GetFileName(FileNames);
                        foreach (string File_Now in PCK_File)
                        {
                            if (Path.GetFileName(File_Now) == Select_Name)
                            {
                                _ = MessageBox.Show(Select_Name + " is already selected. Please specify a different file.");
                                continue;
                            }
                        }
                        Wwise_File_Extract_V1 p = new Wwise_File_Extract_V1(FileNames);
                        int Count = p.Wwise_Get_File_Count();
                        if (Count == 0)
                        {
                            _ = MessageBox.Show(Select_Name + " does not contain the sound file.");
                            p.Pck_Clear();
                            continue;
                        }
                        p.Pck_Clear();
                        PCK_File.Add(FileNames);
                        _ = Info_List.Items.Add("Add:" + Select_Name);
                    }
                    Info_List.Items[1] = "PCK file Count: " + PCK_File.Count;
                }
                catch (Exception e1)
                {
                    Sub_Code.Error_Log_Write(e1.Message);
                    Message_Feed_Out("Error: The selected file may be corrupted.");
                }
            }
        }
        private void Open_Init_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy)
                return;
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Select a Init.bnk",
                Multiselect = false,
                Filter = "Init.bnk file(Init.bnk)|Init.bnk"
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    BNK_Parse p = new BNK_Parse(ofd.FileName);
                    if (!p.IsInitBNK())
                    {
                        Message_Feed_Out("The specified file is not Init.bnk file.");
                        p.Clear();
                        return;
                    }
                    p.Clear();
                    Info_List.Items[2] = "Init file: Selected";
                    Init_File = ofd.FileName;
                }
                catch (Exception e1)
                {
                    Sub_Code.Error_Log_Write(e1.Message);
                    Message_Feed_Out("Error: The selected file may be corrupted.");
                }
            }
        }
        private void Open_JSON_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy)
                return;
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Select a SoundbanksInfo.json",
                Multiselect = false,
                Filter = "SoundbanksInfo.json file(SoundbanksInfo.json)|SoundbanksInfo.json"
            };
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    List<string> Read_All = new List<string>();
                    Read_All.AddRange(File.ReadAllLines(ofd.FileName));
                    if (!Read_All[1].Contains("\"SoundBanksInfo\": {"))
                    {
                        Message_Feed_Out("The specified file is not SoundbanksInfo.json file.");
                        return;
                    }
                    Read_All.Clear();
                    Info_List.Items[3] = "SoundbanksInfo file: Selected";
                    SoundbankInfo_File = ofd.FileName;
                }
                catch (Exception e1)
                {
                    Sub_Code.Error_Log_Write(e1.Message);
                    Message_Feed_Out("Error: The selected file may be corrupted.");
                }
            }
        }
        private void Clear_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy)
                return;
            if (BNK_File.Count == 0 && PCK_File.Count == 0 && Init_File == "" && SoundbankInfo_File == "")
            {
                Message_Feed_Out("It has already been cleared.");
                return;
            }
            MessageBoxResult result = MessageBox.Show("Do you want to clear the contents?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
            {
                BNK_File.Clear();
                PCK_File.Clear();
                BNK_Sound_Count.Clear();
                Init_File = "";
                SoundbankInfo_File = "";
                Info_List_Clear();
            }
        }
        private async void Create_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy || IsOpenDialog)
                return;
            if (BNK_File.Count == 0)
            {
                Message_Feed_Out("No .bnk files are selected. ");
                return;
            }
            else if (Init_File == "")
            {
                Message_Feed_Out("No Init.bnk files are selected. ");
                return;
            }
            else if (SoundbankInfo_File == "")
            {
                Message_Feed_Out("No SoundbanksInfo.json files are selected.");
                return;
            }
            IsOpenDialog = true;
            BetterFolderBrowser bfb = new BetterFolderBrowser()
            {
                Title = "Select the save destination folder.",
                RootFolder = Sub_Code.Get_OpenDirectory_Path(),
                Multiselect = false,
            };
            if (bfb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _ = Sub_Code.Set_Directory_Path(bfb.SelectedFolder);
                if (!Sub_Code.CanDirectoryAccess(bfb.SelectedFolder))
                {
                    Message_Feed_Out("The selected folder could not be accessed.\nYou need to run the software with administrator privileges.");
                    IsOpenDialog = false;
                    return;
                }
                IsBusy = true;
                Message_T.Text = "Analyzing .bnk files.";
                await Task.Delay(50);
                BNK_To_Wwise_Projects BNK_To_Project = new BNK_To_Wwise_Projects(Init_File, BNK_File, PCK_File, SoundbankInfo_File, No_SoundInfo_C.IsChecked.Value);
                if (No_SoundInfo_C.IsChecked.Value && Name_Generate_C.IsChecked.Value)
                    await BNK_To_Project.ShortID_To_Name(Message_T);
                if (Include_Sound_C.IsChecked.Value)
                    await BNK_To_Project.Create_Project_All(bfb.SelectedFolder, false, Message_T);
                else
                    await BNK_To_Project.Create_Project_All(bfb.SelectedFolder, true, Message_T);
                BNK_To_Project.Clear();
                Flash.Flash_Start();
                Message_Feed_Out("Has completed!!!");
                IsBusy = false;
            }
            IsOpenDialog = false;
        }
        private void Attention_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy)
                return;
            string Message_01 = "・This feature was created with the Steam version of WoTB in mind, so I'm not sure if it will work with the sounds of other games.\n";
            string Message_02 = "・I have only verified some sound files, so some files may not work.\n";
            string Message_03 = "・It is not possible to port all the settings exactly, so you may get some errors when starting Wwise.\n";
            string Message_04 = "・SoundbanksInfo.json can be created without specifying it, but it is not realistic because it takes a lot of time.\n";
            _ = MessageBox.Show(Message_01 + Message_02 + Message_03 + Message_04);
        }
        private void Delete_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsBusy || IsClosing)
                return;
            if (Info_List.SelectedIndex == -1 || !Info_List.SelectedItem.ToString().Contains("Add:"))
            {
                Message_Feed_Out("You need to select the'Add:～'item in the list.");
                return;
            }
            string Select_Name = Info_List.SelectedItem.ToString().Substring(4);
            for (int Number = 0; Number < BNK_File.Count; Number++)
            {
                if (Path.GetFileName(BNK_File[Number]) == Select_Name)
                {
                    BNK_File.RemoveAt(Number);
                    BNK_Sound_Count.RemoveAt(Number);
                    break;
                }
            }
            for (int Number = 0; Number < PCK_File.Count; Number++)
            {
                if (Path.GetFileName(PCK_File[Number]) == Select_Name)
                {
                    PCK_File.RemoveAt(Number);
                    break;
                }
            }
            Info_List.Items.RemoveAt(Info_List.SelectedIndex);
            Info_List.Items[0] = "BNK file count: " + BNK_File.Count;
            uint All_Count = 0;
            foreach (uint Counts in BNK_Sound_Count)
                All_Count += Counts;
            Info_List.Items[4] = "Sound Count: " + All_Count;
        }
        private void No_SoundInfo_C_Click(object sender, RoutedEventArgs e)
        {
            if (No_SoundInfo_C.IsChecked.Value)
            {
                MessageBoxResult result = MessageBox.Show("If you enable this setting, you will not be able to build on the Wwise side. Do you want to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.Yes);
                if (result == MessageBoxResult.No)
                    No_SoundInfo_C.IsChecked = false;
                else
                {
                    Name_Generate_C.Visibility = Visibility.Visible;
                    Name_Generate_T.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Name_Generate_C.Visibility = Visibility.Hidden;
                Name_Generate_T.Visibility = Visibility.Hidden;
            }
        }
        private void Name_Generate_C_Click(object sender, RoutedEventArgs e)
        {
            if (Name_Generate_C.IsChecked.Value)
            {
                MessageBoxResult result = MessageBox.Show("After building on the Wwise side, even if it is adapted to WoT, it will be played normally.\n" +
                    "It takes time because the event IDs need to match. Do you want to continue?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.Yes);
                if (result == MessageBoxResult.No)
                    Name_Generate_C.IsChecked = false;
            }
        }
        private async void Change_Name_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing || IsBusy)
                return;
            System.Windows.Forms.OpenFileDialog ofd1 = new System.Windows.Forms.OpenFileDialog()
            {
                Title = "Select the porting source Actor-Mixer Hierarchy.",
                Multiselect = false,
                Filter = "Actor-Mixer Hierarchy(*.wwu)|*.wwu"
            };
            if (ofd1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                System.Windows.Forms.OpenFileDialog ofd2 = new System.Windows.Forms.OpenFileDialog()
                {
                    Title = "Select the Actor-Mixer Hierarchy to port to. ",
                    Multiselect = false,
                    Filter = "Actor-Mixer Hierarchy(*.wwu)|*.wwu"
                };
                if (ofd2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (ofd1.FileName == ofd2.FileName)
                        _ = MessageBox.Show("The source and destination files are the same.");
                    else
                    {
                        Message_T.Text = "Porting the name. Please wait...";
                        await Task.Delay(50);
                        Wwise_Project_Change_Name(ofd1.FileName, ofd2.FileName);
                        Message_Feed_Out("The name porting is complete.");
                    }
                }
                ofd2.Dispose();
            }
            ofd1.Dispose();
        }
        private void Change_Name_Help_B_Click(object sender, RoutedEventArgs e)
        {
            if (IsClosing)
                return;
            string Message_01 = "・First select the source Actor-Mixer Hierarchy file, and secondly select the destination Actor-Mixer Hierarchy file.\n";
            string Message_02 = "・Only containers with the same ShortID will be ported, so it won't work if the project is different.\n";
            string Message_03 = "・It is difficult to explain, so we recommend that you do not use it unless you are familiar with it.";
            _ = MessageBox.Show(Message_01 + Message_02 + Message_03);
        }
        private void Wwise_Project_Change_Name(string From_File, string To_File)
        {
            List<string> ShortIDs = new List<string>();
            List<string> Names = new List<string>();
            string[] From_Actor = File.ReadAllLines(From_File);
            foreach (string Line in From_Actor)
            {
                if (Line.Contains("Name=\"") && Line.Contains("ShortID=\""))
                {
                    ShortIDs.Add(Get_Config.Get_ShortID_Project(Line));
                    Names.Add(Get_Config.Get_Name(Line));
                }
            }
            List<string> To_Actor = new List<string>(File.ReadAllLines(To_File));
            for (int Number = 0; Number < To_Actor.Count; Number++)
            {
                if (To_Actor[Number].Contains("Name=\""))
                {
                    string Name = Get_Config.Get_Name(To_Actor[Number]);
                    int Index = ShortIDs.IndexOf(Name);
                    if (Index == -1)
                        continue;
                    if (To_Actor[Number].Contains("ShortID=\""))
                    {
                        string ShortID_After = To_Actor[Number].Substring(To_Actor[Number].LastIndexOf("ShortID=\""));
                        string ShortID_Before = To_Actor[Number].Substring(0, To_Actor[Number].LastIndexOf("ShortID=\""));
                        ShortID_Before = ShortID_Before.Replace(Name, Names[Index]);
                        To_Actor[Number] = ShortID_Before + ShortID_After;
                    }
                    else
                        To_Actor[Number] = To_Actor[Number].Replace(Name, Names[Index]);
                }
            }
            File.WriteAllLines(To_File, To_Actor);
            To_Actor.Clear();
        }
    }
}