# GitHub Release Update Checker — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add automatic GitHub release checking for the OATControl desktop app (at startup) and mount firmware (on connect), with a modal update dialog for the desktop and inline notification for firmware.

**Architecture:** Single `UpdateChecker` static class handles both GitHub API queries. A new `DlgUpdateAvailable` dialog handles the desktop update flow (changelog display, download with progress, launch installer). MountVM gets three new bindable properties for the firmware update badge. All network calls are fire-and-forget with 5-second timeouts and silent failure.

**Tech Stack:** .NET Framework 4.7.2, WPF, Newtonsoft.Json (already in project), HttpClient

---

## File Structure

| Action | File | Responsibility |
|--------|------|----------------|
| Create | `OATControl/UpdateChecker.cs` | GitHub API queries, version parsing, comparison |
| Create | `OATControl/DlgUpdateAvailable.xaml` | Desktop update dialog layout |
| Create | `OATControl/DlgUpdateAvailable.xaml.cs` | Dialog code-behind (changelog, download, progress) |
| Modify | `OATControl/OATControl.csproj` | Add new files to project |
| Modify | `OATControl/ViewModels/MountVM.cs` | Add 3 properties + firmware check call |
| Modify | `OATControl/MainWindow.xaml` | Add badge on Mount Settings button |
| Modify | `OATControl/MainWindow.xaml.cs` | Add desktop update check on startup |
| Modify | `OATControl/SettingsDialog.xaml` | Add firmware update link below version |

---

### Task 1: Create UpdateChecker and UpdateCheckResult

**Files:**
- Create: `OATControl/UpdateChecker.cs`

- [ ] **Step 1: Create the UpdateChecker.cs file**

```csharp
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OATControl
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string Changelog { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleasePageUrl { get; set; }

        public static UpdateCheckResult NoUpdate => new UpdateCheckResult();
    }

    public static class UpdateChecker
    {
        private const string DesktopRepoUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Desktop/releases/latest";
        private const string FirmwareRepoUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Firmware/releases/latest";

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler())
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OATControl");
        }

        public static async Task<UpdateCheckResult> CheckForDesktopUpdateAsync()
        {
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var release = await FetchLatestReleaseAsync(DesktopRepoUrl);
                if (release == null)
                    return UpdateCheckResult.NoUpdate;

                var tagVersion = ParseVersionTag(release["tag_name"]?.ToString());
                if (tagVersion == null || currentVersion == null)
                    return UpdateCheckResult.NoUpdate;

                if (tagVersion > currentVersion)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = true,
                        LatestVersion = release["tag_name"].ToString(),
                        CurrentVersion = $"V{currentVersion}",
                        Changelog = release["body"]?.ToString() ?? "",
                        DownloadUrl = release["assets"]?[0]?["browser_download_url"]?.ToString(),
                        ReleasePageUrl = release["html_url"]?.ToString()
                    };
                }

                return UpdateCheckResult.NoUpdate;
            }
            catch
            {
                return UpdateCheckResult.NoUpdate;
            }
        }

        public static async Task<UpdateCheckResult> CheckForFirmwareUpdateAsync(string currentFirmwareVersion)
        {
            try
            {
                var currentVersion = ParseVersionTag(currentFirmwareVersion);
                if (currentVersion == null)
                    return UpdateCheckResult.NoUpdate;

                var release = await FetchLatestReleaseAsync(FirmwareRepoUrl);
                if (release == null)
                    return UpdateCheckResult.NoUpdate;

                var tagVersion = ParseVersionTag(release["tag_name"]?.ToString());
                if (tagVersion == null)
                    return UpdateCheckResult.NoUpdate;

                if (tagVersion > currentVersion)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = true,
                        LatestVersion = release["tag_name"].ToString(),
                        CurrentVersion = currentFirmwareVersion,
                        Changelog = release["body"]?.ToString() ?? "",
                        ReleasePageUrl = release["html_url"]?.ToString()
                    };
                }

                return UpdateCheckResult.NoUpdate;
            }
            catch
            {
                return UpdateCheckResult.NoUpdate;
            }
        }

        private static async Task<JObject> FetchLatestReleaseAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }

        private static Version ParseVersionTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return null;

            // Strip leading 'V' or 'v'
            var versionStr = tag.TrimStart('V', 'v');

            Version result;
            if (Version.TryParse(versionStr, out result))
                return result;

            // Try padding to at least 2 parts (e.g., "1.13" → "1.13.0")
            var parts = versionStr.Split('.');
            if (parts.Length < 2)
                return null;

            return null;
        }
    }
}
```

