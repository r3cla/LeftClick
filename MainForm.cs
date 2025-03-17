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
    private Random random = new Random();
    private Point currentPosition;
    
    // New variables for enhancements
    private int clickCount = 0;
    private int maxClicks = 0;
    private bool useMaxClicks = false;
    private Label lblStatus;
    private Label lblClickCount;
    
    // Controls that need class level access
    private NumericUpDown numSeconds;
    private NumericUpDown numMilliseconds;
    private NumericUpDown numMinSeconds;
    private NumericUpDown numMaxSeconds;
    private bool useRandomInterval = false;

    public MainForm()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Set up form properties
        this.Text = "Auto Clicker";
        this.Size = new Size(400, 300);
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // Create controls for fixed interval
        Label lblFixedInterval = new Label { Text = "Fixed Interval:", Location = new Point(20, 60), Size = new Size(120, 20) };
        numSeconds = new NumericUpDown { 
            Location = new Point(140, 60), 
            Size = new Size(60, 20), 
            Minimum = 0, 
            Maximum = 3600,
            Value = 1
        };
        
        Label lblMilliseconds = new Label { Text = "Milliseconds:", Location = new Point(210, 60), Size = new Size(90, 20) };
        numMilliseconds = new NumericUpDown { 
            Location = new Point(300, 60), 
            Size = new Size(70, 20), 
            Minimum = 0, 
            Maximum = 999,
            Value = 0
        };
        
        // Create controls for random interval
        CheckBox chkRandomInterval = new CheckBox { Text = "Use Random Interval", Location = new Point(20, 90), Size = new Size(150, 20) };
        
        Label lblMinSeconds = new Label { Text = "Min Seconds:", Location = new Point(40, 120), Size = new Size(90, 20) };
        numMinSeconds = new NumericUpDown { 
            Location = new Point(140, 120), 
            Size = new Size(60, 20), 
            Minimum = 0, 
            Maximum = 3600,
            Value = 1,
            Enabled = false
        };
        
        Label lblMaxSeconds = new Label { Text = "Max Seconds:", Location = new Point(210, 120), Size = new Size(90, 20) };
        numMaxSeconds = new NumericUpDown { 
            Location = new Point(300, 120), 
            Size = new Size(60, 20), 
            Minimum = 1, 
            Maximum = 3600, 
            Value = 30,
            Enabled = false
        };
        
        // Create controls for scheduled stop
        CheckBox chkMaxClicks = new CheckBox { Text = "Stop After Clicks:", Location = new Point(20, 150), Size = new Size(120, 20) };
        NumericUpDown numMaxClicks = new NumericUpDown { 
            Location = new Point(140, 150), 
            Size = new Size(70, 20), 
            Minimum = 1, 
            Maximum = 999999, 
            Value = 100,
            Enabled = false
        };
        
        Button btnStart = new Button { Text = "Start (F1 or F6)", Location = new Point(20, 20), Size = new Size(170, 30) };
        Button btnStop = new Button { Text = "Stop (F2 or F7)", Location = new Point(200, 20), Size = new Size(170, 30) };
        
        Label lblInstructions = new Label { Text = "1. Set interval(s)\n2. Hit start", Location = new Point(20, 180), Size = new Size(350, 40) };
        
        // Status indicator and click counter
        lblStatus = new Label { 
            Text = "Status: Stopped", 
            Location = new Point(20, 220), 
            Size = new Size(170, 20),
            ForeColor = Color.Red,
            Font = new Font(this.Font, FontStyle.Bold)
        };
        
        lblClickCount = new Label { 
            Text = "Clicks: 0", 
            Location = new Point(200, 220), 
            Size = new Size(170, 20) 
        };

        // Set up event handlers
        btnStart.Click += (s, e) => StartClicking();
        btnStop.Click += (s, e) => StopClicking();
        
        // Set initial values for interval variables
        intervalMinSeconds = (int)numMinSeconds.Value;
        intervalMaxSeconds = (int)numMaxSeconds.Value;
        maxClicks = (int)numMaxClicks.Value;

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
        
        chkMaxClicks.CheckedChanged += (s, e) =>
        {
            useMaxClicks = chkMaxClicks.Checked;
            numMaxClicks.Enabled = useMaxClicks;
        };
        
        numMaxClicks.ValueChanged += (s, e) =>
        {
            maxClicks = (int)numMaxClicks.Value;
        };

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
        this.Controls.Add(chkMaxClicks);
        this.Controls.Add(numMaxClicks);
        this.Controls.Add(btnStart);
        this.Controls.Add(btnStop);
        this.Controls.Add(lblInstructions);
        this.Controls.Add(lblStatus);
        this.Controls.Add(lblClickCount);

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
        clickCount = 0;
        UpdateClickCount();
        
        // Update status indicator
        lblStatus.Text = "Status: Running";
        lblStatus.ForeColor = Color.Green;
        
        clickThread = new Thread(() => ClickingLoop());
        clickThread.IsBackground = true;
        clickThread.Start();
    }

    private void StopClicking()
    {
        isClicking = false;
        
        // Update status indicator (safely from any thread)
        this.Invoke((MethodInvoker)delegate {
            lblStatus.Text = "Status: Stopped";
            lblStatus.ForeColor = Color.Red;
        });
        
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
            
            // Check if we've reached max clicks
            if (useMaxClicks && clickCount >= maxClicks)
            {
                StopClicking();
                break;
            }
            
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
                // Get the current values directly from the controls
                int seconds = 0;
                int milliseconds = 0;
                
                this.Invoke((MethodInvoker)delegate {
                    seconds = (int)numSeconds.Value;
                    milliseconds = (int)numMilliseconds.Value;
                });
                
                // Calculate total interval in milliseconds
                interval = (seconds * 1000) + milliseconds;
                
                // Ensure at least a small delay
                if (interval < 10)
                    interval = 10; // Minimum 10ms delay
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
        
        // Increment click counter
        clickCount++;
        UpdateClickCount();
    }
    
    private void UpdateClickCount()
    {
        // Update the click counter label (safely from any thread)
        this.Invoke((MethodInvoker)delegate {
            lblClickCount.Text = $"Clicks: {clickCount}";
        });
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