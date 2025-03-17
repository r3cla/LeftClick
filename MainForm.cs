using System.Drawing;
using System.Runtime.InteropServices;

namespace MyClicker;

public partial class MainForm : Form
{
    // Import necessary Windows API functions
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out Point lpPoint);

    [DllImport("user32.dll")]
    static extern bool SetCursorPos(int X, int Y);

    [DllImport("user32.dll")]
    static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(Keys vKey);

    // Constants for mouse events
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    // Variables to track state
    private bool isClicking = false;
    private Thread clickThread;
    private int intervalSeconds = 0;
    private int intervalMilliseconds = 0;
    private Point currentPosition;

    public MainForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Set up form properties
        this.Text = "Auto Clicker";
        this.Size = new Size(400, 200);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // Create controls
        Label lblSeconds = new Label { Text = "Seconds:", Location = new Point(20, 60), Size = new Size(70, 20) };
        NumericUpDown numSeconds = new NumericUpDown { Location = new Point(100, 60), Size = new Size(60, 20), Minimum = 0, Maximum = 3600 };
        
        Label lblMilliseconds = new Label { Text = "Milliseconds:", Location = new Point(180, 60), Size = new Size(90, 20) };
        NumericUpDown numMilliseconds = new NumericUpDown { Location = new Point(280, 60), Size = new Size(70, 20), Minimum = 0, Maximum = 999 };
        
        Button btnStart = new Button { Text = "Start (F1 or F6)", Location = new Point(20, 20), Size = new Size(170, 30) };
        Button btnStop = new Button { Text = "Stop (F2 or F7)", Location = new Point(200, 20), Size = new Size(170, 30) };
        
        Label lblInstructions = new Label { Text = "1. Set a speed\n2. Hit start", Location = new Point(20, 100), Size = new Size(350, 50) };

        // Set up event handlers
        btnStart.Click += (s, e) => StartClicking();
        btnStop.Click += (s, e) => StopClicking();
        
        numSeconds.ValueChanged += (s, e) => intervalSeconds = (int)numSeconds.Value;
        numMilliseconds.ValueChanged += (s, e) => intervalMilliseconds = (int)numMilliseconds.Value;

        // Add controls to form
        this.Controls.Add(lblSeconds);
        this.Controls.Add(numSeconds);
        this.Controls.Add(lblMilliseconds);
        this.Controls.Add(numMilliseconds);
        this.Controls.Add(btnStart);
        this.Controls.Add(btnStop);
        this.Controls.Add(lblInstructions);

        // Set up timer for mouse position tracking and hotkey detection
        System.Windows.Forms.Timer positionTimer = new System.Windows.Forms.Timer();
        positionTimer.Interval = 10; // 10ms interval
        positionTimer.Tick += PositionTimer_Tick;
        positionTimer.Start();
    }

    private void StartClicking()
    {
        if (isClicking)
            return;

        isClicking = true;
        
        // Calculate total interval in milliseconds
        int totalInterval = (intervalSeconds * 1000) + intervalMilliseconds;
        if (totalInterval <= 0)
            totalInterval = 100; // Default to 100ms if no interval specified
        
        clickThread = new Thread(() => ClickingLoop(totalInterval));
        clickThread.IsBackground = true;
        clickThread.Start();
    }

    private void StopClicking()
    {
        isClicking = false;
        if (clickThread != null && clickThread.IsAlive)
        {
            clickThread.Join(500); // Wait up to 500ms for thread to end
        }
    }

    private void ClickingLoop(int interval)
    {
        while (isClicking)
        {
            // Capture the current cursor position for the click
            Point pos = currentPosition;
            
            // Perform the click at the current position
            PerformClick(pos.X, pos.Y);
            
            // Wait for the specified interval
            Thread.Sleep(interval);
        }
    }

    private void PerformClick(int x, int y)
    {
        // Click at the specified position
        mouse_event(MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
        mouse_event(MOUSEEVENTF_LEFTUP, x, y, 0, 0);
    }

    private void PositionTimer_Tick(object sender, EventArgs e)
    {
        // Update current mouse position
        GetCursorPos(out currentPosition);
        
        // Check for hotkeys
        if ((GetAsyncKeyState(Keys.F1) < 0) || (GetAsyncKeyState(Keys.F6) < 0))
        {
            StartClicking();
        }
        
        if ((GetAsyncKeyState(Keys.F2) < 0) || (GetAsyncKeyState(Keys.F7) < 0))
        {
            StopClicking();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopClicking();
        base.OnFormClosing(e);
    }
}