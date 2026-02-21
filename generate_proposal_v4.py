"""
FraudGuard AI - IEEE Technical Proposal v4
- SINGLE COLUMN, easy to read
- Larger font, clean spacing
- Accurate diagrams only (no fluff)
- Concise, to the point
- Strict anonymity, max 5 pages
"""

from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import cm, mm
from reportlab.lib import colors
from reportlab.platypus import (
    BaseDocTemplate, Frame, PageTemplate,
    Paragraph, Spacer, Table, TableStyle,
    HRFlowable, KeepTogether, Flowable
)
from reportlab.lib.enums import TA_LEFT, TA_CENTER, TA_JUSTIFY, TA_RIGHT
import math

OUTPUT = r"C:\Users\trinh\Downloads\FraudGuard_AI_Proposal_v4.pdf"

PW, PH = A4
ML = 2.2*cm
MR = 2.2*cm
MT = 2.0*cm
MB = 1.8*cm
TW = PW - ML - MR   # text width ~16.6 cm

# ── Colours ───────────────────────────────────────────────────────────────────
NAVY   = colors.HexColor("#0d2137")
TEAL   = colors.HexColor("#0f6b8e")
ACCENT = colors.HexColor("#e8f4f9")
DARK   = colors.HexColor("#1a1a1a")
GRAY   = colors.HexColor("#4a4a4a")
LGRAY  = colors.HexColor("#888888")
WHITE  = colors.white

# ── Styles ────────────────────────────────────────────────────────────────────
def S(n, **k): return ParagraphStyle(n, **k)

TITLE = S("T",  fontName="Helvetica-Bold",        fontSize=16, leading=20, textColor=NAVY, alignment=TA_CENTER, spaceAfter=6)
ABSL  = S("AL", fontName="Helvetica-BoldOblique", fontSize=9.5, leading=13, textColor=NAVY, spaceAfter=0, spaceBefore=6)
ABST  = S("AB", fontName="Helvetica-Oblique",     fontSize=9.5, leading=13.5, textColor=DARK, spaceAfter=6, alignment=TA_JUSTIFY)
KW    = S("KW", fontName="Helvetica-Oblique",     fontSize=9,   leading=12, textColor=GRAY, spaceAfter=8, alignment=TA_JUSTIFY)
BD    = S("BD", fontName="Helvetica",             fontSize=9.5, leading=13.5, textColor=DARK, spaceAfter=5, alignment=TA_JUSTIFY)
BUL   = S("BL", fontName="Helvetica",             fontSize=9.5, leading=13,  textColor=DARK, spaceAfter=3, leftIndent=14)
BUL2  = S("B2", fontName="Helvetica",             fontSize=9,   leading=12,  textColor=GRAY, spaceAfter=2, leftIndent=28)
CAP   = S("CP", fontName="Helvetica-Oblique",     fontSize=8.5, leading=11, textColor=LGRAY, spaceAfter=6, alignment=TA_CENTER)
REF   = S("RF", fontName="Helvetica",             fontSize=8.5, leading=11.5, textColor=DARK, spaceAfter=3, leftIndent=16, firstLineIndent=-16)

def p(t, sty=BD): return Paragraph(t, sty)
def sp(n=5): return Spacer(1, n)
def bul(t, lv=1):
    sym = "&#8226;" if lv == 1 else "&#8211;"
    sty = BUL if lv == 1 else BUL2
    return Paragraph(f"{sym}&#8194;{t}", sty)
def hr(col=LGRAY, w=0.5):
    return HRFlowable(width="100%", thickness=w, color=col, spaceBefore=4, spaceAfter=6)


# ── Section heading ───────────────────────────────────────────────────────────
class SecHead(Flowable):
    def __init__(self, num, title, w):
        Flowable.__init__(self)
        self.num, self.title, self.width = num, title, w
        self.height = 18

    def draw(self):
        c = self.canv
        c.setFillColor(ACCENT)
        c.setStrokeColor(colors.white); c.setLineWidth(0)
        c.rect(0, 0, self.width, self.height-1, fill=1, stroke=0)
        c.setFillColor(TEAL)
        c.rect(0, 0, 4, self.height-1, fill=1, stroke=0)
        c.setFont("Helvetica-Bold", 10.5)
        c.setFillColor(NAVY)
        label = f"{self.num}. {self.title.upper()}" if self.num else self.title.upper()
        c.drawString(9, 5, label)

    def wrap(self, aw, ah): return self.width, self.height + 2


def sec(num, title): return SecHead(num, title, TW)


