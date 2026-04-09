# CBTSecureDesktop - Secure Computer Based Test Application

A WPF (.NET 8) desktop application that creates a secure exam environment for computer-based testing in universities. **Now with full MySQL/MariaDB database integration!**

## 🎯 Overview

This application implements a **Secure Exam Environment** with **real database integration** that prevents students from accessing other applications or system features during an exam. It uses **Kiosk Mode**, **Low-Level Keyboard Hooking**, and **MySQL database** to create a complete, production-ready CBT system.

---

## ✨ Key Features

### 🔒 **Security Features**
- **Kiosk Mode** - Fullscreen lockdown with hidden taskbar
- **Keyboard Hook** - Blocks Alt+Tab, Windows Key, Alt+F4, etc.
- **Focus Lock** - Prevents window deactivation
- **Session Tracking** - Monitors exam start/end times

### 💾 **Database Integration** (NEW!)
- **MySQL/MariaDB** support via MySqlConnector
- **Real-time authentication** against database
- **Dynamic exam loading** from database
- **Auto-save answers** to database
- **Automatic scoring** and result storage
- **Multi-student** and multi-exam support

---

## 📁 Project Structure

```
CBTSecureDesktop/
│
├── Models/                          # 📦 Database model classes
│   ├── User.cs
│   ├── Mahasiswa.cs
│   ├── Ujian.cs
│   ├── Soal.cs
│   ├── OpsiJawaban.cs
│   └── ... (12 model classes)
│
├── Data/                            # 🗄️ Database layer (NEW!)
│   ├── DatabaseConnection.cs       # MySQL connection manager
│   └── DatabaseService.cs          # Repository with CRUD operations
│
├── Security/
│   ├── KioskManager.cs              # Fullscreen kiosk mode + taskbar hiding
│   └── KeyboardHookService.cs       # Low-level keyboard hook
│
├── UI/
│   ├── LoginWindow.xaml             # Student authentication
│   ├── DashboardWindow.xaml         # Exam selection dashboard
│   └── ExamWindow.xaml              # Secure exam interface
│
├── Services/
│   ├── AuthService.cs               # Database authentication
│   └── ExamService.cs               # Exam data management
│
├── DATABASE_INTEGRATION.md          # 📖 Complete database guide
├── DATABASE_IMPLEMENTATION_SUMMARY.md # 📝 Implementation summary
└── SECURITY_IMPLEMENTATION.md       # 🔐 Security documentation
```

---

## 🗄️ Database Architecture

### Connection Details
```
Server: localhost
Port: 3306
Database: cbt_database
Username: root
Password: (empty - Laragon default)
```

### Key Tables
- **t_user** - User authentication (students, lecturers)
- **t_mahasiswa** - Student information
- **t_ujian** - Exam metadata
- **t_soal** - Exam questions
- **t_opsi_jawaban** - Answer options
- **t_soal_mahasiswa** - Student answers
- **t_ujian_mahasiswa** - Exam sessions & scores

### Database Operations
```csharp
// Authentication
var user = await db.AuthenticateUserAsync(username, password);

// Load exams for student
var exams = await db.GetAvailableExamsForStudentAsync(mahasiswaId);

// Get questions
var questions = await db.GetExamQuestionsAsync(ujianId);

// Save answer
await db.SaveStudentAnswerAsync(soalId, mahasiswaId, opsiJawabanId);

// Submit & calculate score
await db.EndExamSessionAsync(ujianId, mahasiswaId);
```

---

## 🔐 Security Features

### 1. **Kiosk Mode** (KioskManager.cs)

**What it does:**
- Forces the application window into fullscreen mode
- Removes window borders and title bar (WindowStyle = None)
- Keeps the window always on top (Topmost = true)
- Hides the Windows taskbar
- Prevents window deactivation

**Key Methods:**
```csharp
void EnableKioskMode(Window window)   // Activate secure mode
void DisableKioskMode()               // Restore normal mode
bool IsKioskModeActive                // Check if active
```

**Windows API Used:**
- `FindWindow()` - Locate taskbar window
- `ShowWindow()` - Hide/show taskbar

---

### 2. **Keyboard Hook** (KeyboardHookService.cs)

**What it does:**
- Intercepts ALL keyboard input at the operating system level
- Blocks dangerous key combinations before Windows processes them

**Blocked Key Combinations:**
- **Alt + Tab** - Task switcher
- **Alt + F4** - Close window
- **Ctrl + Esc** - Start menu
- **Windows Key** - Start menu / shortcuts
- **Windows + D** - Show desktop
- **Windows + Tab** - Task view

**Key Methods:**
```csharp
void StartHook()      // Activate keyboard interception
void StopHook()       // Deactivate hook
bool IsHookActive     // Check if active
```

