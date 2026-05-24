using System;
using System.Drawing;
using System.Windows.Forms;

namespace CalculatorApp;

public class CalculatorForm : Form
{
    private Label _expressionLabel = null!;
    private Label _resultLabel = null!;

    private string _currentInput = "0";
    private string _previousInput = "";
    private string? _operator = null;
    private bool _shouldReset = false;

    // ボタン定義: (表示テキスト, 列, 行, 列幅, ボタン種別)
    private readonly (string Text, int Col, int Row, int ColSpan, string Kind)[] _buttonDefs =
    [
        ("AC",  0, 0, 1, "clear"),
        ("+/-", 1, 0, 1, "special"),
        ("%",   2, 0, 1, "special"),
        ("÷",   3, 0, 1, "operator"),

        ("7",   0, 1, 1, "number"),
        ("8",   1, 1, 1, "number"),
        ("9",   2, 1, 1, "number"),
        ("×",   3, 1, 1, "operator"),

        ("4",   0, 2, 1, "number"),
        ("5",   1, 2, 1, "number"),
        ("6",   2, 2, 1, "number"),
        ("−",   3, 2, 1, "operator"),

        ("1",   0, 3, 1, "number"),
        ("2",   1, 3, 1, "number"),
        ("3",   2, 3, 1, "number"),
        ("+",   3, 3, 1, "operator"),

        ("0",   0, 4, 2, "number"),
        (".",   2, 4, 1, "number"),
        ("=",   3, 4, 1, "equals"),
    ];

