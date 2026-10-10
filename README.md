# RRes — Valorant Resolution Switcher

Aplikasi utilitas desktop berbasis **WPF (.NET 10)** untuk pemain Valorant yang ingin menggunakan dan mengunci resolusi *stretched* (seperti 4:3 dan 5:4) dengan refresh rate monitor maksimal, modul kompres grafik *Potato / Max FPS*, pemilih multi-monitor, auto-revert otomatis saat game selesai, optimasi process priority, kalkulasi presisi mouse/eDPI, dan animasi splash screen sinematik.

---
## ✨ Fitur Utama

### 1. ⚡ True Exclusive Fullscreen Engine
- **Force Exclusive Fullscreen (FullscreenMode=0)**: Mengeliminasi masalah game tertahan di mode Windowed Fullscreen dengan menyelaraskan konfigurasi `FullscreenMode=0`, `LastConfirmedFullscreenMode=0`, `PreferredFullscreenMode=0`, serta dimensi layar di GameUserSettings.ini.
- **Atomic Resolution Write**: Menulis parameter resolusi dan aspect ratio secara aman ke seluruh profil pemain dengan pelepasan otomatis lock file dan proteksi atribut Read-Only.


### 2. 🖥️ Peningkatan Resolusi & Multi-Display
- **Multi-Monitor Target Selector**: Dropdown interaktif pemilih target monitor bagi perangkat dengan multi-monitor. Mendeteksi nama monitor fisik, adapter GPU, dan status monitor utama (`[Utama]`).
- **Custom Resolution Input Manual (Width × Height)**: Opsi input numerik bebas jika rasio atau resolusi yang diinginkan tidak ada di daftar preset (misal: 1080×1080, 1350×1080, 1152×864).
- **Custom Refresh Rate (Hz Lock)**: Pengunci frekuensi refresh rate (misal 60Hz, 75Hz, 120Hz, 144Hz, 165Hz, 240Hz, 280Hz, 360Hz) agar monitor tidak turun ke 60Hz saat beralih ke resolusi custom.
- **Pintasan & Status GPU Scaling**: Pintasan langsung ke **NVIDIA Control Panel** (`shell:AppsFolder\NVIDIACorp...`), **AMD Radeon Software**, dan **Windows Display Settings** (`ms-settings:display`), serta panduan konfigurasi 'Full-screen' / 'Full Panel' tanpa black bar.

### 3. 👤 Profil Akun & Target Config
- **Interactive Profile Selector**: Mengubah deteksi teks statis menjadi dropdown interaktif kaya data yang menampilkan folder UUID akun spesifik di `%LOCALAPPDATA%\VALORANT\Saved\Config`, waktu modifikasi terakhir, resolusi tersimpan, sensitivitas, dan status Render Scale.
- **Buka Folder Config**: Tombol icon folder `📁` untuk membuka folder konfigurasi profil terpilih atau direktori root config Valorant langsung di Windows Explorer.
- **Opsi Multi-Akun**: Checkbox "Terapkan ke Semua Akun / Profil yang Ditemukan" untuk menerapkan pengaturan ke semua folder UUID sekaligus.

### 4. 🎛️ Perbaikan Alur Tombol & Proteksi Config
- **Pemisahan Aksi Sidebar yang Jelas**:
  * **`APPLY & LAUNCH`** (Merah Utama): Menerapkan resolusi, grafik, dan sensitivitas, lalu langsung meluncurkan VALORANT.
  * **`APPLY ONLY`** (Sekunder): Menerapkan resolusi ke Desktop Windows dan file config tanpa meluncurkan game.
  * **`QUICK LAUNCH (Without Apply)`** (Subtle): Meluncurkan game VALORANT langsung tanpa mengubah resolusi atau setting config.
  * **`REVERT NATIVE`** (Outline): Mengembalikan resolusi Windows dan konfigurasi profil ke resolusi native bawaan.
- **Proteksi & Sinkronisasi Utuh**:
  * **Backup Otomatis (`.bak`)**: Otomatis membuat file cadangan sebelum file `.ini` ditimpa.
  * **Kunci Config (Read-Only)**: Melepas atribut Read-Only saat penulisan dan menguncinya kembali agar tidak direset oleh patch/game.
  * **Sinkronisasi Sensitivitas**: Otomatis menulis `MouseSensitivity` ke `RiotUserSettings.ini` & `GameUserSettings.ini`.
  * **Auto-Revert Watcher**: Pemantau proses `VALORANT-Win64-Shipping.exe` di background yang otomatis mengembalikan resolusi Windows ke native saat game ditutup.

