# WHEA Explorer

**WHEA Explorer** is a Windows desktop tool for browsing **Kernel-WHEA** event log channels and turning raw hardware event payloads into **structured JSON** you can read in the app or copy elsewhere. It targets engineers and support staff who work with **Windows Hardware Error Architecture (WHEA)** and related kernel hardware traces.

---

## Features

| Area | What it does |
|------|----------------|
| **Event list** | Loads events from **Microsoft-Windows-Kernel-WHEA/Operational** and **Microsoft-Windows-Kernel-WHEA/Errors**, newest first. Rows show local time, channel name, and event ID. |
| **Decode selected event** | Selecting a row decodes **RawData** into indented JSON. Switch between **JSON** and **Tree** for the same payload. |
| **Raw payload** | Optional **Show raw event data** section at the top of the tab displays the normalized hex for the current selection (toggle on/off). |
| **Decode raw data** | Paste **hex** (for example from Event Viewer’s Raw Data view) or **Base64** and decode without using the log list. |
| **Signatures** | Recognizes **WHEA kernel event log envelopes** and **CPER (UEFI Common Platform Error Record)** blobs and maps them through an in-process hardware-event parser. |

---

## Requirements

- **OS:** Windows (WPF and event log APIs are Windows-specific).
- **SDK:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later compatible with `net8.0-windows`.
- **Permissions:** Reading the Kernel-WHEA channels may require running under an account that can read the event log (administrator in restricted environments).

---

## Build

From the repository root:

```bash
dotnet build src/WheaExplorer.sln -c Release
```

To run without packaging:

```bash
dotnet run --project src/WheaExplorer/WheaExplorer.csproj -c Release
```

The executable is emitted under `src/WheaExplorer/bin/Release/net8.0-windows/` (for example `WheaExplorer.exe` when publishing as a framework-dependent app).

---

## Usage

1. Start **WHEA Explorer**.
2. Choose **Refresh logs** to load events that include **RawData** from the Kernel-WHEA channels.
3. Select a row to decode it in **Selected event** (JSON or Tree).
4. Use **Decode raw data** to decode buffers copied from other tools.

If no events appear, the channels may be empty, inaccessible, or events may lack extractable RawData; the status bar summarizes the outcome.

---

## Technology

- **UI:** WPF (.NET 8, `UseWPF`).
- **Serialization:** [Newtonsoft.Json](https://www.newtonsoft.com/json) for formatted output.
- **Logs:** `System.Diagnostics.Eventing.Reader` for channel queries and XML-based RawData extraction.

The decoding stack includes ported **hardware event / CPER** layout logic used to interpret binary records after signature detection.

---

## License

This project is released under the [MIT License](LICENSE).

---

## Disclaimer

This tool is provided for diagnostics and education. Decoded output depends on the quality and version of the input binary data; always validate critical conclusions against official documentation and vendor guidance.

**Note**: The decoding logic is inspired by https://github.com/ralish/DecodeWheaRecord repo.