**Windows API Used:**
- `SetWindowsHookEx()` - Install low-level keyboard hook (WH_KEYBOARD_LL)
- `UnhookWindowsHookEx()` - Remove hook
- `CallNextHookEx()` - Pass allowed keys to Windows
- `GetAsyncKeyState()` - Check modifier keys (Alt, Ctrl)

**How it Works:**
1. Registers a global keyboard hook (WH_KEYBOARD_LL = 13)
2. Every keypress triggers the `HookCallback()` method
3. Callback checks if the key is blocked
4. Returns `1` to block the key, or calls `CallNextHookEx()` to allow it

---

## 🚀 Complete Application Workflow

### 1. **Login Phase** 🔐
- Student enters Username (NIM) and Password
- `AuthService` queries `t_user` table in database
- Password verified (plain text or bcrypt)
- Student details loaded from `t_mahasiswa` table
- On success, opens Dashboard

### 2. **Dashboard Phase** 📋
- Queries database for available exams
- Filters exams by student's class (via `t_ujian_kelas`)
- Displays exam details (name, subject, status)
- Student selects an exam
- Security warning dialog
- Starts secure exam mode

### 3. **Exam Phase (SECURE MODE)** 🎯
```
┌─────────────────────────────────────┐
│  ✓ Kiosk Mode Enabled               │
│  ✓ Keyboard Hook Active             │
│  ✓ Fullscreen + Taskbar Hidden      │
│  ✓ Task Switching Blocked           │
│  ✓ Exam Session Tracked in DB       │
│  ✓ Answers Auto-Saved to DB         │
└─────────────────────────────────────┘
```

**What happens:**
- `KioskManager.EnableKioskMode()` locks screen
- `KeyboardHookService.StartHook()` blocks shortcuts
- `DatabaseService.StartExamSessionAsync()` creates session in `t_ujian_mahasiswa`
- Questions loaded from `t_soal` with options from `t_opsi_jawaban`
- Each answer auto-saved to `t_soal_mahasiswa`
- Timer tracks elapsed time

**Student can:**
- Answer multiple-choice questions
- Navigate between questions (Previous/Next)
- See progress indicator

**Student CANNOT:**
- Switch to other apps (Alt+Tab blocked)
- Close window (Alt+F4 blocked)
- Access Start menu (Windows key blocked)
- Minimize window (locked fullscreen)

### 4. **Submission Phase** ✅
- Student clicks "Submit Exam"
- Confirmation dialog
- `DatabaseService.EndExamSessionAsync()`:
  - Calculates score automatically
  - Compares student answers with correct answers
  - Stores final score in `t_ujian_mahasiswa`
- Display score to student
- **Security mode deactivated**
- Application closes

---

## 💻 User Interface

### LoginWindow 🔑
- Clean, modern design with gradient background
- Username (NIM) and Password fields
- Security warning notice
- Loading indicator during authentication
- Database connection validation

### DashboardWindow 📊
- List of available exams from database
- Real exam metadata:
  - Exam name (e.g., "UTS Pemrograman Web")
  - Subject/Course name
  - Exam code
  - Status (menunggu/dimulai/selesai)
- Start Exam button (triggers security mode)
- Security information panel
- Logout option

### ExamWindow 📝
- Secure fullscreen interface (no borders)
- Question display with multiple-choice options
- Timer display (tracks exam duration)
- Question counter (e.g., "Question 1 of 5")
- Navigation buttons (Previous/Next)
- Submit Exam button
- Auto-save on each answer
- Cannot be closed without submitting

---

## 🗄️ Database Setup

### Prerequisites
1. **Laragon** installed (or any MySQL/MariaDB server)
2. **MySQL Server** running on port 3306
3. Database `cbt_database` created

### Quick Setup
```sql
-- 1. Create database (if not exists)
CREATE DATABASE IF NOT EXISTS cbt_database;

-- 2. Import the provided schema
USE cbt_database;
SOURCE cbt_database.sql;
```

### Test Data
```
Username: 224176013
Password: password123
```

**This account has access to:**
- 3 exams (Pemrograman Web, Basis Data, Struktur Data)
- 13 questions across all exams
- In class TI-4A

### Verify Connection
The app will automatically test the connection on startup and show an error if MySQL is not running.

---

## 🛡️ Security Implementation Details

### When Exam Starts:
```csharp
private async void ActivateSecurityMode()
{
    // 1. Enable Kiosk Mode (fullscreen + hide taskbar)
    _kioskManager.EnableKioskMode(this);

    // 2. Start Keyboard Hook (block shortcuts)
    _keyboardHook.StartHook();

    // 3. Start exam session in database
    await _databaseService.StartExamSessionAsync(ujianId, mahasiswaId);
}
```