### 5. 🎯 Mouse & Aim Precision & Raw Accel Companion
- **Raw Accel Companion Integration**: Deteksi otomatis driver dan file `settings.json` Raw Accel dengan status badge interaktif (Driver Detected / Not Installed).
- **Preset Kurva Akselerasi Siap Pakai**: Dukungan preset kurva populer (Linear Tracking, Natural CS/Val, Jump Flick, dan Bypass/Off 1:1 Raw Input).
- **Auto-Sync Stretched Yaw / Domain Weight**: Penyesuaian otomatis Domain Weight horizontal (X: 0.75 pada 4:3, X: 0.703 pada 5:4) di `settings.json` saat resolusi stretched diaktifkan agar reflek muscle memory 1:1 tetap terjaga.
- **Windows Raw Input (1:1 Mouse Precision)**: Mematikan *Enhance Pointer Precision* Windows via `SystemParametersInfo`.
- **Kalkulator eDPI & Scoped Multiplier**: Menghitung eDPI efektif hipfire dan sniper (Marshal/Operator) serta rekomendasi nilai focal match.

### 6. ⚡ Network & Tweaks Anti-Stutter
- **Riot AP Latency Checker**: Pengukur ping real-time ke edge server Singapore, Tokyo, dan Hong Kong via hybrid ICMP + TCP port 443 handshake.
- **Anti-Stutter Registry Tweak**: Mematikan Windows Game Bar & Game DVR (`GameDVR_Enabled = 0`, `AppCaptureEnabled = 0`) untuk meminimalisir frame drop.
- **Flush DNS Resolver**: Tombol instan pembersih cache DNS Windows (`ipconfig /flushdns`).
- **High CPU Priority**: Otomatis menaikkan prioritas proses VALORANT ke prioritas tinggi.

### 7. 🧹 Cache & Crash Log Cleaner
- **Valorant Storage Maintenance**: Pembersihan otomatis file dump dan log Unreal Engine di `%localappdata%\VALORANT\Saved\Logs`, `Crashes`, dan `webcache`.
- **Safe Asynchronous Deletion**: Menghitung kalkulasi ruang disk terpakai (MB/GB) dan menghapus file secara aman tanpa mengganggu file yang sedang dikunci oleh game.

### 8. 🎯 Crosshair Profile Vault
- **Preset Pro-Player Valorant**: Koleksi kode crosshair resmi pemain profesional papan atas (TenZ, aspas, f0rsakeN, Jinggg, Boaster, Demon1, Yay, Chronicle).
- **Custom Vault Persistence**: Simpan, kelola, dan hapus profil crosshair kustom ke penyimpanan lokal `%appdata%\RRes\crosshairs.json`.
- **One-Click Copy to Clipboard**: Tombol salin instan dengan notifikasi visual badge.

### 9. 🎧 Loudness Equalization & Audio Helper
- **Pintasan Cepat Audio Windows**: Membuka dialog properti audio Windows (`mmsys.cpl`) langsung ke tab Playback & Enhancements.
- **Panduan Footstep Clarity**: Langkah visual panduan pengaktifan Loudness Equalization untuk mengangkat suara desiran langkah kaki musuh dan meredam dentuman tembakan/ledakan.

### 10. 💬 Discord Rich Presence Kustom
- **Game Status & Stretched Res**: Integrasi Rich Presence dengan mendeteksi otomatis proses `VALORANT-Win64-Shipping.exe`.
- **Display Status Detail**: Menampilkan resolusi aktif (contoh: *1024x768 @ 144Hz Stretched*), status tweak RRes, durasi waktu bermain, dan otomatis membersihkan status (*ClearPresence*) saat game ditutup.

### 11. 📡 Riot Direct Packet Loss & Jitter Tester
- **10-Probe Batch Quality Diagnostic**: Pengujian batch berurutan ke endpoint AWS/Riot (Singapore, Tokyo, Hong Kong).
- **Kalkulasi Metrik Kompetitif**: Mengukur Latensi Rata-rata (Avg Latency), Jitter fluktuasi ping antar paket (ms), dan Rasio Packet Loss (0% - 100%).
- **Indikator Kualitas Jaringan**: Badge status berkode warna (Stabil / Siap Ranked, Jitter Tinggi / Waspada, Packet Loss / Buruk).

---

## 📁 Struktur Proyek

