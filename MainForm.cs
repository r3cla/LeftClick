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
    private int intervalMinSeconds = 0;
    private int intervalMaxSeconds = 0;
    private bool useRandomInterval = false;
    private Random random = new Random();
    private Point currentPosition;

    public MainForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Set up form properties
        this.Text = "Auto Clicker";
        this.Size = new Size(400, 240);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // Create controls for fixed interval
        Label lblFixedInterval = new Label { Text = "Fixed Interval:", Location = new Point(20, 60), Size = new Size(120, 20) };
        NumericUpDown numSeconds = new NumericUpDown { Location = new Point(140, 60), Size = new Size(60, 20), Minimum = 0, Maximum = 3600 };
        
        Label lblMilliseconds = new Label { Text = "Milliseconds:", Location = new Point(210, 60), Size = new Size(90, 20) };
        NumericUpDown numMilliseconds = new NumericUpDown { Location = new Point(300, 60), Size = new Size(70, 20), Minimum = 0, Maximum = 999 };
        
        // Create controls for random interval
        CheckBox chkRandomInterval = new CheckBox { Text = "Use Random Interval", Location = new Point(20, 90), Size = new Size(150, 20) };
        
        Label lblMinSeconds = new Label { Text = "Min Seconds:", Location = new Point(40, 120), Size = new Size(90, 20) };
        NumericUpDown numMinSeconds = new NumericUpDown { Location = new Point(140, 120), Size = new Size(60, 20), Minimum = 0, Maximum = 3600 };
        
        Label lblMaxSeconds = new Label { Text = "Max Seconds:", Location = new Point(210, 120), Size = new Size(90, 20) };
        NumericUpDown numMaxSeconds = new NumericUpDown { Location = new Point(300, 120), Size = new Size(60, 20), Minimum = 1, Maximum = 3600, Value = 30 };
        
        Button btnStart = new Button { Text = "Start (F1 or F6)", Location = new Point(20, 20), Size = new Size(170, 30) };
        Button btnStop = new Button { Text = "Stop (F2 or F7)", Location = new Point(200, 20), Size = new Size(170, 30) };
        
        Label lblInstructions = new Label { Text = "1. Set interval(s)\n2. Hit start", Location = new Point(20, 160), Size = new Size(350, 50) };

        // Set up event handlers
        btnStart.Click += (s, e) => StartClicking();
        btnStop.Click += (s, e) => StopClicking();
        
        numSeconds.ValueChanged += (s, e) => intervalMinSeconds = (int)numSeconds.Value;
        numMilliseconds.ValueChanged += (s, e) => {};  // Will use as a base interval

        chkRandomInterval.CheckedChanged += (s, e) => 
        {
            useRandomInterval = chkRandomInterval.Checked;
            numMinSeconds.Enabled = useRandomInterval;
            numMaxSeconds.Enabled = useRandomInterval;
            numSeconds.Enabled = !useRandomInterval;
            numMilliseconds.Enabled = !useRandomInterval;
        };

        numMinSeconds.ValueChanged += (s, e) => 
        {
            intervalMinSeconds = (int)numMinSeconds.Value;
            if (intervalMinSeconds > numMaxSeconds.Value)
                numMaxSeconds.Value = intervalMinSeconds + 1;
        };

        numMaxSeconds.ValueChanged += (s, e) => 
        {
            intervalMaxSeconds = (int)numMaxSeconds.Value;
            if (intervalMaxSeconds < numMinSeconds.Value)
                numMinSeconds.Value = intervalMaxSeconds - 1;
        };

        // Initial values
        numMinSeconds.Value = 1;
        numMaxSeconds.Value = 30;
        intervalMinSeconds = 1;
        intervalMaxSeconds = 30;
        
        // Initial enabled state
        numMinSeconds.Enabled = false;
        numMaxSeconds.Enabled = false;

        // Add controls to form
        this.Controls.Add(lblFixedInterval);
        this.Controls.Add(numSeconds);
        this.Controls.Add(lblMilliseconds);
        this.Controls.Add(numMilliseconds);
        this.Controls.Add(chkRandomInterval);
        this.Controls.Add(lblMinSeconds);
        this.Controls.Add(numMinSeconds);
        this.Controls.Add(lblMaxSeconds);
        this.Controls.Add(numMaxSeconds);
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
        
        clickThread = new Thread(() => ClickingLoop());
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

    private void ClickingLoop()
    {
        while (isClicking)
        {
            // Capture the current cursor position for the click
            Point pos = currentPosition;
            
            // Perform the click at the current position
            PerformClick(pos.X, pos.Y);
            
            // Calculate next interval
            int interval;
            if (useRandomInterval)
            {
                // Convert to milliseconds and get a random value in the range
                int minMs = intervalMinSeconds * 1000;
                int maxMs = intervalMaxSeconds * 1000;
                interval = random.Next(minMs, maxMs + 1);
            }
            else
            {
                // Use fixed interval (default to 1 second if nothing set)
                interval = Math.Max(1, intervalMinSeconds) * 1000;
            }
            
            // Wait for the calculated interval
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