- [ ] **Step 2: Add the file to OATControl.csproj**

In `OATControl/OATControl.csproj`, add after the existing `<Compile Include="DlgMessageBox.xaml.cs">` block (around line 145):

```xml
    <Compile Include="UpdateChecker.cs" />
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/UpdateChecker.cs OATControl/OATControl.csproj
git commit -m "feat: add UpdateChecker with GitHub release query logic"
```

---

### Task 2: Create DlgUpdateAvailable dialog

**Files:**
- Create: `OATControl/DlgUpdateAvailable.xaml`
- Create: `OATControl/DlgUpdateAvailable.xaml.cs`
- Modify: `OATControl/OATControl.csproj`

- [ ] **Step 1: Create DlgUpdateAvailable.xaml**

This dialog follows the existing ThemedWindow pattern (see `DlgMessageBox.xaml`). Two visual states: info (changelog + buttons) and downloading (progress bar + cancel).

```xml
<controls:ThemedWindow x:Class="OATControl.DlgUpdateAvailable"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:controls="clr-namespace:OATControl.Controls"
        mc:Ignorable="d"
        Title="Update Available" Height="420" Width="500"
        ResizeMode="NoResize"
        controls:ThemedWindow.TitleBarButtons="Close">
    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Version info -->
        <TextBlock Grid.Row="0" Text="{Binding VersionInfo}" FontWeight="Bold" FontSize="14"
                   Foreground="{DynamicResource AppForegroundBrush}" Margin="0,0,0,12" TextWrapping="Wrap"/>

        <!-- Info state: changelog -->
        <TextBox Grid.Row="1" Text="{Binding Changelog}" IsReadOnly="True" TextWrapping="Wrap"
                 VerticalScrollBarVisibility="Auto" AcceptsReturn="True"
                 Padding="8" Margin="0,0,0,12"
                 Visibility="{Binding IsDownloading, Converter={StaticResource InvertBoolToCollapsed}}"/>

        <!-- Download state: progress -->
        <StackPanel Grid.Row="1" Visibility="{Binding IsDownloading, Converter={StaticResource BoolToVisible}}"
                    VerticalAlignment="Center" Margin="0,0,0,12">
            <TextBlock Text="Downloading update..." Margin="0,0,0,8"
                       Foreground="{DynamicResource AppForegroundBrush}"/>
            <ProgressBar Value="{Binding DownloadProgress}" Height="20" Minimum="0" Maximum="100"/>
            <TextBlock Text="{Binding DownloadStatus}" Margin="0,4,0,0" HorizontalAlignment="Center"
                       Foreground="{DynamicResource AppForegroundBrush}"/>
        </StackPanel>

        <!-- Error message -->
        <TextBlock Grid.Row="1" Text="{Binding ErrorMessage}" Foreground="Red" TextWrapping="Wrap"
                   Visibility="{Binding HasError, Converter={StaticResource BoolToVisible}}"
                   VerticalAlignment="Center" HorizontalAlignment="Center"/>

        <!-- Buttons -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Content="Skip" Width="80" Height="28" Margin="0,0,8,0"
                    Style="{StaticResource MainButtonStyle}"
                    Command="{Binding SkipCommand}"
                    Visibility="{Binding IsDownloading, Converter={StaticResource InvertBoolToCollapsed}}"/>
            <Button Content="Cancel" Width="80" Height="28" Margin="0,0,8,0"
                    Style="{StaticResource MainButtonStyle}"
                    Command="{Binding CancelCommand}"
                    Visibility="{Binding IsDownloading, Converter={StaticResource BoolToVisible}}"/>
            <Button Content="Upgrade" Width="80" Height="28"
                    Style="{StaticResource MainButtonStyle}"
                    Command="{Binding UpgradeCommand}"
                    Visibility="{Binding IsDownloading, Converter={StaticResource InvertBoolToCollapsed}}"/>
        </StackPanel>
    </Grid>

    <controls:ThemedWindow.Resources>
        <converters:BoolToVisibilityConverter x:Key="BoolToVisible" Collapse="False"/>
        <converters:BoolToVisibilityConverter x:Key="InvertBoolToCollapsed" Collapse="True" IsReversed="True"/>
    </controls:ThemedWindow.Resources>
</controls:ThemedWindow>
```