```
RRes/
├── Assets/
│   ├── app.ico                    # Ikon resmi aplikasi Windows
│   ├── app_icon.ico               # Ikon Windows multi-resolusi tersemat
│   ├── splash_logo.png            # Logo lambang 'R' transparan tanpa background
│   └── splash_logo.jpg            # File gambar logo sumber
├── Core/
│   ├── Display/
│   │   ├── NativeMethods.cs       # P/Invoke user32.dll (EnumDisplayDevices, ChangeDisplaySettingsEx)
│   │   ├── ResolutionInfo.cs      # Model data resolusi monitor & rasio aspek
│   │   └── DisplayManager.cs      # Multi-monitor display engine & pengunci refresh rate (Hz)
│   ├── Mouse/
│   │   ├── MouseInputManager.cs   # P/Invoke SystemParametersInfo (Disable Mouse Acceleration)
│   │   └── RawAccelService.cs     # Deteksi driver Raw Accel, editor settings.json, & stretched yaw sync
│   ├── Network/
│   │   └── RiotPingService.cs     # Latency checker & batch jitter/packet loss tester (ICMP & TCP socket)
│   ├── Optimization/
│   │   └── WindowsGameOptimization.cs # Modul anti-stutter registry, GPU control panel launcher & DNS
│   ├── Utils/
│   │   ├── ImageTransparencyHelper.cs # Engine runtime pengolah transparansi bitmap
│   │   └── WindowsAudioHelper.cs  # Helper shortcut dialog Windows Sound (mmsys.cpl & ms-settings)
│   ├── Watcher/
│   │   ├── GameProcessWatcher.cs  # Background watcher VALORANT (Auto-Revert & High Priority)
│   │   └── DiscordRpcService.cs   # Integrasi Discord Rich Presence via DiscordRPC NuGet
│   └── Valorant/
│       ├── CrosshairVaultService.cs # Preset pro & penyimpanan profil custom JSON
│       ├── ValorantCleanerService.cs # Pembersih crash log & webcache %localappdata%
│       ├── ValorantLauncher.cs    # Peluncur game (riotclient:// & RiotClientServices.exe)
│       ├── ValorantProfile.cs     # Model profil akun Valorant (Res, Sens, Render Scale)
│       └── ValorantConfigManager.cs # Editor GameUserSettings.ini (Scalability & Res) & backup
├── Models/
│   ├── CleanerResult.cs           # Model hasil pembersihan disk & format byte
│   ├── CrosshairProfile.cs        # Model profil crosshair (Name, Author, Code, Tag)
│   ├── MonitorInfo.cs             # Model target monitor aktif
│   ├── NetworkQualityResult.cs    # Model hasil uji batch (Latency, Jitter, Loss %, Status)
│   ├── RefreshRateOption.cs       # Model opsi frekuensi Hz
│   └── ResolutionPreset.cs        # Model opsi resolusi preset
├── ViewModels/
│   ├── BaseViewModel.cs           # Implementasi INotifyPropertyChanged
│   ├── RelayCommand.cs            # Implementasi ICommand WPF
│   └── MainViewModel.cs           # Viewmodel utama display, profil, cleaner, crosshair, rpc & network
├── Converters/
│   ├── StatusTypeToBrushConverter.cs # Converter warna status bar
│   └── InverseBooleanConverter.cs    # Converter binding status boolean
├── Views/
│   ├── MainWindow.xaml            # Desain UI Dark Mode modern dengan profil selector, vault & diagnostics
│   ├── MainWindow.xaml.cs         # Code-behind view
│   ├── SplashScreenWindow.xaml    # Jendela splash screen transparan sinematik
│   └── SplashScreenWindow.xaml.cs # Animasi storyboard Fade-In/Out & ScaleTransform
├── App.xaml                       # Resources, styles (ModernSlider, buttons, dark scrollbar)
├── App.xaml.cs                    # Alur startup splash screen & global exception handlers
├── RRes.csproj                    # Konfigurasi .NET 10 WPF & Publish Single-File
└── README.md
```

---

## 🚀 Cara Menjalankan & Membangun

### Persyaratan
- Windows 10 / Windows 11 (x64)
- .NET 10.0 SDK

### Menjalankan dalam Mode Pengembangan
```powershell
dotnet run
```

### Membuat File Tunggal Executable (Single-File Standalone)
Untuk menghasilkan satu file `.exe` mandiri yang dapat langsung didistribusikan tanpa memerlukan instalasi .NET runtime di komputer pengguna:
```powershell
dotnet publish -c Release -p:PublishSingleFile=true
```
Hasil file executable mandiri akan berada di:
`bin\Release\net10.0-windows\win-x64\publish\RRes.exe`
