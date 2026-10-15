using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace RedAnts.Show.Simulator.Simulation;

public sealed record VmixInputDefinition(string Title, string Type, IReadOnlyList<string> TextFields, IReadOnlyList<string> ImageFields);

public sealed record VmixFunctionCall(DateTime At, string Function, string Input, string Field, string Value, string Raw);

public sealed class VmixStub(EventLog log, TitleLibrary titles) : BackgroundService
{
    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, string>> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _overlays = new();
    private readonly HashSet<string> _pinned = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<VmixFunctionCall> _calls = new();

    public int Port { get; set; } = 8099;
    public bool Listening { get; private set; }
    public int Clients { get; private set; }
    public string? Fault { get; private set; }
    public long Functions { get; private set; }

    public List<VmixInputDefinition> Inputs => titles.Titles
        .Select(title => new VmixInputDefinition(
            title,
            "GT",
            titles.Of(title)?.TextFields.Distinct().ToList() ?? [],
            titles.Of(title)?.ImageFields.Distinct().ToList() ?? []))
        .ToList();

    public event Action? Changed;

    public IReadOnlyDictionary<int, string> Overlays { get { lock (_gate) return new Dictionary<int, string>(_overlays); } }

    public IReadOnlyCollection<string> Pinned { get { lock (_gate) return _pinned.ToList(); } }

    public bool IsPinned(string input) { lock (_gate) return _pinned.Contains(input); }

    public void Pin(string input, bool on)
    {
        lock (_gate)
        {
            if (on) _pinned.Add(input);
            else _pinned.Remove(input);
        }
        Changed?.Invoke();
    }

    public IReadOnlyList<string> OnStage()
    {
        lock (_gate)
        {
            var stage = _pinned.Where(p => !_overlays.ContainsValue(p)).ToList();
            stage.AddRange(_overlays.OrderBy(o => o.Key).Select(o => o.Value));
            return stage;
        }
    }

    public IReadOnlyDictionary<string, string> ValuesOf(string input)
    {
        lock (_gate) return _values.TryGetValue(input, out var fields) ? new Dictionary<string, string>(fields) : new Dictionary<string, string>();
    }

    public IReadOnlyList<VmixFunctionCall> LatestCalls(int count = 20)
    {
        lock (_gate) return _calls.AsEnumerable().Reverse().Take(count).ToList();
    }