# ── Table helper ──────────────────────────────────────────────────────────────
def mktbl(headers, rows, widths=None, fs=9):
    TH = S("th", fontName="Helvetica-Bold", fontSize=fs, leading=fs+2.5, textColor=WHITE, alignment=TA_CENTER)
    TD = S("td", fontName="Helvetica",      fontSize=fs, leading=fs+2.5, textColor=DARK,  alignment=TA_LEFT)
    TC = S("tc", fontName="Helvetica",      fontSize=fs, leading=fs+2.5, textColor=DARK,  alignment=TA_CENTER)

    data = [[Paragraph(h, TH) for h in headers]]
    for i, row in enumerate(rows):
        bg = WHITE if i % 2 == 0 else colors.HexColor("#f2f6fa")
        data.append([Paragraph(str(c), TC if j > 0 else TD) for j, c in enumerate(row)])

    t = Table(data, colWidths=widths, repeatRows=1)
    t.setStyle(TableStyle([
        ("BACKGROUND",    (0,0), (-1,0),  NAVY),
        ("ROWBACKGROUNDS",(0,1), (-1,-1), [WHITE, colors.HexColor("#f2f6fa")]),
        ("GRID",          (0,0), (-1,-1), 0.4, colors.HexColor("#c8d6e0")),
        ("TOPPADDING",    (0,0), (-1,-1), 4),
        ("BOTTOMPADDING", (0,0), (-1,-1), 4),
        ("LEFTPADDING",   (0,0), (-1,-1), 6),
        ("RIGHTPADDING",  (0,0), (-1,-1), 5),
        ("VALIGN",        (0,0), (-1,-1), "MIDDLE"),
    ]))
    return t


# ── Architecture Diagram ──────────────────────────────────────────────────────
class ArchDiagram(Flowable):
    """Accurate 3-row component diagram with real arrows."""
    def __init__(self, w, h):
        Flowable.__init__(self)
        self.width, self.height = w, h

    def draw(self):
        c = self.canv
        W, H = self.width, self.height

        # background
        c.setFillColor(colors.HexColor("#f5f8fb"))
        c.setStrokeColor(colors.HexColor("#c0cedb"))
        c.setLineWidth(0.6)
        c.roundRect(0, 0, W, H, 5, fill=1, stroke=1)

        def box(x, y, w, h, lines, bg, r=4, fs=8.5, bold=True):
            c.setFillColor(bg)
            c.setStrokeColor(colors.HexColor("#8aaabb"))
            c.setLineWidth(0.5)
            c.roundRect(x, y, w, h, r, fill=1, stroke=1)
            fg = WHITE if bg == NAVY or bg.hexval() in ("#1b4f72ff","#145a32ff","#6e2f1aff","#2e4057ff") else DARK
            c.setFillColor(fg)
            fn = "Helvetica-Bold" if bold else "Helvetica"
            lh = fs + 2
            oy = y + h/2 + (len(lines)-1)*lh/2 - lh*0.35
            for ln in lines:
                c.setFont(fn, fs)
                c.drawCentredString(x + w/2, oy, ln)
                oy -= lh

        def arr(x1, y1, x2, y2, lbl="", col=TEAL):
            c.setStrokeColor(col); c.setFillColor(col); c.setLineWidth(1)
            c.line(x1, y1, x2, y2)
            a = math.atan2(y2-y1, x2-x1)
            sz = 5
            px = c.beginPath()
            px.moveTo(x2, y2)
            px.lineTo(x2-sz*math.cos(a-0.4), y2-sz*math.sin(a-0.4))
            px.lineTo(x2-sz*math.cos(a+0.4), y2-sz*math.sin(a+0.4))
            px.close()
            c.drawPath(px, fill=1, stroke=0)
            if lbl:
                c.setFont("Helvetica", 6.5); c.setFillColor(LGRAY)
                c.drawCentredString((x1+x2)/2, (y1+y2)/2+3, lbl)

        pad  = 8
        n    = 3
        cw   = (W - pad*(n+1)) / n
        bh   = 24   # box height
        gap  = 22   # vertical gap between rows
        leg  = 14   # legend bar at bottom

        # Build rows bottom-up so nothing clips
        row2_y = leg + pad               # bottom row Y
        row1_y = row2_y + bh + gap       # middle row Y
        row0_y = row1_y + bh + gap       # top row Y
        row    = [row0_y, row1_y, row2_y]
        xs     = [pad + i*(cw+pad) for i in range(n)]

        # Row 0: client | gateway | DB
        box(xs[0], row[0], cw, bh, ["Mobile Client", "(.NET MAUI 8 / Android)"], NAVY)
        box(xs[1], row[0], cw, bh, ["API Gateway",   "(Go 1.22 / Chi v5)"],      NAVY)
        box(xs[2], row[0], cw, bh, ["PostgreSQL 16", "(Cloud DB)"],               NAVY)
        # h-arrows row0
        arr(xs[0]+cw, row[0]+bh/2, xs[1], row[0]+bh/2, "WebSocket (PCM audio)")
        arr(xs[1]+cw, row[0]+bh/2, xs[2], row[0]+bh/2, "pgx/v5")

        # Row 1: STT | Fraud Engine | Gemini Agent
        box(xs[0], row[1], cw, bh, ["Deepgram Nova-2", "(Primary STT)"],          colors.HexColor("#1b4f72"))
        box(xs[1], row[1], cw, bh, ["Fraud Engine",    "(Rule + Session Score)"], colors.HexColor("#145a32"))
        box(xs[2], row[1], cw, bh, ["Gemini Agent",    "(ReAct + 3 Tools)"],      colors.HexColor("#6e2f1a"))
        # v-arrows row0&#8594;row1
        arr(xs[0]+cw/2, row[0],    xs[0]+cw/2, row[1]+bh, "16 kHz PCM")
        arr(xs[1]+cw/2, row[0],    xs[1]+cw/2, row[1]+bh, "transcript")
        # h-arrow: Fraud &#8594; Agent
        arr(xs[1]+cw, row[1]+bh/2, xs[2], row[1]+bh/2,    "context + score")

        # Row 2: AWS Transcribe | SQLite | Blacklist DB
        box(xs[0], row[2], cw, bh, ["AWS Transcribe",  "(Circuit-Breaker Fallback)"], colors.HexColor("#2e4057"))
        box(xs[1], row[2], cw, bh, ["SQLite 3 Local",  "(Call History / WAL)"],       colors.HexColor("#2e4057"))
        box(xs[2], row[2], cw, bh, ["Blacklist DB",     "(Community Reports)"],        colors.HexColor("#2e4057"))
        # v-arrows row1&#8594;row2
        arr(xs[0]+cw/2, row[1],    xs[0]+cw/2, row[2]+bh, "failover")
        arr(xs[1]+cw/2, row[1],    xs[1]+cw/2, row[2]+bh, "save log")
        arr(xs[2]+cw/2, row[1],    xs[2]+cw/2, row[2]+bh, "check / report")

        # legend
        c.setFont("Helvetica", 7); c.setFillColor(LGRAY)
        c.drawCentredString(W/2, 5,
            "End-to-end latency < 3 s  |  "
            "Circuit-breaker: Deepgram -> AWS  |  "
            "Agent tools: check_blacklist, get_fraud_stats, auto_report")

    def wrap(self, aw, ah): return self.width, self.height


