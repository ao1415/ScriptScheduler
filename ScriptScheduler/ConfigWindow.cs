using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace ScriptScheduler
{
    /// <summary>
    /// Config.xaml の相互作用ロジック
    /// </summary>
    public partial class ConfigWindow : Window
    {
        public ObservableCollection<Schedule> Schedules { get; private set; } = new ObservableCollection<Schedule>();

        public ConfigWindow()
        {
            InitializeComponent();

            Deserializer();

            ScheduleList.DataContext = Schedules;
        }

        public void StartTimer()
        {
            foreach (var item in Schedules)
            {
                item.SetTimer();
            }
        }

        private void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.DefaultDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            dialog.Filters.Add(new CommonFileDialogFilter("PowerShell", "*.ps1"));
            dialog.Filters.Add(new CommonFileDialogFilter("すべてのファイル", "*.*"));

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                FilePath_TextBox.Text = dialog.FileName;
                WorkingDirectory_TextBox.Text = System.IO.Path.GetDirectoryName(dialog.FileName);
            }

        }
        private void DirectoryOpenButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.DefaultDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                WorkingDirectory_TextBox.Text = dialog.FileName;
            }

        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TimeSpan.TryParse(TimeSpan_TextBox.Text, out _))
            {
                _ = MessageBox.Show("インターバルの設定が正しくありません", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                int index = ScheduleList.SelectedIndex;
                if (index >= 0)
                {
                    Schedules[index].ResetTimer();

                    Schedules[index].Name = Name_TextBox.Text;
                    Schedules[index].TimeSpan = TimeSpan.Parse(TimeSpan_TextBox.Text);
                    Schedules[index].File = FilePath_TextBox.Text;
                    Schedules[index].WorkingDirectory = WorkingDirectory_TextBox.Text;
                    if (Enable_RadioButton.IsChecked.HasValue && Enable_RadioButton.IsChecked.Value)
                        Schedules[index].Enable = true;
                    else if (DisEnable_RadioButton.IsChecked.HasValue && DisEnable_RadioButton.IsChecked.Value)
                        Schedules[index].Enable = false;

                    Schedules[index].SetTimer();

                    ScheduleList.Items.Refresh();
                    Serializer();
                    Log.Logger.Info("△スケジュールを更新しました");
                }

            }
        }
        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TimeSpan.TryParse(TimeSpan_TextBox.Text, out _))
            {
                _ = MessageBox.Show("インターバルの設定が正しくありません", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                bool scheduleEnable = false;
                if (Enable_RadioButton.IsChecked.HasValue && Enable_RadioButton.IsChecked.HasValue)
                    scheduleEnable = true;
                else if (DisEnable_RadioButton.IsChecked.HasValue && DisEnable_RadioButton.IsChecked.HasValue)
                    scheduleEnable = false;

                Schedules.Add(new Schedule
                {
                    Name = Name_TextBox.Text,
                    TimeSpan = TimeSpan.Parse(TimeSpan_TextBox.Text),
                    File = FilePath_TextBox.Text,
                    WorkingDirectory = WorkingDirectory_TextBox.Text,
                    Enable = scheduleEnable
                });

                Schedules.Last().SetTimer();

                ScheduleList.Items.Refresh();
                Serializer();
                Log.Logger.Info("△スケジュールを追加しました");
            }
        }
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            int index = ScheduleList.SelectedIndex;
            if (index >= 0)
            {
                MessageBoxResult result = MessageBox.Show("本当に削除しますか？", "確認", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.OK)
                {
                    Schedules[index].ResetTimer();

                    ScheduleList.Items.Refresh();
                    Schedules.RemoveAt(index);
                    Serializer();
                    Log.Logger.Info("△スケジュールを削除しました");
                }
            }
        }

        private void ScriptTest_Click(object sender, RoutedEventArgs e)
        {
            PowerShellExec.Exec(FilePath_TextBox.Text, WorkingDirectory_TextBox.Text);

            _ = MessageBox.Show("実行が完了しました", "結果", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ScheduleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ScheduleList.SelectedIndex;
            if (index >= 0)
            {
                Name_TextBox.Text = Schedules[index].Name;
                TimeSpan_TextBox.Text = Schedules[index].TimeSpan.ToString(@"hh\:mm\:ss");
                FilePath_TextBox.Text = Schedules[index].File;
                WorkingDirectory_TextBox.Text = Schedules[index].WorkingDirectory;
                if (Schedules[index].Enable)
                {
                    Enable_RadioButton.IsChecked = true;
                    DisEnable_RadioButton.IsChecked = false;
                }
                else
                {
                    Enable_RadioButton.IsChecked = false;
                    DisEnable_RadioButton.IsChecked = true;
                }
            }

        }

        private readonly string SaveFilepath = Properties.Settings.Default.DataFilePath;
        private void Serializer()
        {
            Log.Logger.Info("▽スケジュールを保存します");
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(SaveFilepath, false))
            {
                writer.WriteLine(Schedules.Count());

                for (int i = 0; i < Schedules.Count(); i++)
                {
                    writer.WriteLine(Schedules[i].Enable);
                    writer.WriteLine(Schedules[i].Name);
                    writer.WriteLine(Schedules[i].TimeSpan);
                    writer.WriteLine(Schedules[i].File);
                    writer.WriteLine(Schedules[i].WorkingDirectory);
                }
            }
            Log.Logger.Info("△スケジュールを保存しました");

        }
        private void Deserializer()
        {
            Log.Logger.Info("▽スケジュールを読み込みます");
            if (System.IO.File.Exists(SaveFilepath))
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(SaveFilepath))
                {
                    int count = int.Parse(reader.ReadLine());

                    for (int i = 0; i < count; i++)
                    {
                        string enable = reader.ReadLine();
                        string name = reader.ReadLine();
                        string timespan = reader.ReadLine();
                        string file = reader.ReadLine();
                        string working = reader.ReadLine();

                        Schedule schedule = new Schedule();

                        schedule.Enable = bool.Parse(enable);
                        schedule.Name = name;
                        schedule.TimeSpan = TimeSpan.Parse(timespan);
                        schedule.File = file;
                        schedule.WorkingDirectory = working;

                        Schedules.Add(schedule);
                    }
                }
            }
            Log.Logger.Info("△スケジュールを読み込みました");
        }

        public bool ShowFlag { get; private set; } = false;
        public void Window_Show()
        {
            if (!ShowFlag)
            {
                ShowFlag = true;
                Show();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.Logger.Info("コンフィグウィンドウを閉じます");
            e.Cancel = true;
            Hide();
            ShowFlag = false;
        }
    }

    public class Schedule
    {
        public bool Enable { get; set; }
        public string Name { get; set; }
        public TimeSpan TimeSpan { get; set; }
        public string File { get; set; }
        public string WorkingDirectory { get; set; }
        private Timer Timer { get; set; } = null;

        public void SetTimer()
        {
            if (Enable)
            {
                Log.Logger.Info("タイマー：" + Name + " を起動します");
                Timer = new Timer(TimeSpan.TotalMilliseconds);
                Timer.Elapsed += (s, e) =>
                {
                    PowerShellExec.Exec(File, WorkingDirectory);
                };
                Timer.Start();
            }
        }
        public void ResetTimer()
        {
            if (Timer != null)
            {
                Log.Logger.Info("タイマー：" + Name + " を停止します");
                Timer.Stop();
                Timer.Dispose();
                Timer = null;
            }
        }

    }

    public static class PowerShellExec
    {
        public static bool Exec(string file, string workspace)
        {
            try
            {
                using (Runspace rs = RunspaceFactory.CreateRunspace())
                {
                    rs.Open();

                    using (PowerShell ps = PowerShell.Create())
                    {
                        PSCommand pscmd = new PSCommand();
                        pscmd.AddScript("cd " + workspace);
                        pscmd.AddScript(file);

                        ps.Commands = pscmd;
                        ps.Runspace = rs;

                        try
                        {
                            var results = ps.Invoke();

                            if (results.Count() > 0)
                            {
                                Log.Logger.Info("実行スクリプト：" + file);
                                foreach (var res in results)
                                {
                                    Log.Logger.Info("実行結果：" + res);
                                }
                            }
                        }
                        catch (System.Management.Automation.PSSecurityException)
                        {
                            _ = MessageBox.Show("スクリプト実行を許可してください\nSet-ExecutionPolicy RemoteSigned -Scope CurrentUser", "確認", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Logger.Error("PowerShellの実行に失敗しました", e);
                return false;
            }

            return true;
        }
    }

}
