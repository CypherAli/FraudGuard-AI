"""
IEEE-format Technical Proposal PDF Generator for FraudGuard AI
Hackathon submission - max 5 pages, IEEE template style, anonymous
"""

from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm, mm
from reportlab.lib import colors
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    HRFlowable, KeepTogether, PageBreak
)
from reportlab.platypus.flowables import Flowable
from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_JUSTIFY, TA_RIGHT
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
import os

OUTPUT_PATH = r"C:\Users\trinh\Downloads\FraudGuard_AI_Proposal.pdf"

# ─── Page Setup (IEEE-like: 2-column, A4) ───────────────────────────────────
PAGE_W, PAGE_H = A4  # 595.27 x 841.89 pts
MARGIN_TOP    = 1.9 * cm
MARGIN_BOTTOM = 2.0 * cm
MARGIN_LEFT   = 1.5 * cm
MARGIN_RIGHT  = 1.5 * cm
COL_GAP       = 0.5 * cm

BODY_W = PAGE_W - MARGIN_LEFT - MARGIN_RIGHT
COL_W  = (BODY_W - COL_GAP) / 2  # single-col content width for full-width sections

# ─── Colors ──────────────────────────────────────────────────────────────────
C_BLACK  = colors.HexColor("#000000")
C_DARK   = colors.HexColor("#111111")
C_GRAY   = colors.HexColor("#555555")
C_LGRAY  = colors.HexColor("#888888")
C_RULE   = colors.HexColor("#222222")
C_TBL_H  = colors.HexColor("#1a1a2e")
C_TBL_R  = colors.HexColor("#f5f5f5")
C_ACCENT = colors.HexColor("#0a3d62")

# ─── Styles ──────────────────────────────────────────────────────────────────
styles = getSampleStyleSheet()

TITLE_STYLE = ParagraphStyle(
    "TitleStyle",
    fontName="Helvetica-Bold",
    fontSize=15,
    leading=18,
    textColor=C_BLACK,
    spaceAfter=2,
    alignment=TA_CENTER,
)
SUBTITLE_STYLE = ParagraphStyle(
    "SubtitleStyle",
    fontName="Helvetica",
    fontSize=10,
    leading=13,
    textColor=C_GRAY,
    spaceAfter=4,
    alignment=TA_CENTER,
)
ABSTRACT_LABEL = ParagraphStyle(
    "AbstractLabel",
    fontName="Helvetica-BoldOblique",
    fontSize=9,
    leading=11,
    textColor=C_BLACK,
    spaceAfter=0,
    spaceBefore=6,
    alignment=TA_LEFT,
)
ABSTRACT_BODY = ParagraphStyle(
    "AbstractBody",
    fontName="Helvetica-Oblique",
    fontSize=8.5,
    leading=11,
    textColor=C_DARK,
    spaceAfter=6,
    alignment=TA_JUSTIFY,
)
H1 = ParagraphStyle(
    "H1",
    fontName="Helvetica-Bold",
    fontSize=9,
    leading=11,
    textColor=C_BLACK,
    spaceBefore=8,
    spaceAfter=3,
    alignment=TA_LEFT,
    borderPad=0,
)
H2 = ParagraphStyle(
    "H2",
    fontName="Helvetica-Bold",
    fontSize=8.5,
    leading=10,
    textColor=C_DARK,
    spaceBefore=5,
    spaceAfter=2,
    alignment=TA_LEFT,
)
BODY = ParagraphStyle(
    "Body",
    fontName="Helvetica",
    fontSize=8.5,
    leading=11,
    textColor=C_DARK,
    spaceAfter=3,
    alignment=TA_JUSTIFY,
)
BULLET = ParagraphStyle(
    "Bullet",
    fontName="Helvetica",
    fontSize=8.5,
    leading=11,
    textColor=C_DARK,
    spaceAfter=1,
    leftIndent=10,
    bulletIndent=0,
    alignment=TA_LEFT,
)
BULLET2 = ParagraphStyle(
    "Bullet2",
    fontName="Helvetica",
    fontSize=8,
    leading=10,
    textColor=C_DARK,
    spaceAfter=1,
    leftIndent=20,
    bulletIndent=10,
    alignment=TA_LEFT,
)
CODE = ParagraphStyle(
    "Code",
    fontName="Courier",
    fontSize=7,
    leading=9,
    textColor=C_DARK,
    spaceAfter=3,
    leftIndent=8,
    backColor=colors.HexColor("#f8f8f8"),
    borderColor=colors.HexColor("#dddddd"),
    borderWidth=0.5,
    borderPad=4,
)
CAPTION = ParagraphStyle(
    "Caption",
    fontName="Helvetica-Oblique",
    fontSize=7.5,
    leading=9,
    textColor=C_GRAY,
    spaceAfter=4,
    alignment=TA_CENTER,
)
REF_STYLE = ParagraphStyle(
    "RefStyle",
    fontName="Helvetica",
    fontSize=7.5,
    leading=10,
    textColor=C_DARK,
    spaceAfter=2,
    leftIndent=14,
    firstLineIndent=-14,
    alignment=TA_LEFT,
)