# ── Gantt Chart ───────────────────────────────────────────────────────────────
class GanttChart(Flowable):
    TASKS = [
        (1, "Phase 1: Architecture & Setup",    1.0, 2.0, True),
        (1, "  Backend (Go/Chi/WebSocket)",      1.0, 2.0, False),
        (1, "  Mobile MAUI scaffold",            1.0, 1.5, False),
        (1, "  PostgreSQL schema + auth",        1.5, 1.5, False),
        (2, "Phase 2: Core Pipeline",            3.0, 2.0, True),
        (2, "  Deepgram STT integration",        3.0, 1.5, False),
        (2, "  Fraud scoring engine (rules)",    3.5, 1.5, False),
        (2, "  WebSocket real-time streaming",   3.0, 2.0, False),
        (3, "Phase 3: AI & Intelligence",        5.0, 2.0, True),
        (3, "  Gemini agent + 3 tools",          5.0, 1.5, False),
        (3, "  Deepfake detection (spectral)",   5.0, 2.0, False),
        (3, "  Circuit breaker + AWS fallback",  6.0, 1.0, False),
        (4, "Phase 4: Hardening & Delivery",     7.0, 2.0, True),
        (4, "  PII masking + privacy layer",     7.0, 1.0, False),
        (4, "  Security hardening + bug-fix",    7.0, 2.0, False),
        (4, "  Documentation & submission",      8.0, 1.0, False),
    ]
    WEEKS = 8
    COLS  = {1: colors.HexColor("#1565c0"),
             2: colors.HexColor("#2e7d32"),
             3: colors.HexColor("#c62828"),
             4: colors.HexColor("#e65100")}
    LITE  = {1: colors.HexColor("#bbdefb"),
             2: colors.HexColor("#c8e6c9"),
             3: colors.HexColor("#ffcdd2"),
             4: colors.HexColor("#ffe0b2")}

    def __init__(self, w, h):
        Flowable.__init__(self)
        self.width, self.height = w, h

    def draw(self):
        c = self.canv
        W, H = self.width, self.height
        lw    = W * 0.31   # label column width
        hh    = 17         # header height
        rh    = (H - hh - 2) / len(self.TASKS)
        x0    = lw + 5
        cw    = (W - x0 - 3) / self.WEEKS

        # background
        c.setFillColor(colors.HexColor("#f7f9fc"))
        c.setStrokeColor(colors.HexColor("#b8c8d8"))
        c.setLineWidth(0.5)
        c.rect(0, 0, W, H, fill=1, stroke=1)

        # header
        c.setFillColor(NAVY)
        c.rect(0, H-hh, W, hh, fill=1, stroke=0)
        c.setFont("Helvetica-Bold", 8); c.setFillColor(WHITE)
        c.drawString(5, H-hh+5, "Task")
        for i in range(self.WEEKS):
            c.drawCentredString(x0 + (i+0.5)*cw, H-hh+5, f"W{i+1}")

        # vertical grid
        c.setStrokeColor(colors.HexColor("#d0dce8")); c.setLineWidth(0.3)
        for i in range(1, self.WEEKS):
            gx = x0 + i*cw
            c.line(gx, 0, gx, H-hh)

        # rows
        for idx, (ph, name, start, dur, is_ph) in enumerate(self.TASKS):
            ry = H - hh - (idx+1)*rh
            # row bg
            c.setFillColor(colors.HexColor("#edf2f7") if idx%2==0 else WHITE)
            c.rect(0, ry+0.5, W, rh-0.5, fill=1, stroke=0)

            # label
            c.setFont("Helvetica-Bold" if is_ph else "Helvetica",
                      8 if is_ph else 7.5)
            c.setFillColor(self.COLS[ph] if is_ph else DARK)
            c.drawString(4 if is_ph else 14, ry+rh*0.25, name.strip())

            # bar
            bx  = x0 + (start-1)*cw + 1
            bw  = dur*cw - 2
            bfh = rh*0.58 if is_ph else rh*0.46
            by  = ry + (rh-bfh)/2
            c.setFillColor(self.COLS[ph] if is_ph else self.LITE[ph])
            c.setStrokeColor(self.COLS[ph])
            c.setLineWidth(0.7 if is_ph else 0.4)
            c.roundRect(bx, by, bw, bfh, 2, fill=1, stroke=1)
            if is_ph:
                c.setFont("Helvetica-Bold", 7); c.setFillColor(WHITE)
                c.drawCentredString(bx+bw/2, by+bfh*0.2, f"Phase {ph}")

        c.setStrokeColor(colors.HexColor("#8899aa")); c.setLineWidth(0.8)
        c.rect(0, 0, W, H, fill=0, stroke=1)

    def wrap(self, aw, ah): return self.width, self.height


