# 🛡️ Operation Shield Wall — Team Kit

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

## Quick Start

1. **Set your team name** — edit `src/ShieldWall.TeamKit/appsettings.json`:
   `json
   {
     "Team": { "Name": "YourTeamName" }
   }
   `

2. **Build and run**:
   `ash
   dotnet run --project src/ShieldWall.TeamKit
   `

3. **Open your browser**: http://localhost:5200

4. **Verify connection**: You should see `● Connected — YourTeamName` in green at the top.

5. **Your mission**: Improve the 3 files in `src/ShieldWall.TeamKit/Services/`:
   - `AlertClassifier.cs` — Classify incoming threat alerts
   - `PatternDetector.cs` — Detect compound threat patterns
   - `ResponseEngine.cs` — Decide the response action

> **Hot reload**: Save your changes, stop with `Ctrl+C`, re-run `dotnet run`.
> Your team stays registered — the server remembers you.

---
# 🛡️ OPERATION SHIELD WALL

## Mission Brief

The Sentinel Grid — Tactical Edge Technologies' distributed monitoring and alert processing network — has lost its AI-driven classification layer. Raw sensor data is streaming in from stations across the grid, but there is no brain to process it.

Threats are going unclassified. Patterns are being missed. The control room is blind.

Four field teams have been activated. Your mission: **build the Threat Assessment Engine** — the logic that processes incoming alerts, scores their severity, detects compound threat patterns, and recommends response actions.

The War Room is watching. Your Mission Effectiveness score updates live. You have **60 minutes**.

---

## Quick Start

```bash
# 1. Clone the repo and switch to your team branch
git clone <REPO_URL>
cd operation-shield-wall
git checkout team/<your-team-name>

# 2. Run the setup
dotnet run --project ShieldWall.TeamKit -- setup

# 3. Your dashboard opens at http://localhost:5100
#    You should see "Connected to Sentinel Grid" in the status panel.

# 4. Wait for the facilitator to start the alert stream.

# 5. Open your code and start implementing:
#    ShieldWall.TeamKit/Services/AlertClassifier.cs
#    ShieldWall.TeamKit/Services/PatternDetector.cs
#    ShieldWall.TeamKit/Services/ResponseEngine.cs
```

**Hot reload:** Use `dotnet watch` instead of `dotnet run` so your dashboard restarts automatically when you save code changes.

```bash
dotnet watch --project ShieldWall.TeamKit
```

**Run tests:**
```bash
dotnet test
```

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

```
ShieldWall/
├── ShieldWall.GameMaster/          ← Runs on the server (don't modify)
├── ShieldWall.Shared/              ← Models and interfaces (read, don't modify)
│   ├── Models/
│   │   ├── SentinelAlert.cs        ← The alert data model
│   │   ├── ClassifiedAlert.cs      ← Your classification output
│   │   ├── ThreatPattern.cs        ← Your pattern detection output
│   │   ├── ResponseAction.cs       ← Your response decision output
│   │   └── Enums.cs                ← ThreatLevel, AlertType, ActionType
│   └── Interfaces/
│       ├── IAlertClassifier.cs     ← Interface you implement
│       ├── IPatternDetector.cs     ← Interface you implement
│       └── IResponseEngine.cs      ← Interface you implement
│
├── ShieldWall.TeamKit/             ← YOUR WORKSPACE
│   ├── Services/
│   │   ├── AlertClassifier.cs      ← ★ IMPLEMENT THIS ★
│   │   ├── PatternDetector.cs      ← ★ IMPLEMENT THIS ★
│   │   ├── ResponseEngine.cs       ← ★ IMPLEMENT THIS ★
│   │   ├── SentinelConnection.cs   ← SignalR client (already wired)
│   │   ├── AlertPipeline.cs        ← Processing pipeline (already wired)
│   │   └── AlertHistoryBuffer.cs   ← Stores last 50 alerts (already wired)
│   ├── Components/
│   │   └── Pages/
│   │       ├── Dashboard.razor     ← Main dashboard (already built)
│   │       └── HeroPanel.razor     ← Placeholder for your additions
│   └── wwwroot/                    ← Static assets, CSS
│
├── ShieldWall.Tests/               ← Write your tests here
│   ├── ClassifierTests.cs          ← Template with example test structure
│   ├── PatternDetectorTests.cs
│   └── ResponseEngineTests.cs
│
└── ShieldWall.sln
```

---

## Useful Commands

| Command | Purpose |
|---------|---------|
| `dotnet watch --project ShieldWall.TeamKit` | Run with hot reload |
| `dotnet run --project ShieldWall.TeamKit` | Run without hot reload |
| `dotnet test` | Run your unit tests |
| `dotnet build` | Build without running |
| `git add -A && git commit -m "message" && git push` | Save your work |

---

## Good luck. The grid is counting on you.

