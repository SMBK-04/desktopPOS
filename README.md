# Enterprise POS — Desktop & Offline Edition

A lightweight, zero-configuration Windows desktop wrapper and offline POS system built with **WPF**, **WebView2**, and an embedded **ASP.NET Core** backend with **SQLite**.

---

## 🌟 Features
- **100% Offline Capability**: Runs locally on `http://127.0.0.1:5288` without any internet connection.
- **Embedded Database**: Local SQLite database stored securely at `%LocalAppData%\EnterprisePOS_Offline\enterprisepos.db`.
- **Silent Thermal Printing**: Integrates directly with ESC/POS 80mm thermal receipt printers (e.g., `BC-80POS`).
- **Sequential Printing Queue**: Auto-prints both Cashier and Kitchen receipts sequentially with zero dialogs and zero paper waste (`margin: 0mm`, `size: 80mm auto`).
- **Single-Click Installer Ready**: Includes Inno Setup build script (`EnterprisePOS_Installer.iss`) for building a standalone `.exe` setup.

---

## 📂 Project Structure
```
EnterprisePOS_Desktop_App/
│
├── src/                        # WPF & WebView2 desktop shell source code
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   ├── EnterprisePOS.Desktop.csproj
│   └── EnterprisePOS_Installer.iss
│
└── publish/                    # Self-contained runnable release build
    ├── EnterprisePOS.Desktop.exe
    ├── Launch_Silent.vbs
    └── EnterprisePOS.dll (embedded offline server)
```

---

## 🚀 Running the App
1. Navigate to the `publish/` directory.
2. Double-click `Launch_Silent.vbs` or `EnterprisePOS.Desktop.exe`.
3. The offline POS will launch automatically in full screen mode.

---

## 🛠️ Building the Windows Installer (`.exe`)
1. Install [Inno Setup](https://jrsoftware.org/isdl.php).
2. Open `src/EnterprisePOS_Installer.iss` in Inno Setup and click **Build > Compile**.
3. The generated installer `EnterprisePOS_Setup_v1.0.0.exe` will be ready for client distribution.