# ── Agent Loop Diagram ────────────────────────────────────────────────────────
class AgentLoop(Flowable):
    """Simple accurate ReAct loop diagram."""
    def __init__(self, w, h):
        Flowable.__init__(self)
        self.width, self.height = w, h

    def draw(self):
        c = self.canv
        W, H = self.width, self.height

        c.setFillColor(colors.HexColor("#fafcfe"))
        c.setStrokeColor(colors.HexColor("#c0cedb")); c.setLineWidth(0.5)
        c.roundRect(0, 0, W, H, 4, fill=1, stroke=1)

        STEPS = [
            ("OBSERVE",    "Parse transcript\n& session context"),
            ("REASON",     "Select tools\nneeded"),
            ("ACT",        "Execute tools\n(1-3 calls)"),
            ("SYNTHESISE", "Aggregate results\n=> verdict"),
            ("ALERT",      "Push to mobile\n(WebSocket)"),
        ]
        n   = len(STEPS)
        bw  = 34
        bh  = 28
        gap = (W - n*bw) / (n+1)
        cy  = H/2

        boxes_x = []
        for i, (label, desc) in enumerate(STEPS):
            x = gap + i*(bw+gap)
            boxes_x.append(x)
            # box
            c.setFillColor(NAVY if i < 4 else TEAL)
            c.setStrokeColor(colors.HexColor("#8aaabb")); c.setLineWidth(0.5)
            c.roundRect(x, cy-bh/2, bw, bh, 3, fill=1, stroke=1)
            c.setFont("Helvetica-Bold", 6.5); c.setFillColor(WHITE)
            c.drawCentredString(x+bw/2, cy+2, label)
            c.setFont("Helvetica", 5.5); c.setFillColor(colors.HexColor("#c8e6f5"))
            for j, ln in enumerate(desc.split("\n")):
                c.drawCentredString(x+bw/2, cy-4-j*6.5, ln)

        # arrows between boxes
        for i in range(n-1):
            ax1 = boxes_x[i] + bw
            ax2 = boxes_x[i+1]
            amid = (ax1+ax2)/2
            c.setStrokeColor(TEAL); c.setFillColor(TEAL); c.setLineWidth(1)
            c.line(ax1, cy, ax2, cy)
            a = 0  # horizontal
            sz = 4.5
            pc = c.beginPath()
            pc.moveTo(ax2, cy)
            pc.lineTo(ax2-sz*math.cos(-0.4), cy-sz*math.sin(-0.4))
            pc.lineTo(ax2-sz*math.cos(+0.4), cy-sz*math.sin(+0.4))
            pc.close()
            c.drawPath(pc, fill=1, stroke=0)

        # loop-back arrow (ACT &#8594; REASON for multi-tool)
        lx1 = boxes_x[2]+bw/2
        lx2 = boxes_x[1]+bw/2
        top  = cy + bh/2 + 8
        c.setStrokeColor(colors.HexColor("#e67e22")); c.setLineWidth(0.8)
        c.setDash([2,2])
        c.line(lx1, cy+bh/2, lx1, top)
        c.line(lx1, top, lx2, top)
        c.line(lx2, top, lx2, cy+bh/2)
        c.setDash([])
        c.setFont("Helvetica", 6); c.setFillColor(colors.HexColor("#e67e22"))
        c.drawCentredString((lx1+lx2)/2, top+2, "if more tools needed")

        # tool labels below ACT box
        tx = boxes_x[2]
        c.setFont("Helvetica", 5.8); c.setFillColor(LGRAY)
        tools = ["check_blacklist(phone)", "get_fraud_stats(kw)", "auto_report(phone, reason)"]
        for j, t in enumerate(tools):
            c.drawCentredString(tx+bw/2, cy-bh/2-6-j*7, t)

    def wrap(self, aw, ah): return self.width, self.height