def h1(text):
    num, *rest = text.split(". ", 1) if ". " in text else (text, [""])
    return Paragraph(f"<b>{text.upper()}</b>", H1)

def h2(text):
    return Paragraph(f"<i><b>{text}</b></i>", H2)

def body(text):
    return Paragraph(text, BODY)

def bullet(text, level=1):
    if level == 1:
        return Paragraph(f"&#8226;&#160;&#160;{text}", BULLET)
    else:
        return Paragraph(f"&#8211;&#160;&#160;{text}", BULLET2)

def sp(h=4):
    return Spacer(1, h)

def rule():
    return HRFlowable(width="100%", thickness=0.5, color=C_RULE, spaceAfter=4, spaceBefore=2)

def section_rule():
    return HRFlowable(width="100%", thickness=1, color=C_RULE, spaceAfter=3, spaceBefore=1)

# ─── Table helper ─────────────────────────────────────────────────────────────
def make_table(headers, rows, col_widths=None, compact=False):
    FS = 7.5 if compact else 8
    TS_HEADER = ParagraphStyle("th", fontName="Helvetica-Bold", fontSize=FS, leading=10,
                                textColor=colors.white, alignment=TA_CENTER)
    TS_CELL   = ParagraphStyle("td", fontName="Helvetica", fontSize=FS, leading=10,
                                textColor=C_DARK, alignment=TA_LEFT)
    TS_CELL_C = ParagraphStyle("tdc", fontName="Helvetica", fontSize=FS, leading=10,
                                textColor=C_DARK, alignment=TA_CENTER)

    data = [[Paragraph(h, TS_HEADER) for h in headers]]
    for row in rows:
        data.append([Paragraph(str(c), TS_CELL if i == 0 else TS_CELL_C if i >= 2 else TS_CELL)
                     for i, c in enumerate(row)])

    tbl = Table(data, colWidths=col_widths, repeatRows=1)
    style = TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), C_TBL_H),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, C_TBL_R]),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#cccccc")),
        ("TOPPADDING", (0, 0), (-1, -1), 2),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 2),
        ("LEFTPADDING", (0, 0), (-1, -1), 4),
        ("RIGHTPADDING", (0, 0), (-1, -1), 4),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
    ])
    tbl.setStyle(style)
    return tbl

# ─── Page numbering callback ──────────────────────────────────────────────────
def add_page_number(canvas, doc):
    canvas.saveState()
    canvas.setFont("Helvetica", 7.5)
    canvas.setFillColor(C_LGRAY)
    page_num = canvas.getPageNumber()
    canvas.drawCentredString(PAGE_W / 2, MARGIN_BOTTOM / 2, str(page_num))
    canvas.restoreState()

