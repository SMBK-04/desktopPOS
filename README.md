# Enterprise POS — Desktop & Offline Edition

A lightweight, high-performance Windows desktop POS system built with **WPF**, **WebView2**, and an embedded **ASP.NET Core** backend with **SQLite**.

---

## 🌟 Features
- **100% Offline Capability**: Runs locally on `http://127.0.0.1:5288` with zero external database dependencies.
- **Embedded SQLite Database**: Local database stored securely at `%LocalAppData%\EnterprisePOS_Offline\enterprisepos.db` with auto-migration and seeding.
- **Enterprise Solutions Branding**: Native application icon (`app.ico`), styled startup splash screen, and taskbar branding.
- **Silent Thermal Printing**: Integrates directly with ESC/POS 80mm thermal receipt printers (e.g., `BC-80POS`).
- **Sequential Printing Queue**: Auto-prints both Cashier and Kitchen receipts sequentially with zero dialogs and zero paper waste (`margin: 0mm`, `size: 80mm auto`).
- **Zero Runtime Prerequisites**: Self-contained installer bundles all .NET and Windows Desktop runtimes.

---

## 📂 Repository Structure
```
desktopPOS/
│
├── src/                          # WPF & WebView2 desktop shell
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   ├── EnterprisePOS.Desktop.csproj
│   ├── EnterprisePOS_Installer.iss   # Inno Setup build script
│   ├── app.ico                       # Enterprise Solutions native icon
│   └── logo.png                      # Branding asset
│
└── backend_src/                  # Offline ASP.NET Core & SQLite backend source
    ├── Controllers/
    ├── Data/ (ApplicationDbContext with SQLite)
    ├── Models/
    ├── Views/
    ├── wwwroot/
    └── EnterprisePOS.csproj
```

---

## 🛠️ Building & Packaging

### 1. Build Desktop Shell & Backend
```bash
# Publish backend (self-contained win-x64)
dotnet publish backend_src/EnterprisePOS.csproj -c Release -r win-x64 --self-contained true -o publish_selfcontained/backend

# Publish WPF frontend
dotnet publish src/EnterprisePOS.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish_selfcontained
```

### 2. Generate Windows Installer (`.exe`)
Open `src/EnterprisePOS_Installer.iss` in **Inno Setup** and click **Compile** to generate `EnterprisePOS_Setup_v1.0.0.exe`.