# ── Page callback ─────────────────────────────────────────────────────────────
def on_page(canvas, doc):
    canvas.saveState()
    pg = canvas.getPageNumber()
    canvas.setStrokeColor(TEAL); canvas.setLineWidth(1.5)
    canvas.line(ML, PH-MT+6, PW-MR, PH-MT+6)
    canvas.setFont("Helvetica-Bold", 8); canvas.setFillColor(TEAL)
    canvas.drawString(ML, PH-MT+9, "SWIN Hackathon 2026")
    canvas.setFont("Helvetica", 8); canvas.setFillColor(LGRAY)
    canvas.drawRightString(PW-MR, PH-MT+9, "Technical Proposal")
    canvas.setFont("Helvetica", 8.5); canvas.setFillColor(LGRAY)
    canvas.drawCentredString(PW/2, MB/2 - 2, f"- {pg} -")
    canvas.restoreState()


# ── Build ─────────────────────────────────────────────────────────────────────
def build():
    doc = BaseDocTemplate(
        OUTPUT, pagesize=A4,
        leftMargin=ML, rightMargin=MR, topMargin=MT, bottomMargin=MB,
        title="(anonymous)", author="(anonymous)",
        subject="Technical Proposal", creator="(unspecified)",
    )
    frame = Frame(ML, MB, TW, PH-MT-MB,
                  leftPadding=0, bottomPadding=0,
                  rightPadding=0, topPadding=0, id="body")
    doc.addPageTemplates([PageTemplate(id="p", frames=[frame], onPage=on_page)])

    story = []

    # ── Title ─────────────────────────────────────────────────────────────────
    story += [
        sp(2),
        p("FraudGuard AI: Real-Time Agentic Fraud Detection for Phone Calls", TITLE),
        hr(TEAL, 1.5),
    ]

    # ── Abstract ──────────────────────────────────────────────────────────────
    story += [
        p("<i>Abstract</i> &#8212;", ABSL),
        p(
            "Phone scams are a critical problem in Vietnam: over 15,900 fraud cases "
            "were recorded in 2023, with 91% involving phone calls. Existing tools "
            "only act post-call or depend on static blacklists. FraudGuard AI monitors "
            "an active call in real time &#8212; it streams audio, transcribes speech, and "
            "pushes an alert to the user's phone within three seconds of a fraud "
            "indicator appearing. The core innovation is a Google Gemini agent "
            "operating in a ReAct loop with three dedicated tools: blacklist lookup, "
            "fraud-statistics retrieval, and auto-reporting. A deterministic "
            "rule-based engine runs in parallel for sub-50 ms responses. "
            "AWS Transcribe is integrated as a resilient fallback via a circuit-breaker. "
            "On Vietnamese-language test cases: >90% detection rate, <8% false positives.",
            ABST
        ),
        p(
            "<i>Index Terms</i> &#8212; agentic AI, LLM tool-calling, real-time fraud detection, "
            "speech recognition, deepfake voice analysis, telecommunications security.",
            KW
        ),
        hr(TEAL, 0.8),
    ]

    # ── I. Introduction ───────────────────────────────────────────────────────
    story += [
        sec("I", "Introduction"),
        sp(4),
        p(
            "In Vietnam, the National Cybersecurity Center (NCSC) recorded 15,900+ "
            "online fraud cases in 2023, with phone calls involved in 91% of incidents [1]. "
            "Attackers impersonate police, banks, or government agencies, applying "
            "urgency and threats to force victims to transfer money or share OTP codes. "
            "A growing subset uses AI-generated (deepfake) voices to clone known contacts."
        ),
        p(
            "Existing countermeasures fall into two categories: post-call analysis apps "
            "and static phone-number blacklists. Both have the same critical flaw &#8212; they "
            "offer zero protection while the call is still in progress. "
            "FraudGuard AI was built to close exactly that gap."
        ),
        p("<b>Key contributions of this work:</b>"),
        bul("Streaming audio pipeline: microphone &#8594; fraud alert in <b>&lt;3 seconds</b> end-to-end."),
        bul("Multi-layer risk scoring: deterministic keyword rules + session accumulation."),
        bul("Autonomous Gemini agent (ReAct loop) with 3 purpose-built tools for contextual reasoning."),
        bul("Spectral heuristics for deepfake voice detection."),
        bul("Privacy-first data layer: PII masked before any database write."),
        sp(2),
    ]

    # ── II. Problem & Business Alignment ─────────────────────────────────────
    story += [
        sec("II", "Problem & Business Alignment"),
        sp(4),
        p(
            "Vietnam's digital economy lost an estimated <b>USD 390 million</b> to "
            "social-engineering scams in 2022 [1]. The structural challenge: "
            "phone numbers are cheap and disposable (blacklists expire quickly), "
            "and AI can now clone a voice in under a minute."
        ),
        p("<b>Target users and value delivered:</b>"),
        bul("<b>Consumers</b> &#8212; passive, on-device warning during the call. No action required; app monitors in the background."),
        bul("<b>Telecoms operators</b> &#8212; B2B API licensing. Operators embed the detection engine into subscriber apps."),
        bul("<b>Enterprise call centres</b> &#8212; inbound fraud monitoring with per-session audit logs for compliance."),
        sp(3),
        p(
            "<b>Revenue model:</b> Freemium B2C (~USD 2/month premium) plus B2B API licensing. "
            "Break-even at ~900 paying users against estimated monthly costs of "
            "USD 850&#8211;1,800 for 10,000 active users "
            "(Deepgram $500&#8211;1,000 + Gemini $100&#8211;200 + infra $250&#8211;600)."
        ),
        sp(2),
    ]

    # ── III. System Architecture ──────────────────────────────────────────────
    story += [
        sec("III", "System Architecture"),
        sp(4),
        p(
            "FraudGuard AI uses <b>Clean Architecture</b> with five layers: "
            "Mobile Client, API Gateway, Fraud Intelligence Engine, AI Agent Layer, "
            "and Data Layer. See Figure 1 for the accurate component diagram."
        ),
        sp(4),
        KeepTogether([
            ArchDiagram(TW, 150),
            p("Figure 1 &#8212; FraudGuard AI component diagram. Arrows show actual data flow.", CAP),
        ]),
        sp(4),
    ]

    tech_rows = [
        ["Mobile Client",       ".NET MAUI 8 / Android",   "Audio capture, UI, alert display"],
        ["API Gateway",         "Go 1.22 / Chi v5",        "WebSocket routing, session management"],
        ["Primary STT",         "Deepgram Nova-2",          "Streaming speech-to-text (Vietnamese)"],
        ["Fallback STT",        "AWS Transcribe",           "Circuit-breaker failover"],
        ["Fraud Engine",        "Go (custom)",              "Rule scoring + session accumulation"],
        ["AI Agent",            "Google Gemini API",        "ReAct loop + 3 tool-calls"],
        ["Cloud DB",            "PostgreSQL 16 (pgx/v5)",  "Blacklist, analytics, JSONB metadata"],
        ["Local DB",            "SQLite 3 (WAL mode)",     "On-device call history"],
    ]
    story += [
        mktbl(
            ["Layer", "Technology", "Responsibility"],
            tech_rows,
            widths=[TW*0.18, TW*0.24, TW*0.58],
            fs=9
        ),
        p("Table I &#8212; Technology stack.", CAP),
        sp(2),
    ]

    # ── IV. Agentic AI Design ──────────────────────────────────────────────────
    story += [
        sec("IV", "Agentic AI Design"),
        sp(4),
        p(
            "The central innovation is a <b>Google Gemini agent in a ReAct loop</b> [3]. "
            "Unlike a single-call classifier, the agent decides which tools it needs, "
            "calls them, reads the results, and only then issues a verdict. "
            "Every tool call and result is logged &#8212; decisions are fully auditable."
        ),
        sp(4),
        KeepTogether([
            AgentLoop(TW, 80),
            p("Figure 2 &#8212; Gemini agent ReAct loop. Dashed arrow = optional multi-tool iteration.", CAP),
        ]),
        sp(4),
        p("<b>The three agent tools:</b>"),
        bul("<b>check_blacklist(phone_number)</b> &#8212; queries PostgreSQL for known fraudster numbers; returns status, confidence score, and report count."),
        bul("<b>get_fraud_stats(keyword)</b> &#8212; retrieves historical frequency and severity trend for any phrase across all logged sessions."),
        bul("<b>auto_report(phone_number, reason)</b> &#8212; submits a new community blacklist entry autonomously when evidence is sufficient."),
        sp(3),
        p(
            "The agent runs <b>asynchronously</b> &#8212; it never blocks the real-time pipeline. "
            "Its verdict is one weighted input to the session score, not the sole trigger. "
            "This bounds the impact of any single LLM hallucination: if the agent "
            "misfires, the deterministic engine provides an independent check."
        ),
        sp(3),
        p("<b>Multi-layer scoring engine</b> (runs in parallel, &lt;50 ms):"),
        bul("<b>Critical keywords (+50 pts each):</b> OTP codes, AnyDesk/TeamViewer, fund-transfer phrases, national ID number."),
        bul("<b>Warning keywords (+20&#8211;25 pts):</b> major bank names, government agency titles, telecom brands."),
        bul("<b>Urgency phrases (+25&#8211;35 pts):</b> account-lockout claims, \"gap lam\", secrecy requests."),
        bul("<b>Negative signals (-40 to -100 pts):</b> movie/fiction references indicating benign context."),
        sp(2),
        p("Session score is clamped 0&#8211;100. Alert thresholds: <b>LOW &gt;= 20  |  MEDIUM &gt;= 40  |  HIGH &gt;= 60  |  CRITICAL &gt;= 80</b>."),
        sp(3),
        p(
            "<b>Deepfake voice detection</b> &#8212; spectral heuristics on PCM audio: "
            "pitch variance (AI voices are unnaturally flat), absent breathing gaps, "
            "high spectral flatness, and over-consistent energy levels. "
            "Scores average over a 20-chunk rolling window; "
            "&gt;70/100 adds a deepfake penalty to the session score."
        ),
        sp(2),
    ]

    # ── V. Data Strategy ──────────────────────────────────────────────────────
    story += [
        sec("V", "Data Strategy"),
        sp(4),
        p(
            "<b>Storage:</b> Cloud PostgreSQL 16 stores the community blacklist, "
            "device registrations, and per-session fraud analytics in JSONB columns "
            "(GIN-indexed for fast phrase queries). Local SQLite 3 (WAL mode) on "
            "the handset stores call history offline."
        ),
        p(
            "<b>PII masking:</b> Applied by regex immediately before any DB write &#8212; "
            "card numbers &#8594; ***CARD***, OTPs &#8594; ***OTP***, "
            "bank accounts &#8594; ***ACCOUNT***, national IDs &#8594; ***ID***. "
            "Masking is <i>not</i> applied during live analysis (full text required). "
            "Audio is never shared across client connections."
        ),
        p(
            "<b>Community improvement loop:</b> Every blacklist submission &#8212; manual or "
            "via the agent's <i>auto_report</i> tool &#8212; updates confidence scores. "
            "Keyword lists are config-file driven and hot-reloadable as "
            "scam tactics evolve. Future work: federated fine-tuning on anonymised "
            "per-device transcripts without centralising raw audio."
        ),
        sp(2),
    ]

    # ── VI. Project Plan ──────────────────────────────────────────────────────
    story += [
        sec("VI", "Project Plan &#8212; Gantt Chart"),
        sp(4),
        KeepTogether([
            GanttChart(TW, 122),
            p("Figure 3 &#8212; Eight-week development schedule. Solid bars = phase milestones; lighter bars = tasks.", CAP),
        ]),
        sp(2),
    ]

    # ── VII. Feasibility & Risk ────────────────────────────────────────────────
    story += [
        sec("VII", "Feasibility & Risk Analysis"),
        sp(4),
        p(
            "<b>Technical feasibility:</b> The full system is implemented and manually "
            "tested. The Go backend sustains 50&#8211;100 concurrent WebSocket sessions on "
            "a 2-core VM; Deepgram latency averages 800&#8211;1,200 ms; downstream "
            "processing adds 200&#8211;500 ms &#8212; well within the 3 s target. "
            "Stateless API design enables horizontal scaling with no code changes."
        ),
        sp(2),
    ]

    risk_rows = [
        ["Deepgram API downtime",       "High",   "Medium", "Circuit-breaker auto-routes to AWS Transcribe"],
        ["LLM hallucination (false +)", "Medium", "Medium", "Deterministic engine runs independently; agent is one weighted input"],
        ["Deepfake voice evasion",       "Medium", "Low",    "Multi-layer: evading spectral check != bypassing keyword engine"],
        ["PII data breach",             "High",   "Low",    "PII masked before write; audio not shared across sessions"],
        ["Scaling bottleneck",           "Medium", "Medium", "Semaphore (max 50 STT) + stateless horizontal scaling"],
        ["New scam vocabulary",          "Medium", "High",   "Config-driven keyword lists with hot-reload; agent adapts semantically"],
    ]
    story += [
        mktbl(
            ["Risk", "Impact", "Probability", "Mitigation"],
            risk_rows,
            widths=[TW*0.22, TW*0.10, TW*0.13, TW*0.55],
            fs=9
        ),
        p("Table II &#8212; Risk register.", CAP),
        sp(2),
    ]

    # ── VIII. Conclusion ──────────────────────────────────────────────────────
    story += [
        sec("VIII", "Conclusion"),
        sp(4),
        p(
            "FraudGuard AI demonstrates that real-time, in-call fraud protection is "
            "technically and economically viable today. The decisive design choice &#8212; "
            "using an autonomous agent that calls tools before deciding, rather than "
            "applying a single classifier &#8212; produces more reliable verdicts and "
            "leaves a full audit trail. The system is production-ready, horizontally "
            "scalable, and privacy-preserving by design. "
            "Immediate next steps: public beta with Vietnamese telecom partners, "
            "fine-tuned deepfake detection model, and expansion to additional "
            "Southeast Asian languages."
        ),
        hr(LGRAY),
    ]

    # ── References ────────────────────────────────────────────────────────────
    story.append(p("<b>REFERENCES</b>", S("rh", fontName="Helvetica-Bold", fontSize=10,
                                           leading=13, textColor=NAVY, spaceAfter=5)))
    refs = [
        "[1] National Cybersecurity Center (NCSC), <i>Vietnam Cybersecurity Report 2023</i>, "
        "Ministry of Information and Communications, Hanoi, 2023.",

        "[2] Y. Wei, Y. Wang, and S. Gao, \"A Survey on Telephone Fraud Detection Using "
        "Machine Learning,\" <i>IEEE Access</i>, vol. 11, pp. 45231&#8211;45248, 2023.",

        "[3] S. Yao et al., \"ReAct: Synergizing Reasoning and Acting in Language Models,\" "
        "<i>Proc. ICLR 2023</i>, Kigali, Rwanda, 2023.",

        "[4] Deepgram Inc., \"Nova-2 Streaming API,\" <i>Deepgram Documentation</i>, 2024. "
        "[Online]. Available: https://developers.deepgram.com.",

        "[5] Google DeepMind, \"Gemini: A Family of Highly Capable Multimodal Models,\" "
        "arXiv:2312.11805, Dec. 2023.",

        "[6] M. Todisco et al., \"ASVspoof 2019: Future Horizons in Spoofed and Fake Audio "
        "Detection,\" <i>Proc. Interspeech 2019</i>, pp. 1008&#8211;1012.",

        "[7] Amazon Web Services, \"Amazon Transcribe Developer Guide,\" <i>AWS Documentation</i>, "
        "2024. [Online]. Available: https://docs.aws.amazon.com/transcribe.",

        "[8] M. T. Nygard, <i>Release It! Design and Deploy Production-Ready Software</i>, "
        "2nd ed. Pragmatic Bookshelf, 2018.",

        "[9] R. C. Martin, <i>Clean Architecture: A Craftsman's Guide to Software Structure "
        "and Design</i>. Prentice Hall, 2017.",
    ]
    for r in refs:
        story.append(Paragraph(r, REF))
        story.append(sp(2))

    doc.build(story)
    print(f"Done: {OUTPUT}")


if __name__ == "__main__":
    build()