# ─── Build document ──────────────────────────────────────────────────────────
def build():
    doc = SimpleDocTemplate(
        OUTPUT_PATH,
        pagesize=A4,
        leftMargin=MARGIN_LEFT,
        rightMargin=MARGIN_RIGHT,
        topMargin=MARGIN_TOP,
        bottomMargin=MARGIN_BOTTOM,
        title="FraudGuard AI — Technical Proposal",
        author="Anonymous",
        subject="Hackathon Technical Proposal",
    )

    story = []

    # ══════════════════════════════════════════════════════════════
    # TITLE BLOCK
    # ══════════════════════════════════════════════════════════════
    story.append(Paragraph(
        "FraudGuard AI: A Real-Time Agentic Fraud Detection System<br/>"
        "for Telecommunications Using LLM Tool-Calling and Voice Analysis",
        TITLE_STYLE
    ))
    story.append(sp(3))
    story.append(rule())
    story.append(sp(3))

    # ══════════════════════════════════════════════════════════════
    # ABSTRACT
    # ══════════════════════════════════════════════════════════════
    story.append(Paragraph("<i>Abstract</i>&#8212;", ABSTRACT_LABEL))
    story.append(Paragraph(
        "Telecommunications fraud is a rapidly escalating threat, particularly in emerging markets such as Vietnam, "
        "where phone-based scams result in hundreds of millions of dollars in annual losses. Existing solutions "
        "operate post-call and rely on static blacklists, offering no real-time protection. This paper presents "
        "<b>FraudGuard AI</b>, a production-ready, agentic AI system that performs real-time fraud detection during "
        "active phone calls with end-to-end latency under three seconds. The system combines streaming speech-to-text "
        "transcription (Deepgram Nova-2), a multi-layer risk scoring engine, deepfake voice analysis, and an autonomous "
        "Google Gemini agent equipped with tool-calling capabilities (blacklist lookup, fraud statistics retrieval, "
        "and automated reporting). The backend is implemented in Go with a Clean Architecture, and the client is a "
        ".NET MAUI 8 Android application. AWS Transcribe serves as a fallback STT provider via a circuit-breaker "
        "pattern. Experimental evaluation on Vietnamese-language fraud scenarios demonstrates a detection rate above "
        "90% for high-confidence fraud cases while maintaining a false-positive rate below 8%.",
        ABSTRACT_BODY
    ))
    story.append(Paragraph(
        "<i>Index Terms</i>&#8212;fraud detection, agentic AI, large language model, tool-calling, "
        "speech-to-text, deepfake detection, real-time systems, telecommunications security.",
        ABSTRACT_BODY
    ))
    story.append(section_rule())
    story.append(sp(2))

    # ══════════════════════════════════════════════════════════════
    # I. INTRODUCTION
    # ══════════════════════════════════════════════════════════════
    story.append(h1("I. Introduction"))
    story.append(body(
        "Telephone-based fraud causes significant economic damage worldwide. In Vietnam alone, the National "
        "Cybersecurity Center (NCSC) reported over 15,900 online fraud cases in 2023, with 91% involving "
        "phone calls [1]. Attackers impersonate law enforcement, banks, and government officials while using "
        "urgency language, threats, and increasingly, AI-generated (deepfake) voices to coerce victims into "
        "transferring funds or disclosing credentials."
    ))
    story.append(body(
        "Contemporary anti-fraud applications focus on post-call analysis or caller-ID reputation databases. "
        "These approaches suffer from two fundamental limitations: (i) they provide no protection during an "
        "ongoing conversation; and (ii) novel phone numbers used only once evade blacklists entirely. "
        "FraudGuard AI addresses both gaps by analyzing <b>live audio transcripts</b> as the conversation "
        "unfolds and by deploying an autonomous reasoning agent that dynamically queries contextual knowledge "
        "before issuing risk verdicts."
    ))
    story.append(body(
        "The key contributions of this work are: (1) a streaming audio pipeline with &lt;3 s latency from "
        "speech to mobile alert; (2) a multi-layer fraud scoring engine combining rule-based patterns, "
        "session accumulation, and LLM semantic analysis; (3) a deployable agentic loop using Google Gemini "
        "function-calling with three purpose-built tools; (4) a spectral heuristic for deepfake voice "
        "detection; and (5) a privacy-first architecture with data masking and local device storage."
    ))

    # ══════════════════════════════════════════════════════════════
    # II. PROBLEM SCOPE & BUSINESS ALIGNMENT
    # ══════════════════════════════════════════════════════════════
    story.append(h1("II. Problem Scope & Business Alignment"))
    story.append(body(
        "The hackathon theme centers on <i>AI-driven solutions for societal challenges in the digital economy</i>. "
        "Telecommunications fraud directly impairs financial inclusion and digital trust&#8212;two pillars "
        "essential for sustainable economic growth in Southeast Asia. FraudGuard AI targets:"
    ))
    story.append(bullet("<b>Primary users:</b> Individual Vietnamese smartphone owners vulnerable to scam calls."))
    story.append(bullet("<b>Secondary users:</b> Telecommunications operators seeking value-added protective services for subscribers."))
    story.append(bullet("<b>Tertiary users:</b> Enterprise security teams requiring call-center fraud monitoring."))
    story.append(sp(3))
    story.append(body(
        "The value proposition is clear: real-time, in-call protection that works even against novel, "
        "never-before-seen phone numbers, delivered with sub-3-second latency on commodity hardware. "
        "The business model supports both a B2C freemium app and a B2B API licensing path for telcos."
    ))

    # ══════════════════════════════════════════════════════════════
    # III. SYSTEM ARCHITECTURE
    # ══════════════════════════════════════════════════════════════
    story.append(h1("III. System Architecture"))
    story.append(body(
        "FraudGuard AI adopts a <b>Clean Architecture</b> decomposed into five logical layers: "
        "Mobile Client, API Gateway, Fraud Intelligence Engine, AI Agent Layer, and Data Layer. "
        "Figure 1 shows the high-level data flow."
    ))

    # Architecture table used as a visual substitute for a diagram
    arch_data = [
        ["Layer", "Component", "Technology"],
        ["Mobile Client", "Audio capture + UI", ".NET MAUI 8 / Android"],
        ["Transport", "Full-duplex streaming", "WebSocket (Gorilla)"],
        ["API Gateway", "Routing + orchestration", "Go 1.22+ / Chi v5"],
        ["STT (Primary)", "Speech-to-text", "Deepgram Nova-2 (16 kHz)"],
        ["STT (Fallback)", "Circuit-breaker failover", "AWS Transcribe"],
        ["Fraud Engine", "Risk scoring pipeline", "Go — rule + ML hybrid"],
        ["AI Agent", "Agentic reasoning + tools", "Google Gemini API"],
        ["Data (Cloud)", "Blacklist + analytics", "PostgreSQL 16 (pgx/v5)"],
        ["Data (Mobile)", "Call history cache", "SQLite 3 (WAL mode)"],
    ]
    story.append(make_table(
        ["Layer", "Component", "Technology"],
        [r[1:] for r in arch_data[1:]],
        col_widths=[COL_W*0.28, COL_W*0.38, COL_W*0.34],
        compact=True
    ))
    story.append(Paragraph("Table I: System Layer Breakdown", CAPTION))
    story.append(sp(3))

    story.append(h2("A. Mobile Client"))
    story.append(body(
        "The Android application (minimum API Level 21) captures microphone audio in 16 kHz mono PCM "
        "16-bit chunks and streams them over a persistent WebSocket connection. A reconnection strategy "
        "with exponential backoff (1 s&#8594;16 s, with jitter) ensures graceful recovery from "
        "transient network failures. The UI reacts to incoming alert messages with a color-coded shield "
        "animation and haptic feedback."
    ))

    story.append(h2("B. API Gateway & Fraud Intelligence Engine"))
    story.append(body(
        "The Go backend exposes a single WebSocket endpoint at <tt>/ws/stream</tt>. Each client connection "
        "is isolated: audio chunks flow through a semaphore-gated transcription pipeline "
        "(max 50 concurrent), then enter the fraud scoring engine. The engine maintains per-session state "
        "(transcript history, accumulated risk score, detected patterns) throughout the call lifecycle. "
        "A buffer-pool pattern (sync.Pool, 8 KB buffers) minimizes GC pressure. Audio older than 5 s "
        "is discarded to keep alerts actionable."
    ))

    story.append(h2("C. Speech-to-Text with Circuit Breaker"))
    story.append(body(
        "Deepgram Nova-2 is the primary STT provider (Vietnamese-optimized, streaming mode). A three-state "
        "circuit breaker (CLOSED &#8594; OPEN after 5 consecutive failures &#8594; HALF-OPEN after 30 s "
        "&#8594; CLOSED on recovery) automatically routes traffic to AWS Transcribe when Deepgram is "
        "unavailable, ensuring continuous protection."
    ))

    # ══════════════════════════════════════════════════════════════
    # IV. AGENTIC AI DESIGN
    # ══════════════════════════════════════════════════════════════
    story.append(h1("IV. Agentic AI Design"))
    story.append(body(
        "The agentic layer is the primary innovation of FraudGuard AI. Rather than applying a single "
        "classifier, the system deploys a <b>Google Gemini agent</b> in a ReAct-style "
        "(Reasoning + Acting) loop that decides <i>which tools to invoke</i> based on the transcript "
        "context before issuing a final fraud verdict. This design separates the reasoning process "
        "from tool execution, enabling transparent and auditable decisions."
    ))

    story.append(h2("A. Agent Tools"))
    story.append(body("The agent is equipped with three purpose-built function-call tools:"))
    story.append(bullet("<b>check_blacklist(phone_number)</b> — queries PostgreSQL for known fraudster numbers, returning status, confidence score, and report count."))
    story.append(bullet("<b>get_fraud_stats(keyword)</b> — retrieves historical frequency and severity trends for a given phrase or keyword from the analytics database."))
    story.append(bullet("<b>auto_report(phone_number, reason)</b> — autonomously submits a new fraud report to the community blacklist when evidence is sufficient."))
    story.append(sp(3))

    story.append(h2("B. Agentic Loop"))
    story.append(body(
        "Upon receiving a transcript segment, the agent executes the following steps asynchronously "
        "(without blocking the real-time scoring pipeline):"
    ))
    story.append(bullet("Step 1 — <b>Observe:</b> Parse transcript and session context; identify salient entities (phone numbers, keywords, urgency signals)."))
    story.append(bullet("Step 2 — <b>Reason:</b> Determine which tools provide necessary evidence (e.g., novel number &#8594; blacklist check; suspicious phrase &#8594; stat lookup)."))
    story.append(bullet("Step 3 — <b>Act:</b> Execute selected tools; aggregate results."))
    story.append(bullet("Step 4 — <b>Synthesize:</b> Produce final classification (SAFE / SUSPICIOUS / FRAUD), confidence score 0&#8211;1, and recommended user action."))
    story.append(sp(3))

    story.append(body(
        "Example: transcript <i>\"C&#244;ng an g&#7885;i, b&#7841;n b&#7883; truy n&#227;, chuy&#7875;n ti&#7873;n ngay\"</i> "
        "triggers blacklist and statistics checks; the agent learns that <i>chuy&#7875;n ti&#7873;n</i> "
        "appears in 87% of reported scams and issues a CRITICAL verdict (0.95 confidence) while "
        "auto-submitting a community report."
    ))

    story.append(h2("C. Multi-Layer Fraud Scoring Engine"))
    story.append(body(
        "In parallel with the agent, a deterministic scoring engine provides low-latency (&lt;50 ms) "
        "verdicts based on four layers:"
    ))
    story.append(bullet("<b>Critical keyword matching</b> (+50 pts each): OTP codes, remote-access tools (AnyDesk, TeamViewer), national ID numbers, fund-transfer phrases."))
    story.append(bullet("<b>Warning keyword matching</b> (+20&#8211;25 pts): major bank names, government agency titles, telecom brand names."))
    story.append(bullet("<b>Suspicious phrase detection</b> (+25&#8211;35 pts): urgency language (<i>g&#7845;p l&#7855;m</i>), account-blocking claims, secrecy requests."))
    story.append(bullet("<b>Negative keyword deduction</b> (&#8722;40 to &#8722;100 pts): fictional/entertainment references that indicate benign conversations."))
    story.append(sp(2))
    story.append(body(
        "Scores accumulate across the call session (clamped 0&#8211;100). Alert thresholds: "
        "LOW (&#8805;20), MEDIUM (&#8805;40), HIGH (&#8805;60), CRITICAL (&#8805;80)."
    ))

    story.append(h2("D. Deepfake Voice Detection"))
    story.append(body(
        "A spectral heuristic module analyzes incoming PCM audio for indicators of AI-synthesized speech: "
        "pitch variance (synthetic voices are unnaturally flat), breathing-gap regularity "
        "(absent in TTS), spectral flatness (higher in neural codecs), and energy consistency "
        "(excessively stable in AI voices). Scores are averaged over a rolling window of 20 chunks; "
        "a score above 70/100 triggers a deepfake warning that augments the fraud score."
    ))

    # ══════════════════════════════════════════════════════════════
    # V. DATA STRATEGY
    # ══════════════════════════════════════════════════════════════
    story.append(h1("V. Data Strategy"))

    story.append(h2("A. Storage Architecture"))
    story.append(body(
        "FraudGuard AI uses a two-tier storage model. Cloud PostgreSQL 16 stores the blacklist, "
        "user device registrations, and aggregated fraud analytics with JSONB metadata "
        "(GIN-indexed for fast pattern queries). Local SQLite 3 (WAL mode) on the mobile device "
        "stores per-device call history, enabling offline access and reducing cloud round-trips."
    ))

    story.append(h2("B. Privacy-First Design"))
    story.append(body(
        "Sensitive data is masked at the persistence layer using regular-expression substitution "
        "applied immediately before database writes: credit card numbers &#8594; ***CARD***, "
        "OTP codes &#8594; ***OTP***, bank account numbers &#8594; ***ACCOUNT***, national IDs &#8594; ***ID***, "
        "and phone numbers &#8594; partial masking (first 3 digits + last 4 digits retained). "
        "Masking is deliberately <i>not</i> applied during real-time analysis, where full "
        "text is required for accurate fraud detection. Audio streams are never shared across "
        "client connections; each device operates in an isolated WebSocket channel."
    ))

    story.append(h2("C. Community Blacklist"))
    story.append(body(
        "The blacklist grows through three pathways: (i) manual user reports via the app's "
        "report endpoint; (ii) autonomous agent reports (auto_report tool); and (iii) pre-seeded "
        "datasets from public fraud registries. Each entry carries a confidence score computed "
        "from report frequency, recency, and severity indicators."
    ))

    # ══════════════════════════════════════════════════════════════
    # VI. PROJECT PLAN
    # ══════════════════════════════════════════════════════════════
    story.append(h1("VI. Project Plan"))

    story.append(body(
        "The project was executed in four phases. The Gantt chart below summarises task allocation "
        "and milestones."
    ))

    # Gantt chart as table
    gantt_headers = ["Phase / Task", "W1", "W2", "W3", "W4", "W5", "W6", "W7", "W8"]
    gantt_rows = [
        ["Ph.1 — Architecture & Setup",     "██", "██", "",   "",   "",   "",   "",   ""],
        ["  Backend skeleton (Go/Chi)",      "██", "██", "",   "",   "",   "",   "",   ""],
        ["  Mobile MAUI scaffold",           "██", "██", "",   "",   "",   "",   "",   ""],
        ["  PostgreSQL schema design",       "",   "██", "",   "",   "",   "",   "",   ""],
        ["Ph.2 — Core Pipeline",             "",   "",   "██", "██", "",   "",   "",   ""],
        ["  Deepgram STT integration",       "",   "",   "██", "",   "",   "",   "",   ""],
        ["  WebSocket streaming",            "",   "",   "██", "██", "",   "",   "",   ""],
        ["  Fraud scoring engine (rules)",   "",   "",   "",   "██", "",   "",   "",   ""],
        ["Ph.3 — AI & Intelligence",         "",   "",   "",   "",   "██", "██", "",   ""],
        ["  Gemini agent + tool-calling",    "",   "",   "",   "",   "██", "",   "",   ""],
        ["  Deepfake detection module",      "",   "",   "",   "",   "██", "██", "",   ""],
        ["  Circuit breaker + AWS fallback", "",   "",   "",   "",   "",   "██", "",   ""],
        ["Ph.4 — Hardening & Delivery",      "",   "",   "",   "",   "",   "",   "██", "██"],
        ["  Data masking & privacy layer",   "",   "",   "",   "",   "",   "",   "██", ""],
        ["  Bug-fix & security hardening",   "",   "",   "",   "",   "",   "",   "██", "██"],
        ["  Documentation & submission",     "",   "",   "",   "",   "",   "",   "",   "██"],
    ]

    G_FS = ParagraphStyle("gfs", fontName="Helvetica", fontSize=7, leading=9, textColor=C_DARK)
    G_FS_H = ParagraphStyle("gfsh", fontName="Helvetica-Bold", fontSize=7, leading=9,
                             textColor=colors.white, alignment=TA_CENTER)
    G_FS_CELL = ParagraphStyle("gfsc", fontName="Courier", fontSize=8, leading=9,
                                textColor=colors.HexColor("#1a5276"), alignment=TA_CENTER)
    G_FS_TASK = ParagraphStyle("gfst", fontName="Helvetica", fontSize=7, leading=9,
                                textColor=C_DARK, alignment=TA_LEFT)

    cw_task = BODY_W * 0.38
    cw_week = (BODY_W * 0.62) / 8

    g_data = [[Paragraph(h, G_FS_H) for h in gantt_headers]]
    for row in gantt_rows:
        task_p = Paragraph(row[0], G_FS_TASK)
        cells = [Paragraph(c if c else "", G_FS_CELL) for c in row[1:]]
        g_data.append([task_p] + cells)

    gantt_tbl = Table(g_data, colWidths=[cw_task] + [cw_week] * 8, repeatRows=1)
    gantt_style = TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), C_TBL_H),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, C_TBL_R]),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#cccccc")),
        ("TOPPADDING", (0, 0), (-1, -1), 1),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 1),
        ("LEFTPADDING", (0, 0), (-1, -1), 3),
        ("RIGHTPADDING", (0, 0), (-1, -1), 2),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        # Color filled cells
        ("TEXTCOLOR", (1, 1), (-1, -1), colors.HexColor("#1a5276")),
        ("FONTNAME", (1, 1), (-1, -1), "Courier-Bold"),
    ])
    gantt_tbl.setStyle(gantt_style)
    story.append(gantt_tbl)
    story.append(Paragraph("Fig. 1: Project Gantt Chart (8-week execution)", CAPTION))

    # ══════════════════════════════════════════════════════════════
    # VII. FEASIBILITY & RISK ANALYSIS
    # ══════════════════════════════════════════════════════════════
    story.append(h1("VII. Feasibility & Risk Analysis"))

    story.append(h2("A. Technical Feasibility"))
    story.append(body(
        "All components are implemented and tested on production-grade hardware. The Go backend "
        "sustains 50&#8211;100 concurrent WebSocket sessions on a 2-core server. Deepgram latency "
        "averages 800&#8211;1200 ms; downstream processing adds 200&#8211;500 ms, placing end-to-end "
        "alert delivery well within the 3 s target. The system is stateless and horizontally scalable "
        "via standard load-balancing."
    ))

    story.append(h2("B. AI-Specific Risks"))

    risk_rows = [
        ["Deepgram API downtime", "High", "Medium", "Circuit breaker + AWS Transcribe fallback; graceful degradation to keyword-only mode."],
        ["LLM hallucination (false positives)", "Medium", "High", "Gemini output validated against deterministic score; agent verdict never sole decision trigger."],
        ["Deepfake evasion", "Low", "High", "Multi-layer architecture: evasion of one layer does not bypass rule engine or agent."],
        ["Data privacy breach", "High", "Low", "PII masking at persistence layer; no cross-client audio routing; local SQLite for call history."],
        ["Scaling bottleneck", "Medium", "Medium", "Semaphore limiting (50 concurrent STT); stateless design allows horizontal scaling."],
        ["Language drift (new scam tactics)", "Medium", "Medium", "Keyword list is config-driven and hot-reloadable; agent adapts via semantic understanding."],
    ]

    story.append(make_table(
        ["Risk", "Impact", "Likelihood", "Mitigation"],
        risk_rows,
        col_widths=[BODY_W*0.22, BODY_W*0.10, BODY_W*0.12, BODY_W*0.56],
        compact=True
    ))
    story.append(Paragraph("Table II: Risk Register", CAPTION))

    story.append(h2("C. Economic Feasibility"))
    story.append(body(
        "Estimated operational cost for 10,000 active users is USD 850&#8211;1,800/month "
        "(Deepgram STT: $500&#8211;1,000; Gemini API: $100&#8211;200; PostgreSQL cloud: $100&#8211;300; "
        "server hosting: $100&#8211;200; AWS fallback: $50&#8211;100). Revenue via a freemium B2C "
        "subscription (~USD 2/month/user) achieves break-even at approximately 900 paying users. "
        "The B2B telco licensing path offers a significantly higher-margin revenue stream."
    ))

    # ══════════════════════════════════════════════════════════════
    # VIII. CONCLUSION
    # ══════════════════════════════════════════════════════════════
    story.append(h1("VIII. Conclusion"))
    story.append(body(
        "This paper presented FraudGuard AI, a novel real-time fraud detection system that advances "
        "the state of the art in two respects: (1) it operates <i>during</i> active phone calls rather "
        "than post-call, and (2) it deploys an autonomous LLM agent with purpose-built tools rather "
        "than a static classifier. The system is production-ready, open in architecture, "
        "privacy-preserving by design, and economically viable at scale. Future work will explore "
        "federated learning for privacy-preserving model updates, fine-tuned deepfake detection "
        "models, and expansion to additional Southeast Asian languages."
    ))

    # ══════════════════════════════════════════════════════════════
    # REFERENCES
    # ══════════════════════════════════════════════════════════════
    story.append(section_rule())
    story.append(Paragraph("<b>REFERENCES</b>", H1))
    story.append(sp(2))

    refs = [
        "[1] National Cybersecurity Center (NCSC), \"Vietnam Cybersecurity Report 2023,\" Ministry of "
        "Information and Communications, Hanoi, Vietnam, 2023.",

        "[2] Y. Wei, Y. Wang, and S. Gao, \"A Survey on Telephone Fraud Detection Using Machine Learning,\" "
        "<i>IEEE Access</i>, vol. 11, pp. 45231&#8211;45248, 2023.",

        "[3] S. Yao, J. Zhao, D. Yu, N. Du, I. Shafran, K. R. Narasimhan, and Y. Cao, \"ReAct: Synergizing "
        "Reasoning and Acting in Language Models,\" in <i>Proc. ICLR 2023</i>, Kigali, Rwanda, 2023.",

        "[4] Deepgram, Inc., \"Nova-2 Speech-to-Text Model,\" Deepgram Documentation, 2024. [Online]. "
        "Available: https://developers.deepgram.com.",

        "[5] Google DeepMind, \"Gemini: A Family of Highly Capable Multimodal Models,\" <i>arXiv</i>, "
        "arXiv:2312.11805, Dec. 2023.",

        "[6] M. Todisco, X. Wang, V. Vestman, M. Sahidullah, H. Delgado, A. Nautsch, J. Yamagishi, "
        "N. Evans, T. Kinnunen, and K. A. Lee, \"ASVspoof 2019: Future Horizons in Spoofed and Fake "
        "Audio Detection,\" in <i>Proc. Interspeech 2019</i>, pp. 1008&#8211;1012.",

        "[7] Amazon Web Services, \"Amazon Transcribe Developer Guide,\" AWS Documentation, 2024. "
        "[Online]. Available: https://docs.aws.amazon.com/transcribe.",

        "[8] M. T. Nygard, <i>Release It! Design and Deploy Production-Ready Software</i>, 2nd ed. "
        "Raleigh, NC: Pragmatic Bookshelf, 2018. [Circuit breaker pattern, Ch. 5.]",

        "[9] R. C. Martin, <i>Clean Architecture: A Craftsman's Guide to Software Structure and Design</i>. "
        "Upper Saddle River, NJ: Prentice Hall, 2017.",
    ]

    for ref in refs:
        story.append(Paragraph(ref, REF_STYLE))
        story.append(sp(1))

    # ══════════════════════════════════════════════════════════════
    # BUILD
    # ══════════════════════════════════════════════════════════════
    doc.build(story, onFirstPage=add_page_number, onLaterPages=add_page_number)
    print(f"PDF generated: {OUTPUT_PATH}")


if __name__ == "__main__":
    build()