### When Exam Ends:
```csharp
private async void DeactivateSecurityMode()
{
    // 1. Calculate and save score
    await _databaseService.EndExamSessionAsync(ujianId, mahasiswaId);

    // 2. Stop Keyboard Hook
    _keyboardHook.StopHook();

    // 3. Disable Kiosk Mode
    _kioskManager.DisableKioskMode();
}
```

### Preventing Accidental Exit:
```csharp
private void ExamWindow_Closing(object? sender, CancelEventArgs e)
{
    if (_kioskManager.IsKioskModeActive)
    {
        // Show warning and cancel close
        e.Cancel = true;
    }
}
```

---

## 🧪 Testing the Security Features

### Test Kiosk Mode:
1. Start an exam
2. ✓ Window should be fullscreen
3. ✓ Taskbar should be hidden
4. ✓ Window cannot be minimized
5. ✓ Window cannot be resized

### Test Keyboard Hook:
During exam, try pressing:
- **Alt + Tab** → ❌ Blocked
- **Windows Key** → ❌ Blocked
- **Alt + F4** → ❌ Blocked
- **Ctrl + Esc** → ❌ Blocked
- **Windows + D** → ❌ Blocked

Normal keys (typing answers) → ✓ Work normally

---

## ⚙️ Technical Requirements

- **.NET 8.0** (Windows)
- **WPF** (Windows Presentation Foundation)
- **Windows 10/11** (for API compatibility)
- **MySQL/MariaDB Server** (Laragon recommended)
- **MySqlConnector** NuGet package
- **Administrator privileges** recommended for full taskbar control

---

## 🚀 Quick Start Guide

### 1. Setup Database (Laragon)
```bash
# Start Laragon
# MySQL should be running on localhost:3306

# Create database
CREATE DATABASE cbt_database;

# Import schema
USE cbt_database;
SOURCE cbt_database.sql;
```

### 2. Clone & Build
```bash
git clone https://github.com/yourusername/CBTSecureDesktop
cd CBTSecureDesktop
dotnet restore
dotnet build
```

### 3. Run Application
```bash
dotnet run
# OR press F5 in Visual Studio
```

### 4. Login with Test Account
```
Username: 224176013
Password: password123
```

### 5. Take an Exam
- Select an exam from the dashboard
- Click "START EXAM"
- Security mode activates automatically
- Answer questions (auto-saved to database)
- Submit to see your score

---

## 📊 Database Queries for Testing

```sql
-- Check if student logged in
SELECT * FROM t_user WHERE username = '224176013';

-- View available exams
SELECT u.*, m.nama 
FROM t_ujian u
JOIN t_matakuliah m ON u.matakuliah_id = m.matakuliah_id
WHERE u.status = 'dimulai';

-- Check student answers
SELECT s.pertanyaan, o.jawaban, o.nilai
FROM t_soal_mahasiswa sm
JOIN t_soal s ON sm.soal_id = s.soal_id
JOIN t_opsi_jawaban o ON sm.opsi_jawaban_id = o.opsi_jawaban_id
WHERE sm.mahasiswa_id = 16;

-- View exam results
SELECT * FROM t_ujian_mahasiswa 
WHERE mahasiswa_id = 16 
ORDER BY created_at DESC;
```

---

## 📝 Code Highlights

### Database: Authenticate User
```csharp
public async Task<User?> AuthenticateUserAsync(string username, string password)
{
    using var connection = _dbConnection.GetConnection();
    await connection.OpenAsync();

    string query = "SELECT * FROM t_user WHERE username = @username";
    using var command = new MySqlCommand(query, connection);
    command.Parameters.AddWithValue("@username", username);

    using var reader = await command.ExecuteReaderAsync();
    if (await reader.ReadAsync())
    {
        var user = new User { /* map fields */ };
        return VerifyPassword(password, user.Password) ? user : null;
    }
    return null;
}
```

### Database: Save Answer
```csharp
public async Task<bool> SaveStudentAnswerAsync(long soalId, long mahasiswaId, long opsiJawabanId)
{
    // Check if answer exists
    var existingId = await GetExistingAnswer(soalId, mahasiswaId);

    string query = existingId != null
        ? "UPDATE t_soal_mahasiswa SET opsi_jawaban_id = @opsi WHERE..."
        : "INSERT INTO t_soal_mahasiswa VALUES (...)";

    // Execute query
    return await ExecuteNonQueryAsync(query) > 0;
}
```

### Database: Calculate Score
```csharp
private async Task<int> CalculateExamScoreAsync(long ujianId, long mahasiswaId)
{
    string query = @"
        SELECT COUNT(*) as correct
        FROM t_soal_mahasiswa sm
        JOIN t_opsi_jawaban o ON sm.opsi_jawaban_id = o.opsi_jawaban_id
        WHERE ujian_id = @ujianId AND mahasiswa_id = @mahasiswaId 
        AND o.nilai = 1";

    int correct = await ExecuteScalarAsync(query);
    int total = await GetTotalQuestions(ujianId);

    return (correct * 100) / total;
}
```