    public void ClearValues()
    {
        lock (_gate) { _values.Clear(); _overlays.Clear(); _calls.Clear(); _pinned.Clear(); }
        Changed?.Invoke();
    }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            var listener = new TcpListener(IPAddress.Any, Port);
            try
            {
                listener.Start();
                Listening = true;
                Fault = null;
                log.Add("vMix", $"vMix-Stellvertreter hört auf TCP {Port}");
                Changed?.Invoke();
                while (!stop.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(stop);
                    _ = ServeAsync(client, stop);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException e)
            {
                Listening = false;
                Fault = e.Message;
                log.Add("vMix", $"Port {Port} nicht verfügbar: {e.Message}");
                Changed?.Invoke();
                try { await Task.Delay(TimeSpan.FromSeconds(3), stop); } catch (OperationCanceledException) { break; }
            }
            finally { listener.Stop(); Listening = false; }
        }
    }

    private async Task ServeAsync(TcpClient client, CancellationToken stop)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "?";
        Clients++;
        log.Add("vMix", $"Verbindung von {remote}");
        Changed?.Invoke();
        try
        {
            using (client)
            await using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                while (!stop.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(stop);
                    if (line is null) break;
                    var reply = Handle(line.Trim());
                    if (reply is not null) await stream.WriteAsync(Encoding.UTF8.GetBytes(reply), stop);
                    if (line.Trim().Equals("QUIT", StringComparison.OrdinalIgnoreCase)) break;
                }
            }
        }
        catch (Exception e) when (e is IOException or SocketException or OperationCanceledException) { }
        finally
        {
            Clients--;
            log.Add("vMix", $"Verbindung von {remote} beendet");
            Changed?.Invoke();
        }
    }

    private string? Handle(string line)
    {
        if (line.Length == 0) return null;
        if (line.Equals("XML", StringComparison.OrdinalIgnoreCase))
        {
            var xml = StateXml();
            return $"XML {Encoding.UTF8.GetByteCount(xml)}\r\n{xml}";
        }
        if (line.Equals("QUIT", StringComparison.OrdinalIgnoreCase)) return null;
        if (!line.StartsWith("FUNCTION ", StringComparison.OrdinalIgnoreCase))
        {
            log.Add("vMix", $"Unbekannt: {line}");
            return "ER Unknown command\r\n";
        }
        var rest = line[9..].Trim();
        var space = rest.IndexOf(' ');
        var function = space < 0 ? rest : rest[..space];
        var args = space < 0 ? new Dictionary<string, string>() : ParseArgs(rest[(space + 1)..]);
        Apply(function, args, line);
        return $"FUNCTION OK {function}\r\n";
    }

    private void Apply(string function, Dictionary<string, string> args, string raw)
    {
        var input = args.GetValueOrDefault("Input", "");
        var field = args.GetValueOrDefault("SelectedName", "");
        var value = args.GetValueOrDefault("Value", "");
        lock (_gate)
        {
            Functions++;
            if (function.StartsWith("OverlayInput", StringComparison.OrdinalIgnoreCase))
            {
                var channel = int.TryParse(new string(function.Skip(12).TakeWhile(char.IsDigit).ToArray()), out var c) ? c : 1;
                if (function.EndsWith("Out", StringComparison.OrdinalIgnoreCase)) _overlays.Remove(channel);
                else _overlays[channel] = input;
            }
            else if (function is "SetText" or "SetColor" or "SetImage")
            {
                if (!_values.TryGetValue(input, out var fields)) _values[input] = fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                fields[field] = value;
            }
            _calls.Add(new VmixFunctionCall(DateTime.Now, function, input, field, value, raw));
            if (_calls.Count > 500) _calls.RemoveRange(0, _calls.Count - 500);
        }
        if (function is "SetText" or "SetColor" or "SetImage") log.Add("vMix", $"{function} {input} {field} = \"{value}\"");
        else log.Add("vMix", $"{function} {input}".Trim());
        Changed?.Invoke();
    }

    private static Dictionary<string, string> ParseArgs(string query)
    {
        var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq < 0) { args[WebUtility.UrlDecode(part)] = ""; continue; }
            args[WebUtility.UrlDecode(part[..eq])] = WebUtility.UrlDecode(part[(eq + 1)..]);
        }
        return args;
    }

    private string StateXml()
    {
        lock (_gate)
        {
            var inputs = new XElement("inputs");
            var number = 1;
            foreach (var definition in Inputs)
            {
                var input = new XElement("input",
                    new XAttribute("key", Guid.NewGuid().ToString()),
                    new XAttribute("number", number++),
                    new XAttribute("type", definition.Type),
                    new XAttribute("title", definition.Title),
                    new XAttribute("state", "Paused"));
                var values = _values.GetValueOrDefault(definition.Title);
                foreach (var text in definition.TextFields)
                    input.Add(new XElement("text", new XAttribute("index", 0), new XAttribute("name", text), values?.GetValueOrDefault(text) ?? ""));
                foreach (var image in definition.ImageFields)
                    input.Add(new XElement("image", new XAttribute("index", 0), new XAttribute("name", image), values?.GetValueOrDefault(image) ?? ""));
                inputs.Add(input);
            }
            var overlays = new XElement("overlays");
            for (var channel = 1; channel <= 4; channel++)
                overlays.Add(new XElement("overlay", new XAttribute("number", channel), _overlays.GetValueOrDefault(channel) ?? ""));
            return new XElement("vmix", new XElement("version", "27.0.0.0"), new XElement("edition", "Simulator"), inputs, overlays).ToString(SaveOptions.DisableFormatting);
        }
    }
}