- [ ] **Step 2: Create DlgUpdateAvailable.xaml.cs**

Follows the pattern from `DlgMessageBox.xaml.cs`: inherits `Controls.ThemedWindow`, sets `Owner` to main window, uses `INotifyPropertyChanged` and `DelegateCommand`.

```csharp
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using OATCommunications.WPF;

namespace OATControl
{
    public partial class DlgUpdateAvailable : Controls.ThemedWindow, INotifyPropertyChanged
    {
        private readonly UpdateCheckResult _result;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isDownloading;
        private int _downloadProgress;
        private string _downloadStatus;
        private bool _hasError;
        private string _errorMessage;
        private DelegateCommand _skipCommand;
        private DelegateCommand _upgradeCommand;
        private DelegateCommand _cancelCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        public DlgUpdateAvailable(UpdateCheckResult result)
        {
            _result = result;
            this.Owner = Application.Current.MainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            InitializeComponent();
            this.DataContext = this;

            _skipCommand = new DelegateCommand(s => Close());
            _upgradeCommand = new DelegateCommand(s => StartDownload());
            _cancelCommand = new DelegateCommand(s => CancelDownload());
        }

        public string VersionInfo => $"OATControl {_result.LatestVersion} is now available (you have {_result.CurrentVersion})";
        public string Changelog => _result.Changelog;

        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(nameof(IsDownloading)); }
        }

        public int DownloadProgress
        {
            get => _downloadProgress;
            set { _downloadProgress = value; OnPropertyChanged(nameof(DownloadProgress)); }
        }

        public string DownloadStatus
        {
            get => _downloadStatus;
            set { _downloadStatus = value; OnPropertyChanged(nameof(DownloadStatus)); }
        }

        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(nameof(HasError)); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }

        public System.Windows.Input.ICommand SkipCommand => _skipCommand;
        public System.Windows.Input.ICommand UpgradeCommand => _upgradeCommand;
        public System.Windows.Input.ICommand CancelCommand => _cancelCommand;

        private async void StartDownload()
        {
            if (string.IsNullOrEmpty(_result.DownloadUrl))
            {
                HasError = true;
                ErrorMessage = "No download URL available.";
                return;
            }

            IsDownloading = true;
            HasError = false;

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "OATControlSetup.exe");

                using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
                using (var response = await client.GetAsync(_result.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long bytesRead = 0;
                        int read;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read, _cts.Token);
                            bytesRead += read;

                            if (totalBytes > 0)
                            {
                                var progress = (int)(bytesRead * 100 / totalBytes);
                                DownloadProgress = progress;
                                DownloadStatus = $"{bytesRead / 1024} KB / {totalBytes / 1024} KB ({progress}%)";
                            }
                            else
                            {
                                DownloadStatus = $"{bytesRead / 1024} KB downloaded";
                            }
                        }
                    }

                    Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    Application.Current.Shutdown();
                }
            }
            catch (OperationCanceledException)
            {
                IsDownloading = false;
                DownloadProgress = 0;
                DownloadStatus = "";
            }
            catch (Exception ex)
            {
                IsDownloading = false;
                HasError = true;
                ErrorMessage = $"Download failed: {ex.Message}";
            }
        }

        private void CancelDownload()
        {
            _cts.Cancel();
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
```

- [ ] **Step 3: Add files to OATControl.csproj**

Add near the other dialog entries (around lines 143-146 and 242):

```xml
    <Compile Include="DlgUpdateAvailable.xaml.cs">
      <DependentUpon>DlgUpdateAvailable.xaml</DependentUpon>
    </Compile>
```

And in the `<Page>` items (around line 242):

