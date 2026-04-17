# 🛡️ Operation Shield Wall

## Prerequisites

Before you begin, make sure you have the following installed:

| Tool | Version | Download |
|------|---------|----------|
| **.NET SDK** | **10.0** or later | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |
| **IDE** | VS Code (recommended), Visual Studio 2022+, or JetBrains Rider | [code.visualstudio.com](https://code.visualstudio.com/) |
| **Git** | Any recent version | [git-scm.com](https://git-scm.com/) |

**VS Code recommended extensions** (optional but helpful):
- [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) — IntelliSense, debugging, solution explorer
- [GitHub Copilot](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot) — AI pair programming (if you have a license)

**Verify your setup:**
```bash
dotnet --version
# Should output 10.0.x
```

---

## Project Structure

```
ShieldWall/
├── ShieldWall.slnx                        ← Solution file (all 4 projects)
│
├── src/
│   ├── ShieldWall.GameMaster/             ← Game server — run this first
│   │   ├── Data/
│   │   │   └── alert-scenario.json        ← 160 alerts across 4 phases
│   │   ├── Hubs/
│   │   │   └── SentinelHub.cs             ← SignalR hub
│   │   ├── Services/                      ← Scoring, orchestration, streaming
│   │   ├── wwwroot/
│   │   │   ├── gamemaster.html            ← Facilitator control panel
│   │   │   └── warroom.html               ← Live war room scoreboard
│   │   └── appsettings.json               ← CORS, credentials, scenario path
│   │
│   ├── ShieldWall.Shared/                 ← Read only — do not modify
│   │   ├── Enums/                         ← ThreatLevel, ActionType, AlertType, GamePhase
│   │   ├── Interfaces/                    ← IAlertClassifier, IPatternDetector, IResponseEngine
│   │   └── Models/                        ← SentinelAlert, ClassifiedAlert, ThreatPattern, etc.
│   │
│   └── ShieldWall.TeamKit/                ← YOUR WORKSPACE
│       ├── Services/
│       │   ├── AlertClassifier.cs         ← ★ IMPLEMENT THIS ★
│       │   ├── PatternDetector.cs         ← ★ IMPLEMENT THIS ★
│       │   ├── ResponseEngine.cs          ← ★ IMPLEMENT THIS ★
│       │   ├── AlertPipeline.cs           ← Already wired — do not modify
│       │   └── SentinelConnection.cs      ← Already wired — do not modify
│       ├── Components/
│       │   ├── Pages/
│       │   │   └── Home.razor             ← Mission briefing + alert feed
│       │   └── Shared/
│       │       ├── ScorePanel.razor       ← Live Mission Effectiveness score
│       │       ├── AlertFeed.razor        ← Incoming alert stream
│       │       └── ConnectionBadge.razor  ← Connection status indicator
│       └── appsettings.json              ← Set your team name here
│
└── tests/
    └── ShieldWall.Tests/
        ├── TeamKit/                       ← Tests for classifier and response engine
        ├── Scoring/                       ← Tests for classification, action, latency scoring
        ├── Scenario/                      ← Tests for alert stream engine and scenario loader
        ├── Orchestrator/                  ← Tests for game orchestration
        └── Models/                        ← Tests for shared model contracts
```

---

## Running Locally

### Step 1 — Clone and set your team name

```bash
git clone https://github.com/antonjoyglow/regeneration-shieldwall.git
cd regeneration-shieldwall
```

Edit `src/ShieldWall.TeamKit/appsettings.json` and set your team name:

```json
{
  "Team": { "Name": "YourTeamName" }
}
```

### Step 2 — Start the GameMaster (terminal 1)

```bash
dotnet run --project src/ShieldWall.GameMaster
```

GameMaster starts at **http://localhost:5100**

| URL | Purpose |
|-----|---------|
| `http://localhost:5100/gamemaster.html` | Facilitator control panel (start/pause/replay phases) |
| `http://localhost:5100/warroom.html` | Live team scoreboard for the room |

> **GameMaster credentials:** username `shieldwall`, password `tactical2026`

### Step 3 — Start your TeamKit (terminal 2)

```bash
dotnet run --project src/ShieldWall.TeamKit
```

TeamKit opens at **http://localhost:5200**

You should see **`● Connected — YourTeamName`** in green at the top within a few seconds. If not, make sure the GameMaster is running.

### Step 4 — Start coding

Open the three files in `src/ShieldWall.TeamKit/Services/` and implement your logic:

- `AlertClassifier.cs` — classify each incoming alert into a `ThreatLevel`
- `PatternDetector.cs` — detect compound threat patterns across the alert history
- `ResponseEngine.cs` — decide the `ActionType` for each classified alert

After every change: stop TeamKit with `Ctrl+C`, re-run `dotnet run --project src/ShieldWall.TeamKit`. Your team registration persists — the GameMaster remembers you.

**Or use hot reload** to skip the restart cycle:

```bash
dotnet watch --project src/ShieldWall.TeamKit
```

---

## Running Tests

```bash
dotnet test
```

The test suite covers the scoring engine, scenario loader, stream engine, and includes templates for your classifier and response engine:

| Folder | What it tests |
|--------|---------------|
| `TeamKit/` | `AlertClassifier` and `ResponseEngine` (extend these!) |
| `Scoring/` | Classification accuracy, action scoring, latency multipliers |
| `Scenario/` | Alert stream dispatch, phase management, scenario loading |
| `Orchestrator/` | Game lifecycle and phase transitions |
| `Models/` | Shared model contracts |

---

## Mission Brief

The Sentinel Grid — Tactical Edge Technologies' distributed monitoring and alert processing network — has lost its AI-driven classification layer. Raw sensor data is streaming in from stations across the grid, but there is no brain to process it.

Threats are going unclassified. Patterns are being missed. The control room is blind.

Four field teams have been activated. Your mission: **build the Threat Assessment Engine** — the logic that processes incoming alerts, scores their severity, detects compound threat patterns, and recommends response actions.

The War Room is watching. Your Mission Effectiveness score updates live. You have **60 minutes**.

---

## The Alert Stream

The Sentinel Grid broadcasts alerts in real time via SignalR. Each alert looks like this:

| Field | Type | Description |
|-------|------|-------------|
| `AlertId` | string | Unique ID: "SA-0042" |
| `Timestamp` | DateTime | When the alert was generated |
| `Sector` | string | Grid sector: "Alpha-7", "Bravo-3", "Delta-1" |
| `Type` | enum | `Perimeter`, `Cyber`, `Sensor`, `Comms`, `Environmental` |
| `RawSeverity` | int | 1-10 (source's assessment) |
| `ConfidenceScore` | double | 0.0-1.0 (how reliable is this alert?) |
| `Source` | string | Origin: "Radar-North", "Firewall-DMZ", etc. |
| `CorrelationGroup` | string? | Shared ID linking related alerts (null if standalone) |
| `Metadata` | Dictionary | Additional contextual data |

**The stream escalates over four phases:**

- **Phase 1 (0-15 min):** Slow, clear signals. Get your basics working.
- **Phase 2 (15-30 min):** Noise increases. Confidence scores vary. Can you filter false positives?
- **Phase 3 (30-45 min):** Correlated alerts appear. Multiple small alerts in the same sector = compound threat. Can you detect patterns?
- **Phase 4 (45-60 min):** Alert volume triples. Can your engine keep up under pressure?

---

## Your Objectives

### ⚔️ MANDATORY — The General's Orders

These three capabilities **must** be operational. Your code, your logic, your design.

---

### 1. Alert Classification → `Services/AlertClassifier.cs`

Every incoming alert needs a threat level. You decide how.

```
Input:  SentinelAlert (severity, confidence, type, source, metadata)
Output: ClassifiedAlert (ThreatLevel, ComputedScore, Reasoning)

ThreatLevel: Critical | High | Medium | Low | Noise
```

**Think about:**
- A severity-9 alert with 0.2 confidence — Critical or Noise?
- A severity-2 alert with 0.99 confidence from a trusted source — Low or Medium?
- Does the alert type matter? Is a Cyber alert more urgent than Environmental?
- Can you learn which sources are reliable over time?

**Scoring:** Your ThreatLevel is compared to ground truth. Exact match = 100%. One level off = 50%. Two+ levels off = 0%. Correctly identifying Noise = bonus.

---

### 2. Pattern Detection → `Services/PatternDetector.cs`

Individual alerts tell part of the story. Patterns tell the rest.

```
Input:  Last 50 classified alerts (chronological)
Output: List of detected ThreatPatterns

ThreatPattern: PatternId, EscalatedLevel, AlertIds[], PatternDescription
```

**Think about:**
- Alerts sharing a `CorrelationGroup` are explicitly linked — group and escalate them.
- 3+ alerts in the same Sector within 2 minutes, even without correlation IDs — is that a pattern?
- An escalating trend: severity 2, then 4, then 6 in the same sector — worth flagging?

**Scoring:** Known compound threats exist in the stream. Each correctly detected pattern = major points. False detections = small penalty.

---

### 3. Response Engine → `Services/ResponseEngine.cs`

For every classified alert, decide what to do.

```
Input:  ClassifiedAlert + active ThreatPatterns
Output: ResponseAction (Action, Justification, Priority)

Action: Dismiss | Monitor | Escalate
Priority: 1 (highest) to 5 (lowest)
```

**Think about:**
- A Low alert that's part of an active compound pattern — Dismiss or Escalate?
- Over-escalation wastes resources (small penalty). Under-escalation misses threats (heavy penalty).
- Priority ordering: if three alerts need escalation simultaneously, which goes first?

**Scoring:** Correct action = full points. Over-escalation = -10%. Under-escalation = -30%.

---

### 🏅 HERO MISSIONS — Above and Beyond

The General gave you three orders. Exceptional teams go further. **Everything here counts in the debrief.** There is no predefined list of what's possible — these are ideas to spark your thinking:

**Sector Heat Map** — Visualize which sectors are under the most pressure. The UI has a `HeroPanel.razor` component ready for your additions. Bind data to it. Show the room where the action is.

**Predictive Alerting** — Based on patterns so far, can you predict which sector gets hit next? The SignalR connection has a `SubmitPrediction` method. Correct predictions = bonus points on the War Room.

**Source Reliability Tracking** — Not all sources are equally trustworthy. Track accuracy over time and weight your classification dynamically. Adaptive algorithms impress.

**Alert Deduplication** — The stream contains near-duplicates (same sector, same type, within seconds). Identify and merge them. Cleaner data = better decisions.

**Performance Tuning** — Phase 4 triples the volume. If your engine blocks or lags, your effectiveness drops visibly on the War Room. Profile and optimize.

**Unit Tests** — Write tests for your classification and pattern logic. Show them in your debrief. `ShieldWall.Tests/` has templates ready.

**Custom UI** — Charts, trend lines, alert timelines, escalation queues, analytics panels. The Blazor dashboard is yours to extend. Make it yours.

**Anything else you can think of.** If it serves the mission, build it.

---

## Rules of Engagement

1. **Use AI tools freely.** ChatGPT, Claude, GitHub Copilot — whatever you have access to. Using AI well is a skill we value. Be ready to explain how you used it and where your own judgment mattered.

2. **Watch your score.** Your Mission Effectiveness updates live on your dashboard and the War Room. If it drops, investigate why. Iterate. Improve.

3. **Everyone contributes.** Divide the work. One person on classification, one on patterns, one on response, others on hero missions, testing, or UI. The best teams have everyone's fingerprints on the code.

4. **Commit your work.** Push to your team branch regularly. Your code will be reviewed after the event.

5. **60 minutes.** When the stream ends, stop coding. Prepare your debrief.

---

## Debrief (After the Mission — 30 minutes)

Each team presents ~7 minutes to the command room. Suggested structure:

1. **Your Algorithm** (2 min) — How did you classify alerts? What was your scoring formula? Show the code.

2. **Pattern Detection** (1.5 min) — How did you find compound threats? Time windows? Correlation groups? Something creative?

3. **AI Usage** (1.5 min) — How did you use AI tools? What prompts worked? Where did AI help? Where did you override it?

4. **Innovations** (2 min) — What hero missions did you build? Demo the UI. Show the tests. What would you do with another hour?

---

## Project Structure

> See the full structure at the top of this document.

The three files you need to implement are in `src/ShieldWall.TeamKit/Services/`:

```
src/ShieldWall.TeamKit/Services/
├── AlertClassifier.cs      ← ★ IMPLEMENT THIS ★
├── PatternDetector.cs      ← ★ IMPLEMENT THIS ★
├── ResponseEngine.cs       ← ★ IMPLEMENT THIS ★
├── AlertPipeline.cs        ← Already wired — do not modify
└── SentinelConnection.cs   ← Already wired — do not modify
```

Test templates are in `tests/ShieldWall.Tests/TeamKit/`:

```
tests/ShieldWall.Tests/TeamKit/
├── AlertClassifierTests.cs
└── ResponseEngineTests.cs
```

---

## Useful Commands

| Command | Purpose |
|---------|---------|
| `dotnet run --project src/ShieldWall.GameMaster` | Start the GameMaster server |
| `dotnet run --project src/ShieldWall.TeamKit` | Run your TeamKit dashboard |
| `dotnet watch --project src/ShieldWall.TeamKit` | Run TeamKit with hot reload |
| `dotnet test` | Run all tests |
| `dotnet build` | Build the full solution |
| `git add -A && git commit -m "message" && git push` | Save your work |

---

## Good luck. The grid is counting on you.

