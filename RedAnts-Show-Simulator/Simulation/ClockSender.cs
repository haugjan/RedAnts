using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RedAnts.Show.Simulator.Simulation;

public sealed class ClockSender(EventLog log) : BackgroundService
{
    private static readonly Encoding Utf32Be = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(100);

    private readonly object _gate = new();
    private DateTime _lastSecond = DateTime.MinValue;

    public string TargetHost { get; set; } = "127.0.0.1";
    public int TargetPort { get; set; } = 50085;
    public bool Sending { get; set; } = true;
    public bool Running { get; private set; }
    public bool CountsDown { get; set; }
    public int PeriodSeconds { get; set; } = 20 * 60;
    public int Seconds { get; private set; }
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Period { get; set; } = "1";
    public string Mode { get; set; } = "GAME TIME";
    public string HomeName { get; set; } = "Red Ants";
    public string AwayName { get; set; } = "Gast";
    public string HomePenalty1 { get; set; } = "";
    public string HomePenalty2 { get; set; } = "";
    public string AwayPenalty1 { get; set; } = "";
    public string AwayPenalty2 { get; set; } = "";
    public long Sent { get; private set; }
    public string LastTelegram { get; private set; } = "";
    public string? Fault { get; private set; }

    public event Action? Changed;

    public string Time => $"{Seconds / 60:00}:{Seconds % 60:00}";

    public static readonly string[] Modes = ["GAME TIME", "INTERMISSION", "TIME-OUT", "WARMUP", "TIME TO FACE-OFF", "LOCAL TIME"];

    public void Start()
    {
        lock (_gate) { Running = true; _lastSecond = DateTime.UtcNow; }
        log.Add("Uhr", "Uhr gestartet");
        Changed?.Invoke();
    }

    public void Stop()
    {
        lock (_gate) Running = false;
        log.Add("Uhr", $"Uhr gestoppt bei {Time}");
        Changed?.Invoke();
    }

    public void Toggle() { if (Running) Stop(); else Start(); }

    public void SetSeconds(int seconds)
    {
        lock (_gate) Seconds = Math.Clamp(seconds, 0, 99 * 60 + 59);
        Changed?.Invoke();
    }

    public void Goal(bool home)
    {
        if (home) HomeScore++; else AwayScore++;
        log.Add("Uhr", $"Tor {(home ? "Heim" : "Gast")}: {HomeScore}:{AwayScore}");
        Changed?.Invoke();
    }

    public void Correct(bool home, int delta)
    {
        if (home) HomeScore = Math.Max(0, HomeScore + delta); else AwayScore = Math.Max(0, AwayScore + delta);
        Changed?.Invoke();
    }

    public void NextPeriod()
    {
        Period = Period switch { "1" => "2", "2" => "3", "3" => "4", "4" => "5", _ => "1" };
        lock (_gate) { Seconds = CountsDown ? PeriodSeconds : 0; Running = false; }
        log.Add("Uhr", $"Drittel {Period}");
        Changed?.Invoke();
    }

    public void Reset()
    {
        lock (_gate) { Running = false; Seconds = CountsDown ? PeriodSeconds : 0; }
        HomeScore = 0; AwayScore = 0; Period = "1"; Mode = "GAME TIME";
        HomePenalty1 = HomePenalty2 = AwayPenalty1 = AwayPenalty2 = "";
        log.Add("Uhr", "Uhr zurückgesetzt");
        Changed?.Invoke();
    }

    public string Telegram() =>
        string.Join(';', [Time, HomeScore.ToString(), AwayScore.ToString(), Period, HomePenalty1, HomePenalty2, AwayPenalty1, AwayPenalty2, "", "", HomeName, AwayName, Mode]);

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        using var udp = new UdpClient();
        while (!stop.IsCancellationRequested)
        {
            Advance();
            if (Sending)
            {
                var telegram = Telegram();
                try
                {
                    await udp.SendAsync(Utf32Be.GetBytes(telegram), new IPEndPoint(IPAddress.Parse(TargetHost), TargetPort), stop);
                    Sent++;
                    Fault = null;
                    if (telegram != LastTelegram) { LastTelegram = telegram; Changed?.Invoke(); }
                }
                catch (Exception e) when (e is SocketException or FormatException) { Fault = e.Message; }
            }
            try { await Task.Delay(Tick, stop); }
            catch (OperationCanceledException) { break; }
        }
    }

    private void Advance()
    {
        lock (_gate)
        {
            if (!Running) return;
            var now = DateTime.UtcNow;
            if (now - _lastSecond < TimeSpan.FromSeconds(1)) return;
            _lastSecond = now;
            if (CountsDown) { if (Seconds > 0) Seconds--; else Running = false; }
            else { if (Seconds < PeriodSeconds) Seconds++; else Running = false; }
        }
        Changed?.Invoke();
    }
}