```xml
    <Page Include="DlgUpdateAvailable.xaml">
      <Generator>MSBuild:Compile</Generator>
      <SubType>Designer</SubType>
    </Page>
```

- [ ] **Step 4: Commit**

```bash
git add OATControl/DlgUpdateAvailable.xaml OATControl/DlgUpdateAvailable.xaml.cs OATControl/OATControl.csproj
git commit -m "feat: add DlgUpdateAvailable dialog with download and progress"
```

---

### Task 3: Wire up desktop update check at startup

**Files:**
- Modify: `OATControl/MainWindow.xaml.cs` (lines 1-6, 34-42)
- Modify: `OATControl/ViewModels/MountVM.cs` (line 261-267)

- [ ] **Step 1: Add desktop update check in MainWindow.xaml.cs**

Add `using` statements at the top (after existing usings, around line 5):

```csharp
using System.Threading.Tasks;
```

Modify the `OnContentRendered` method (line 34-42) to add the update check after `vm.OnAppBooted()`:

```csharp
protected override void OnContentRendered(EventArgs e)
{
    base.OnContentRendered(e);
    if (this.DataContext is MountVM vm)
    {
        vm.OnAppBooted();
    }

    _ = Task.Run(() => UpdateChecker.CheckForDesktopUpdateAsync())
        .ContinueWith(t =>
        {
            if (!t.IsFaulted && !t.IsCanceled && t.Result?.UpdateAvailable == true)
            {
                Dispatcher.Invoke(() =>
                {
                    var dlg = new DlgUpdateAvailable(t.Result);
                    dlg.ShowDialog();
                });
            }
        });
}
```

- [ ] **Step 2: Commit**

```bash
git add OATControl/MainWindow.xaml.cs
git commit -m "feat: check for desktop updates on app startup"
```

---

### Task 4: Add firmware update properties to MountVM

**Files:**
- Modify: `OATControl/ViewModels/MountVM.cs`

- [ ] **Step 1: Add backing fields**

Near the existing `_firmwareVersion` field at line 199, add:

```csharp
        private bool _firmwareUpdateAvailable;
        private string _latestFirmwareVersion;
        private string _latestFirmwareReleaseUrl;
```

- [ ] **Step 2: Add properties**

Near the existing `FirmwareVersion` property (around line 4738), add:

```csharp
        public bool FirmwareUpdateAvailable
        {
            get => _firmwareUpdateAvailable;
            set => SetPropertyValue(ref _firmwareUpdateAvailable, value);
        }

        public string LatestFirmwareVersion
        {
            get => _latestFirmwareVersion;
            set => SetPropertyValue(ref _latestFirmwareVersion, value);
        }

        public string LatestFirmwareReleaseUrl
        {
            get => _latestFirmwareReleaseUrl;
            set => SetPropertyValue(ref _latestFirmwareReleaseUrl, value);
        }
```

- [ ] **Step 3: Add firmware update check call after connection**

In `MountVM.cs`, after `_oatMount.SetFirmwareVersion(FirmwareVersion)` (line 2856), add the firmware update check:

```csharp
            _ = Task.Run(() => UpdateChecker.CheckForFirmwareUpdateAsync(ScopeVersion))
                .ContinueWith(t =>
                {
                    if (!t.IsFaulted && !t.IsCanceled && t.Result?.UpdateAvailable == true)
                    {
                        FirmwareUpdateAvailable = true;
                        LatestFirmwareVersion = t.Result.LatestVersion;
                        LatestFirmwareReleaseUrl = t.Result.ReleasePageUrl;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
```

This must be placed inside the `try` block of `ConnectToOat`, after the firmware version has been successfully parsed, near line 2856.

- [ ] **Step 4: Commit**

```bash
git add OATControl/ViewModels/MountVM.cs
git commit -m "feat: add firmware update check on mount connection"
```

---

### Task 5: Add firmware update badge on Mount Settings button

**Files:**
- Modify: `OATControl/MainWindow.xaml` (line 623)

- [ ] **Step 1: Replace the Mount Settings button with a Grid containing the button and badge**

Replace line 623:

```xml
<Button Grid.Row="4" Grid.Column="2" Content="Mount Settings" Height="24 " Margin="1,2" Width="125" HorizontalAlignment="Right" Padding="0" IsEnabled="{Binding MountConnected}" Style="{StaticResource MainButtonStyle}" Command="{Binding ShowSettingsCommand}"/>
```

With:

```xml
<Grid Grid.Row="4" Grid.Column="2" Width="125" Height="24" Margin="1,2" HorizontalAlignment="Right">
    <Button Content="Mount Settings" Padding="0" IsEnabled="{Binding MountConnected}" Style="{StaticResource MainButtonStyle}" Command="{Binding ShowSettingsCommand}"/>
    <Border Visibility="{Binding FirmwareUpdateAvailable, Converter={StaticResource CollapseIfFalse}}"
            Width="16" Height="16" CornerRadius="8" Background="#E74C3C"
            HorizontalAlignment="Right" VerticalAlignment="Top" Margin="0,-6,-6,0">
        <TextBlock Text="!" Foreground="White" FontWeight="Bold" FontSize="10"
                   HorizontalAlignment="Center" VerticalAlignment="Center"/>
    </Border>
</Grid>
```

This reuses the existing `CollapseIfFalse` converter already defined at line 18 of MainWindow.xaml.

- [ ] **Step 2: Commit**

```bash
git add OATControl/MainWindow.xaml
git commit -m "feat: add firmware update badge on Mount Settings button"
```

---

### Task 6: Add firmware update link in SettingsDialog

**Files:**
- Modify: `OATControl/SettingsDialog.xaml` (around line 189)

- [ ] **Step 1: Add firmware update hyperlink below the version display**

Replace the firmware version TextBlock (line 189):

```xml
<TextBlock Grid.Row="0" Grid.Column="1" Text="{Binding ScopeVersion}" Margin="0,12,0,2"  Style="{StaticResource TextValueWide}"/>
```

With:

```xml
<StackPanel Grid.Row="0" Grid.Column="1" Margin="0,12,0,2">
    <TextBlock Text="{Binding ScopeVersion}" Style="{StaticResource TextValueWide}"/>
    <TextBlock Visibility="{Binding FirmwareUpdateAvailable, Converter={StaticResource TrueBoolToVisible}}"
               Foreground="#FF4A90D9" Cursor="Hand" HorizontalAlignment="Center"
               MouseDown="OnFirmwareUpdateClick">
        <Run Text="new "/><Run Text="{Binding LatestFirmwareVersion, Mode=OneWay}"/><Run Text=" available"/>
    </TextBlock>
</StackPanel>
```

Note: `TrueBoolToVisible` is already defined in SettingsDialog.xaml resources at line 13.

- [ ] **Step 2: Add click handler in SettingsDialog.xaml.cs**

Add `using System.Diagnostics;` at the top if not already present, and add this method to the `SettingsDialog` class (inside the class body, around line 60):

```csharp
private void OnFirmwareUpdateClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
{
    if (_mount?.LatestFirmwareReleaseUrl is string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add OATControl/SettingsDialog.xaml OATControl/SettingsDialog.xaml.cs
git commit -m "feat: add firmware update link in Settings dialog"
```

---

### Task 7: Build and verify

- [ ] **Step 1: Build the solution in Visual Studio**

Open `OATControl/OATControl.sln` in Visual Studio and build. Fix any compilation errors.

Expected: clean build with no errors.

- [ ] **Step 2: Manual test — desktop update check**

Run OATControl. Since the current version matches the latest release (V1.2.0.0), no dialog should appear. To test the dialog, temporarily change the version in `AssemblyInfo.cs` to `1.0.0.0`, rebuild, and verify the dialog appears.

- [ ] **Step 3: Manual test — firmware update check**

Connect to a mount (real or simulated). Verify that if the firmware version is older than the latest GitHub release, the badge appears on "Mount Settings" and the inline link shows in the Settings dialog. Click the link to verify it opens the browser.

- [ ] **Step 4: Manual test — offline resilience**

Disconnect from the internet. Start OATControl and connect to a mount. Verify no errors, no dialogs, no badges appear. The app behaves exactly as before.

- [ ] **Step 5: Final commit**

```bash
git add -A
git commit -m "feat: complete GitHub release update checker"
```