    public CalculatorForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "電卓";
        BackColor = Color.FromArgb(22, 33, 62);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);

        const int btnW = 70;
        const int btnH = 60;
        const int gap = 8;
        const int padX = 16;
        const int padY = 16;

        // ディスプレイパネル
        var displayPanel = new Panel
        {
            BackColor = Color.FromArgb(15, 52, 96),
            BorderStyle = BorderStyle.None,
            Location = new Point(padX, padY),
            Size = new Size(btnW * 4 + gap * 3, 80),
            Padding = new Padding(10, 8, 10, 8)
        };

        _expressionLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(160, 174, 192),
            Font = new Font("Segoe UI", 11f),
            TextAlign = ContentAlignment.MiddleRight,
            Text = ""
        };

        _resultLabel = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(226, 232, 240),
            Font = new Font("Segoe UI", 26f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleRight,
            Text = "0"
        };

        displayPanel.Controls.Add(_resultLabel);
        displayPanel.Controls.Add(_expressionLabel);
        Controls.Add(displayPanel);

        // ボタン配置
        int btnAreaTop = padY + 80 + gap;
        foreach (var (text, col, row, colSpan, kind) in _buttonDefs)
        {
            int x = padX + col * (btnW + gap);
            int y = btnAreaTop + row * (btnH + gap);
            int w = btnW * colSpan + gap * (colSpan - 1);

            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, btnH),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14f),
                Cursor = Cursors.Hand,
                Tag = kind
            };

            btn.FlatAppearance.BorderSize = 0;
            ApplyButtonStyle(btn, kind);
            btn.Click += OnButtonClick;
            btn.MouseEnter += (s, _) => ApplyHoverStyle((Button)s!, true);
            btn.MouseLeave += (s, _) => ApplyHoverStyle((Button)s!, false);

            Controls.Add(btn);
        }

        int formW = padX * 2 + btnW * 4 + gap * 3;
        int formH = btnAreaTop + btnH * 5 + gap * 4 + padY;
        ClientSize = new Size(formW, formH);

        KeyPreview = true;
        KeyDown += OnKeyDown;
    }

    private static void ApplyButtonStyle(Button btn, string kind)
    {
        btn.BackColor = kind switch
        {
            "number"   => Color.FromArgb(26, 26, 46),
            "operator" => Color.FromArgb(83, 52, 131),
            "equals"   => Color.FromArgb(233, 69, 96),
            "clear"    => Color.FromArgb(15, 52, 96),
            "special"  => Color.FromArgb(15, 52, 96),
            _          => Color.FromArgb(26, 26, 46)
        };

        btn.ForeColor = kind switch
        {
            "clear"   => Color.FromArgb(252, 129, 129),
            "special" => Color.FromArgb(144, 205, 244),
            _         => Color.FromArgb(226, 232, 240)
        };
    }

    private static void ApplyHoverStyle(Button btn, bool hovering)
    {
        string kind = (string)(btn.Tag ?? "number");
        if (!hovering)
        {
            ApplyButtonStyle(btn, kind);
            return;
        }
        btn.BackColor = kind switch
        {
            "number"   => Color.FromArgb(45, 45, 78),
            "operator" => Color.FromArgb(107, 68, 168),
            "equals"   => Color.FromArgb(255, 90, 122),
            _          => Color.FromArgb(26, 64, 128)
        };
    }

    private void OnButtonClick(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        HandleInput(btn.Text);
    }

    private void HandleInput(string value)
    {
        switch (value)
        {
            case "AC":  ClearAll(); break;
            case "+/-": ToggleSign(); break;
            case "%":   Percent(); break;
            case "÷":   SetOperator("/"); break;
            case "×":   SetOperator("*"); break;
            case "−":   SetOperator("-"); break;
            case "+":   SetOperator("+"); break;
            case "=":   Calculate(); break;
            case ".":   AppendDecimal(); break;
            default:    AppendNumber(value); break;
        }
    }

    private void AppendNumber(string num)
    {
        if (_shouldReset)
        {
            _currentInput = num;
            _shouldReset = false;
        }
        else
        {
            _currentInput = _currentInput == "0" ? num : _currentInput + num;
        }
        UpdateDisplay();
    }

    private void AppendDecimal()
    {
        if (_shouldReset)
        {
            _currentInput = "0.";
            _shouldReset = false;
            UpdateDisplay();
            return;
        }
        if (!_currentInput.Contains('.'))
        {
            _currentInput += ".";
            UpdateDisplay();
        }
    }

    private void SetOperator(string op)
    {
        if (_operator != null && !_shouldReset)
            Calculate(intermediate: true);

        _previousInput = _currentInput;
        _operator = op;
        _shouldReset = true;

        string sym = op switch { "+" => "+", "-" => "−", "*" => "×", "/" => "÷", _ => op };
        _expressionLabel.Text = $"{_previousInput} {sym}";
    }

    private void Calculate(bool intermediate = false)
    {
        if (_operator == null || string.IsNullOrEmpty(_previousInput)) return;

        if (!double.TryParse(_previousInput, out double prev) ||
            !double.TryParse(_currentInput, out double curr))
            return;

        double result;
        try
        {
            result = _operator switch
            {
                "+" => prev + curr,
                "-" => prev - curr,
                "*" => prev * curr,
                "/" when curr == 0 => throw new DivideByZeroException(),
                "/" => prev / curr,
                _ => curr
            };
        }
        catch (DivideByZeroException)
        {
            _currentInput = "エラー";
            _operator = null;
            _expressionLabel.Text = "";
            UpdateDisplay();
            return;
        }

        if (!intermediate)
        {
            string sym = _operator switch { "+" => "+", "-" => "−", "*" => "×", "/" => "÷", _ => _operator };
            _expressionLabel.Text = $"{_previousInput} {sym} {_currentInput} =";
            _operator = null;
        }

        _currentInput = Math.Round(result, 10).ToString("G");
        _shouldReset = !intermediate;
        UpdateDisplay();

        if (!intermediate)
            _previousInput = "";
        else
            _previousInput = _currentInput;
    }

    private void ClearAll()
    {
        _currentInput = "0";
        _previousInput = "";
        _operator = null;
        _shouldReset = false;
        _expressionLabel.Text = "";
        UpdateDisplay();
    }

    private void ToggleSign()
    {
        if (_currentInput == "0" || _currentInput == "エラー") return;
        _currentInput = _currentInput.StartsWith('-')
            ? _currentInput[1..]
            : "-" + _currentInput;
        UpdateDisplay();
    }

    private void Percent()
    {
        if (double.TryParse(_currentInput, out double val))
        {
            _currentInput = Math.Round(val / 100, 10).ToString("G");
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        _resultLabel.Text = _currentInput;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.D0: case Keys.NumPad0: AppendNumber("0"); break;
            case Keys.D1: case Keys.NumPad1: AppendNumber("1"); break;
            case Keys.D2: case Keys.NumPad2: AppendNumber("2"); break;
            case Keys.D3: case Keys.NumPad3: AppendNumber("3"); break;
            case Keys.D4: case Keys.NumPad4: AppendNumber("4"); break;
            case Keys.D5: case Keys.NumPad5: AppendNumber("5"); break;
            case Keys.D6: case Keys.NumPad6: AppendNumber("6"); break;
            case Keys.D7: case Keys.NumPad7: AppendNumber("7"); break;
            case Keys.D8: case Keys.NumPad8: AppendNumber("8"); break;
            case Keys.D9: case Keys.NumPad9: AppendNumber("9"); break;
            case Keys.Decimal: case Keys.OemPeriod: AppendDecimal(); break;
            case Keys.Add:      SetOperator("+"); break;
            case Keys.Subtract: SetOperator("-"); break;
            case Keys.Multiply: SetOperator("*"); break;
            case Keys.Divide:   SetOperator("/"); e.Handled = true; break;
            case Keys.Enter: case Keys.Return: Calculate(); break;
            case Keys.Escape:   ClearAll(); break;
            case Keys.Back:
                if (_currentInput.Length > 1 && _currentInput != "エラー")
                    _currentInput = _currentInput[..^1];
                else
                    _currentInput = "0";
                UpdateDisplay();
                break;
        }
    }
}