### Security: Blocking Alt+Tab
```csharp
if (vkCode == VK_TAB && IsAltPressed())
{
    Debug.WriteLine("Blocked: ALT + TAB");
    return (IntPtr)1; // Block the key
}
```

### Security: Hiding Taskbar
```csharp
int hwnd = FindWindow("Shell_TrayWnd", "");
ShowWindow(hwnd, SW_HIDE);
```

---

## 🎓 Educational Purpose & Use Cases

This is a **production-ready CBT system** demonstrating:

1. **Full-Stack .NET Development** - WPF + MySQL integration
2. **Database-Driven Architecture** - Real-time data persistence
3. **Secure Exam Environment** - OS-level security features
4. **Windows API Integration** - Low-level system hooks
5. **MVVM Pattern** - Clean separation of concerns
6. **Repository Pattern** - Structured data access

**Perfect for:**
- University computer-based exams
- Certification testing centers
- Online proctored exams
- Skills assessment platforms

---

## 🚨 Important Notes

### Security Features:
✅ **Implemented:**
- Kiosk Mode (fullscreen lockdown)
- Keyboard hook (block shortcuts)
- Focus lock (prevent deactivation)
- Database-driven authentication
- Session tracking
- Auto-save answers
- Automatic scoring

⚠️ **Additional Production Considerations:**
- Process monitoring (detect unauthorized apps)
- Screen capture blocking
- Network restrictions
- Virtual machine detection
- Webcam proctoring
- Biometric authentication
- Advanced anti-cheating measures

### Database Security:
✅ **Current Implementation:**
- Parameterized queries (SQL injection prevention)
- Connection pooling
- Error handling
- Password hashing support (bcrypt ready)

🔧 **Production Recommendations:**
- Use proper BCrypt.Net password hashing
- Implement role-based access control
- Add database encryption
- Set up regular backups
- Monitor suspicious activity

### Permissions:
- Application may require **Administrator rights** for full functionality
- Keyboard hooks require proper Windows permissions
- Taskbar manipulation works best with elevated privileges

---

## 📚 Documentation

- **[DATABASE_INTEGRATION.md](DATABASE_INTEGRATION.md)** - Complete database guide with workflow diagrams
- **[DATABASE_IMPLEMENTATION_SUMMARY.md](DATABASE_IMPLEMENTATION_SUMMARY.md)** - Quick implementation summary
- **[SECURITY_IMPLEMENTATION.md](SECURITY_IMPLEMENTATION.md)** - Security feature documentation
- **[ARCHITECTURE_DIAGRAM.md](ARCHITECTURE_DIAGRAM.md)** - System architecture overview
- **[QUICK_START.md](QUICK_START.md)** - Getting started guide

---

## 🔄 Future Enhancements

Potential additions for enterprise use:

1. **Exam Management**
   - Question bank management
   - Question shuffling implementation
   - Time limits per question
   - Essay/free-text questions
   - Image-based questions

2. **Proctoring Features**
   - Real-time webcam monitoring
   - Screen recording
   - Suspicious activity detection
   - AI-powered cheating detection

3. **Reporting & Analytics**
   - Detailed score reports
   - Question difficulty analysis
   - Student performance trends
   - Export to PDF/Excel

4. **Advanced Security**
   - Process whitelist/blacklist
   - Multi-monitor lockdown
   - Browser lockdown mode
   - Network traffic analysis

5. **User Management**
   - Admin dashboard
   - Bulk student import
   - Role management
   - Exam scheduling

---

## 📞 Support & Troubleshooting

### Common Issues:

**"Database connection failed"**
- Ensure Laragon/MySQL is running
- Check connection string in `DatabaseConnection.cs`
- Verify database `cbt_database` exists

**"Taskbar won't hide"**
- Run application as Administrator
- Check Windows permissions
- Verify KioskManager is activated

**"Keyboard shortcuts still work"**
- Restart application
- Check Debug output for hook errors
- Run as Administrator

**"No exams showing"**
- Verify student exists in `t_mahasiswa`
- Check `t_ujian_kelas` links exam to class
- Ensure exam status is 'dimulai' or 'menunggu'

### Debug Logs:
Check Visual Studio Output window for:
- Database operation logs
- Security event logs
- Error messages

---

## ✅ Build & Run

```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

**Test Credentials (Demo Mode):**
- Student ID: Any string with 5+ characters
- Password: Any string with 4+ characters

---

## 📜 License

Educational demonstration project for secure CBT systems.

---

**Built with .NET 8 + WPF + Windows API**
